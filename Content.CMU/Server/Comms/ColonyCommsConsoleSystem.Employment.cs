using Content.Shared.Audio;
using Content.Shared.CMU14;
using Content.Shared.CMU14.Employment;
using Content.Shared.GameTicking;
using Robust.Shared.Audio;

namespace Content.Server.CMU14.Comms;

public sealed partial class ColonyCommsConsoleSystem
{
    public bool SirenActive { get; private set; }

    private void InitializeEmploymentIntegration()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnEmploymentRoundRestart);
    }

    private void OnEmploymentRoundRestart(RoundRestartCleanupEvent args)
    {
        SetColonySiren(false);
    }

    public void SetColonySiren(bool active)
    {
        if (SirenActive == active)
            return;

        SirenActive = active;
        var sirens = AllEntityQuery<ColonySirenComponent>();
        while (sirens.MoveNext(out var uid, out _))
        {
            if (active)
            {
                if (!TryComp<AmbientSoundComponent>(uid, out var ambient))
                {
                    ambient = AddComp<AmbientSoundComponent>(uid);
                    _ambientSound.SetSound(uid, new SoundPathSpecifier("/Audio/CMU14/Machines/ColonySiren.ogg"), ambient);
                    _ambientSound.SetRange(uid, 48f, ambient);
                    _ambientSound.SetVolume(uid, -1f, ambient);
                    _ambientSound.SetAmbience(uid, true, ambient);
                }
                else
                    _ambientSound.SetVolume(uid, -2f, ambient);
            }
            else if (TryComp<AmbientSoundComponent>(uid, out var ambient))
                _ambientSound.SetVolume(uid, -999f, ambient);
        }
        var consoles = AllEntityQuery<ColonyCommsConsoleComponent>();
        while (consoles.MoveNext(out _, out var console))
            console.SirenActive = active;
        RaiseLocalEvent(new CMUEmploymentSirenChangedEvent());
    }
}
