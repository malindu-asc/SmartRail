import 'package:fpdart/fpdart.dart';
import 'package:smartrail_core/smartrail_core.dart';

/// Who is travelling, and whether help is needed at the station.
class Travellers {
  const Travellers({
    this.adults = 1,
    this.children = 0,
    this.seniors = 0,
    this.needsHelp = false,
  });

  final int adults;
  final int children;
  final int seniors;
  final bool needsHelp;

  int get total => adults + children + seniors;

  Travellers copyWith({
    int? adults,
    int? children,
    int? seniors,
    bool? needsHelp,
  }) => Travellers(
    adults: adults ?? this.adults,
    children: children ?? this.children,
    seniors: seniors ?? this.seniors,
    needsHelp: needsHelp ?? this.needsHelp,
  );
}

class FareLine {
  const FareLine({
    required this.category,
    required this.count,
    required this.each,
    required this.total,
  });

  final String category;
  final int count;
  final num each;
  final num total;
}

class Fare {
  const Fare({
    required this.currency,
    required this.lines,
    required this.total,
  });

  final String currency;
  final List<FareLine> lines;
  final num total;

  /// "LKR 320". The fare is shown for information; nothing is charged in this version.
  String get label => '$currency ${total.round()}';
}

/// One train that can be booked between the two stations asked for.
class TicketOffer {
  const TicketOffer({
    required this.journeyId,
    required this.trainNumber,
    required this.fromCode,
    required this.toCode,
    required this.departure,
    required this.expectedDeparture,
    required this.arrival,
    required this.expectedArrival,
    required this.durationMinutes,
    required this.platform,
    required this.status,
    required this.confidence,
    required this.delayMinutes,
    required this.seatRequired,
    required this.fare,
  });

  final String journeyId;
  final String trainNumber;
  final String fromCode;
  final String toCode;
  final DateTime departure;
  final DateTime expectedDeparture;
  final DateTime arrival;
  final DateTime expectedArrival;
  final int durationMinutes;
  final String? platform;
  final String status;
  final String confidence;
  final int? delayMinutes;
  final bool seatRequired;
  final Fare fare;
}

class OffersResult {
  const OffersResult({
    required this.timeZone,
    required this.fromName,
    required this.toName,
    required this.offers,
  });

  final String timeZone;
  final String fromName;
  final String toName;
  final List<TicketOffer> offers;
}

/// What is being asked for: the stations, the earliest time and who travels.
class OfferQuery {
  const OfferQuery({
    required this.from,
    required this.to,
    this.after,
    this.travellers = const Travellers(),
  });

  final String from;
  final String to;
  final DateTime? after;
  final Travellers travellers;

  @override
  bool operator ==(Object other) =>
      other is OfferQuery &&
      other.from == from &&
      other.to == to &&
      other.after == after &&
      other.travellers.adults == travellers.adults &&
      other.travellers.children == travellers.children &&
      other.travellers.seniors == travellers.seniors;

  @override
  int get hashCode => Object.hash(
    from,
    to,
    after,
    travellers.adults,
    travellers.children,
    travellers.seniors,
  );
}

class Seat {
  const Seat(this.coach, this.number);

  final String coach;
  final int number;

  @override
  bool operator ==(Object other) =>
      other is Seat && other.coach == coach && other.number == number;

  @override
  int get hashCode => Object.hash(coach, number);

  @override
  String toString() => '$coach$number';
}

class SeatMap {
  const SeatMap({
    required this.coaches,
    required this.seatsPerCoach,
    required this.taken,
    required this.yours,
  });

  final List<String> coaches;
  final int seatsPerCoach;
  final Set<Seat> taken;
  final Set<Seat> yours;
}

class SeatHold {
  const SeatHold({required this.id, required this.expiresAt});

  final String id;
  final DateTime expiresAt;
}

enum TicketState { valid, affected, past }

class TicketSummary {
  const TicketSummary({
    required this.id,
    required this.reference,
    required this.fromCode,
    required this.fromName,
    required this.toCode,
    required this.toName,
    required this.departure,
    required this.arrival,
    required this.timeZone,
    required this.state,
    required this.passengers,
    required this.seats,
  });

  final String id;
  final String reference;
  final String fromCode;
  final String fromName;
  final String toCode;
  final String toName;
  final DateTime departure;
  final DateTime arrival;
  final String timeZone;
  final TicketState state;
  final int passengers;

  /// For example `B13`. Empty for a train without seat reservation.
  final List<String> seats;
}

class TicketDetail {
  const TicketDetail({
    required this.summary,
    required this.bookingCode,
    required this.trainNumber,
    required this.journeyId,
    required this.travellers,
    required this.currency,
    required this.fareTotal,
    required this.qrPayload,
    required this.fetchedAt,
    this.fromCache = false,
  });

  final TicketSummary summary;
  final String bookingCode;
  final String trainNumber;
  final String journeyId;
  final Travellers travellers;
  final String currency;
  final num fareTotal;
  final String qrPayload;
  final DateTime fetchedAt;
  final bool fromCache;
}

abstract interface class TicketsRepository {
  Future<Either<Failure, OffersResult>> offers(OfferQuery query);

  Future<Either<Failure, SeatMap>> seatMap({
    required String journeyId,
    required String from,
    required String to,
  });

  Future<Either<Failure, SeatHold>> holdSeats({
    required String journeyId,
    required String from,
    required String to,
    required List<Seat> seats,
  });

  /// Confirms the booking. No payment is taken in this version.
  Future<Either<Failure, TicketDetail>> book({
    required String journeyId,
    required String from,
    required String to,
    required Travellers travellers,
    String? holdId,
  });

  Future<Either<Failure, List<TicketSummary>>> list({required bool past});

  /// The ticket; when the platform cannot be reached, the last copy kept on this phone (marked `fromCache`).
  Future<Either<Failure, TicketDetail>> detail(String id);

  Future<Either<Failure, TicketDetail>> claim({
    required String bookingCode,
    required String contact,
  });
}
