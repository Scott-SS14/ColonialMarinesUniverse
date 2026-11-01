using Robust.Shared.Serialization;

namespace Content.Shared.CMU14.Employment;

[Serializable, NetSerializable]
public enum CMUEmploymentOfficeAction : byte { Refresh, Paper, Approve, Reject, Reprint, Report, Book, Release }

[Serializable, NetSerializable]
public sealed class CMUEmploymentOfficeMessage(CMUEmploymentOfficeAction action, int revision,
    int entry = 0, string text = "", int minutes = 0, bool confirmed = false) : BoundUserInterfaceMessage
{
    public readonly CMUEmploymentOfficeAction Action = action;
    public readonly int Revision = revision;
    public readonly int Entry = entry;
    public readonly string Text = text;
    public readonly int Minutes = minutes;
    public readonly bool Confirmed = confirmed;
}

[Serializable, NetSerializable]
public sealed class CMUEmploymentRequestEntry(int id, string reference, string subject, string description, string status, bool pending)
{
    public readonly int Id = id;
    public readonly string Reference = reference;
    public readonly string Subject = subject;
    public readonly string Description = description;
    public readonly string Status = status;
    public readonly bool Pending = pending;
}

[Serializable, NetSerializable]
public sealed class CMUEmploymentJailEntry(int id, string name, string charges, string officer, double due, bool released)
{
    public readonly int Id = id;
    public readonly string Name = name;
    public readonly string Charges = charges;
    public readonly string Officer = officer;
    public readonly double Due = due;
    public readonly bool Released = released;
}

[Serializable, NetSerializable]
public sealed class CMUEmploymentOfficeState
{
    public bool Enabled;
    public NetEntity? ReaderCard;
    public NetEntity Scope;
    public bool PublicDesk;
    public bool JailOnly;
    public int Version;
    public bool PublicSession;
    public bool Administrator;
    public bool Leader;
    public bool Officer;
    public bool PaperInserted;
    public bool RequestsApproval;
    public double Time;
    public string Department = string.Empty;
    public string SecurityForce = string.Empty;
    public List<CMUEmploymentRequestEntry> Requests = new();
    public List<CMUEmploymentJailEntry> Jail = new();
}
