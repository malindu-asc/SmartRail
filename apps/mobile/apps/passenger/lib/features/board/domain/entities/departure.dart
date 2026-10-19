/// What a board row says about a train, as one thing, in the order that matters to a passenger.
enum DepartureState {
  cancelled,
  delayed,
  platformChanged,
  noLiveData,
  onTime,
  scheduled,
}

/// One train leaving a station. Times are UTC instants; the screen shows them in the network time zone.
class Departure {
  const Departure({
    required this.journeyId,
    required this.trainNumber,
    required this.routeCode,
    required this.destinationCode,
    required this.destinationName,
    required this.scheduled,
    required this.expected,
    required this.estimateSource,
    required this.platformChanged,
    required this.status,
    required this.confidence,
    required this.isStale,
    this.delayMinutes,
    this.punctuality,
    this.platform,
    this.ageSeconds,
  });

  final String journeyId;
  final String trainNumber;
  final String routeCode;
  final String destinationCode;
  final String destinationName;

  /// The timetable time. It never changes.
  final DateTime scheduled;

  /// What passengers are told: the actual time, the live estimate, or the scheduled time. Never earlier than [scheduled].
  final DateTime expected;

  /// Confirmed, Live or Scheduled: what [expected] rests on.
  final String estimateSource;

  final int? delayMinutes;
  final String? punctuality;
  final String? platform;
  final bool platformChanged;

  /// Public status: Planned, Running, Cancelled, Completed, ...
  final String status;

  /// Scheduled, Live, Confirmed or Unknown.
  final String confidence;

  final bool isStale;
  final int? ageSeconds;

  bool get isCancelled => status == 'Cancelled';

  /// Running, but the feed has been silent for too long: only the timetable is known.
  bool get hasNoLiveData => !isCancelled && confidence == 'Unknown';

  bool get isDelayed => !isCancelled && (delayMinutes ?? 0) >= 2;

  /// The live time differs from the timetable, so both are shown.
  bool get expectedDiffers =>
      !isCancelled && !hasNoLiveData && expected != scheduled;

  DepartureState get state {
    if (isCancelled) return DepartureState.cancelled;
    if (isDelayed) return DepartureState.delayed;
    if (platformChanged) return DepartureState.platformChanged;
    if (hasNoLiveData) return DepartureState.noLiveData;
    if (confidence == 'Live' || confidence == 'Confirmed') {
      return DepartureState.onTime;
    }
    return DepartureState.scheduled;
  }
}

/// A board as it was fetched. [fromCache] is true when the phone could not reach the platform and this is the last good copy.
class BoardSnapshot {
  const BoardSnapshot({
    required this.stationCode,
    required this.stationName,
    required this.timeZone,
    required this.asOf,
    required this.fetchedAt,
    required this.items,
    this.fromCache = false,
  });

  final String stationCode;
  final String stationName;
  final String timeZone;

  /// The server's time for this board.
  final DateTime asOf;

  /// When the phone last got it (or had it confirmed). The age of the data is measured from here.
  final DateTime fetchedAt;

  final List<Departure> items;
  final bool fromCache;
}
