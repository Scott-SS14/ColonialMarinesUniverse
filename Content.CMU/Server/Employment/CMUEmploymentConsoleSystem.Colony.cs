using System.Linq;
using Content.Server.CMU14.ColonyEconomy;
using Content.Server.CMU14.Comms;
using Content.Server.CrewManifest;
using Content.Server.Station.Systems;
using Content.Shared.CMU14.Employment;
using Content.Shared.Database;

namespace Content.Server.CMU14.Employment;

public sealed partial class CMUEmploymentConsoleSystem
{
    [Dependency] private ColonyCommsConsoleSystem _colonyComms = default!;
    [Dependency] private DepartmentConsoleSystem _departments = default!;
    [Dependency] private CrewManifestSystem _crewManifest = default!;
    [Dependency] private StationSystem _stations = default!;

    private bool CanUseColony(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor)
        => ent.Comp.Theme == CMUEmploymentTheme.Colony && ent.Comp.Operator == actor && Authorized(ent);

    private bool CanBroadcastColony(Entity<CMUEmploymentConsoleComponent> ent)
    {
        return ent.Comp.Theme == CMUEmploymentTheme.Colony && Authorized(ent)
            && ent.Comp.CommunicationsAccess is { } required && Card(ent.Owner, true) is { } card
            && (_access.TryGetTags(card)?.Contains(required) ?? false);
    }

    private void OnColonyMessage(Entity<CMUEmploymentConsoleComponent> ent, ref CMUEmploymentColonyMessage args)
    {
        TryColonyAction(ent, args.Actor, args);
        UpdateUi(ent);
    }

    public bool TryColonyAction(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor, CMUEmploymentColonyMessage request)
    {
        if (!CanUseColony(ent, actor) || request.Revision != ent.Comp.Revision)
            return Reject(ent, actor, "cmu-employment-denied");

        if (request.Action == CMUEmploymentColonyAction.Refresh)
        {
            if (ent.Comp.ColonySnapshot != null && _timing.CurTime < ent.Comp.ColonyRefreshAt)
                return false;
            RefreshColonySnapshot(ent);
            return true;
        }
        if (!CanBroadcastColony(ent))
            return Reject(ent, actor, "cmu-employment-comms-denied");
        switch (request.Action)
        {
            case CMUEmploymentColonyAction.Announce:
                if (_timing.CurTime < ent.Comp.ColonyCommsReadyAt)
                    return Reject(ent, actor, "cmu-employment-comms-cooldown");
                if (request.Text == null || request.Text.Length > 512 || !ValidText(request.Text.Trim(), 512))
                    return Reject(ent, actor, "cmu-employment-comms-invalid");
                var text = request.Text.Trim();
                _colonyComms.BroadcastColonyAlert(ent.Owner, text);
                ent.Comp.ColonyCommsReadyAt = _timing.CurTime + TimeSpan.FromSeconds(10);
                _adminLog.Add(LogType.RMCIdModify, LogImpact.High,
                    $"{ToPrettyString(actor):player} broadcast colony announcement from {ToPrettyString(ent.Owner):entity}: {text}");
                _popup.PopupEntity(Loc.GetString("cmu-employment-comms-sent"), ent.Owner, actor);
                return true;
            case CMUEmploymentColonyAction.Siren:
                if (_timing.CurTime < ent.Comp.ColonySirenReadyAt)
                    return Reject(ent, actor, "cmu-employment-comms-cooldown");
                _colonyComms.SetColonySiren(!_colonyComms.SirenActive);
                ent.Comp.ColonySirenReadyAt = _timing.CurTime + TimeSpan.FromSeconds(2);
                _adminLog.Add(LogType.RMCIdModify, LogImpact.High,
                    $"{ToPrettyString(actor):player} set colony siren to {_colonyComms.SirenActive} from {ToPrettyString(ent.Owner):entity}");
                return true;
            default:
                return false;
        }
    }

    private void OnColonySirenChanged(CMUEmploymentSirenChangedEvent args)
    {
        var query = EntityQueryEnumerator<CMUEmploymentConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            if (console.Theme == CMUEmploymentTheme.Colony)
                UpdateUi((uid, console));
        }
    }

    private void RefreshColonySnapshot(Entity<CMUEmploymentConsoleComponent> ent)
    {
        var station = _stations.GetOwningStation(ent.Owner);
        var manifest = station != null ? _crewManifest.GetCrewManifest(station.Value).entries : null;
        ent.Comp.ColonySnapshot = new(++ent.Comp.ColonySnapshotRevision, station != null ? Name(station.Value) : string.Empty,
            manifest, _departments.GetEmploymentDepartments(ent.Owner), _timing.CurTime.ToString(@"hh\:mm\:ss"));
        ent.Comp.ColonyRefreshAt = _timing.CurTime + TimeSpan.FromSeconds(2);
    }

    private void OnPayrollMessage(Entity<CMUEmploymentConsoleComponent> ent, ref CMUEmploymentPayrollMessage args)
    {
        TryPayrollAction(ent, args.Actor, args);
        UpdateUi(ent);
    }

    public bool TryPayrollAction(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor, CMUEmploymentPayrollMessage request)
    {
        if (!CanUseColony(ent, actor) || !request.Confirmed || request.Revision != ent.Comp.Revision
            || request.Action == CMUEmploymentPayrollAction.PayColony
                && CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner)?.Department != null)
            return Reject(ent, actor, "cmu-employment-denied");
        if (ent.Comp.ColonySnapshot is not { } snapshot || snapshot.Version != request.Snapshot
            || !snapshot.Departments.Any(entry => entry.Console == request.Department)
            || !EntityManager.TryGetEntity(request.Department, out var departmentUid))
            return Reject(ent, actor, "cmu-employment-payroll-stale");

        var fresh = _departments.GetEmploymentDepartments(ent.Owner);
        var relevant = request.Action == CMUEmploymentPayrollAction.PayColony
            ? snapshot.Departments : snapshot.Departments.Where(entry => entry.Console == request.Department).ToList();
        if (request.Action == CMUEmploymentPayrollAction.PayColony && fresh.Count != snapshot.Departments.Count
            || relevant.Any(before => !fresh.Any(now => SamePayroll(before, now))))
        {
            RefreshColonySnapshot(ent);
            return Reject(ent, actor, "cmu-employment-payroll-stale");
        }
        EntityUid? employee = null;
        if (request.Employee is { } cardNet)
        {
            if (!EntityManager.TryGetEntity(cardNet, out var cardUid))
                return Reject(ent, actor, "cmu-employment-payroll-stale");
            employee = cardUid;
        }
        if (request.Action == CMUEmploymentPayrollAction.Transfer
            && (Card(ent.Owner, false) != employee || employee == null))
            return Reject(ent, actor, "cmu-employment-stale");

        var paying = request.Action is CMUEmploymentPayrollAction.PayEmployee or CMUEmploymentPayrollAction.PayDepartment or CMUEmploymentPayrollAction.PayColony;
        if (paying && _timing.CurTime < ent.Comp.ColonyPayrollReadyAt)
            return Reject(ent, actor, "cmu-employment-payroll-cooldown");
        if (NeedsOfficeApproval(ent, actor))
            return QueuePayroll(ent, actor, request, snapshot);
        if (!_departments.ManageEmploymentDepartment(ent.Owner, departmentUid.Value, employee, request.Action, request.Amount))
            return Reject(ent, actor, "cmu-employment-payroll-failed");
        if (paying)
            ent.Comp.ColonyPayrollReadyAt = _timing.CurTime + TimeSpan.FromSeconds(10);
        if (request.Action == CMUEmploymentPayrollAction.Transfer)
            ent.Comp.Revision++;
        _adminLog.Add(LogType.RMCIdModify, LogImpact.High,
            $"{ToPrettyString(actor):player} performed colony payroll action {request.Action} on {ToPrettyString(departmentUid.Value):entity}; employee {employee}; amount {request.Amount}");
        RefreshColonySnapshot(ent);
        _popup.PopupEntity(Loc.GetString("cmu-employment-payroll-applied"), ent.Owner, actor);
        return true;
    }

    private static bool SamePayroll(CMUEmploymentDepartment a, CMUEmploymentDepartment b)
    {
        if (a.Console != b.Console || a.Budget != b.Budget || a.DefaultSalary != b.DefaultSalary || a.Members.Count != b.Members.Count)
            return false;
        var members = b.Members.ToDictionary(member => member.IdCardUid);
        foreach (var before in a.Members)
        {
            if (!members.TryGetValue(before.IdCardUid, out var now) || before.Salary != now.Salary
                || before.HasOverride != now.HasOverride || before.Name != now.Name || before.JobTitle != now.JobTitle)
                return false;
        }
        return true;
    }
}
