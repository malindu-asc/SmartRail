import 'package:fpdart/fpdart.dart';
import 'package:smartrail_core/smartrail_core.dart';

/// Active: signed on. Ready: sign-on is open. Upcoming: later today. Past: ended or missed.
enum DutyStatus { active, ready, upcoming, past }

enum JourneyStatus { next, later, running, done }

class DutyJourney {
  const DutyJourney({
    required this.id,
    required this.trainNumber,
    required this.origin,
    required this.destination,
    required this.status,
    required this.departsAt,
    required this.arrivesAt,
    required this.platform,
    required this.stops,
    required this.delayMinutes,
  });

  final String id;
  final String trainNumber;
  final String origin;
  final String destination;
  final JourneyStatus status;
  final DateTime departsAt;
  final DateTime arrivesAt;
  final String? platform;
  final int stops;
  final int? delayMinutes;

  static DutyJourney fromJson(Map<String, Object?> json) => DutyJourney(
    id: json['journeyId']! as String,
    trainNumber: json['trainNumber']! as String,
    origin: json['origin']! as String,
    destination: json['destination']! as String,
    status: switch (json['status']) {
      'Next' => JourneyStatus.next,
      'Running' => JourneyStatus.running,
      'Done' => JourneyStatus.done,
      _ => JourneyStatus.later,
    },
    departsAt: DateTime.parse(json['departsUtc']! as String),
    arrivesAt: DateTime.parse(json['arrivesUtc']! as String),
    platform: json['platform'] as String?,
    stops: (json['stops'] as num?)?.toInt() ?? 0,
    delayMinutes: (json['delayMinutes'] as num?)?.toInt(),
  );
}

class Duty {
  const Duty({
    required this.id,
    required this.status,
    required this.from,
    required this.to,
    required this.unitCode,
    required this.canSignOn,
    required this.needsReview,
    required this.signedOnAt,
    required this.journeys,
  });

  final String id;
  final DutyStatus status;
  final DateTime from;
  final DateTime to;
  final String? unitCode;
  final bool canSignOn;
  final bool needsReview;
  final DateTime? signedOnAt;
  final List<DutyJourney> journeys;

  static Duty fromJson(Map<String, Object?> json) => Duty(
    id: json['id']! as String,
    status: switch (json['status']) {
      'Active' => DutyStatus.active,
      'Ready' => DutyStatus.ready,
      'Past' => DutyStatus.past,
      _ => DutyStatus.upcoming,
    },
    from: DateTime.parse(json['fromUtc']! as String),
    to: DateTime.parse(json['toUtc']! as String),
    unitCode: json['unitCode'] as String?,
    canSignOn: json['canSignOn'] == true,
    needsReview: json['needsReview'] == true,
    signedOnAt: json['signedOnAt'] == null
        ? null
        : DateTime.parse(json['signedOnAt']! as String),
    journeys: [
      for (final j in (json['journeys'] as List? ?? const []))
        DutyJourney.fromJson((j as Map).cast<String, Object?>()),
    ],
  );
}

/// What `GET /api/driver/duty` answers: today's shifts.
class MyDuty {
  const MyDuty({required this.date, required this.duties});

  final String date;
  final List<Duty> duties;

  /// The shift that matters now: the one signed on, else the one that can be signed on, else the next one.
  Duty? get current =>
      duties.where((d) => d.status == DutyStatus.active).firstOrNull ??
      duties.where((d) => d.status == DutyStatus.ready).firstOrNull ??
      duties.where((d) => d.status == DutyStatus.upcoming).firstOrNull;

  static MyDuty fromJson(Map<String, Object?> json) => MyDuty(
    date: json['date']! as String,
    duties: [
      for (final d in (json['duties'] as List? ?? const []))
        Duty.fromJson((d as Map).cast<String, Object?>()),
    ],
  );
}

enum StopState { pending, arrived, departed, skipped, cancelled }

class DriverStop {
  const DriverStop({
    required this.sequence,
    required this.stationCode,
    required this.name,
    required this.kind,
    required this.state,
    required this.scheduled,
    required this.expected,
    required this.actual,
    required this.platform,
    required this.originalPlatform,
    required this.isNext,
  });

  final int sequence;
  final String stationCode;
  final String name;
  final String kind;
  final StopState state;
  final DateTime? scheduled;
  final DateTime? expected;
  final DateTime? actual;
  final String? platform;
  final String? originalPlatform;
  final bool isNext;

  static DriverStop fromJson(Map<String, Object?> json) => DriverStop(
    sequence: (json['sequence']! as num).toInt(),
    stationCode: json['stationCode']! as String,
    name: json['stationName']! as String,
    kind: json['kind']! as String,
    state: switch (json['state']) {
      'Arrived' => StopState.arrived,
      'Departed' => StopState.departed,
      'Skipped' => StopState.skipped,
      'Cancelled' => StopState.cancelled,
      _ => StopState.pending,
    },
    scheduled: _time(json['scheduledUtc']),
    expected: _time(json['expectedUtc']),
    actual: _time(json['actualUtc']),
    platform: json['platform'] as String?,
    originalPlatform: json['originalPlatform'] as String?,
    isNext: json['isNext'] == true,
  );

  static DateTime? _time(Object? value) =>
      value is String ? DateTime.parse(value) : null;
}

/// The one tap that makes sense now: "arrived" or "departed" at a stop.
class NextEvent {
  const NextEvent({
    required this.stopSequence,
    required this.stationName,
    required this.type,
  });

  final int stopSequence;
  final String stationName;
  final String type;

  bool get isArrival => type == 'arrived';
}

class DriverJourney {
  const DriverJourney({
    required this.id,
    required this.trainNumber,
    required this.origin,
    required this.destination,
    required this.lifecycle,
    required this.delayMinutes,
    required this.delayReason,
    required this.dutyState,
    required this.unitCode,
    required this.canAct,
    required this.next,
    required this.stops,
  });

  final String id;
  final String trainNumber;
  final String origin;
  final String destination;
  final String lifecycle;
  final int? delayMinutes;
  final String? delayReason;
  final String dutyState;
  final String? unitCode;
  final bool canAct;
  final NextEvent? next;
  final List<DriverStop> stops;

  bool get isComplete => lifecycle == 'Completed';

  DriverStop? get nextStop => next == null
      ? null
      : stops.where((s) => s.sequence == next!.stopSequence).firstOrNull;

  /// The stop the train is standing at, when it has arrived and not yet left.
  DriverStop? get atStop =>
      stops.where((s) => s.state == StopState.arrived).firstOrNull;

  static DriverJourney fromJson(Map<String, Object?> json) {
    final next = json['next'];
    return DriverJourney(
      id: json['journeyId']! as String,
      trainNumber: json['trainNumber']! as String,
      origin: json['origin']! as String,
      destination: json['destination']! as String,
      lifecycle: json['lifecycle']! as String,
      delayMinutes: (json['delayMinutes'] as num?)?.toInt(),
      delayReason: json['delayReasonCode'] as String?,
      dutyState: json['dutyState']! as String,
      unitCode: json['unitCode'] as String?,
      canAct: json['canAct'] == true,
      next: next is Map
          ? NextEvent(
              stopSequence: (next['stopSequence']! as num).toInt(),
              stationName: next['stationName']! as String,
              type: next['type']! as String,
            )
          : null,
      stops: [
        for (final s in (json['stops'] as List? ?? const []))
          DriverStop.fromJson((s as Map).cast<String, Object?>()),
      ],
    );
  }
}

/// The code of the conflict the server answers when the driver taps a stop the train is not at (its message says what was expected).
const stopDoesNotMatchCode = 'Journeys.StopDoesNotMatch';

/// What the driver app reads and does about its duty (SPEC-011).
abstract interface class DutyRepository {
  Future<Either<Failure, MyDuty>> myDuty();

  Future<Either<Failure, List<Duty>>> history();

  Future<Either<Failure, Unit>> signOn(String dutyId);

  Future<Either<Failure, Unit>> signOff(String dutyId);

  Future<Either<Failure, DriverJourney>> journey(String journeyId);

  /// [type] is arrived, departed or skipped. The server stores a message with the same [sequence] once.
  Future<Either<Failure, Unit>> sendEvent({
    required String journeyId,
    required int stopSequence,
    required String type,
    required int sequence,
    required DateTime occurredAt,
  });

  Future<Either<Failure, Unit>> delayReason(String journeyId, String code);
}
