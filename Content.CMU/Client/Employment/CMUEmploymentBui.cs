using Content.Shared.CMU14.Employment;
using Robust.Client.UserInterface;

namespace Content.Client.CMU14.Employment;

public sealed class CMUEmploymentBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private CMUEmploymentWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<CMUEmploymentWindow>();
        _window.OnSlot += slot => SendMessage(new CMUEmploymentSlotMessage(slot));
        _window.OnApply += request => SendMessage(request);
        _window.OnColony += request => SendMessage(request);
        _window.OnPayroll += request => SendMessage(request);
        _window.OnOffice += request => SendMessage(request);
        _window.OnRefresh += () => SendMessage(new CMUEmploymentRefreshMessage());
        SendMessage(new CMUEmploymentRefreshMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is CMUEmploymentState employment)
            _window?.UpdateState(employment);
    }
}
