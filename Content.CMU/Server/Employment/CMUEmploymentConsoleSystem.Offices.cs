using System.Linq;
using Content.Server.CMU14.Round;
using Content.Shared.Access.Components;
using Content.Shared.CMU14.Employment;
using Content.Shared.GameTicking;
using Content.Shared.Paper;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.Employment;

public sealed partial class CMUEmploymentConsoleSystem
{
    [Dependency] private AuRoundSystem _officeRound = default!;
    private const string ApprovalPaperSlot = "CMUEmploymentApprovalPaper";
    private int _officeVersion;

    private bool PublicOfficeSession(Entity<CMUEmploymentConsoleComponent> ent)
        => _power.IsPowered(ent.Owner) && ent.Comp.Operator is { } actor && !TerminatingOrDeleted(actor)
           && _blocker.CanInteract(actor, ent.Owner) && _interaction.InRangeUnobstructed(actor, ent.Owner)
           && Card(ent.Owner, true) is { } card && TryComp<IdCardComponent>(card, out var id)
           && !string.IsNullOrWhiteSpace(id.FullName);

    private bool HasOperatorAccess(Entity<CMUEmploymentConsoleComponent> ent, EntityUid card)
    {
        if (CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner)?.PublicDesk == true)
            return TryComp<IdCardComponent>(card, out var id) && !string.IsNullOrWhiteSpace(id.FullName);
        return _access.TryGetTags(card)?.Contains(ent.Comp.RequiredAccess) ?? false;
    }

    private bool OfficeAdministrator(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor)
        => ent.Comp.Theme == CMUEmploymentTheme.Colony && ent.Comp.Operator == actor && Authorized(ent)
           && CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner)?.RequiresApproval != true
           && Card(ent.Owner, true) is { } card
           && (_access.TryGetTags(card)?.Contains("AU14AccessColonyAdministrator") ?? false);

    private bool NeedsOfficeApproval(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor)
        => CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner)?.RequiresApproval == true && !OfficeAdministrator(ent, actor);

    private EntityUid OfficeScope(EntityUid uid) => _stations.GetOwningStation(uid) ?? Transform(uid).MapUid ?? uid;

    private string SecurityForce()
    {
        var planet = _officeRound.ActivePlanet ?? _officeRound.GetSelectedPlanet();
        if (planet?.ColonyJobOverrides is { } overrides)
        {
            foreach (var key in new[] { "AU14JobLEOLeaderBase", "AU14JobCivilianCMBMarshal", "AU14JobLEOBase", "AU14JobCivilianCMBDeputy" })
            {
                if (overrides.TryGetValue(key, out var role) && ProtoMan.TryIndex(role, out var job)
                    && !string.IsNullOrWhiteSpace(job.RoundForce))
                    return job.RoundForce.ToUpperInvariant();
            }
        }
        return "SECURITY";
    }

    private string? GetOfficeBrand(Entity<CMUEmploymentConsoleComponent> ent)
    {
        if (CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner) is not { } office)
            return null;
        if (office.SecurityBranding)
            return Loc.GetString("cmu-employment-security-brand", ("force", SecurityForce()));
        return office.Department is { } department && ProtoMan.TryIndex(department, out var definition)
            ? Loc.GetString("cmu-employment-department-brand", ("name", Loc.GetString(definition.Name))) : null;
    }

    private EntProtoId<PaperComponent>? GetOfficeFormPrototype(Entity<CMUEmploymentConsoleComponent> ent)
    {
        if (CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner)?.SecurityBranding != true)
            return null;
        return SecurityForce() switch
        {
            "CMB" => "CMUPaperCMB",
            "NSPA" => "CMUPaperTWE",
            _ => "CMUPaperBase",
        };
    }

    private string? GetOfficeLogo(Entity<CMUEmploymentConsoleComponent> ent)
    {
        if (CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner)?.SecurityBranding != true)
            return null;
        return SecurityForce() switch
        {
            "CMB" => "/Textures/CMU14/Interface/Paper/CMBHeader.png",
            "NSPA" => "/Textures/CMU14/Interface/Paper/TWEHeader.png",
            _ => string.Empty,
        };
    }

    public CMUEmploymentOfficeState? GetOfficeState(Entity<CMUEmploymentConsoleComponent> ent)
    {
        if (ent.Comp.Theme != CMUEmploymentTheme.Colony)
            return null;
        var office = CompOrNull<CMUEmploymentOfficeComponent>(ent.Owner);
        var session = PublicOfficeSession(ent);
        var leader = Authorized(ent);
        var admin = ent.Comp.Operator is { } actor && OfficeAdministrator(ent, actor);
        var officer = OfficeOfficer(ent);
        var state = new CMUEmploymentOfficeState
        {
            Enabled = office != null || admin,
            ReaderCard = Card(ent.Owner, true) is { } readerCard ? GetNetEntity(readerCard) : null,
            PublicDesk = office?.PublicDesk == true,
            JailOnly = office?.Jail == true,
            Version = _officeVersion,
            Scope = GetNetEntity(OfficeScope(ent.Owner)),
            PublicSession = session,
            Administrator = admin,
            Leader = leader,
            Officer = officer,
            RequestsApproval = office?.RequiresApproval == true,
            PaperInserted = _containers.TryGetContainer(ent.Owner, ApprovalPaperSlot, out var slot) && slot.ContainedEntities.Count > 0,
            Time = _timing.CurTime.TotalSeconds,
            Department = office?.Department?.Id ?? string.Empty,
            SecurityForce = office?.SecurityBranding == true ? SecurityForce() : string.Empty,
        };
        if (!session && !leader)
            return state;
        var scope = OfficeScope(ent.Owner);
        var ownCard = Card(ent.Owner, true);
        foreach (var request in _officeRequests.Values.OrderByDescending(entry => entry.Id))
        {
            if (request.Scope != scope || !admin && !(leader && request.Department == state.Department) && request.RequesterCard != ownCard)
                continue;
            state.Requests.Add(new(request.Id, request.Reference, request.Subject, request.Description,
                Loc.GetString("cmu-employment-request-status-" + request.Status), request.Status == "pending"));
        }
        if (officer)
        {
            foreach (var entry in _officeJail.Values.Where(entry => entry.Scope == scope).OrderByDescending(entry => entry.Id))
                state.Jail.Add(new(entry.Id, entry.Name, entry.Charges, entry.Officer, entry.Due.TotalSeconds, entry.Released));
        }
        return state;
    }

    private void OnOfficeRoundRestart(RoundRestartCleanupEvent args)
    {
        _officeRequests.Clear();
        _officeJail.Clear();
        _officeVersion++;
    }

    private void RefreshOffices()
    {
        _officeVersion++;
        var query = EntityQueryEnumerator<CMUEmploymentConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            if (console.Theme == CMUEmploymentTheme.Colony)
                UpdateUi((uid, console));
        }
    }
}
