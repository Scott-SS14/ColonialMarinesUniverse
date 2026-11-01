using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.CMU14.Employment;
using Content.Shared.Paper;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Server.CMU14.Employment;

public sealed partial class CMUEmploymentConsoleSystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private MetaDataSystem _meta = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly List<EntityUid> _finishedPrintJobs = new();

    private void OnStartup(Entity<CMUEmploymentConsoleComponent> ent, ref ComponentStartup args)
    {
        UpdateMachineVisuals(ent);
    }

    private void UpdateMachineVisuals(Entity<CMUEmploymentConsoleComponent> ent)
    {
        var state = !_power.IsPowered(ent.Owner) ? CMUEmploymentMachineState.Off
            : HasComp<CMUEmploymentPrintJobComponent>(ent.Owner) ? CMUEmploymentMachineState.Printing
            : CMUEmploymentMachineState.Ready;
        _appearance.SetData(ent.Owner, CMUEmploymentVisuals.State, state);
        var profile = GetGovforProfile(ent);
        var sprite = profile?.Sprite ?? new ResPath(ent.Comp.Theme switch
        {
            CMUEmploymentTheme.Govfor => "CMU14/Structures/Machines/Employment/govfor.rsi",
            CMUEmploymentTheme.Colony => "CMU14/Structures/Machines/Employment/colony.rsi",
            _ => "CMU14/Structures/Machines/Employment/weyland.rsi",
        });
        _appearance.SetData(ent.Owner, CMUEmploymentVisuals.Sprite, sprite);
    }

    private void BeginPrint(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor, IdCardComponent id,
        string title, CMUEmploymentAction action, List<string> accessNames, int accessCount, bool iff)
    {
        var job = EnsureComp<CMUEmploymentPrintJobComponent>(ent.Owner);
        var documentType = Loc.GetString(action switch
        {
            CMUEmploymentAction.Hire => "cmu-employment-form-hire",
            CMUEmploymentAction.Edit => "cmu-employment-form-edit",
            _ => "cmu-employment-form-terminate",
        });
        var name = id.FullName ?? Loc.GetString("cmu-employment-unassigned");
        var icon = ent.Comp.JobIcons.TryGetValue(id.JobIcon, out var iconLabel)
            ? Loc.GetString(iconLabel) : Loc.GetString("cmu-employment-form-icon-retained");
        if (action == CMUEmploymentAction.Terminate)
            icon = Loc.GetString("cmu-employment-form-no-icon");
        var permissions = accessNames.Count == 0 ? Loc.GetString("cmu-employment-form-no-access")
            : string.Join("\n", accessNames.Select(entry => "• " + FormattedMessage.EscapeText(entry)));
        var branding = Loc.GetString(ent.Comp.Theme switch
        {
            CMUEmploymentTheme.Govfor => "cmu-employment-form-brand-govfor",
            CMUEmploymentTheme.Colony => "cmu-employment-form-brand-colony",
            _ => "cmu-employment-form-brand-weyland",
        });
        if (GetGovforProfile(ent) is { } profile)
            branding = Loc.GetString("cmu-employment-brand-selected", ("name", Loc.GetString(profile.Brand)));
        var policy = Loc.GetString(ent.Comp.Theme switch
        {
            CMUEmploymentTheme.Govfor => "cmu-employment-form-policy-govfor",
            CMUEmploymentTheme.Colony => "cmu-employment-form-policy-colony",
            _ => "cmu-employment-form-policy-weyland",
        });
        var reference = $"{GetNetEntity(ent.Owner)}-{ent.Comp.Revision:D4}";
        job.Prototype = GetFormPrototype(ent);
        job.Issuer = actor;
        job.Remaining = float.IsFinite(ent.Comp.PrintDuration) ? Math.Clamp(ent.Comp.PrintDuration, 0.1f, 10f) : 2f;
        job.DocumentName = Loc.GetString("cmu-employment-form-name", ("name", name));
        job.Description = Loc.GetString("cmu-employment-form-description", ("name", name),
            ("title", title), ("count", accessCount), ("action", documentType));
        job.Content = Loc.GetString("cmu-employment-form-content",
            ("brand", branding), ("policy", policy), ("type", documentType),
            ("reference", FormattedMessage.EscapeText(reference)),
            ("time", _timing.CurTime.ToString(@"hh\:mm\:ss")),
            ("name", FormattedMessage.EscapeText(name)), ("title", FormattedMessage.EscapeText(title)),
            ("operator", FormattedMessage.EscapeText(Name(actor))), ("count", accessCount),
            ("permissions", permissions), ("icon", FormattedMessage.EscapeText(icon)),
            ("iff", Loc.GetString(iff ? "cmu-employment-form-iff-authorized" : "cmu-employment-form-iff-none")));
        UpdateMachineVisuals(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<CMUEmploymentPrintJobComponent, CMUEmploymentConsoleComponent>();
        while (query.MoveNext(out var uid, out var job, out _))
        {
            if (!_power.IsPowered(uid))
                continue;
            job.Remaining -= frameTime;
            if (job.Remaining <= 0f)
                _finishedPrintJobs.Add(uid);
        }
        foreach (var uid in _finishedPrintJobs)
        {
            if (!TryComp<CMUEmploymentConsoleComponent>(uid, out var console)
                || !TryComp<CMUEmploymentPrintJobComponent>(uid, out var job))
                continue;

            var paper = Spawn(job.Prototype, Transform(uid).Coordinates);
            Comp<PaperComponent>(paper).EditingDisabled = true;
            _paper.SetContent(paper, job.Content);
            _meta.SetEntityName(paper, job.DocumentName);
            _meta.SetEntityDescription(paper, job.Description);
            _transform.PlaceNextTo(paper, uid);
            _audio.PlayPvs(console.PrintSound, uid);
            if (!TerminatingOrDeleted(job.Issuer) && _blocker.CanInteract(job.Issuer, uid)
                && _interaction.InRangeUnobstructed(job.Issuer, uid))
                _hands.TryPickupAnyHand(job.Issuer, paper);
            RemComp<CMUEmploymentPrintJobComponent>(uid);
            UpdateMachineVisuals((uid, console));
            UpdateUi((uid, console));
        }
        _finishedPrintJobs.Clear();
    }
}
