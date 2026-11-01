using System;
using System.Collections.Generic;
using System.Globalization;
using Content.Shared.CMU14.Employment;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;

namespace Content.Client.CMU14.Employment;

public sealed partial class CMUEmploymentWindow
{
    public event Action<CMUEmploymentPayrollMessage>? OnPayroll;

    private readonly List<CMUEmploymentDepartment> _payrollDepartments = new();
    private CMUEmploymentDepartment? _payrollDepartment;
    private NetEntity? _payrollEmployee;
    private int _payrollVersion = -1;
    private bool _payrollPending;

    private void InitializePayroll()
    {
        PayrollApp.Text = Loc.GetString("cmu-employment-app-payroll");
        PayrollHeading.Text = Loc.GetString("cmu-employment-payroll-heading");
        PayrollHint.Text = Wrap(Loc.GetString("cmu-employment-payroll-hint"), 62);
        PayrollRefresh.Text = Loc.GetString("cmu-employment-refresh");
        DefaultSalaryCaption.Text = Loc.GetString("cmu-employment-payroll-default");
        IndividualSalaryCaption.Text = Loc.GetString("cmu-employment-payroll-individual");
        DepartmentFundingCaption.Text = Loc.GetString("cmu-employment-payroll-funding");
        SetDefaultSalary.Text = Loc.GetString("cmu-employment-payroll-set-default");
        SetIndividualSalary.Text = Loc.GetString("cmu-employment-payroll-set-individual");
        ClearSalaryOverride.Text = Loc.GetString("cmu-employment-payroll-clear");
        FundDepartment.Text = Loc.GetString("cmu-employment-payroll-fund");
        TransferEmployee.Text = Loc.GetString("cmu-employment-payroll-transfer");
        ConfirmPayroll.Text = Loc.GetString("cmu-employment-payroll-confirm");
        PayEmployee.Text = Loc.GetString("cmu-employment-payroll-pay-employee");
        PayDepartment.Text = Loc.GetString("cmu-employment-payroll-pay-department");
        PayColony.Text = Loc.GetString("cmu-employment-payroll-pay-colony");
        PayrollApp.OnPressed += _ => SelectColonyPage(3);
        PayrollRefresh.OnPressed += _ => RequestColonyRefresh();
        PayrollDepartment.OnItemSelected += args =>
        {
            if (args.Id < 0 || args.Id >= _payrollDepartments.Count)
                return;
            PayrollDepartment.SelectId(args.Id);
            _payrollDepartment = _payrollDepartments[args.Id];
            _payrollEmployee = null;
            PopulatePayrollEmployees();
        };
        PayrollEmployee.OnItemSelected += args =>
        {
            if (_payrollDepartment is not { } department || args.Id < 0 || args.Id >= department.Members.Count)
                return;
            PayrollEmployee.SelectId(args.Id);
            _payrollEmployee = department.Members[args.Id].IdCardUid;
            IndividualSalaryText.Text = department.Members[args.Id].Salary.ToString(CultureInfo.InvariantCulture);
            ConfirmPayroll.Pressed = false;
            UpdatePayrollControls();
        };
        foreach (var field in new[] { DefaultSalaryText, IndividualSalaryText, DepartmentFundingText })
            field.OnTextChanged += _ => { ConfirmPayroll.Pressed = false; UpdatePayrollControls(); };
        ConfirmPayroll.OnToggled += _ => UpdatePayrollControls();
        SetDefaultSalary.OnPressed += _ => SendPayroll(CMUEmploymentPayrollAction.DefaultSalary, DefaultSalaryText.Text);
        SetIndividualSalary.OnPressed += _ => SendPayroll(CMUEmploymentPayrollAction.IndividualSalary, IndividualSalaryText.Text);
        ClearSalaryOverride.OnPressed += _ => SendPayroll(CMUEmploymentPayrollAction.ClearOverride);
        FundDepartment.OnPressed += _ => SendPayroll(CMUEmploymentPayrollAction.FundDepartment, DepartmentFundingText.Text);
        TransferEmployee.OnPressed += _ => SendPayroll(CMUEmploymentPayrollAction.Transfer);
        PayEmployee.OnPressed += _ => SendPayroll(CMUEmploymentPayrollAction.PayEmployee);
        PayDepartment.OnPressed += _ => SendPayroll(CMUEmploymentPayrollAction.PayDepartment);
        PayColony.OnPressed += _ => SendPayroll(CMUEmploymentPayrollAction.PayColony);
        foreach (var button in new[] { PayrollApp, PayrollRefresh, SetDefaultSalary, SetIndividualSalary,
                     ClearSalaryOverride, FundDepartment, TransferEmployee, PayEmployee, PayDepartment, PayColony })
            BindButton(button);
    }

    private void UpdatePayroll()
    {
        if (_state is not { } state)
            return;
        PayrollFrame.Visible = state.Theme == CMUEmploymentTheme.Colony && _colonyPage == 3;
        if (!PayrollFrame.Visible)
            return;
        PayrollFrame.PanelOverride = Box(_surface, PanelEdge, 1);
        PayrollHeading.FontOverride = _displayFont;
        PayrollHint.FontColorOverride = TextSecondary;
        StyleButton(PayrollApp);
        if (state.Colony is not { } snapshot)
        {
            _payrollVersion = -1;
            _payrollDepartment = null;
            _payrollEmployee = null;
            _payrollDepartments.Clear();
            PayrollDepartment.Clear();
            PayrollEmployee.Clear();
        }
        else if (_payrollVersion != snapshot.Version)
        {
            _payrollVersion = snapshot.Version;
            var selected = _payrollDepartment?.Console;
            _payrollDepartments.Clear();
            _payrollDepartments.AddRange(snapshot.Departments);
            PayrollDepartment.Clear();
            var index = 0;
            for (var i = 0; i < _payrollDepartments.Count; i++)
            {
                PayrollDepartment.AddItem(_payrollDepartments[i].Name, i);
                if (_payrollDepartments[i].Console == selected)
                    index = i;
            }
            _payrollDepartment = _payrollDepartments.Count > 0 ? _payrollDepartments[index] : null;
            if (_payrollDepartment != null)
                PayrollDepartment.SelectId(index);
            PopulatePayrollEmployees();
        }
        UpdatePayrollControls();
    }

    private void PopulatePayrollEmployees()
    {
        PayrollEmployee.Clear();
        ConfirmPayroll.Pressed = false;
        if (_payrollDepartment is not { } department)
            return;
        DefaultSalaryText.Text = department.DefaultSalary.ToString(CultureInfo.InvariantCulture);
        var index = 0;
        for (var i = 0; i < department.Members.Count; i++)
        {
            var member = department.Members[i];
            PayrollEmployee.AddItem(Loc.GetString("cmu-employment-payroll-employee", ("name", member.Name),
                ("salary", member.Salary), ("mode", Loc.GetString(member.HasOverride ? "cmu-employment-payroll-override" : "cmu-employment-payroll-default-mode"))), i);
            if (member.IdCardUid == _payrollEmployee)
                index = i;
        }
        _payrollEmployee = department.Members.Count > 0 ? department.Members[index].IdCardUid : null;
        if (_payrollEmployee != null)
        {
            PayrollEmployee.SelectId(index);
            IndividualSalaryText.Text = department.Members[index].Salary.ToString(CultureInfo.InvariantCulture);
        }
        UpdatePayrollControls();
    }

    private static bool ValidSalary(string text, out int value)
        => int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) && value is >= 0 and <= 1_000_000;

    private void UpdatePayrollControls()
    {
        if (_state is not { } state)
            return;
        var active = state.Theme == CMUEmploymentTheme.Colony && state.Powered && state.Authenticated
            && !_payrollPending && _payrollDepartment != null;
        if (!active && ConfirmPayroll.Pressed)
            ConfirmPayroll.Pressed = false;
        foreach (var field in new[] { DefaultSalaryText, IndividualSalaryText, DepartmentFundingText })
            field.Editable = active;
        var confirmed = active && ConfirmPayroll.Pressed;
        ConfirmPayroll.Disabled = !active;
        PayrollDepartment.Disabled = !active;
        PayrollEmployee.Disabled = !active || _payrollEmployee == null;
        PayrollRefresh.Disabled = !state.Authenticated || _payrollPending;
        SetDefaultSalary.Disabled = !confirmed || !ValidSalary(DefaultSalaryText.Text, out _);
        SetIndividualSalary.Disabled = !confirmed || _payrollEmployee == null || !ValidSalary(IndividualSalaryText.Text, out _);
        ClearSalaryOverride.Disabled = !confirmed || _payrollEmployee == null;
        FundDepartment.Disabled = !confirmed || !ValidSalary(DepartmentFundingText.Text, out var funding) || funding <= 0;
        TransferEmployee.Disabled = !confirmed || state.Target == null;
        PayEmployee.Disabled = !confirmed || _payrollEmployee == null;
        PayDepartment.Disabled = !confirmed;
        PayColony.Disabled = !confirmed;
        PayrollBudget.Text = _payrollDepartment is { } department
            ? Loc.GetString("cmu-employment-payroll-budget", ("amount", department.Budget.ToString("F0", CultureInfo.InvariantCulture)))
            : Loc.GetString("cmu-employment-manifest-load");
        foreach (var button in new[] { PayrollRefresh, SetDefaultSalary, SetIndividualSalary, ClearSalaryOverride,
                     FundDepartment, TransferEmployee, PayEmployee, PayDepartment, PayColony })
            StyleButton(button);
    }

    private void SendPayroll(CMUEmploymentPayrollAction action, string? text = null)
    {
        if (_state is not { Colony: { } snapshot } state || _payrollDepartment is not { } department
            || _payrollPending || !state.Authenticated || !state.Powered || !ConfirmPayroll.Pressed)
            return;
        var amount = 0;
        if (text != null && !ValidSalary(text, out amount))
            return;
        _payrollPending = true;
        ConfirmPayroll.Pressed = false;
        UpdatePayrollControls();
        OnPayroll?.Invoke(new(action, state.Revision, snapshot.Version, department.Console,
            action == CMUEmploymentPayrollAction.Transfer ? state.Target : _payrollEmployee, amount, true));
    }
}
