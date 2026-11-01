using Content.Shared.Access;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.CMU14.Employment;

[RegisterComponent]
public sealed partial class CMUEmploymentOfficeComponent : Component
{
    [DataField]
    public ProtoId<DepartmentPrototype>? Department;

    [DataField]
    public bool PublicDesk;

    [DataField]
    public bool SecurityBranding;

    [DataField]
    public bool Jail;

    [DataField]
    public bool RequiresApproval = true;

    [DataField]
    public string AdministratorFax = "Colony Administrator";

    [DataField]
    public string ApprovalStamp = "stamp-component-stamped-name-au14-admin";

    public TimeSpan SubmitReadyAt;
}
