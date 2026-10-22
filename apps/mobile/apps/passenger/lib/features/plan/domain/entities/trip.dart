/// What a passenger asks: where from, where to, and the earliest time. A null [after] means now.
class TripQuery {
  const TripQuery({required this.from, required this.to, this.after});

  final String from;
  final String to;
  final DateTime? after;

  @override
  bool operator ==(Object other) =>
      other is TripQuery &&
      other.from == from &&
      other.to == to &&
      other.after == after;

  @override
  int get hashCode => Object.hash(from, to, after);
}

/// One train ridden from [fromName] to [toName]. [departure] and [arrival] are the timetable; [expectedDeparture] and
/// [expectedArrival] are what the live data says (the same as the timetable when there is none).
class TripLeg {
  const TripLeg({
    required this.journeyId,
    required this.trainNumber,
    required this.fromCode,
    required this.fromName,
    required this.toCode,
    required this.toName,
    required this.departure,
    required this.expectedDeparture,
    required this.arrival,
    required this.expectedArrival,
    required this.platform,
    required this.status,
    required this.confidence,
    required this.delayMinutes,
  });

  final String journeyId;
  final String trainNumber;
  final String fromCode;
  final String fromName;
  final String toCode;
  final String toName;
  final DateTime departure;
  final DateTime expectedDeparture;
  final DateTime arrival;
  final DateTime expectedArrival;
  final String? platform;
  final String status;
  final String confidence;
  final int? delayMinutes;

  bool get isLate =>
      expectedArrival.isAfter(arrival) || (delayMinutes ?? 0) >= 2;

  bool get hasNoLiveData => confidence == 'Unknown';
}

/// Where the passenger changes trains, and whether the change is safe (REQ-PAX-006).
class TripTransfer {
  const TripTransfer({
    required this.stationCode,
    required this.stationName,
    required this.waitMinutes,
    required this.atRisk,
  });

  final String stationCode;
  final String stationName;
  final int waitMinutes;
  final bool atRisk;
}

class TripOption {
  const TripOption({
    required this.departure,
    required this.expectedDeparture,
    required this.arrival,
    required this.expectedArrival,
    required this.durationMinutes,
    required this.legs,
    required this.transfer,
  });

  final DateTime departure;
  final DateTime expectedDeparture;
  final DateTime arrival;
  final DateTime expectedArrival;
  final int durationMinutes;
  final List<TripLeg> legs;
  final TripTransfer? transfer;

  int get transfers => legs.length - 1;

  bool get isDirect => legs.length == 1;

  bool get isLate => legs.any((l) => l.isLate);
}

class TripResult {
  const TripResult({
    required this.timeZone,
    required this.fromName,
    required this.toName,
    required this.options,
    required this.fetchedAt,
    this.fromCache = false,
  });

  final String timeZone;
  final String fromName;
  final String toName;
  final List<TripOption> options;
  final DateTime fetchedAt;
  final bool fromCache;
}
