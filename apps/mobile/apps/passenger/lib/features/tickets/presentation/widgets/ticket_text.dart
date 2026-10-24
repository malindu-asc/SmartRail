import 'package:smartrail_core/smartrail_core.dart';

import '../../../../l10n/app_localizations.dart';
import '../../domain/tickets_domain.dart';

/// "Today 15:20" or "20 Oct 15:20", in the railway's time zone.
String ticketWhen(
  AppLocalizations l10n,
  DateTime departure,
  NetworkClock clock,
  DateTime now,
) {
  final time = clock.formatTime(departure);
  if (clock.formatDate(departure) == clock.formatDate(now)) {
    return l10n.ticketsWhen(time);
  }
  return l10n.ticketsWhenDate(clock.formatDate(departure), time);
}

/// "Coach B · Seat 13" or "Coach B · Seats 13, 14" (several coaches: just the seats). Empty for a train without seats.
String seatsText(AppLocalizations l10n, List<String> seats) {
  if (seats.isEmpty) return '';
  final coaches = seats.map((s) => s[0]).toSet();
  final numbers = seats.map((s) => s.substring(1)).join(', ');
  if (coaches.length > 1) return seats.join(', ');
  final coach = coaches.single;
  return seats.length == 1
      ? l10n.ticketsCoachSeat(coach, numbers)
      : l10n.ticketsCoachSeats(coach, numbers);
}

String ticketWhenAndSeat(
  AppLocalizations l10n,
  TicketSummary ticket,
  NetworkClock clock,
  DateTime now,
) {
  final when = ticketWhen(l10n, ticket.departure, clock, now);
  final seats = seatsText(l10n, ticket.seats);
  return seats.isEmpty ? when : '$when · $seats';
}

/// "1 adult, 2 children".
String travellersText(AppLocalizations l10n, Travellers t) => [
  if (t.adults > 0) l10n.travellersAdults(t.adults),
  if (t.children > 0) l10n.travellersChildren(t.children),
  if (t.seniors > 0) l10n.travellersSeniors(t.seniors),
].join(', ');
