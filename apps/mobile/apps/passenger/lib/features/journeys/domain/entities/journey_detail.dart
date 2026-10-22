/// What a journey is doing overall, in the order that matters (REQ-PAX-004: always shown with an icon and words, never colour alone).
enum JourneyHeadline {
  cancelled,
  noLiveData,
  delayed,
  runningOnTime,
  scheduled,
  completed,
}

enum StopWording {
  departedOnTime,
  departedLate,
  arrivedOnTime,
  arrivedLate,
  arrivedEarly,
  atPlatform,
  expectedOnTime,
  expected,
  estimate,
  scheduled,
  skipped,
}

class JourneyStop {
  const JourneyStop({
    required this.sequence,
    required this.stationCode,
    required this.stationName,
    required this.state,
    required this.estimateSource,
    required this.platformChanged,
    this.scheduledArrival,
    this.scheduledDeparture,
    this.estimatedArrival,
    this.estimatedDeparture,
    this.actualArrival,
    this.actualDeparture,
    this.platform,
  });

  final int sequence;
  final String stationCode;
  final String stationName;

  /// Pending, Arrived, Departed, Skipped or Cancelled.
  final String state;

  /// Confirmed (it happened), Live (an estimate) or Scheduled (no live basis).
  final String estimateSource;

  final DateTime? scheduledArrival;
  final DateTime? scheduledDeparture;
  final DateTime? estimatedArrival;
  final DateTime? estimatedDeparture;
  final DateTime? actualArrival;
  final DateTime? actualDeparture;
  final String? platform;
  final bool platformChanged;

  bool get hasHappened => state == 'Arrived' || state == 'Departed';

  /// The timetable time shown in the left column: leaving time, or arrival time at the last stop. Never changes.
  DateTime? get scheduledTime => scheduledDeparture ?? scheduledArrival;

  /// The time the train actually did, or is expected to do, the same event as [scheduledTime]. Null when nothing live is known.
  DateTime? get liveTime => scheduledDeparture != null
      ? (actualDeparture ?? estimatedDeparture)
      : (actualArrival ?? estimatedArrival);
}

class JourneyDetail {
  const JourneyDetail({
    required this.journeyId,
    required this.trainNumber,
    required this.routeCode,
    required this.serviceDate,
    required this.timeZone,
    required this.status,
    required this.confidence,
    required this.originCode,
    required this.originName,
    required this.destinationCode,
    required this.destinationName,
    required this.isStale,
    required this.stops,
    required this.fetchedAt,
    this.delayMinutes,
    this.punctuality,
    this.ageSeconds,
    this.asOf,
    this.fromCache = false,
  });

  final String journeyId;
  final String trainNumber;
  final String routeCode;
  final String serviceDate;
  final String timeZone;

  /// Planned (also for Ready), Running, Cancelled, Completed, ...
  final String status;

  /// Scheduled, Live, Confirmed or Unknown.
  final String confidence;

  final String originCode;
  final String originName;
  final String destinationCode;
  final String destinationName;
  final int? delayMinutes;
  final String? punctuality;
  final bool isStale;
  final int? ageSeconds;

  /// The server's time of the last live update; null when there has been none.
  final DateTime? asOf;

  final List<JourneyStop> stops;

  /// When the phone last got this answer (or had it confirmed).
  final DateTime fetchedAt;
  final bool fromCache;

  JourneyStop? get firstStop => stops.isEmpty ? null : stops.first;
  JourneyStop? get lastStop => stops.isEmpty ? null : stops.last;

  /// The scheduled time of the first stop: the time the train leaves its origin.
  DateTime? get scheduledDeparture => firstStop?.scheduledDeparture;

  bool get isCancelled => status == 'Cancelled';
  bool get isRunning => status == 'Running';
  bool get isCompleted => status == 'Completed';

  JourneyHeadline get headline {
    if (isCancelled) return JourneyHeadline.cancelled;
    if (isRunning && confidence == 'Unknown') return JourneyHeadline.noLiveData;
    if ((isRunning || isCompleted) && (delayMinutes ?? 0) >= 2) {
      return JourneyHeadline.delayed;
    }
    if (isCompleted) return JourneyHeadline.completed;
    if (isRunning) return JourneyHeadline.runningOnTime;
    return JourneyHeadline.scheduled;
  }

  /// The last stop the train is known to have reached, for "Last known: departed Oak Park 14:33".
  JourneyStop? get lastKnownStop {
    for (final stop in stops.reversed) {
      if (stop.hasHappened) return stop;
    }
    return null;
  }

  /// A stop whose platform changed and that the train has not left yet: the platform banner.
  JourneyStop? get platformBannerStop {
    for (final stop in stops) {
      if (stop.platformChanged &&
          stop.platform != null &&
          stop.state != 'Departed') {
        return stop;
      }
    }
    return null;
  }

  /// How to word a stop's line (REQ-PAX-002: the scheduled time stays visible, the live one is labelled).
  StopWording wordingFor(JourneyStop stop) {
    if (stop.state == 'Skipped' || stop.state == 'Cancelled') {
      return StopWording.skipped;
    }

    if (stop.state == 'Departed') {
      final late = _minutesLate(stop.actualDeparture, stop.scheduledDeparture);
      return late >= 2 ? StopWording.departedLate : StopWording.departedOnTime;
    }

    if (stop.state == 'Arrived') {
      if (stop == lastStop) {
        final late = _minutesLate(stop.actualArrival, stop.scheduledArrival);
        if (late >= 2) return StopWording.arrivedLate;
        return late <= -2
            ? StopWording.arrivedEarly
            : StopWording.arrivedOnTime;
      }
      return StopWording.atPlatform;
    }

    // Pending. With no live basis there is only the timetable.
    if (confidence == 'Unknown' ||
        stop.estimateSource != 'Live' ||
        stop.liveTime == null) {
      return StopWording.scheduled;
    }

    final late = _minutesLate(stop.liveTime, stop.scheduledTime);
    if (late < 2) return StopWording.expectedOnTime;

    final firstPending = stops.firstWhere(
      (s) => s.state == 'Pending',
      orElse: () => stop,
    );
    return identical(firstPending, stop)
        ? StopWording.expected
        : StopWording.estimate;
  }

  static int _minutesLate(DateTime? actual, DateTime? scheduled) {
    if (actual == null || scheduled == null) return 0;
    return (actual.difference(scheduled).inSeconds / 60).round();
  }
}
