using Content.Shared.CrewManifest;
using Content.Shared.CMU14.ColonyEconomy;
using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.Employment;

[Serializable, NetSerializable]
public enum CMUEmploymentColonyAction : byte { Refresh, Announce, Siren }

[Serializable, NetSerializable]
public sealed class CMUEmploymentColonyMessage(CMUEmploymentColonyAction action, int revision, string text = "")
    : BoundUserInterfaceMessage
{
    public readonly CMUEmploymentColonyAction Action = action;
    public readonly int Revision = revision;
    public readonly string Text = text;
}

[Serializable, NetSerializable]
public sealed class CMUEmploymentDepartment(string key, NetEntity console, string name, float budget, int salary, List<DepartmentEmployeeEntry> members)
{
    public readonly string Key = key;
    public readonly NetEntity Console = console;
    public readonly float Budget = budget;
    public readonly int DefaultSalary = salary;
    public readonly string Name = name;
    public readonly List<DepartmentEmployeeEntry> Members = members;
}

[Serializable, NetSerializable]
public sealed class CMUEmploymentColonyState(int version, string station, CrewManifestEntries? manifest,
    List<CMUEmploymentDepartment> departments, string updated)
{
    public readonly int Version = version;
    public readonly string Station = station;
    public readonly CrewManifestEntries? Manifest = manifest;
    public readonly List<CMUEmploymentDepartment> Departments = departments;
    public readonly string Updated = updated;
}

public sealed class CMUEmploymentSirenChangedEvent : EntityEventArgs;

[Serializable, NetSerializable]
public enum CMUEmploymentPayrollAction : byte { Transfer, DefaultSalary, IndividualSalary, ClearOverride, PayEmployee, PayDepartment, PayColony, FundDepartment }

[Serializable, NetSerializable]
public sealed class CMUEmploymentPayrollMessage(CMUEmploymentPayrollAction action, int revision, int snapshot,
    NetEntity department, NetEntity? employee, int amount, bool confirmed) : BoundUserInterfaceMessage
{
    public readonly CMUEmploymentPayrollAction Action = action;
    public readonly int Revision = revision;
    public readonly int Snapshot = snapshot;
    public readonly NetEntity Department = department;
    public readonly NetEntity? Employee = employee;
    public readonly int Amount = amount;
    public readonly bool Confirmed = confirmed;
}
