using Content.Shared.Access;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.Employment;

[Serializable, NetSerializable]
public enum CMUEmploymentUiKey : byte { Key }

[Serializable, NetSerializable]
public enum CMUEmploymentTheme : byte { Govfor, Colony, Weyland }

[Serializable, NetSerializable]
public enum CMUEmploymentAction : byte { Hire, Edit, Terminate }

[Serializable, NetSerializable]
public enum CMUEmploymentVisuals : byte { State, Sprite }

[Serializable, NetSerializable]
public enum CMUEmploymentMachineState : byte { Off, Ready, Printing }

[Serializable, NetSerializable]
public sealed class CMUEmploymentSlotMessage(bool operatorSlot) : BoundUserInterfaceMessage
{
    public readonly bool OperatorSlot = operatorSlot;
}

[Serializable, NetSerializable]
public sealed class CMUEmploymentRefreshMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class CMUEmploymentApplyMessage(
    NetEntity card, int revision, CMUEmploymentAction action, string title, string blankName,
    List<ProtoId<AccessLevelPrototype>> access, bool iff, bool confirmed,
    ProtoId<JobIconPrototype>? jobIcon = null) : BoundUserInterfaceMessage
{
    public readonly NetEntity Card = card;
    public readonly int Revision = revision;
    public readonly CMUEmploymentAction Action = action;
    public readonly string Title = title;
    public readonly string BlankName = blankName;
    public readonly List<ProtoId<AccessLevelPrototype>> Access = access;
    public readonly bool Iff = iff;
    public readonly bool Confirmed = confirmed;
    public readonly ProtoId<JobIconPrototype>? JobIcon = jobIcon;
}

[Serializable, NetSerializable]
public sealed class CMUEmploymentIconOption(ProtoId<JobIconPrototype> id, string name)
{
    public readonly ProtoId<JobIconPrototype> Id = id;
    public readonly string Name = name;
}

[Serializable, NetSerializable]
public sealed class CMUEmploymentAccessOption(ProtoId<AccessLevelPrototype> id, string name, bool restricted)
{
    public readonly ProtoId<AccessLevelPrototype> Id = id;
    public readonly string Name = name;
    public readonly bool Restricted = restricted;
}

[Serializable, NetSerializable]
public sealed class CMUEmploymentPresetOption(string name, string title, List<ProtoId<AccessLevelPrototype>> access,
    ProtoId<JobIconPrototype>? jobIcon = null)
{
    public readonly string Name = name;
    public readonly string Title = title;
    public readonly List<ProtoId<AccessLevelPrototype>> Access = access;
    public readonly ProtoId<JobIconPrototype>? JobIcon = jobIcon;
}

[Serializable, NetSerializable]
public sealed class CMUEmploymentReceipt(int revision, NetEntity card, CMUEmploymentAction action, string name, bool printed = false, bool pending = false)
{
    public readonly bool Printed = printed;
    public readonly bool Pending = pending;
    public readonly int Revision = revision;
    public readonly NetEntity Card = card;
    public readonly CMUEmploymentAction Action = action;
    public readonly string Name = name;
}

[Serializable, NetSerializable]
public sealed class CMUEmploymentState : BoundUserInterfaceState
{
    public string Heading = string.Empty;
    public CMUEmploymentTheme Theme;
    public string Brand = string.Empty;
    public string Logo = string.Empty;
    public CMUEmploymentColonyState? Colony;
    public bool CanBroadcast;
    public bool SirenActive;
    public CMUEmploymentOfficeState? Office;
    public Color? Accent;
    public Color? Background;
    public Color? Surface;
    public bool Powered;
    public bool Printing;
    public bool Authenticated;
    public NetEntity? Operator;
    public string OperatorName = string.Empty;
    public bool OperatorCardInserted;
    public string OperatorCardName = string.Empty;
    public NetEntity? Target;
    public string TargetName = string.Empty;
    public string JobTitle = string.Empty;
    public ProtoId<JobIconPrototype> JobIcon = "JobIconUnknown";
    public ProtoId<JobIconPrototype>? DefaultJobIcon;
    public List<CMUEmploymentIconOption> JobIcons = new();
    public bool Blank;
    public bool LocalEmployee;
    public bool HasIff;
    public int Revision;
    public CMUEmploymentReceipt? Receipt;
    public List<ProtoId<AccessLevelPrototype>> CurrentAccess = new();
    public List<CMUEmploymentAccessOption> Options = new();
    public List<CMUEmploymentPresetOption> Presets = new();
    public List<string> History = new();
}
