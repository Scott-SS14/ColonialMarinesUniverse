using Content.Server.CMU14.Round;
using Content.Server.GameTicking;
using Content.Shared.CMU14.Employment;
using Content.Shared.Paper;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.Employment;

public sealed partial class CMUEmploymentConsoleSystem
{
    private CMUEmploymentGovforProfile? GetGovforProfile(Entity<CMUEmploymentConsoleComponent> ent)
    {
        if (ent.Comp.Theme != CMUEmploymentTheme.Govfor)
            return null;
        var platoon = EntityManager.EntitySysManager
            .GetEntitySystem<PlatoonSpawnRuleSystem>().SelectedGovforPlatoon;
        return platoon != null && ent.Comp.GovforProfiles.TryGetValue(platoon.ID, out var profile) ? profile : null;
    }

    private EntProtoId<PaperComponent> GetFormPrototype(Entity<CMUEmploymentConsoleComponent> ent)
        => GetGovforProfile(ent)?.Paper ?? ent.Comp.FormPrototype;

    private void OnRunLevelChanged(GameRunLevelChangedEvent args)
    {
        if (args.New != GameRunLevel.InRound)
            return;

        var query = EntityQueryEnumerator<CMUEmploymentConsoleComponent>();
        while (query.MoveNext(out var uid, out var console))
        {
            console.Revision++;
            UpdateMachineVisuals((uid, console));
            UpdateUi((uid, console));
        }
    }
}
