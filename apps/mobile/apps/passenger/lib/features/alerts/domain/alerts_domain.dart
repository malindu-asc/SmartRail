/// The four kinds of alert in the Alerts tab (design 33).
enum AlertKind { platformChanged, cancelled, delay, correction }

/// What was seen about one train the last time it was looked at. A change from this is what becomes an alert.
class Observed {
  const Observed({
    required this.journeyId,
    required this.scheduledDeparture,
    required this.originName,
    required this.destinationName,
    required this.cancelled,
    required this.delayMinutes,
    required this.expectedAt,
    required this.platform,
  });

  final String journeyId;
  final DateTime scheduledDeparture;
  final String originName;
  final String destinationName;
  final bool cancelled;

  /// Minutes behind, 0 when on time or when nothing live is known.
  final int delayMinutes;

  /// When it is expected where the passenger is interested: the arrival for a followed journey, the departure for a station board.
  final DateTime expectedAt;
  final String? platform;

  Map<String, Object?> toJson() => {
    'cancelled': cancelled,
    'delay': delayMinutes,
    'platform': platform,
  };
}

/// The state kept between looks: only what the rules compare.
class Baseline {
  const Baseline({
    required this.cancelled,
    required this.reportedDelay,
    required this.platform,
  });

  final bool cancelled;

  /// The delay the passenger was last told about.
  final int reportedDelay;
  final String? platform;

  Map<String, Object?> toJson() => {
    'cancelled': cancelled,
    'delay': reportedDelay,
    'platform': platform,
  };

  factory Baseline.fromJson(Map<String, Object?> json) => Baseline(
    cancelled: json['cancelled'] as bool? ?? false,
    reportedDelay: json['delay'] as int? ?? 0,
    platform: json['platform'] as String?,
  );
}

class AlertItem {
  const AlertItem({
    required this.id,
    required this.kind,
    required this.journeyId,
    required this.at,
    required this.scheduledDeparture,
    required this.originName,
    required this.destinationName,
    this.newPlatform,
    this.oldPlatform,
    this.minutes,
    this.expectedAt,
  });

  final String id;
  final AlertKind kind;
  final String journeyId;

  /// When the alert was raised.
  final DateTime at;
  final DateTime scheduledDeparture;
  final String originName;
  final String destinationName;
  final String? newPlatform;
  final String? oldPlatform;
  final int? minutes;
  final DateTime? expectedAt;

  Map<String, Object?> toJson() => {
    'id': id,
    'kind': kind.name,
    'journeyId': journeyId,
    'at': at.toUtc().toIso8601String(),
    'scheduledDeparture': scheduledDeparture.toUtc().toIso8601String(),
    'originName': originName,
    'destinationName': destinationName,
    'newPlatform': newPlatform,
    'oldPlatform': oldPlatform,
    'minutes': minutes,
    'expectedAt': expectedAt?.toUtc().toIso8601String(),
  };

  static AlertItem? fromJson(Map<String, Object?> json) {
    try {
      return AlertItem(
        id: json['id']! as String,
        kind: AlertKind.values.byName(json['kind']! as String),
        journeyId: json['journeyId']! as String,
        at: DateTime.parse(json['at']! as String),
        scheduledDeparture: DateTime.parse(
          json['scheduledDeparture']! as String,
        ),
        originName: json['originName']! as String,
        destinationName: json['destinationName']! as String,
        newPlatform: json['newPlatform'] as String?,
        oldPlatform: json['oldPlatform'] as String?,
        minutes: json['minutes'] as int?,
        expectedAt: json['expectedAt'] == null
            ? null
            : DateTime.parse(json['expectedAt']! as String),
      );
    } on Object {
      return null;
    }
  }
}

/// The rules (REQ-PAX-007, REQ-PAX-015): what a change in a followed train means to the passenger.
///
/// * A train that is cancelled raises "Cancelled" once. If it comes back, one "Correction" says so (never silently).
/// * A delay is told when it first reaches 2 minutes (the first on-time band, ASM-021) and again each time it moves by 2 or more.
/// * A new platform is told when the platform changes from one that was known.
/// * The first look at a train only records what it is: nothing is announced for a state that was already true when it was followed.
abstract final class AlertRules {
  static const delayStep = 2;

  /// The alerts a new look raises, and the baseline to keep. [baseline] is null the first time.
  static (Baseline, List<AlertItem>) detect({
    required Baseline? baseline,
    required Observed seen,
    required DateTime now,
    required String Function() newId,
  }) {
    final next = Baseline(
      cancelled: seen.cancelled,
      reportedDelay:
          baseline?.reportedDelay ??
          (seen.delayMinutes >= delayStep ? seen.delayMinutes : 0),
      platform: seen.platform ?? baseline?.platform,
    );
    if (baseline == null) return (next, const []);

    AlertItem make(
      AlertKind kind, {
      String? newPlatform,
      String? oldPlatform,
      int? minutes,
    }) => AlertItem(
      id: newId(),
      kind: kind,
      journeyId: seen.journeyId,
      at: now,
      scheduledDeparture: seen.scheduledDeparture,
      originName: seen.originName,
      destinationName: seen.destinationName,
      newPlatform: newPlatform,
      oldPlatform: oldPlatform,
      minutes: minutes,
      expectedAt: seen.expectedAt,
    );

    final alerts = <AlertItem>[];
    var reported = baseline.reportedDelay;

    if (seen.cancelled && !baseline.cancelled) {
      alerts.add(make(AlertKind.cancelled));
    } else if (!seen.cancelled && baseline.cancelled) {
      alerts.add(make(AlertKind.correction));
    }

    if (!seen.cancelled) {
      final delay = seen.delayMinutes;
      final moved = (delay - reported).abs() >= delayStep;
      if (delay >= delayStep && moved) {
        alerts.add(make(AlertKind.delay, minutes: delay));
        reported = delay;
      } else if (delay < delayStep && reported >= delayStep) {
        // Back on time: the next delay is a new story.
        reported = 0;
      }
    }

    final was = baseline.platform;
    if (!seen.cancelled &&
        was != null &&
        seen.platform != null &&
        seen.platform != was) {
      alerts.add(
        make(
          AlertKind.platformChanged,
          newPlatform: seen.platform,
          oldPlatform: was,
        ),
      );
    }

    return (
      Baseline(
        cancelled: seen.cancelled,
        reportedDelay: reported,
        platform: seen.platform ?? was,
      ),
      alerts,
    );
  }
}
