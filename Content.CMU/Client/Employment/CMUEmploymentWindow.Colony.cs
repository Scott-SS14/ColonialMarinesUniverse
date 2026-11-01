using System;
using System.Linq;
using Content.Client.CrewManifest.UI;
using Content.Shared.CMU14.Employment;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

namespace Content.Client.CMU14.Employment;

public sealed partial class CMUEmploymentWindow
{
    public event Action<CMUEmploymentColonyMessage>? OnColony;

    private CrewManifestListing _manifest = default!;
    private int _colonyPage;
    private int _colonyVersion = -1;
    private string? _department;

    private void InitializeColony()
    {
        _manifest = new CrewManifestListing { Orientation = BoxContainer.LayoutOrientation.Vertical, HorizontalExpand = true };
        PersonnelApp.Text = Loc.GetString("cmu-employment-app-personnel");
        ManifestApp.Text = Loc.GetString("cmu-employment-app-manifest");
        CommunicationsApp.Text = Loc.GetString("cmu-employment-app-comms");
        ManifestRefresh.Text = Loc.GetString("cmu-employment-refresh");
        ManifestHint.Text = Wrap(Loc.GetString("cmu-employment-manifest-hint"), 65);
        CommunicationsHeading.Text = Loc.GetString("colony-comms-console-title");
        CommunicationsHint.Text = Wrap(Loc.GetString("cmu-employment-comms-hint"), 64);
        BroadcastCaption.Text = Loc.GetString("cmu-employment-broadcast-caption");
        BroadcastText.PlaceHolder = Loc.GetString("cmu-employment-broadcast-placeholder");
        ConfirmBroadcast.Text = Loc.GetString("cmu-employment-broadcast-confirm");
        SendBroadcast.Text = Loc.GetString("colony-comms-console-send-message");
        ConfirmSiren.Text = Loc.GetString("cmu-employment-siren-confirm");
        CommunicationsAccessHint.Text = Wrap(Loc.GetString("cmu-employment-comms-access-hint"), 64);
        PersonnelApp.OnPressed += _ => SelectColonyPage(0);
        ManifestApp.OnPressed += _ => SelectColonyPage(1);
        CommunicationsApp.OnPressed += _ => SelectColonyPage(2);
        ManifestRefresh.OnPressed += _ => RequestColonyRefresh();
        BroadcastText.OnTextChanged += _ =>
        {
            ConfirmBroadcast.Pressed = false;
            UpdateColonyControls();
        };
        ConfirmBroadcast.OnToggled += _ => UpdateColonyControls();
        ConfirmSiren.OnToggled += _ => UpdateColonyControls();
        SendBroadcast.OnPressed += _ =>
        {
            if (_state is not { } state || SendBroadcast.Disabled || !ConfirmBroadcast.Pressed)
                return;
            ConfirmBroadcast.Pressed = false;
            OnColony?.Invoke(new(CMUEmploymentColonyAction.Announce, state.Revision, BroadcastText.Text));
        };
        ToggleSiren.OnPressed += _ =>
        {
            if (_state is not { } state || ToggleSiren.Disabled || !ConfirmSiren.Pressed)
                return;
            ConfirmSiren.Pressed = false;
            OnColony?.Invoke(new(CMUEmploymentColonyAction.Siren, state.Revision));
        };
        foreach (var button in new[] { PersonnelApp, ManifestApp, CommunicationsApp, ManifestRefresh, SendBroadcast, ToggleSiren })
            BindButton(button);
    }

    private void SelectColonyPage(int page)
    {
        _colonyPage = page;
        Confirm.Pressed = false;
        ConfirmPayroll.Pressed = false;
        ConfirmBroadcast.Pressed = false;
        ConfirmSiren.Pressed = false;
        UpdateActions();
        if ((page == 1 || page == 3) && _state is { Authenticated: true })
            RequestColonyRefresh();
    }

    private void RequestColonyRefresh()
    {
        if (_state is { Theme: CMUEmploymentTheme.Colony, Authenticated: true } state)
            OnColony?.Invoke(new(CMUEmploymentColonyAction.Refresh, state.Revision));
    }

    private bool TryUpdateColonyOnly(CMUEmploymentState state)
    {
        if (_state is not { Theme: CMUEmploymentTheme.Colony } old || state.Theme != old.Theme
            || state.Revision != old.Revision || state.Target != old.Target || state.Operator != old.Operator
            || state.Authenticated != old.Authenticated || state.Powered != old.Powered || state.Printing != old.Printing
            || state.TargetName != old.TargetName || state.JobTitle != old.JobTitle || state.JobIcon != old.JobIcon
            || state.HasIff != old.HasIff || !state.CurrentAccess.SequenceEqual(old.CurrentAccess)
            || state.Receipt?.Revision != old.Receipt?.Revision || state.Receipt?.Printed != old.Receipt?.Printed
            || state.Colony?.Version == old.Colony?.Version && state.SirenActive == old.SirenActive && !_payrollPending
                && state.Office?.Version == old.Office?.Version)
            return false;

        // Hub refreshes must not discard an unfinished credential draft.
        _state = state;
        _payrollPending = false;
        UpdateActions();
        return true;
    }

    private void UpdateColony()
    {
        if (_state is not { } state)
            return;
        var colony = state.Theme == CMUEmploymentTheme.Colony;
        if (!colony)
            _colonyPage = 0;
        ColonyApplications.Visible = colony;
        EmploymentPage.Visible = _colonyPage == 0;
        WorkflowPanel.Visible = _colonyPage == 0;
        ManifestFrame.Visible = colony && _colonyPage == 1;
        CommunicationsFrame.Visible = colony && _colonyPage == 2;
        foreach (var button in new[] { PersonnelApp, ManifestApp, CommunicationsApp })
            StyleButton(button);
        ManifestFrame.PanelOverride = Box(_surface, PanelEdge, 1);
        DepartmentSidebar.PanelOverride = Box(_background, PanelEdge, 1);
        CommunicationsFrame.PanelOverride = Box(_surface, PanelEdge, 1);
        ManifestHeading.FontOverride = _displayFont;
        CommunicationsHeading.FontOverride = _displayFont;
        ManifestTimestamp.FontColorOverride = TextSecondary;
        ManifestHint.FontColorOverride = TextSecondary;
        CommunicationsHint.FontColorOverride = TextSecondary;
        CommunicationsAccessHint.FontColorOverride = TextSecondary;
        UpdateColonyControls();
        UpdatePayroll();
        if (state.Colony is not { } snapshot)
        {
            if (_colonyVersion != -1)
            {
                ManifestHost.RemoveAllChildren();
                _manifest.RemoveAllChildren();
                DepartmentButtons.RemoveAllChildren();
                _colonyVersion = -1;
            }
            ManifestHeading.Text = Loc.GetString("cmu-employment-app-manifest");
            ManifestTimestamp.Text = Loc.GetString("cmu-employment-manifest-load");
            return;
        }
        ManifestHeading.Text = string.IsNullOrWhiteSpace(snapshot.Station)
            ? Loc.GetString("cmu-employment-app-manifest") : snapshot.Station;
        ManifestTimestamp.Text = Loc.GetString("cmu-employment-manifest-updated", ("time", snapshot.Updated));
        if (_colonyPage != 1 || _colonyVersion == snapshot.Version)
            return;
        _colonyVersion = snapshot.Version;
        DepartmentButtons.RemoveAllChildren();
        var all = new Button { Text = Loc.GetString("cmu-employment-manifest-station") };
        BindButton(all);
        StyleButton(all);
        all.OnPressed += _ => { _department = null; ShowColonyRoster(); };
        DepartmentButtons.AddChild(all);
        foreach (var department in snapshot.Departments)
        {
            var button = new Button
            {
                Text = Loc.GetString("cmu-employment-department-button", ("name", department.Name), ("count", department.Members.Count)),
                ToolTip = department.Name,
            };
            BindButton(button);
            StyleButton(button);
            var key = department.Key;
            button.OnPressed += _ => { _department = key; ShowColonyRoster(); };
            DepartmentButtons.AddChild(button);
        }
        ShowColonyRoster();
    }

    private void ShowColonyRoster()
    {
        ManifestHost.RemoveAllChildren();
        if (_state?.Colony is not { } snapshot)
            return;
        if (_department != null)
        {
            var department = snapshot.Departments.FirstOrDefault(entry => entry.Key == _department);
            if (department != null)
            {
                ManifestHost.AddChild(new Label { Text = department.Name, FontOverride = _displayFont, FontColorOverride = _accent });
                foreach (var member in department.Members)
                {
                    var line = Loc.GetString("cmu-employment-department-member", ("name", member.Name), ("title", member.JobTitle));
                    ManifestHost.AddChild(new Label { Text = line, ToolTip = line, ClipText = true, FontOverride = _font });
                }
                if (department.Members.Count == 0)
                    ManifestHost.AddChild(new Label { Text = Loc.GetString("cmu-employment-department-empty"), FontOverride = _font });
                return;
            }
            _department = null;
        }
        _manifest.RemoveAllChildren();
        if (snapshot.Manifest is { } manifest && manifest.Entries.Length > 0)
        {
            _manifest.AddCrewManifestEntries(manifest);
            ManifestHost.AddChild(_manifest);
        }
        else
            ManifestHost.AddChild(new Label { Text = Wrap(Loc.GetString("cmu-employment-manifest-empty"), 44), FontOverride = _font });
    }

    private void UpdateColonyControls()
    {
        if (_state is not { } state)
            return;
        var permitted = state.Theme == CMUEmploymentTheme.Colony && state.Powered && state.CanBroadcast;
        if (!permitted)
        {
            ConfirmBroadcast.Pressed = false;
            ConfirmSiren.Pressed = false;
        }
        BroadcastText.Editable = permitted;
        ConfirmBroadcast.Disabled = !permitted;
        ConfirmSiren.Disabled = !permitted;
        SendBroadcast.Disabled = !permitted || !ConfirmBroadcast.Pressed
            || string.IsNullOrWhiteSpace(BroadcastText.Text) || BroadcastText.Text.Length > 512;
        ToggleSiren.Disabled = !permitted || !ConfirmSiren.Pressed;
        ManifestRefresh.Disabled = !state.Authenticated || !state.Powered;
        BroadcastCount.Text = Loc.GetString("cmu-employment-broadcast-count", ("count", BroadcastText.Text.Length));
        SirenStatus.Text = Loc.GetString(state.SirenActive ? "cmu-employment-siren-active" : "cmu-employment-siren-inactive");
        SirenStatus.FontColorOverride = state.SirenActive ? WarningText : TextSecondary;
        ToggleSiren.Text = Loc.GetString(state.SirenActive ? "cmu-employment-siren-stop" : "cmu-employment-siren-start");
        foreach (var button in new[] { ManifestRefresh, SendBroadcast, ToggleSiren })
            StyleButton(button);
    }
}
