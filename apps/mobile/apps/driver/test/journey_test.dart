import 'package:driver_app/app/locale_provider.dart';
import 'package:driver_app/features/duty/domain/duty_domain.dart';
import 'package:driver_app/features/duty/presentation/providers/duty_providers.dart';
import 'package:driver_app/features/duty/presentation/screens/journey_screen.dart';
import 'package:driver_app/features/messages/messages.dart';
import 'package:driver_app/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

DriverStop _stop(int seq, String name, StopState state, {bool next = false}) =>
    DriverStop(
      sequence: seq,
      stationCode: name.substring(0, 3).toUpperCase(),
      name: name,
      kind: 'Stop',
      state: state,
      scheduled: DateTime.utc(2026, 10, 21, 3, seq * 10),
      expected: DateTime.utc(2026, 10, 21, 3, seq * 10),
      actual: null,
      platform: '1',
      originalPlatform: '1',
      isNext: next,
    );

class _Repo implements DutyRepository {
  _Repo(this.journeyResult);

  final Either<Failure, DriverJourney> journeyResult;

  @override
  Future<Either<Failure, DriverJourney>> journey(String journeyId) async =>
      journeyResult;

  @override
  Future<Either<Failure, MyDuty>> myDuty() async =>
      right(const MyDuty(date: '2026-10-21', duties: []));

  @override
  Future<Either<Failure, List<Duty>>> history() async => right(const []);

  @override
  Future<Either<Failure, Unit>> signOn(String dutyId) async => right(unit);

  @override
  Future<Either<Failure, Unit>> signOff(String dutyId) async => right(unit);

  @override
  Future<Either<Failure, Unit>> sendEvent({
    required String journeyId,
    required int stopSequence,
    required String type,
    required int sequence,
    required DateTime occurredAt,
  }) async => right(unit);

  @override
  Future<Either<Failure, Unit>> delayReason(String journeyId, String code) async =>
      right(unit);
}

Widget _app(DutyRepository repo) => ProviderScope(
  overrides: [
    dutyRepositoryProvider.overrideWithValue(repo),
    alertsProvider.overrideWith((ref) => Stream.value(const <AlertItem>[])),
  ],
  child: MaterialApp(
    theme: SmartRailTheme.driver(),
    locale: const Locale('en'),
    supportedLocales: supportedLanguages,
    localizationsDelegates: const [
      AppLocalizations.delegate,
      GlobalMaterialLocalizations.delegate,
      GlobalWidgetsLocalizations.delegate,
      GlobalCupertinoLocalizations.delegate,
    ],
    home: const JourneyScreen(journeyId: 'j1'),
  ),
);

DriverJourney _running() => DriverJourney(
  id: 'j1',
  trainNumber: '1002',
  origin: 'Central',
  destination: 'Riverside',
  lifecycle: 'Running',
  delayMinutes: 3,
  delayReason: null,
  dutyState: 'SignedOn',
  unitCode: 'MAIN-F',
  canAct: true,
  next: const NextEvent(
    stopSequence: 2,
    stationName: 'Oak Park',
    type: 'arrived',
  ),
  stops: [
    _stop(1, 'Central', StopState.departed),
    _stop(2, 'Oak Park', StopState.pending, next: true),
    _stop(3, 'Riverside', StopState.pending),
  ],
);

void main() {
  testWidgets('a running journey shows its next stop and the buttons', (tester) async {
    await tester.pumpWidget(_app(_Repo(right(_running()))));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.text('Oak Park'), findsWidgets);
    expect(find.text('Arrived at Oak Park'), findsOneWidget);
    expect(find.text('Delay reason'), findsOneWidget);
    expect(find.text('Not stopping here'), findsOneWidget);
    expect(find.text('Help'), findsOneWidget);

    // Unmount so the refresh timer stops before the test ends.
    await tester.pumpWidget(const SizedBox());
    await tester.pump();
  });

  testWidgets('a journey that cannot be loaded says so, not loading forever', (tester) async {
    await tester.pumpWidget(_app(_Repo(left(const NetworkFailure('network')))));
    await tester.pumpAndSettle();

    expect(find.text('Try again'), findsOneWidget);

    await tester.pumpWidget(const SizedBox());
    await tester.pump();
  });
}
