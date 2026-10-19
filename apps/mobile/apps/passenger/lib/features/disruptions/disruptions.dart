import 'package:fpdart/fpdart.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';

enum DisruptionSeverity { minor, major, severe }

class DisruptionStation {
  const DisruptionStation({required this.code, required this.name});

  final String code;
  final String name;
}

/// A notice about trouble on the network (design 34): what, where, how bad, and when it should clear.
class Disruption {
  const Disruption({
    required this.id,
    required this.title,
    required this.message,
    required this.severity,
    required this.networkWide,
    required this.stations,
    required this.updatedAt,
    this.expectedEnd,
  });

  final String id;

  /// Plain text. Shown as text, never as markup.
  final String title;
  final String message;
  final DisruptionSeverity severity;
  final bool networkWide;
  final List<DisruptionStation> stations;
  final DateTime updatedAt;
  final DateTime? expectedEnd;
}

class DisruptionJourney {
  const DisruptionJourney({
    required this.journeyId,
    required this.trainNumber,
    required this.stationCode,
    required this.destinationName,
    required this.scheduled,
    required this.expected,
    required this.status,
    required this.delayMinutes,
    required this.platform,
  });

  final String journeyId;
  final String trainNumber;
  final String stationCode;
  final String destinationName;
  final DateTime scheduled;
  final DateTime expected;
  final String status;
  final int? delayMinutes;
  final String? platform;

  bool get isCancelled => status == 'Cancelled';
}

class DisruptionDetail {
  const DisruptionDetail({required this.disruption, required this.journeys});

  final Disruption disruption;
  final List<DisruptionJourney> journeys;
}

abstract final class DisruptionModel {
  static DateTime _time(Object? v) => DateTime.parse(v! as String).toUtc();

  static Disruption disruption(Map d) => Disruption(
    id: d['id']! as String,
    title: d['title']! as String,
    message: d['message']! as String,
    severity: switch (d['severity']) {
      'Severe' => DisruptionSeverity.severe,
      'Major' => DisruptionSeverity.major,
      _ => DisruptionSeverity.minor,
    },
    networkWide: d['networkWide']! as bool,
    stations: [
      for (final s in d['stations']! as List)
        DisruptionStation(
          code: (s as Map)['code']! as String,
          name: s['name']! as String,
        ),
    ],
    updatedAt: _time(d['updatedAtUtc']),
    expectedEnd: d['expectedEndUtc'] == null
        ? null
        : _time(d['expectedEndUtc']),
  );

  static DisruptionDetail detail(Map d) => DisruptionDetail(
    disruption: disruption(d['disruption']! as Map),
    journeys: [
      for (final j in d['journeys']! as List)
        DisruptionJourney(
          journeyId: (j as Map)['journeyId']! as String,
          trainNumber: j['trainNumber']! as String,
          stationCode: j['stationCode']! as String,
          destinationName: j['destinationName']! as String,
          scheduled: _time(j['scheduledDepartureUtc']),
          expected: _time(j['expectedDepartureUtc']),
          status: j['status']! as String,
          delayMinutes: j['delayMinutes'] as int?,
          platform: j['platform'] as String?,
        ),
    ],
  );
}

abstract interface class DisruptionsRepository {
  Future<Either<Failure, List<Disruption>>> list();

  Future<Either<Failure, DisruptionDetail>> detail(String id);
}

class RemoteDisruptionsRepository implements DisruptionsRepository {
  RemoteDisruptionsRepository(this._client);

  final ApiClient _client;

  @override
  Future<Either<Failure, List<Disruption>>> list() async {
    final result = await _client.get('/api/operations/disruptions');
    return result.flatMap((r) {
      final body = r.body;
      if (body is! List) {
        return left(const UnknownFailure('disruptions.bad_response'));
      }
      try {
        return right([
          for (final d in body) DisruptionModel.disruption(d as Map),
        ]);
      } on Object {
        return left(const UnknownFailure('disruptions.bad_response'));
      }
    });
  }

  @override
  Future<Either<Failure, DisruptionDetail>> detail(String id) async {
    final result = await _client.get('/api/operations/disruptions/$id');
    return result.flatMap((r) {
      final body = r.body;
      if (body is! Map) {
        return left(const UnknownFailure('disruptions.bad_response'));
      }
      try {
        return right(DisruptionModel.detail(body));
      } on Object {
        return left(const UnknownFailure('disruptions.bad_response'));
      }
    });
  }
}

final disruptionsRepositoryProvider = Provider<DisruptionsRepository>(
  (ref) => RemoteDisruptionsRepository(ref.watch(apiClientProvider)),
);

/// The disruptions passengers should know about. A failure is quietly an empty list: a missing banner is better than an error on Home.
final disruptionsProvider =
    AsyncNotifierProvider.autoDispose<DisruptionsNotifier, List<Disruption>>(
      DisruptionsNotifier.new,
    );

class DisruptionsNotifier extends AsyncNotifier<List<Disruption>> {
  @override
  Future<List<Disruption>> build() => _load();

  Future<List<Disruption>> _load() async {
    final result = await ref.read(disruptionsRepositoryProvider).list();
    return result.getOrElse((_) => const []);
  }

  Future<void> refresh() async {
    final next = await AsyncValue.guard(_load);
    if (ref.mounted) state = next;
  }
}

final disruptionDetailProvider = FutureProvider.autoDispose
    .family<DisruptionDetail, String>((ref, id) async {
      final result = await ref.read(disruptionsRepositoryProvider).detail(id);
      return result.match((failure) => throw failure, (detail) => detail);
    });
