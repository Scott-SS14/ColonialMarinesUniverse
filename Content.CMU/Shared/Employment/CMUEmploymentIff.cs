using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

public sealed partial class GunIFFSystem
{
    public bool MatchesEmploymentCardIff(EntityUid card, EntProtoId<IFFFactionComponent> faction, bool authorized)
    {
        if (!TryComp<ItemIFFComponent>(card, out var iff))
            return !authorized;
        return authorized ? iff.Factions.Count == 1 && iff.Factions.Contains(faction) : iff.Factions.Count == 0;
    }

    public void ClearEmploymentCardIff(Entity<ItemIFFComponent> card)
    {
        if (card.Comp.Factions.Count == 0)
            return;

        card.Comp.Factions.Clear();
        Dirty(card);
    }
}
