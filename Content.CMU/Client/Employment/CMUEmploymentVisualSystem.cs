using Content.Shared.CMU14.Employment;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.CMU14.Employment;

public sealed class CMUEmploymentVisualSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprites = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CMUEmploymentVisualComponent, AppearanceChangeEvent>(OnAppearance);
    }

    private void OnAppearance(Entity<CMUEmploymentVisualComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite
            || !_sprites.LayerMapTryGet((ent.Owner, sprite), CMUEmploymentVisuals.State, out var layer, false)
            || !_appearance.TryGetData<CMUEmploymentMachineState>(ent.Owner, CMUEmploymentVisuals.State, out var state, args.Component)
            || !_appearance.TryGetData<ResPath>(ent.Owner, CMUEmploymentVisuals.Sprite, out var path, args.Component))
            return;

        _sprites.LayerSetRsi((ent.Owner, sprite), layer, path, state switch
        {
            CMUEmploymentMachineState.Ready => "computer",
            CMUEmploymentMachineState.Printing => "printing",
            _ => "off",
        });
    }
}
