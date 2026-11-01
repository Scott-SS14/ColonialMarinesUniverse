using System.Linq;
using Content.Shared.Access.Components;
using Content.Shared.CMU14.Employment;
using Content.Shared.Database;
using Robust.Shared.Utility;

namespace Content.Server.CMU14.Employment;

public sealed partial class CMUEmploymentConsoleSystem
{
    private readonly Dictionary<int, JailBooking> _officeJail = new();
    private int _nextJailBooking;

    private sealed class JailBooking
    {
        public int Id;
        public EntityUid Scope;
        public EntityUid Card;
        public string Name = string.Empty;
        public string Charges = string.Empty;
        public string Officer = string.Empty;
        public TimeSpan Due;
        public bool Released;
    }

    private bool OfficeOfficer(Entity<CMUEmploymentConsoleComponent> ent)
        => (PublicOfficeSession(ent) || Authorized(ent)) && Card(ent.Owner, true) is { } card
           && TryComp<AccessComponent>(card, out var access) && access.Enabled
           && (_access.TryGetTags(card)?.Contains("CMAccessColonyBrig") ?? false);

    private void HandleJail(Entity<CMUEmploymentConsoleComponent> ent, EntityUid actor, CMUEmploymentOfficeMessage request)
    {
        if (!OfficeOfficer(ent) || !request.Confirmed || request.Text == null || !ValidText(request.Text, 512))
        {
            Reject(ent, actor, "cmu-employment-denied");
            return;
        }
        var scope = OfficeScope(ent.Owner);
        if (request.Action == CMUEmploymentOfficeAction.Book)
        {
            if (request.Minutes is < 1 or > 120 || Card(ent.Owner, false) is not { } card
                || !TryComp<IdCardComponent>(card, out var id) || string.IsNullOrWhiteSpace(id.FullName))
            {
                Reject(ent, actor, "cmu-employment-jail-invalid");
                return;
            }
            if (_officeJail.Values.Any(entry => entry.Scope == scope && entry.Card == card && !entry.Released)
                || _officeJail.Values.Count(entry => !entry.Released) >= 80)
            {
                Reject(ent, actor, "cmu-employment-office-full");
                return;
            }
            if (HasComp<CMUEmploymentPrintJobComponent>(ent.Owner))
            {
                Reject(ent, actor, "cmu-employment-printer-busy");
                return;
            }
            foreach (var old in _officeJail.Values.Where(entry => entry.Released).OrderBy(entry => entry.Id)
                         .Take(Math.Max(0, _officeJail.Count - 159)).ToList())
                _officeJail.Remove(old.Id);
            var booking = new JailBooking
            {
                Id = ++_nextJailBooking, Scope = scope, Card = card, Name = id.FullName,
                Charges = request.Text, Officer = Name(actor),
                Due = _timing.CurTime + TimeSpan.FromMinutes(request.Minutes),
            };
            _officeJail.Add(booking.Id, booking);
            var document = Loc.GetString("cmu-employment-jail-document", ("name", FormattedMessage.EscapeText(booking.Name)),
                ("charges", FormattedMessage.EscapeText(booking.Charges)), ("officer", FormattedMessage.EscapeText(booking.Officer)),
                ("minutes", request.Minutes), ("due", booking.Due.ToString(@"hh\:mm\:ss")));
            PrintOfficeDocument(ent, actor, Loc.GetString("cmu-employment-jail-booking-paper"), document);
            _adminLog.Add(LogType.RMCIdModify, LogImpact.High,
                $"{ToPrettyString(actor):player} booked {booking.Name} for {request.Minutes} minutes: {booking.Charges}");
        }
        else if (_officeJail.TryGetValue(request.Entry, out var booking) && booking.Scope == scope && !booking.Released)
        {
            booking.Released = true;
            _adminLog.Add(LogType.RMCIdModify, LogImpact.High,
                $"{ToPrettyString(actor):player} recorded release of {booking.Name}: {request.Text}");
        }
        else
            return;
        ent.Comp.Revision++;
        RefreshOffices();
    }
}
