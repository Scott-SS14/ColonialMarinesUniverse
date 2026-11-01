using Content.Shared.Access.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Access.Systems;

public abstract partial class SharedAccessSystem
{
    public bool MatchesEmploymentAccess(Entity<AccessComponent> card,
        ICollection<ProtoId<AccessLevelPrototype>> tags)
    {
        return card.Comp.Enabled && card.Comp.Groups.Count == 0
            && card.Comp.Tags.SetEquals(tags);
    }

    public void ReplaceEmploymentAccess(Entity<AccessComponent> card,
        IEnumerable<ProtoId<AccessLevelPrototype>> tags)
    {
        // Old groups would otherwise grant the previous employer's permissions again after map initialization.
        card.Comp.Groups.Clear();
        TrySetTags(card.Owner, tags, card.Comp);
        SetAccessEnabled(card.Owner, true, card.Comp);
    }
}
