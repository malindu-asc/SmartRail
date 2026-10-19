import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';
import 'package:passenger_app/core/storage/key_value_store.dart';
import 'package:passenger_app/features/follow/data/local_follow_repository.dart';
import 'package:passenger_app/features/follow/domain/follow_repository.dart';
import 'package:passenger_app/features/stations/data/repositories/station_repositories.dart';
import 'package:smartrail_core/smartrail_core.dart';

import 'support/support.dart';

const debounce = Duration(milliseconds: 300);

Future<void> typeQuery(WidgetTester tester, String text) async {
  await tester.enterText(find.byType(TextField), text);
  await tester.pump(debounce);
  await tester.pumpAndSettle();
}

void main() {
  group('finding a station (AC-4)', () {
    testWidgets('typing a name finds it by name or by code', (tester) async {
      await pumpApp(tester, start: '/home/search');

      await typeQuery(tester, 'oak');
      expect(find.text('Oak Park'), findsOneWidget);
      expect(find.text('Riverside'), findsNothing);

      await typeQuery(tester, 'RIV');
      expect(find.text('Riverside'), findsOneWidget);
    });

    testWidgets('it waits for a pause in typing before asking', (tester) async {
      final repo = FakeStationRepository(stations);
      await pumpApp(tester, stationRepo: repo, start: '/home/search');

      await tester.enterText(find.byType(TextField), 'c');
      await tester.pump(const Duration(milliseconds: 100));
      await tester.enterText(find.byType(TextField), 'ce');
      await tester.pump(const Duration(milliseconds: 100));
      await tester.enterText(find.byType(TextField), 'cen');
      await tester.pump(debounce);
      await tester.pumpAndSettle();

      expect(repo.queries, ['cen']);
    });

    testWidgets('no match says so and suggests the code', (tester) async {
      await pumpApp(tester, start: '/home/search');

      await typeQuery(tester, 'zzz');

      expect(find.text('No stations match'), findsOneWidget);
      expect(find.textContaining('for example CEN'), findsOneWidget);
    });

    testWidgets('a failed search says so and can retry', (tester) async {
      final repo = FakeStationRepository(stations)
        ..failure = const NetworkFailure.timeout();
      await pumpApp(tester, stationRepo: repo, start: '/home/search');

      await typeQuery(tester, 'cen');
      expect(find.text('Something went wrong'), findsOneWidget);

      repo.failure = null;
      await tester.tap(find.text('Try again'));
      await tester.pumpAndSettle();
      expect(find.text('Central'), findsOneWidget);
    });

    testWidgets('choosing a station opens its departures and remembers it', (
      tester,
    ) async {
      final store = MemoryStore({'onboarding_seen': true});
      final harness = await pumpApp(
        tester,
        store: store,
        boards: FakeBoardRepository([
          ok(board([dep()])),
        ]),
        start: '/home/search',
      );

      await typeQuery(tester, 'central');
      await tester.tap(find.text('Central'));
      await tester.pumpAndSettle();

      expect(harness.location, '/home/station/CEN');
      expect(LocalRecentStationsRepository(store).recent().first.code, 'CEN');
    });

    testWidgets('with nothing typed, recent stations come first', (
      tester,
    ) async {
      final store = MemoryStore({'onboarding_seen': true});
      await LocalRecentStationsRepository(store).add(stations[1]);
      await pumpApp(tester, store: store, start: '/home/search');

      expect(find.text('Recent'), findsOneWidget);
      expect(find.text('Oak Park'), findsOneWidget);
    });

    testWidgets('clearing the box goes back to recent stations', (
      tester,
    ) async {
      final store = MemoryStore({'onboarding_seen': true});
      await LocalRecentStationsRepository(store).add(stations[1]);
      await pumpApp(tester, store: store, start: '/home/search');

      await typeQuery(tester, 'riv');
      expect(find.text('Results'), findsOneWidget);

      await tester.tap(find.byIcon(Icons.close));
      await tester.pumpAndSettle();
      expect(find.text('Recent'), findsOneWidget);
    });
  });

  group('Home (AC-3)', () {
    testWidgets('greets by the railway clock', (tester) async {
      await pumpApp(tester);

      // 08:48 UTC is 14:18 in the network time zone, so afternoon, not morning.
      expect(find.text('Good afternoon'), findsOneWidget);
      expect(find.text('Where to today?'), findsOneWidget);
    });

    testWidgets('with no follows it invites the passenger to find a station', (
      tester,
    ) async {
      final harness = await pumpApp(tester);

      expect(find.text('No followed stations yet'), findsOneWidget);
      await tester.tap(find.text('Find a station'));
      await tester.pumpAndSettle();
      expect(harness.location, '/home/search');
    });

    testWidgets('a followed station shows its next train', (tester) async {
      final store = MemoryStore({'onboarding_seen': true});
      await LocalFollowRepository(
        store,
      ).saveStations(const [FollowedStation(code: 'CEN', name: 'Central')]);
      await pumpApp(
        tester,
        store: store,
        boards: FakeBoardRepository([
          ok(board([dep(scheduled: 0, expected: 12, delay: 12)])),
        ]),
      );

      expect(find.text('Followed stations'), findsOneWidget);
      expect(find.text('Central'), findsWidgets);
      expect(
        find.textContaining('14:25 Riverside · in 19 min'),
        findsOneWidget,
      );
      expect(find.text('+12 min'), findsOneWidget);
    });

    testWidgets('a followed station opens its departures', (tester) async {
      final store = MemoryStore({'onboarding_seen': true});
      await LocalFollowRepository(
        store,
      ).saveStations(const [FollowedStation(code: 'CEN', name: 'Central')]);
      final harness = await pumpApp(
        tester,
        store: store,
        boards: FakeBoardRepository([
          ok(board([dep()])),
        ]),
      );

      await tester.tap(find.textContaining('14:25 Riverside'));
      await tester.pumpAndSettle();
      expect(harness.location, '/home/station/CEN');
    });

    testWidgets('a followed journey shows and opens', (tester) async {
      final store = MemoryStore({'onboarding_seen': true});
      await LocalFollowRepository(store).saveJourneys([
        FollowedJourney(
          journeyId: 'j1',
          trainNumber: '1001',
          originName: 'Central',
          destinationName: 'Riverside',
          scheduledDeparture: t1425,
        ),
      ]);
      final harness = await pumpApp(tester, store: store);

      expect(find.text('Followed journeys'), findsOneWidget);
      await tester.tap(find.textContaining('Riverside').first);
      await tester.pumpAndSettle();
      expect(harness.location, '/home/journey/j1');
    });

    testWidgets('From and To are picked from search, and can be swapped', (
      tester,
    ) async {
      await pumpApp(tester);

      await tester.tap(find.text('Choose a station').first);
      await tester.pumpAndSettle();
      await typeQuery(tester, 'oak');
      await tester.tap(find.text('Oak Park'));
      await tester.pumpAndSettle();

      expect(find.text('Oak Park'), findsOneWidget);

      await tester.tap(find.text('Choose a station').first);
      await tester.pumpAndSettle();
      await typeQuery(tester, 'riv');
      await tester.tap(find.text('Riverside'));
      await tester.pumpAndSettle();

      final fromBefore = tester.getTopLeft(find.text('Oak Park')).dy;
      final toBefore = tester.getTopLeft(find.text('Riverside')).dy;
      expect(fromBefore, lessThan(toBefore));

      await tester.tap(
        find.byWidgetPredicate(
          (w) => w is SvgIcon && w.name == AppIcons.swapSmall,
        ),
      );
      await tester.pumpAndSettle();

      expect(
        tester.getTopLeft(find.text('Riverside')).dy,
        lessThan(tester.getTopLeft(find.text('Oak Park')).dy),
      );
    });

    testWidgets('Find trains stays off until both stations are chosen', (
      tester,
    ) async {
      await pumpApp(tester);

      final button = find.widgetWithText(FilledButton, 'Find trains');
      expect(tester.widget<FilledButton>(button).onPressed, isNull);
    });
  });
}
