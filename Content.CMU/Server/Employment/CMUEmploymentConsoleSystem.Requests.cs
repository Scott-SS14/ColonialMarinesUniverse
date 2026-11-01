using System.Linq;
using Content.Server.CMU14.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access;
using Content.Shared.CMU14.Employment;
using Content.Shared.Database;
using Content.Shared.Paper;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.CMU14.Employment;

public sealed partial class CMUEmploymentConsoleSystem
{
    [Dependency] private WantedSystem _officeFax = default!;
    private readonly Dictionary<int, OfficeRequest> _officeRequests = new();
    private int _nextOfficeRequest;

    private sealed class OfficeRequest
    {
        public int Id;
        public string Reference = string.Empty;
        public EntityUid Scope;
        public EntityUid Source;
        public EntityUid RequesterCard;
        public string Department = string.Empty;
        public string Subject = string.Empty;
        public string Description = string.Empty;
        public string Document = string.Empty;
        public string Status = "pending";
        public string Fingerprint = string.Empty;
        public string Force = string.Empty;
        public bool NativeEmployee;
        public CMUEmploymentApplyMessage? Hiring;
        public CMUEmploymentPayrollMessage? Payroll;
        public CMUEmploymentDepartment? PayrollBefore;
    }

    private bool IsNativeOfficeEmployee(Entity<CMUEmploymentConsoleComponent> ent, EntityUid card)
        => ent.Comp.Theme == CMUEmploymentTheme.Colony && _departments.GetEmploymentDepartments(ent.Owner)
            .Any(department => department.Members.Any(member => member.IdCardUid == GetNetEntity(card)));

    private string CardFingerprint(EntityUid card)
    {
        if (!TryComp<IdCardComponent>(card, out var id))
            return string.Empty;
        var icon = id.JobIcon;
        var tags = _access.TryGetTags(card);
        return string.Join("|", id.FullName, id.LocalizedJobTitle, icon.Id,
            tags == null ? string.Empty : string.Join(",", tags.Select(tag => tag.Id).OrderBy(tag => tag, StringComparer.Ordinal)));
    }

    private bool QueueHiring(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor,
        CMUEmploymentApplyMessage request, EntityUid card, IdCardComponent id, HashSet<ProtoId<AccessLevelPrototype>> selected)
    {
        var copy = new CMUEmploymentApplyMessage(request.Card, request.Revision, request.Action, request.Title,
            request.BlankName, new(selected), request.Iff, true, request.JobIcon);
        var office = Comp<CMUEmploymentOfficeComponent>(ent.Owner);
        var permissions = selected.Select(tag => ent.Comp.AccessNames.TryGetValue(tag, out var label)
            ? Loc.GetString(label) : ProtoMan.Index(tag).GetAccessLevelName());
        var entry = NewOfficeRequest(ent, id.FullName ?? request.BlankName,
            Loc.GetString(request.Action == CMUEmploymentAction.Terminate ? "cmu-employment-request-dismissal" : "cmu-employment-request-hiring", ("action", Loc.GetString(request.Action switch
                {
                    CMUEmploymentAction.Hire => "cmu-employment-form-hire",
                    CMUEmploymentAction.Edit => "cmu-employment-form-edit",
                    _ => "cmu-employment-form-terminate",
                })), ("title", request.Title),
                ("permissions", string.Join(", ", permissions)), ("iff", Loc.GetString(request.Iff ? "cmu-employment-yes" : "cmu-employment-no"))));
        entry.Hiring = copy;
        entry.Fingerprint = CardFingerprint(card);
        entry.NativeEmployee = IsNativeOfficeEmployee(ent, card);
        entry.Force = office.SecurityBranding ? SecurityForce() : string.Empty;
        if (!SubmitOfficeRequest(ent, actor, entry))
            return false;
        ent.Comp.Revision++;
        ent.Comp.LastReceipt = new(ent.Comp.Revision, request.Card, request.Action, entry.Subject, false, true);
        UpdateUi(ent);
        return true;
    }

    private bool QueuePayroll(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor,
        CMUEmploymentPayrollMessage request, CMUEmploymentColonyState snapshot)
    {
        if (request.Amount is < 0 or > 1_000_000 || request.Action == CMUEmploymentPayrollAction.PayColony)
            return Reject(ent, actor, "cmu-employment-denied");
        var department = snapshot.Departments.Find(entry => entry.Console == request.Department);
        if (department == null || request.Employee != null && request.Action != CMUEmploymentPayrollAction.Transfer
            && !department.Members.Any(member => member.IdCardUid == request.Employee))
            return Reject(ent, actor, "cmu-employment-payroll-stale");
        var subject = department.Name;
        if (request.Employee is { } employeeNet && EntityManager.TryGetEntity(employeeNet, out var employeeUid)
            && TryComp<IdCardComponent>(employeeUid, out var employeeId))
            subject = Loc.GetString("cmu-employment-request-employee", ("department", department.Name),
                ("name", employeeId.FullName ?? Name(employeeUid.Value)));
        var entry = NewOfficeRequest(ent, subject,
            Loc.GetString("cmu-employment-request-payroll", ("action", Loc.GetString("cmu-employment-payroll-action-" + request.Action.ToString().ToLowerInvariant())), ("amount", request.Amount)));
        entry.Payroll = new(request.Action, request.Revision, request.Snapshot, request.Department,
            request.Employee, request.Amount, true);
        entry.PayrollBefore = department;
        if (request.Action == CMUEmploymentPayrollAction.Transfer && request.Employee is { } transferCard
            && EntityManager.TryGetEntity(transferCard, out var transferUid))
            entry.Fingerprint = CardFingerprint(transferUid.Value);
        if (!SubmitOfficeRequest(ent, actor, entry))
            return false;
        RefreshColonySnapshot(ent);
        return true;
    }

    private OfficeRequest NewOfficeRequest(Entity<CMUEmploymentConsoleComponent> ent, string subject, string description)
    {
        var id = ++_nextOfficeRequest;
        return new OfficeRequest
        {
            Id = id, Reference = $"CMU-REQ-{id:D6}", Scope = OfficeScope(ent.Owner), Source = ent.Owner,
            RequesterCard = Card(ent.Owner, true) ?? ent.Owner,
            Department = CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner)?.Department?.Id ?? string.Empty,
            Subject = subject, Description = description,
        };
    }

    private bool SubmitOfficeRequest(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor, OfficeRequest request)
    {
        var office = CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner);
        if (HasComp<CMUEmploymentPrintJobComponent>(ent.Owner) || office != null && _timing.CurTime < office.SubmitReadyAt)
            return Reject(ent, actor, "cmu-employment-office-busy");
        if (_officeRequests.Values.Any(entry => entry.Status == "pending" && entry.Scope == request.Scope
            && (request.Hiring != null && entry.Hiring?.Card == request.Hiring.Card
                || request.Payroll != null && entry.Payroll?.Department == request.Payroll.Department
                    && entry.Payroll?.Employee == request.Payroll.Employee && entry.Payroll?.Action == request.Payroll.Action)))
            return Reject(ent, actor, "cmu-employment-office-duplicate");
        if (_officeRequests.Values.Count(entry => entry.Status == "pending") >= 128)
            return Reject(ent, actor, "cmu-employment-office-full");
        foreach (var old in _officeRequests.Values.Where(entry => entry.Status != "pending").OrderBy(entry => entry.Id)
                     .Take(Math.Max(0, _officeRequests.Count - 255)).ToList())
            _officeRequests.Remove(old.Id);
        request.Document = Loc.GetString("cmu-employment-office-document", ("reference", request.Reference),
            ("department", FormattedMessage.EscapeText(GetOfficeBrand(ent) ?? Name(ent.Owner))),
            ("requester", FormattedMessage.EscapeText(Name(actor))), ("subject", FormattedMessage.EscapeText(request.Subject)),
            ("description", FormattedMessage.EscapeText(request.Description)), ("time", _timing.CurTime.ToString(@"hh\:mm\:ss")));
        _officeRequests.Add(request.Id, request);
        if (office != null)
            office.SubmitReadyAt = _timing.CurTime + TimeSpan.FromSeconds(10);
        PrintOfficeDocument(ent, actor, request.Reference, request.Document);
        var faxed = _officeFax.SendCustomFax(office?.AdministratorFax ?? "Colony Administrator", request.Reference,
            request.Document, paperPrototype: GetFormPrototype(ent).Id);
        _popup.PopupEntity(Loc.GetString(faxed ? "cmu-employment-office-submitted" : "cmu-employment-office-no-fax"), ent.Owner, actor);
        _adminLog.Add(LogType.RMCIdModify, LogImpact.Medium,
            $"{ToPrettyString(actor):player} submitted office request {request.Reference}: {request.Description}");
        RefreshOffices();
        return true;
    }

    private void PrintOfficeDocument(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor, string name, string content)
    {
        var job = EnsureComp<CMUEmploymentPrintJobComponent>(ent.Owner);
        job.UpdatesReceipt = false;
        job.Prototype = GetFormPrototype(ent);
        job.Issuer = actor;
        job.Remaining = 2f;
        job.DocumentName = name;
        job.Description = Loc.GetString("cmu-employment-office-paper-description");
        job.Content = content;
        UpdateMachineVisuals(ent);
    }

    private void OnOfficeMessage(Entity<CMUEmploymentConsoleComponent> ent, ref CMUEmploymentOfficeMessage args)
    {
        HandleOfficeAction(ent, args.Actor, args);
    }

    public void HandleOfficeAction(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor, CMUEmploymentOfficeMessage args)
    {
        if (ent.Comp.Theme != CMUEmploymentTheme.Colony)
            return;
        if (args.Action == CMUEmploymentOfficeAction.Paper)
        {
            if (OfficeAdministrator(ent, actor))
                SwapApprovalPaper(ent, actor);
            UpdateUi(ent);
            return;
        }
        if (args.Revision != ent.Comp.Revision || ent.Comp.Operator != actor || !PublicOfficeSession(ent) && !Authorized(ent))
        {
            Reject(ent, actor, "cmu-employment-denied");
            return;
        }
        switch (args.Action)
        {
            case CMUEmploymentOfficeAction.Refresh:
                UpdateUi(ent);
                break;
            case CMUEmploymentOfficeAction.Report:
                if (CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner)?.PublicDesk == true
                    && args.Text != null && ValidText(args.Text, 512))
                {
                    var entry = NewOfficeRequest(ent, Comp<IdCardComponent>(Card(ent.Owner, true)!.Value).FullName ?? Name(actor), args.Text);
                    SubmitOfficeRequest(ent, actor, entry);
                }
                break;
            case CMUEmploymentOfficeAction.Approve:
            case CMUEmploymentOfficeAction.Reject:
                if (OfficeAdministrator(ent, actor) && args.Confirmed)
                    DecideOfficeRequest(ent, actor, args.Entry, args.Action == CMUEmploymentOfficeAction.Approve);
                break;
            case CMUEmploymentOfficeAction.Reprint:
                if (_officeRequests.TryGetValue(args.Entry, out var request) && request.Scope == OfficeScope(ent.Owner)
                    && (OfficeAdministrator(ent, actor) || request.RequesterCard == Card(ent.Owner, true)
                        || Authorized(ent) && CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner)?.Department?.Id == request.Department)
                    && !HasComp<CMUEmploymentPrintJobComponent>(ent.Owner))
                    PrintOfficeDocument(ent, actor, request.Reference, request.Document);
                break;
            case CMUEmploymentOfficeAction.Book:
            case CMUEmploymentOfficeAction.Release:
                HandleJail(ent, actor, args);
                break;
        }
        UpdateUi(ent);
    }

    private void SwapApprovalPaper(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor)
    {
        var slot = _containers.EnsureContainer<ContainerSlot>(ent.Owner, ApprovalPaperSlot);
        if (slot.ContainedEntity is { } inserted)
        {
            if (_containers.Remove(inserted, slot))
                _hands.PickupOrDrop(actor, inserted);
        }
        else if (_hands.TryGetActiveItem(actor, out var held) && held is { } paper && HasComp<PaperComponent>(paper))
            _containers.Insert(paper, slot);
    }

    private void DecideOfficeRequest(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor, int id, bool approve)
    {
        if (!_officeRequests.TryGetValue(id, out var request) || request.Scope != OfficeScope(ent.Owner) || request.Status != "pending")
            return;
        if (!approve)
        {
            request.Status = "rejected";
            _adminLog.Add(LogType.RMCIdModify, LogImpact.High,
                $"{ToPrettyString(actor):player} rejected office request {request.Reference}");
            RefreshOffices();
            return;
        }
        var paperSlot = _containers.EnsureContainer<ContainerSlot>(ent.Owner, ApprovalPaperSlot);
        var stamp = CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner)?.ApprovalStamp ?? "stamp-component-stamped-name-au14-admin";
        if (paperSlot.ContainedEntity is not { } paperUid || !TryComp<PaperComponent>(paperUid, out var paper)
            || paper.Content != request.Document || !paper.StampedBy.Any(mark => mark.StampedName == stamp))
        {
            Reject(ent, actor, "cmu-employment-office-stamp-required");
            return;
        }
        if (!TryComp<CMUEmploymentConsoleComponent>(request.Source, out var source)
            || source.Theme != CMUEmploymentTheme.Colony || source.Faction != ent.Comp.Faction
            || OfficeScope(request.Source) != request.Scope
            || request.Force.Length > 0 && request.Force != SecurityForce())
        {
            request.Status = "stale";
            RefreshOffices();
            return;
        }
        var applied = false;
        if (request.Hiring is { } hiring)
        {
            if (Card(ent.Owner, false) is not { } card || GetNetEntity(card) != hiring.Card || CardFingerprint(card) != request.Fingerprint)
            {
                Reject(ent, actor, "cmu-employment-office-id-required");
                return;
            }
            if (request.NativeEmployee && hiring.Action != CMUEmploymentAction.Hire
                && !IsNativeOfficeEmployee((request.Source, source), card))
            {
                request.Status = "stale";
                RefreshOffices();
                return;
            }
            var department = _departments.GetEmploymentDepartments(ent.Owner).Find(entry => entry.Key == request.Department);
            if (hiring.Action != CMUEmploymentAction.Terminate && department == null)
            {
                Reject(ent, actor, "cmu-employment-payroll-stale");
                return;
            }
            applied = TryApply(ent, actor, new(hiring.Card, ent.Comp.Revision, hiring.Action, hiring.Title,
                hiring.BlankName, new(hiring.Access), hiring.Iff, true, hiring.JobIcon), source, request.Department);
        }
        else if (request.Payroll is { } payroll)
        {
            var current = _departments.GetEmploymentDepartments(ent.Owner).Find(entry => entry.Console == payroll.Department);
            if (current == null || request.PayrollBefore == null || !SamePayroll(request.PayrollBefore, current))
            {
                request.Status = "stale";
                RefreshOffices();
                return;
            }
            if (payroll.Action == CMUEmploymentPayrollAction.Transfer
                && (Card(ent.Owner, false) is not { } card || GetNetEntity(card) != payroll.Employee
                    || CardFingerprint(card) != request.Fingerprint))
            {
                Reject(ent, actor, "cmu-employment-office-id-required");
                return;
            }
            RefreshColonySnapshot(ent);
            applied = TryPayrollAction(ent, actor, new(payroll.Action, ent.Comp.Revision, ent.Comp.ColonySnapshot!.Version,
                payroll.Department, payroll.Employee, payroll.Amount, true));
            if (applied && !HasComp<CMUEmploymentPrintJobComponent>(ent.Owner))
                PrintOfficeDocument(ent, actor, request.Reference, request.Document);
        }
        else
            applied = true;
        if (!applied)
            return;
        request.Status = "approved";
        _adminLog.Add(LogType.RMCIdModify, LogImpact.High,
            $"{ToPrettyString(actor):player} approved office request {request.Reference}");
        RefreshOffices();
    }
}
