using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Access;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.CMU14.Employment;
using Content.Shared.CMU14.ColonyEconomy;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.StatusIcon;
using Content.Shared._RMC14.Power;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.CMU14.Employment;

public sealed partial class CMUEmploymentConsoleSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedAccessSystem _access = default!;
    [Dependency] private ISharedAdminLogManager _adminLog = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedIdCardSystem _ids = default!;
    [Dependency] private GunIFFSystem _iff = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedRMCPowerSystem _power = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        Subs.BuiEvents<CMUEmploymentConsoleComponent>(CMUEmploymentUiKey.Key, subs =>
        {
            subs.Event<CMUEmploymentSlotMessage>(OnSlot);
            subs.Event<CMUEmploymentApplyMessage>(OnApply);
            subs.Event<CMUEmploymentRefreshMessage>(OnRefresh);
            subs.Event<CMUEmploymentColonyMessage>(OnColonyMessage);
            subs.Event<CMUEmploymentPayrollMessage>(OnPayrollMessage);
            subs.Event<CMUEmploymentOfficeMessage>(OnOfficeMessage);
        });
        SubscribeLocalEvent<CMUEmploymentConsoleComponent, BoundUIOpenedEvent>(OnOpened);
        SubscribeLocalEvent<CMUEmploymentConsoleComponent, BoundUIClosedEvent>(OnClosed);
        SubscribeLocalEvent<CMUEmploymentConsoleComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<CMUEmploymentConsoleComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<CMUEmploymentConsoleComponent, PowerChangedEvent>(OnPower);
        SubscribeLocalEvent<CMUEmploymentConsoleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<Content.Server.GameTicking.GameRunLevelChangedEvent>(OnRunLevelChanged);
        SubscribeLocalEvent<CMUEmploymentSirenChangedEvent>(OnColonySirenChanged);
        SubscribeLocalEvent<Content.Shared.GameTicking.RoundRestartCleanupEvent>(OnOfficeRoundRestart);
    }

    private EntityUid? Card(EntityUid console, bool operatorSlot)
    {
        var slot = operatorSlot ? CMUEmploymentConsoleComponent.OperatorSlot : CMUEmploymentConsoleComponent.TargetSlot;
        return _containers.TryGetContainer(console, slot, out var container) && container is ContainerSlot cards
            ? cards.ContainedEntity : null;
    }

    private bool Authorized(Entity<CMUEmploymentConsoleComponent> ent)
    {
        if (CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner)?.Jail == true)
            return false;
        return _power.IsPowered(ent.Owner)
            && ent.Comp.Operator is { } actor
            && !TerminatingOrDeleted(actor)
            && _blocker.CanInteract(actor, ent.Owner)
            && _interaction.InRangeUnobstructed(actor, ent.Owner)
            && Card(ent.Owner, true) is { } card
            && TryComp<IdCardComponent>(card, out _)
            && TryComp<AccessComponent>(card, out var access)
            && access.Enabled
            && (_access.TryGetTags(card)?.Contains(ent.Comp.RequiredAccess) ?? false);
    }

    private void OnOpened(Entity<CMUEmploymentConsoleComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateMachineVisuals(ent);
        UpdateUi(ent);
    }

    private void OnClosed(Entity<CMUEmploymentConsoleComponent> ent, ref BoundUIClosedEvent args)
    {
        if (ent.Comp.Operator != args.Actor)
            return;

        ent.Comp.Operator = null;
        ent.Comp.Revision++;
        UpdateUi(ent);
    }

    private void OnRefresh(Entity<CMUEmploymentConsoleComponent> ent, ref CMUEmploymentRefreshMessage args)
    {
        UpdateUi(ent);
    }

    private void OnPower(Entity<CMUEmploymentConsoleComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            ent.Comp.Operator = null;
        ent.Comp.Revision++;
        UpdateMachineVisuals(ent);
        UpdateUi(ent);
    }

    private void OnInserted(Entity<CMUEmploymentConsoleComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        ent.Comp.Revision++;
        UpdateUi(ent);
    }

    private void OnRemoved(Entity<CMUEmploymentConsoleComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == CMUEmploymentConsoleComponent.OperatorSlot)
            ent.Comp.Operator = null;
        ent.Comp.Revision++;
        UpdateUi(ent);
    }

    private void OnSlot(Entity<CMUEmploymentConsoleComponent> ent, ref CMUEmploymentSlotMessage args)
    {
        if (!_blocker.CanInteract(args.Actor, ent.Owner)
            || !_interaction.InRangeUnobstructed(args.Actor, ent.Owner))
            return;

        var slot = args.OperatorSlot ? CMUEmploymentConsoleComponent.OperatorSlot : CMUEmploymentConsoleComponent.TargetSlot;
        var container = _containers.EnsureContainer<ContainerSlot>(ent.Owner, slot);
        if (container.ContainedEntity is { } inserted)
        {
            if (_containers.Remove(inserted, container))
                _hands.PickupOrDrop(args.Actor, inserted);
            return;
        }

        if (!_power.IsPowered(ent.Owner)
            || !_hands.TryGetActiveItem(args.Actor, out var held)
            || held is not { } card
            || !TryComp<IdCardComponent>(card, out _)
            || !TryComp<AccessComponent>(card, out var access))
        {
            Reject(ent, args.Actor, "cmu-employment-insert-id");
            return;
        }

        if (args.OperatorSlot && (!access.Enabled || !HasOperatorAccess(ent, card)))
        {
            Reject(ent, args.Actor, "cmu-employment-denied");
            return;
        }

        if (!_containers.Insert(card, container))
            return;

        if (args.OperatorSlot)
            ent.Comp.Operator = args.Actor;
        ent.Comp.Revision++;
        UpdateUi(ent);
    }

    private void OnApply(Entity<CMUEmploymentConsoleComponent> ent, ref CMUEmploymentApplyMessage args)
    {
        if (!TryApply(ent, args.Actor, args))
            UpdateUi(ent);
    }

    public bool TryApply(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor, CMUEmploymentApplyMessage request,
        CMUEmploymentConsoleComponent? approvedPolicy = null, string? approvedDepartment = null)
    {
        if (!Authorized(ent) || ent.Comp.Operator != actor)
            return Reject(ent, actor, "cmu-employment-denied");

        if (HasComp<CMUEmploymentPrintJobComponent>(ent.Owner))
            return Reject(ent, actor, "cmu-employment-printer-busy");

        if (!request.Confirmed
            || request.Revision != ent.Comp.Revision
            || Card(ent.Owner, false) is not { } card
            || GetNetEntity(card) != request.Card
            || !TryComp<IdCardComponent>(card, out var id)
            || !TryComp<AccessComponent>(card, out var access))
        {
            return Reject(ent, actor, "cmu-employment-stale");
        }

        if (request.Action is not (CMUEmploymentAction.Hire or CMUEmploymentAction.Edit or CMUEmploymentAction.Terminate))
            return false;

        var office = CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner);
        if (approvedPolicy == null && office?.Department != null && request.Action != CMUEmploymentAction.Hire
            && !IsNativeOfficeEmployee(ent, card))
            return Reject(ent, actor, "cmu-employment-not-employed");
        var employment = CompOrNull<CMUEmploymentCardComponent>(card);
        if (request.Action != CMUEmploymentAction.Hire && employment?.Employer != ent.Comp.Faction.Id && !(approvedPolicy != null && OfficeAdministrator(ent, actor)) && !IsNativeOfficeEmployee(ent, card))
            return Reject(ent, actor, "cmu-employment-not-employed");

        var terminate = request.Action == CMUEmploymentAction.Terminate;
        if (!terminate && (request.Title == null || request.Title.Length > 128
            || request.BlankName == null || request.BlankName.Length > 256))
            return Reject(ent, actor, "cmu-employment-invalid-text");
        if (request.Access == null)
            return Reject(ent, actor, "cmu-employment-invalid-access");
        var title = terminate ? Loc.GetString("cmu-employment-unassigned") : request.Title?.Trim() ?? string.Empty;
        var blankName = request.BlankName?.Trim() ?? string.Empty;
        if (!terminate && (!ValidText(title, 40)
            || (string.IsNullOrWhiteSpace(id.FullName) && !ValidText(blankName, 64))))
        {
            return Reject(ent, actor, "cmu-employment-invalid-text");
        }

        if (!terminate && request.Access.Count > (approvedPolicy ?? ent.Comp).AllowedAccess.Count)
            return Reject(ent, actor, "cmu-employment-invalid-access");

        var policy = approvedPolicy ?? ent.Comp;
        var selected = new HashSet<ProtoId<AccessLevelPrototype>>();
        ProtoId<JobIconPrototype>? jobIcon = terminate ? "JobIconUnknown" : request.JobIcon ?? policy.DefaultJobIcon;
        if (!terminate && jobIcon is { } requestedIcon
            && (!policy.JobIcons.ContainsKey(requestedIcon) || !ProtoMan.HasIndex(requestedIcon)))
        {
            return Reject(ent, actor, "cmu-employment-invalid-icon");
        }
        if (jobIcon is { } icon && !ProtoMan.HasIndex(icon))
            return Reject(ent, actor, "cmu-employment-invalid-icon");
        if (!terminate)
        {
            foreach (var tag in request.Access)
            {
                if (!policy.AllowedAccess.Contains(tag) || !ProtoMan.HasIndex(tag))
                    return Reject(ent, actor, "cmu-employment-invalid-access");
                selected.Add(tag);
            }
        }

        Entity<DepartmentConsoleComponent>? enrollment = null;
        var departmentKey = approvedDepartment ?? office?.Department?.Id;
        if (!terminate && departmentKey != null)
        {
            var department = _departments.GetEmploymentDepartments(ent.Owner).Find(entry => entry.Key == departmentKey);
            if (department == null || !EntityManager.TryGetEntity(department.Console, out var departmentUid)
                || !TryComp<DepartmentConsoleComponent>(departmentUid, out var departmentComp))
                return Reject(ent, actor, "cmu-employment-payroll-stale");
            if (departmentComp.DepartmentAccessLevel is { } departmentAccess)
            {
                if (!policy.AllowedAccess.Contains(departmentAccess) || !ProtoMan.HasIndex(departmentAccess))
                    return Reject(ent, actor, "cmu-employment-invalid-access");
                selected.Add(departmentAccess);
            }
            enrollment = (departmentUid.Value, departmentComp);
        }

        // Validate the complete request before touching the card; a failed request must not erase access.
        if (!ProtoMan.HasIndex(ent.Comp.Faction))
            return Reject(ent, actor, "cmu-employment-invalid-access");
        if (!ProtoMan.HasIndex(GetFormPrototype(ent)))
            return Reject(ent, actor, "cmu-employment-printer-unavailable");

        if (!terminate && employment?.Employer == ent.Comp.Faction.Id
            && _access.MatchesEmploymentAccess((card, access), selected)
            && id.LocalizedJobTitle == title && (jobIcon == null || id.JobIcon == jobIcon)
            && _iff.MatchesEmploymentCardIff(card, ent.Comp.Faction, request.Iff))
        {
            return Reject(ent, actor, "cmu-employment-no-changes");
        }

        if (approvedPolicy == null && NeedsOfficeApproval(ent, actor))
            return QueueHiring(ent, actor, request, card, id, selected);
        var previousAccess = new List<ProtoId<AccessLevelPrototype>>(access.Tags);
        var previousTitle = id.LocalizedJobTitle ?? string.Empty;
        var previousIcon = id.JobIcon;
        _access.ReplaceEmploymentAccess((card, access), selected);
        var iff = EnsureComp<ItemIFFComponent>(card);
        if (!terminate && request.Iff)
            _iff.SetIdFaction((card, iff), ent.Comp.Faction);
        else
            _iff.ClearEmploymentCardIff((card, iff));

        if (!terminate && string.IsNullOrWhiteSpace(id.FullName))
            _ids.TryChangeFullName(card, blankName, id, actor);
        _ids.TryChangeJobTitle(card, title, id, actor);
        if (jobIcon is { } newIcon)
            _ids.TryChangeJobIcon(card, ProtoMan.Index(newIcon), id, actor);

        employment = EnsureComp<CMUEmploymentCardComponent>(card);
        employment.Employer = terminate ? string.Empty : ent.Comp.Faction.Id;
        if (enrollment is { } departmentEnrollment)
            _departments.EnrollEmploymentMember(departmentEnrollment, card);
        if (terminate && ent.Comp.Theme == CMUEmploymentTheme.Colony)
            _departments.RemoveEmploymentMember(card);

        var action = Loc.GetString(request.Action switch
        {
            CMUEmploymentAction.Hire => "cmu-employment-hired",
            CMUEmploymentAction.Edit => "cmu-employment-edited",
            _ => "cmu-employment-terminated",
        });
        var accessNames = new List<string>();
        foreach (var tag in selected)
            accessNames.Add(ent.Comp.AccessNames.TryGetValue(tag, out var label)
                ? Loc.GetString(label) : ProtoMan.Index(tag).GetAccessLevelName());
        accessNames.Sort(StringComparer.CurrentCulture);
        var currentIcon = id.JobIcon;
        var entry = Loc.GetString("cmu-employment-history-entry", ("time", _timing.CurTime.ToString(@"hh\:mm\:ss")),
            ("operator", Name(actor)), ("action", action), ("name", id.FullName ?? Name(card)),
            ("title", title), ("access", string.Join(", ", accessNames)),
            ("iff", Loc.GetString(!terminate && request.Iff ? "cmu-employment-yes" : "cmu-employment-no")),
            ("icon", ent.Comp.JobIcons.TryGetValue(currentIcon, out var iconName) ? Loc.GetString(iconName) : currentIcon.Id));
        var historyLimit = Math.Clamp(ent.Comp.HistoryLimit, 0, 100);
        if (historyLimit > 0)
        {
            while (ent.Comp.History.Count >= historyLimit)
                ent.Comp.History.RemoveAt(0);
            ent.Comp.History.Add(entry);
        }

        _adminLog.Add(LogType.RMCIdModify, LogImpact.High,
            $"{ToPrettyString(actor):player} {request.Action} on {ToPrettyString(card):entity} at {ToPrettyString(ent.Owner):entity}; " +
            $"title {previousTitle} -> {title}; access [{string.Join(", ", previousAccess)}] -> [{string.Join(", ", selected)}]; " +
            $"card IFF replaced with {(!terminate && request.Iff ? ent.Comp.Faction.Id : "none")}; " +
            $"HUD icon {previousIcon} -> {id.JobIcon}");
        ent.Comp.Revision++;
        ent.Comp.LastReceipt = new(ent.Comp.Revision, GetNetEntity(card), request.Action, id.FullName ?? Name(card));
        BeginPrint(ent, actor, id, title, request.Action, accessNames, selected.Count,
            !terminate && request.Iff);
        UpdateUi(ent);
        _popup.PopupEntity(Loc.GetString("cmu-employment-applied"), ent.Owner, actor);
        return true;
    }

    private static bool ValidText(string text, int maximum)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > maximum)
            return false;
        foreach (var character in text)
        {
            if (char.IsControl(character) || character is '[' or ']' or '<' or '>')
                return false;
        }
        return true;
    }

    private bool Reject(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor, LocId message)
    {
        _popup.PopupEntity(Loc.GetString(message), ent.Owner, actor);
        return false;
    }

    private void UpdateUi(Entity<CMUEmploymentConsoleComponent> ent)
    {
        var authorized = Authorized(ent);
        var profile = GetGovforProfile(ent);
        var operatorCard = Card(ent.Owner, true);
        var state = new CMUEmploymentState
        {
            Heading = Loc.GetString(ent.Comp.Heading),
            Theme = ent.Comp.Theme,
            Logo = GetOfficeLogo(ent) ?? GetLogo(ent),
            Office = GetOfficeState(ent),
            Colony = ent.Comp.Theme == CMUEmploymentTheme.Colony && authorized ? ent.Comp.ColonySnapshot : null,
            CanBroadcast = CanBroadcastColony(ent),
            SirenActive = ent.Comp.Theme == CMUEmploymentTheme.Colony && _colonyComms.SirenActive,
            Brand = profile != null ? Loc.GetString(profile.Brand) : string.Empty,
            Accent = profile?.Accent,
            Background = profile?.Background,
            Surface = profile?.Surface,
            Powered = _power.IsPowered(ent.Owner),
            Printing = HasComp<CMUEmploymentPrintJobComponent>(ent.Owner),
            Authenticated = authorized,
            Operator = ent.Comp.Operator is { } actor ? GetNetEntity(actor) : null,
            OperatorName = ent.Comp.Operator is { } user && !TerminatingOrDeleted(user) ? Name(user) : string.Empty,
            OperatorCardInserted = operatorCard != null,
            OperatorCardName = operatorCard is { } operatorUid && TryComp<IdCardComponent>(operatorUid, out var operatorId)
                ? operatorId.FullName ?? Name(operatorUid) : string.Empty,
            Revision = ent.Comp.Revision,
            Receipt = ent.Comp.LastReceipt,
            DefaultJobIcon = ent.Comp.DefaultJobIcon,
        };
        if (Card(ent.Owner, false) is { } card && TryComp<IdCardComponent>(card, out var id))
        {
            state.Target = GetNetEntity(card);
            state.TargetName = id.FullName ?? string.Empty;
            state.JobTitle = id.LocalizedJobTitle ?? string.Empty;
            state.JobIcon = id.JobIcon;
            state.Blank = string.IsNullOrWhiteSpace(id.FullName);
            state.LocalEmployee = TryComp<CMUEmploymentCardComponent>(card, out var employee)
                && employee.Employer == ent.Comp.Faction.Id || IsNativeOfficeEmployee(ent, card);
            state.HasIff = _iff.HasFaction(card, ent.Comp.Faction);
            if (_access.TryGetTags(card) is { } tags)
                state.CurrentAccess.AddRange(tags);
        }
        if (authorized)
        {
            foreach (var (icon, label) in ent.Comp.JobIcons)
            {
                if (ProtoMan.HasIndex(icon))
                    state.JobIcons.Add(new(icon, Loc.GetString(label)));
            }
            foreach (var tag in ent.Comp.AllowedAccess)
            {
                if (ProtoMan.TryIndex(tag, out var definition))
                {
                    var name = ent.Comp.AccessNames.TryGetValue(tag, out var label)
                        ? Loc.GetString(label) : definition.GetAccessLevelName();
                    state.Options.Add(new(tag, name, ent.Comp.RestrictedAccess.Contains(tag)));
                }
            }
            foreach (var preset in ent.Comp.Presets)
                state.Presets.Add(new(Loc.GetString(preset.Name), Loc.GetString(preset.JobTitle), new(preset.Access), preset.JobIcon));
            state.History.AddRange(ent.Comp.History);
        }
        if (GetOfficeBrand(ent) is { } officeBrand)
            state.Brand = officeBrand;
        _ui.SetUiState(ent.Owner, CMUEmploymentUiKey.Key, state);
    }
}
