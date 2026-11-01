using Content.Shared.Paper;
using Robust.Shared.Prototypes;

namespace Content.Server.CMU14.Employment;

[RegisterComponent]
public sealed partial class CMUEmploymentPrintJobComponent : Component
{
    public bool UpdatesReceipt = true;
    public EntProtoId<PaperComponent> Prototype;
    public string Content = string.Empty;
    public string DocumentName = string.Empty;
    public string Description = string.Empty;
    public EntityUid Issuer;
    public float Remaining;
}
