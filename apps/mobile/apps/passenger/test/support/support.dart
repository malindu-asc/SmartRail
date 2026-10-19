import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_riverpod/misc.dart' show Override;
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:passenger_app/app/app_overrides.dart';
import 'package:passenger_app/app/passenger_app.dart';
import 'package:passenger_app/core/polling/poller.dart';
import 'package:passenger_app/features/alerts/presentation/providers/alerts_providers.dart';
import 'package:passenger_app/core/routing/app_router.dart';
import 'package:passenger_app/core/storage/key_value_store.dart';
import 'package:passenger_app/core/time/network_clock_provider.dart';
import 'package:passenger_app/features/board/domain/entities/departure.dart';
import 'package:passenger_app/features/board/domain/repositories/board_repository.dart';
import 'package:passenger_app/features/board/presentation/providers/board_providers.dart';
import 'package:passenger_app/features/journeys/domain/entities/journey_detail.dart';
import 'package:passenger_app/features/journeys/domain/repositories/journey_repository.dart';
import 'package:passenger_app/features/journeys/presentation/providers/journey_providers.dart';
import 'package:passenger_app/features/stations/domain/entities/station.dart';
import 'package:passenger_app/features/stations/domain/repositories/station_repository.dart';
import 'package:passenger_app/features/stations/presentation/providers/station_providers.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';

/// 08:55 UTC is 14:25 in the network time zone (UTC+5:30).
final t1425 = DateTime.utc(2026, 10, 14, 8, 55);

/// 14:18 network time: seven minutes before the 14:25 train.
final now1418 = DateTime.utc(2026, 10, 14, 8, 48);

DateTime at(int minutesAfter1425) =>
    t1425.add(Duration(minutes: minutesAfter1425));

class FakeProbe implements ApiProbe {
  bool reachable = true;

  @override
  Future<bool> isReachable() async => reachable;
}

/// Answers from a list, one per call; the last answer repeats. Counts calls so polling can be asserted.
class FakeBoardRepository implements BoardRepository {
  FakeBoardRepository(this.answers);

  final List<Either<Failure, BoardSnapshot>> answers;
  final List<String> asked = [];

  @override
  Future<Either<Failure, BoardSnapshot>> fetch(String stationCode) async {
    asked.add(stationCode);
    final index = asked.length - 1;
    return answers[index < answers.length ? index : answers.length - 1];
  }
}

class FakeJourneyRepository implements JourneyRepository {
  FakeJourneyRepository(this.answers);

  final List<Either<Failure, JourneyDetail>> answers;
  final List<String> asked = [];

  @override
  Future<Either<Failure, JourneyDetail>> fetch(String journeyId) async {
    asked.add(journeyId);
    final index = asked.length - 1;
    return answers[index < answers.length ? index : answers.length - 1];
  }
}

class FakeStationRepository implements StationRepository {
  FakeStationRepository(this.all);

  final List<Station> all;
  final List<String> queries = [];
  Failure? failure;

  @override
  Future<Either<Failure, List<Station>>> search(String query) async {
    queries.add(query);
    if (failure != null) return left(failure!);
    final q = query.toLowerCase();
    return right(
      all
          .where(
            (s) =>
                s.name.toLowerCase().contains(q) ||
                s.code.toLowerCase().contains(q),
          )
          .toList(),
    );
  }
}

const stations = [
  Station(code: 'CEN', name: 'Central', facilities: ['toilets']),
  Station(code: 'OAK', name: 'Oak Park'),
  Station(code: 'MIL', name: 'Mill Lane'),
  Station(code: 'RIV', name: 'Riverside'),
  Station(code: 'HIL', name: 'Hill Town'),
  Station(code: 'HAR', name: 'Harbour'),
];

Departure dep({
  String id = 'j1',
  int scheduled = 0,
  int? expected,
  String name = 'Riverside',
  String status = 'Running',
  String confidence = 'Live',
  int? delay,
  String? platform = '1',
  bool platformChanged = false,
  int? ageSeconds,
}) => Departure(
  journeyId: id,
  trainNumber: '1001',
  routeCode: 'MAIN',
  destinationCode: 'RIV',
  destinationName: name,
  scheduled: at(scheduled),
  expected: at(expected ?? scheduled),
  estimateSource: confidence == 'Scheduled' ? 'Scheduled' : 'Live',
  delayMinutes: delay,
  punctuality: delay == null
      ? null
      : (delay >= 10
            ? 'MajorDelay'
            : delay >= 2
            ? 'MinorDelay'
            : 'OnTime'),
  platform: platform,
  platformChanged: platformChanged,
  status: status,
  confidence: confidence,
  isStale: false,
  ageSeconds: ageSeconds,
);

BoardSnapshot board(
  List<Departure> items, {
  String code = 'CEN',
  String name = 'Central',
  bool fromCache = false,
  DateTime? fetchedAt,
}) => BoardSnapshot(
  stationCode: code,
  stationName: name,
  timeZone: 'Asia/Colombo',
  asOf: now1418,
  fetchedAt: fetchedAt ?? now1418,
  items: items,
  fromCache: fromCache,
);

JourneyStop stop(
  int sequence,
  String code,
  String name, {
  String state = 'Pending',
  int? scheduledArr,
  int? scheduledDep,
  int? estArr,
  int? estDep,
  int? actualArr,
  int? actualDep,
  String source = 'Scheduled',
  String? platform,
  bool platformChanged = false,
}) => JourneyStop(
  sequence: sequence,
  stationCode: code,
  stationName: name,
  state: state,
  estimateSource: source,
  scheduledArrival: scheduledArr == null ? null : at(scheduledArr),
  scheduledDeparture: scheduledDep == null ? null : at(scheduledDep),
  estimatedArrival: estArr == null ? null : at(estArr),
  estimatedDeparture: estDep == null ? null : at(estDep),
  actualArrival: actualArr == null ? null : at(actualArr),
  actualDeparture: actualDep == null ? null : at(actualDep),
  platform: platform,
  platformChanged: platformChanged,
);

JourneyDetail journey(
  List<JourneyStop> stops, {
  String id = 'j1',
  String status = 'Running',
  String confidence = 'Live',
  int? delay,
  DateTime? asOf,
  bool fromCache = false,
}) => JourneyDetail(
  journeyId: id,
  trainNumber: '1001',
  routeCode: 'MAIN',
  serviceDate: '2026-10-14',
  timeZone: 'Asia/Colombo',
  status: status,
  confidence: confidence,
  originCode: stops.first.stationCode,
  originName: stops.first.stationName,
  destinationCode: stops.last.stationCode,
  destinationName: stops.last.stationName,
  isStale: false,
  stops: stops,
  fetchedAt: now1418,
  asOf: asOf ?? now1418,
  delayMinutes: delay,
  punctuality: delay == null ? null : (delay >= 2 ? 'MinorDelay' : 'OnTime'),
  fromCache: fromCache,
);

/// Central 14:25, Oak Park 14:31, Mill Lane 14:38, Riverside 14:47: on time and running.
JourneyDetail onTimeJourney() => journey([
  stop(
    1,
    'CEN',
    'Central',
    state: 'Departed',
    scheduledDep: 0,
    actualDep: 0,
    source: 'Confirmed',
  ),
  stop(
    2,
    'OAK',
    'Oak Park',
    scheduledArr: 6,
    scheduledDep: 7,
    estArr: 6,
    estDep: 7,
    source: 'Live',
    platform: '5',
    platformChanged: true,
  ),
  stop(
    3,
    'MIL',
    'Mill Lane',
    scheduledArr: 13,
    scheduledDep: 14,
    estArr: 14,
    estDep: 14,
    source: 'Live',
  ),
  stop(4, 'RIV', 'Riverside', scheduledArr: 22, estArr: 23, source: 'Live'),
], delay: 0);

/// Departed Central 2 minutes late, Oak Park 10 minutes late, Mill Lane and Riverside later still.
JourneyDetail delayedJourney() => journey([
  stop(
    1,
    'CEN',
    'Central',
    state: 'Departed',
    scheduledDep: 0,
    actualDep: 2,
    source: 'Confirmed',
  ),
  stop(
    2,
    'OAK',
    'Oak Park',
    state: 'Departed',
    scheduledArr: 6,
    scheduledDep: 7,
    actualArr: 14,
    actualDep: 17,
    source: 'Confirmed',
  ),
  stop(
    3,
    'MIL',
    'Mill Lane',
    scheduledArr: 13,
    scheduledDep: 14,
    estArr: 26,
    estDep: 27,
    source: 'Live',
  ),
  stop(4, 'RIV', 'Riverside', scheduledArr: 22, estArr: 35, source: 'Live'),
], delay: 12);

JourneyDetail noLiveJourney() => journey(
  [
    stop(
      1,
      'CEN',
      'Central',
      state: 'Departed',
      scheduledDep: 0,
      actualDep: 0,
      source: 'Confirmed',
    ),
    stop(
      2,
      'OAK',
      'Oak Park',
      scheduledArr: 6,
      scheduledDep: 7,
      source: 'Scheduled',
    ),
    stop(
      3,
      'MIL',
      'Mill Lane',
      scheduledArr: 13,
      scheduledDep: 14,
      source: 'Scheduled',
    ),
    stop(4, 'RIV', 'Riverside', scheduledArr: 22, source: 'Scheduled'),
  ],
  confidence: 'Unknown',
  asOf: now1418.subtract(const Duration(minutes: 12)),
);

Either<Failure, T> ok<T>(T value) => right(value);

Either<Failure, T> bad<T>(Failure failure) => left(failure);

class Harness {
  Harness(this.container);

  final ProviderContainer container;

  /// Where the router is now, e.g. /home/station/CEN.
  String get location => container
      .read(appRouterProvider)
      .routerDelegate
      .currentConfiguration
      .last
      .matchedLocation;

  void go(String path) => container.read(appRouterProvider).go(path);
}

/// Starts the real app on fakes. Polling and reachability checks are off unless a test turns them on; the clock is fixed at 14:18.
Future<Harness> pumpApp(
  WidgetTester tester, {
  BoardRepository? boards,
  JourneyRepository? journeys,
  StationRepository? stationRepo,
  KeyValueStore? store,
  FakeProbe? probe,
  Duration poll = Duration.zero,
  String? start = '/home',
  List<Override> extra = const [],
}) async {
  final container = ProviderContainer(
    retry: (retryCount, error) => null,
    overrides: [
      ...appOverrides,
      keyValueStoreProvider.overrideWithValue(
        store ?? MemoryStore({'onboarding_seen': true}),
      ),
      boardRepositoryProvider.overrideWithValue(
        boards ?? FakeBoardRepository([ok(board([]))]),
      ),
      journeyRepositoryProvider.overrideWithValue(
        journeys ?? FakeJourneyRepository([ok(onTimeJourney())]),
      ),
      stationRepositoryProvider.overrideWithValue(
        stationRepo ?? FakeStationRepository(stations),
      ),
      apiProbeProvider.overrideWithValue(probe ?? FakeProbe()),
      reachabilityIntervalProvider.overrideWithValue(Duration.zero),
      pollIntervalProvider.overrideWithValue(poll),
      alertsPollIntervalProvider.overrideWithValue(Duration.zero),
      nowProvider.overrideWithValue(() => now1418),
      ...extra,
    ],
  );
  addTearDown(container.dispose);

  await tester.pumpWidget(
    UncontrolledProviderScope(
      container: container,
      child: const PassengerApp(),
    ),
  );
  final harness = Harness(container);
  if (start != null) {
    harness.go(start);
  }
  await tester.pumpAndSettle();
  return harness;
}

/// Leaves the screen so polling timers stop, as they do when a passenger closes the app, before the test ends.
Future<void> unmount(WidgetTester tester) async {
  await tester.pumpWidget(const SizedBox());
  await tester.pump(const Duration(milliseconds: 50));
}
