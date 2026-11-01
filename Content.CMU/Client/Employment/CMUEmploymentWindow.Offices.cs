using System;
using System.Collections.Generic;
using System.Globalization;
using Content.Shared.CMU14.Employment;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;

namespace Content.Client.CMU14.Employment;

public sealed partial class CMUEmploymentWindow
{
    public event Action<CMUEmploymentOfficeMessage>? OnOffice;
    private readonly List<CMUEmploymentRequestEntry> _requests = new();
    private readonly List<CMUEmploymentJailEntry> _bookings = new();
    private readonly List<(CMUEmploymentJailEntry Entry, Label Label)> _jailClocks = new();
    private int _requestId;
    private int _bookingId;
    private int _renderedOfficeVersion = -1;
    private bool _renderedOfficer;
    private bool _renderedLeader;
    private bool _renderedAdmin;
    private NetEntity? _officeReader;
    private NetEntity? _officeScope;
    private CMUEmploymentOfficeState? _officeClockState;
    private double _officeTime;
    private float _officeElapsed;
    private int _jailSecond = -1;

    private void InitializeOffices()
    {
        OfficeServicesApp.Text = Loc.GetString("cmu-employment-office-services");
        OfficeRequestsApp.Text = Loc.GetString("cmu-employment-office-requests");
        OfficeJailApp.Text = Loc.GetString("cmu-employment-office-jail");
        OfficeServicesHeading.Text = Loc.GetString("cmu-employment-office-services-heading");
        OfficeServicesHint.Text = Wrap(Loc.GetString("cmu-employment-office-services-hint"), 60);
        OfficeReportText.PlaceHolder = Loc.GetString("cmu-employment-office-report-placeholder");
        OfficeReportSend.Text = Loc.GetString("cmu-employment-office-report-send");
        OfficeRequestsHeading.Text = Loc.GetString("cmu-employment-office-requests-heading");
        OfficeRefresh.Text = Loc.GetString("cmu-employment-refresh");
        OfficePaper.Text = Loc.GetString("cmu-employment-office-paper");
        OfficeDecisionConfirm.Text = Loc.GetString("cmu-employment-office-confirm");
        OfficeApprove.Text = Loc.GetString("cmu-employment-office-approve");
        OfficeReject.Text = Loc.GetString("cmu-employment-office-reject");
        OfficeReprint.Text = Loc.GetString("cmu-employment-office-reprint");
        OfficeJailHeading.Text = Loc.GetString("cmu-employment-jail-heading");
        OfficeJailHint.Text = Wrap(Loc.GetString("cmu-employment-jail-hint"), 60);
        OfficeJailMinutes.PlaceHolder = Loc.GetString("cmu-employment-jail-minutes");
        OfficeJailReason.PlaceHolder = Loc.GetString("cmu-employment-jail-reason");
        OfficeJailConfirm.Text = Loc.GetString("cmu-employment-jail-confirm");
        OfficeBook.Text = Loc.GetString("cmu-employment-jail-book");
        OfficeRelease.Text = Loc.GetString("cmu-employment-jail-release");
        OfficeServicesApp.OnPressed += _ => SelectColonyPage(5);
        OfficeRequestsApp.OnPressed += _ => { SelectColonyPage(4); SendOffice(CMUEmploymentOfficeAction.Refresh); };
        OfficeJailApp.OnPressed += _ => { SelectColonyPage(6); SendOffice(CMUEmploymentOfficeAction.Refresh); };
        OfficeRefresh.OnPressed += _ => SendOffice(CMUEmploymentOfficeAction.Refresh);
        OfficeReportSend.OnPressed += _ => SendOffice(CMUEmploymentOfficeAction.Report, OfficeReportText.Text);
        OfficePaper.OnPressed += _ => SendOffice(CMUEmploymentOfficeAction.Paper);
        OfficeApprove.OnPressed += _ => SendOffice(CMUEmploymentOfficeAction.Approve);
        OfficeReject.OnPressed += _ => SendOffice(CMUEmploymentOfficeAction.Reject);
        OfficeReprint.OnPressed += _ => SendOffice(CMUEmploymentOfficeAction.Reprint);
        OfficeBook.OnPressed += _ => SendOffice(CMUEmploymentOfficeAction.Book, OfficeJailReason.Text);
        OfficeRelease.OnPressed += _ => SendOffice(CMUEmploymentOfficeAction.Release, OfficeJailReason.Text);
        OfficeRequestChoices.OnItemSelected += args =>
        {
            if (args.Id < 0 || args.Id >= _requests.Count)
                return;
            OfficeRequestChoices.SelectId(args.Id);
            _requestId = _requests[args.Id].Id;
            OfficeDecisionConfirm.Pressed = false;
            UpdateOfficeControls();
        };
        OfficeJailChoices.OnItemSelected += args =>
        {
            if (args.Id < 0 || args.Id >= _bookings.Count)
                return;
            OfficeJailChoices.SelectId(args.Id);
            _bookingId = _bookings[args.Id].Id;
            OfficeJailConfirm.Pressed = false;
            UpdateOfficeControls();
        };
        foreach (var field in new[] { OfficeReportText, OfficeJailMinutes, OfficeJailReason })
            field.OnTextChanged += _ => { OfficeJailConfirm.Pressed = false; UpdateOfficeControls(); };
        OfficeDecisionConfirm.OnToggled += _ => UpdateOfficeControls();
        OfficeJailConfirm.OnToggled += _ => UpdateOfficeControls();
        foreach (var button in new[] { OfficeServicesApp, OfficeRequestsApp, OfficeJailApp, OfficeRefresh,
                     OfficeReportSend, OfficePaper, OfficeApprove, OfficeReject, OfficeReprint, OfficeBook, OfficeRelease })
            BindButton(button);
    }

    private void UpdateOffices()
    {
        if (_state is not { } state)
            return;
        var office = state.Office;
        var publicOnly = office?.PublicDesk == true && !state.Authenticated;
        if (office?.JailOnly == true)
            _colonyPage = 6;
        else if (publicOnly && _colonyPage is not (4 or 5 or 6))
            _colonyPage = 5;
        else if (office?.Enabled != true && _colonyPage >= 4)
            _colonyPage = 0;
        var management = state.Authenticated && office?.JailOnly != true;
        ColonyApplications.Visible = state.Theme == CMUEmploymentTheme.Colony && management;
        OfficeApplications.Visible = office?.Enabled == true;
        OfficeServicesApp.Visible = office?.PublicDesk == true;
        OfficeRequestsApp.Visible = office?.JailOnly != true;
        OfficeJailApp.Visible = office?.Officer == true || office?.JailOnly == true;
        EmploymentPage.Visible = _colonyPage == 0 && !publicOnly && office?.JailOnly != true;
        WorkflowPanel.Visible = EmploymentPage.Visible;
        ManifestFrame.Visible = _colonyPage == 1 && management;
        CommunicationsFrame.Visible = _colonyPage == 2 && management;
        PayrollFrame.Visible = _colonyPage == 3 && management;
        OfficeServicesFrame.Visible = _colonyPage == 5 && office?.PublicDesk == true;
        OfficeRequestsFrame.Visible = _colonyPage == 4 && office?.Enabled == true;
        OfficeJailFrame.Visible = _colonyPage == 6 && office?.Officer == true;
        PayColony.Visible = office?.Administrator == true && string.IsNullOrEmpty(office.Department);
        if (publicOnly && office?.PublicSession == true)
            Status.Text = Loc.GetString("cmu-employment-office-identified", ("name", state.OperatorCardName));
        foreach (var panel in new[] { OfficeServicesFrame, OfficeRequestsFrame, OfficeJailFrame })
            panel.PanelOverride = Box(_surface, PanelEdge, 1);
        if (office == null)
            return;
        if (!ReferenceEquals(_officeClockState, office))
        {
            _officeClockState = office;
            _officeTime = office.Time;
            _officeElapsed = 0;
        }
        if (_renderedOfficeVersion != office.Version || _renderedOfficer != office.Officer
            || _renderedLeader != office.Leader || _renderedAdmin != office.Administrator || _officeReader != office.ReaderCard || _officeScope != office.Scope)
        {
            _renderedOfficeVersion = office.Version;
            _renderedOfficer = office.Officer;
            _renderedLeader = office.Leader;
            _renderedAdmin = office.Administrator;
            _officeReader = office.ReaderCard;
            _officeScope = office.Scope;
            _requests.Clear();
            _requests.AddRange(office.Requests);
            OfficeRequestChoices.Clear();
            var selected = 0;
            for (var i = 0; i < _requests.Count; i++)
            {
                OfficeRequestChoices.AddItem(_requests[i].Reference + " / " + _requests[i].Subject, i);
                if (_requests[i].Id == _requestId)
                    selected = i;
            }
            _requestId = _requests.Count > 0 ? _requests[selected].Id : 0;
            if (_requests.Count > 0)
                OfficeRequestChoices.SelectId(selected);
            _bookings.Clear();
            _bookings.AddRange(office.Jail);
            OfficeJailChoices.Clear();
            OfficeJailList.RemoveAllChildren();
            _jailClocks.Clear();
            for (var i = 0; i < _bookings.Count; i++)
            {
                var entry = _bookings[i];
                OfficeJailChoices.AddItem(entry.Name, i);
                var label = new Label { FontOverride = _font };
                OfficeJailList.AddChild(label);
                _jailClocks.Add((entry, label));
            }
            _bookingId = _bookings.Count > 0 ? _bookings[0].Id : 0;
            if (_bookings.Count > 0)
                OfficeJailChoices.SelectId(0);
            _jailSecond = -1;
            OfficeDecisionConfirm.Pressed = false;
            OfficeJailConfirm.Pressed = false;
        }
        UpdateOfficeControls();
    }

    private void UpdateOfficeControls()
    {
        if (_state is not { } state || state.Office is not { } office)
            return;
        OfficeRequestsApp.Disabled = !office.PublicSession && !office.Leader;
        OfficeJailApp.Disabled = !office.Officer;
        var selected = _requests.Find(entry => entry.Id == _requestId);
        OfficeRequestDetails.Text = selected == null ? Loc.GetString("cmu-employment-office-no-requests")
            : Wrap(selected.Reference + " / " + selected.Subject + "\n" + selected.Description + " / " + selected.Status, 62);
        OfficePaper.Visible = office.Administrator;
        OfficeApprove.Visible = office.Administrator;
        OfficeReject.Visible = office.Administrator;
        OfficeDecisionConfirm.Visible = office.Administrator;
        OfficePaper.Text = Loc.GetString(office.PaperInserted ? "cmu-employment-office-paper-eject" : "cmu-employment-office-paper");
        OfficeApprove.Disabled = !office.Administrator || !office.PaperInserted || selected?.Pending != true || !OfficeDecisionConfirm.Pressed;
        OfficeReject.Disabled = !office.Administrator || selected?.Pending != true || !OfficeDecisionConfirm.Pressed;
        OfficeReprint.Disabled = selected == null || state.Printing;
        OfficeReportSend.Disabled = !office.PublicSession || state.Printing || string.IsNullOrWhiteSpace(OfficeReportText.Text) || OfficeReportText.Text.Length > 512;
        OfficeBook.Disabled = !office.Officer || !OfficeJailConfirm.Pressed || state.Target == null || state.Printing
            || !int.TryParse(OfficeJailMinutes.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes)
            || minutes is < 1 or > 120 || string.IsNullOrWhiteSpace(OfficeJailReason.Text) || OfficeJailReason.Text.Length > 512;
        OfficeRelease.Disabled = !office.Officer || !OfficeJailConfirm.Pressed || _bookingId == 0 || string.IsNullOrWhiteSpace(OfficeJailReason.Text) || OfficeJailReason.Text.Length > 512;
        foreach (var button in new[] { OfficeServicesApp, OfficeRequestsApp, OfficeJailApp, OfficeRefresh,
                     OfficeReportSend, OfficePaper, OfficeApprove, OfficeReject, OfficeReprint, OfficeBook, OfficeRelease })
            StyleButton(button);
    }

    private void SendOffice(CMUEmploymentOfficeAction action, string text = "")
    {
        if (_state is not { } state)
            return;
        int.TryParse(OfficeJailMinutes.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes);
        OnOffice?.Invoke(new(action, state.Revision,
            action == CMUEmploymentOfficeAction.Release ? _bookingId : _requestId,
            text, minutes, action is CMUEmploymentOfficeAction.Book or CMUEmploymentOfficeAction.Release
                ? OfficeJailConfirm.Pressed : OfficeDecisionConfirm.Pressed));
        OfficeDecisionConfirm.Pressed = false;
        OfficeJailConfirm.Pressed = false;
    }

    private void TickOfficeClocks(float delta)
    {
        _officeElapsed += delta;
        if (!OfficeJailFrame.Visible || (int) _officeElapsed == _jailSecond)
            return;
        _jailSecond = (int) _officeElapsed;
        foreach (var (entry, label) in _jailClocks)
        {
            var remaining = Math.Max(0, entry.Due - _officeTime - _officeElapsed);
            var status = Loc.GetString(entry.Released ? "cmu-employment-jail-released"
                : remaining <= 0 ? "cmu-employment-jail-served" : "cmu-employment-jail-time",
                ("time", TimeSpan.FromSeconds(remaining).ToString(@"hh\:mm\:ss")));
            label.Text = entry.Name + " / " + status;
            label.ToolTip = entry.Charges + " / " + entry.Officer;
            label.FontColorOverride = !entry.Released && remaining <= 0 ? WarningText : TextPrimary;
        }
    }
}
