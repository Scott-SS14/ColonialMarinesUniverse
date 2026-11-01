using Content.Server.Station.Systems;
using Content.Shared.Access;
using Content.Shared._RMC14.Synth;
using Robust.Shared.Prototypes;
using Content.Shared.Access.Components;
using Content.Shared.CMU14.ColonyEconomy;
using Content.Shared.CMU14.Employment;

namespace Content.Server.CMU14.ColonyEconomy;

public sealed partial class DepartmentConsoleSystem
{
    [Dependency] private StationSystem _employmentStations = default!;

    public List<CMUEmploymentDepartment> GetEmploymentDepartments(EntityUid terminal)
    {
        var station = _employmentStations.GetOwningStation(terminal);
        var map = Transform(terminal).MapUid;
        var office = CompOrNull<CMUEmploymentOfficeComponent>(terminal);
        var result = new List<CMUEmploymentDepartment>();
        var seen = new HashSet<string>();
        var query = EntityQueryEnumerator<DepartmentConsoleComponent>();
        while (query.MoveNext(out var uid, out var department))
        {
            if (office?.Department is { } own && department.DepartmentId != own)
                continue;
            if (!department.ColonyManaged)
                continue;
            if (station != null ? _employmentStations.GetOwningStation(uid) != station : Transform(uid).MapUid != map)
                continue;
            var key = department.DepartmentId?.Id ?? GetNetEntity(uid).ToString();
            if (!seen.Add(key))
                continue;
            var members = new List<DepartmentEmployeeEntry>();
            foreach (var card in department.Members)
            {
                if (!TryComp<IdCardComponent>(card, out var id))
                    continue;
                var overridden = department.SalaryOverrides.TryGetValue(card, out var salary);
                members.Add(new(GetNetEntity(card), id.FullName ?? Loc.GetString("department-console-unknown"),
                    id.LocalizedJobTitle ?? Loc.GetString("department-console-unknown"),
                    overridden ? salary : department.DefaultSalary, overridden));
            }
            members.Sort((a, b) => StringComparer.CurrentCulture.Compare(a.Name, b.Name));
            result.Add(new(key, GetNetEntity(uid), department.DepartmentName, department.DepartmentBudget, department.DefaultSalary, members));
        }
        result.Sort((a, b) => StringComparer.CurrentCulture.Compare(a.Name, b.Name));
        return result;
    }

    public bool ManageEmploymentDepartment(EntityUid terminal, EntityUid departmentUid,
        EntityUid? employee, CMUEmploymentPayrollAction action, int amount)
    {
        if (amount < 0 || amount > 1_000_000)
            return false;
        var available = GetEmploymentDepartments(terminal);
        var selected = available.Find(entry => entry.Console == GetNetEntity(departmentUid));
        if (selected == null || !TryComp<DepartmentConsoleComponent>(departmentUid, out var department))
            return false;

        if (action == CMUEmploymentPayrollAction.PayColony)
        {
            var scope = new HashSet<EntityUid>();
            foreach (var entry in available)
            {
                var uid = GetEntity(entry.Console);
                if (!CanPayEmploymentDepartment(uid, null))
                    return false;
                scope.Add(uid);
            }
            return DispenseEmploymentPayroll(scope, null);
        }
        switch (action)
        {
            case CMUEmploymentPayrollAction.DefaultSalary:
                if (department.DefaultSalary == amount)
                    return false;
                OnSetDefaultSalary(departmentUid, department, new DepartmentConsoleSetDefaultSalaryBuiMsg(amount));
                return true;
            case CMUEmploymentPayrollAction.FundDepartment:
                return TransferToDepartment(departmentUid, amount);
            case CMUEmploymentPayrollAction.PayDepartment:
                return CanPayEmploymentDepartment(departmentUid, null)
                    && DispenseEmploymentPayroll(new HashSet<EntityUid> { departmentUid }, null);
        }
        if (employee is not { } card || !TryComp<IdCardComponent>(card, out _))
            return false;
        if (action == CMUEmploymentPayrollAction.Transfer)
        {
            var alreadyThere = department.Members.Contains(card);
            if (alreadyThere)
                return false;
            var tags = _accessSystem.TryGetTags(card);
            if (tags == null)
                return false;
            var next = new HashSet<ProtoId<AccessLevelPrototype>>(tags);
            var query = EntityQueryEnumerator<DepartmentConsoleComponent>();
            while (query.MoveNext(out _, out var previous))
            {
                if (previous.Members.Contains(card) && previous.DepartmentAccessLevel is { } oldAccess)
                    next.Remove(oldAccess);
            }
            if (department.DepartmentAccessLevel is { } newAccess)
                next.Add(newAccess);
            _accessSystem.TrySetTags(card, next);
            MoveEmploymentMember(departmentUid, department, card);
            return true;
        }
        if (!department.Members.Contains(card))
            return false;
        switch (action)
        {
            case CMUEmploymentPayrollAction.IndividualSalary:
                if (department.SalaryOverrides.TryGetValue(card, out var current) && current == amount)
                    return false;
                OnSetIndividualSalary(departmentUid, department, new DepartmentConsoleSetIndividualSalaryBuiMsg(GetNetEntity(card), amount));
                return true;
            case CMUEmploymentPayrollAction.ClearOverride:
                if (!department.SalaryOverrides.ContainsKey(card))
                    return false;
                OnRemoveOverride(departmentUid, department, new DepartmentConsoleRemoveOverrideBuiMsg(GetNetEntity(card)));
                return true;
            case CMUEmploymentPayrollAction.PayEmployee:
                return CanPayEmploymentDepartment(departmentUid, card)
                    && DispenseEmploymentPayroll(new HashSet<EntityUid> { departmentUid }, card);
            default:
                return false;
        }
    }

    private bool CanPayEmploymentDepartment(EntityUid uid, EntityUid? recipient)
    {
        if (!TryComp<DepartmentConsoleComponent>(uid, out var department) || !float.IsFinite(department.DepartmentBudget))
            return false;
        foreach (var card in department.Members)
        {
            if (recipient != null && card != recipient)
                continue;
            if (!TryComp<IdCardComponent>(card, out var id)
                || id.OriginalOwner != null && HasComp<SynthComponent>(id.OriginalOwner))
                continue;
            var salary = department.SalaryOverrides.TryGetValue(card, out var value) ? value : department.DefaultSalary;
            if (salary < 0 || salary > 1_000_000 || id.AccountBalance > int.MaxValue - salary)
                return false;
        }
        return true;
    }
    public void EnrollEmploymentMember(Entity<DepartmentConsoleComponent> department, EntityUid card)
    {
        if (!department.Comp.Members.Contains(card))
            MoveEmploymentMember(department.Owner, department.Comp, card);
    }

    public void RemoveEmploymentMember(EntityUid card)
    {
        var query = EntityQueryEnumerator<DepartmentConsoleComponent>();
        while (query.MoveNext(out var uid, out var department))
        {
            if (department.Members.Contains(card))
                OnFire(uid, department, new DepartmentConsoleFireBuiMsg(GetNetEntity(card)));
        }
    }
}
