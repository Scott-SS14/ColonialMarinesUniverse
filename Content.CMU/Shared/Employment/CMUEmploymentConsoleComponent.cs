using Content.Shared.Access;
using Content.Shared.CMU14.util;
using Content.Shared.Paper;
using Content.Shared.StatusIcon;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.CMU14.Employment;

[RegisterComponent]
public sealed partial class CMUEmploymentConsoleComponent : Component
{
    public const string OperatorSlot = "CMUEmploymentOperator";
    public const string TargetSlot = "CMUEmploymentTarget";

    [DataField(required: true)]
    public ProtoId<AccessLevelPrototype> RequiredAccess;

    [DataField(required: true)]
    public EntProtoId<IFFFactionComponent> Faction;

    [DataField(required: true)]
    public LocId Heading;

    [DataField]
    public CMUEmploymentTheme Theme;

    [DataField(required: true)]
    public ResPath Logo;

    [DataField]
    public ProtoId<AccessLevelPrototype>? CommunicationsAccess;

    public CMUEmploymentColonyState? ColonySnapshot;
    public int ColonySnapshotRevision;
    public TimeSpan ColonyRefreshAt;
    public TimeSpan ColonyCommsReadyAt;
    public TimeSpan ColonySirenReadyAt;
    public TimeSpan ColonyPayrollReadyAt;

    [DataField]
    public List<ProtoId<AccessLevelPrototype>> AllowedAccess = new();

    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> RestrictedAccess = new();

    [DataField]
    public Dictionary<ProtoId<AccessLevelPrototype>, LocId> AccessNames = new();

    [DataField]
    public Dictionary<ProtoId<JobIconPrototype>, LocId> JobIcons = new();

    [DataField]
    public ProtoId<JobIconPrototype>? DefaultJobIcon;

    [DataField]
    public List<CMUEmploymentPreset> Presets = new();

    [DataField]
    public int HistoryLimit = 30;

    [DataField(required: true)]
    public EntProtoId<PaperComponent> FormPrototype;

    [DataField]
    public Dictionary<ProtoId<PlatoonPrototype>, CMUEmploymentGovforProfile> GovforProfiles = new();

    [DataField]
    public float PrintDuration = 2f;

    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/diagnoser_printing.ogg");

    public EntityUid? Operator;
    public int Revision;
    public CMUEmploymentReceipt? LastReceipt;
    public List<string> History = new();
}

[DataDefinition]
public sealed partial class CMUEmploymentGovforProfile
{
    [DataField(required: true)]
    public LocId Brand;

    [DataField(required: true)]
    public ResPath Sprite;

    [DataField(required: true)]
    public EntProtoId<PaperComponent> Paper;

    [DataField(required: true)]
    public Color Accent;

    [DataField(required: true)]
    public Color Background;

    [DataField(required: true)]
    public Color Surface;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CMUEmploymentVisualComponent : Component;

[DataDefinition]
public sealed partial class CMUEmploymentPreset
{
    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId JobTitle;

    [DataField]
    public List<ProtoId<AccessLevelPrototype>> Access = new();

    [DataField]
    public ProtoId<JobIconPrototype>? JobIcon;
}

[RegisterComponent]
public sealed partial class CMUEmploymentCardComponent : Component
{
    [DataField]
    public string Employer = string.Empty;

}
