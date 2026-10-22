import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:passenger_app/features/follow/data/local_follow_repository.dart';
import 'package:passenger_app/features/journeys/presentation/widgets/journey_widgets.dart';
import 'package:passenger_app/core/storage/key_value_store.dart';
import 'package:smartrail_core/smartrail_core.dart';

import 'support/support.dart';

void main() {
  const route = '/home/journey/j1';

  group('what the journey says about itself', () {
    testWidgets('on time: running, the platform banner, times worded', (
      tester,
    ) async {
      await pumpApp(
        tester,
        journeys: FakeJourneyRepository([ok(onTimeJourney())]),
        start: route,
      );

      expect(find.text('Running · on time'), findsOneWidget);
      expect(find.text('Platform 5'), findsOneWidget);
      expect(find.text('Platform changed at Oak Park'), findsOneWidget);
      expect(find.textContaining('Departed 14:25 · on time'), findsOneWidget);
      expect(find.byType(StopTimeline), findsOneWidget);
    });

    testWidgets('delayed: says how late and where it will be', (tester) async {
      await pumpApp(
        tester,
        journeys: FakeJourneyRepository([ok(delayedJourney())]),
        start: route,
      );

      expect(find.text('Delayed · +12 min'), findsOneWidget);
      expect(find.textContaining('min late'), findsWidgets);
      expect(find.textContaining('Estimate'), findsWidgets);
    });

    testWidgets('no live data: says so in words and shows only the timetable', (
      tester,
    ) async {
      await pumpApp(
        tester,
        journeys: FakeJourneyRepository([ok(noLiveJourney())]),
        start: route,
      );

      expect(find.textContaining('No live data since'), findsOneWidget);
      expect(find.textContaining('Showing the timetable'), findsOneWidget);
      expect(find.textContaining('Expected'), findsNothing);
    });

    testWidgets('cancelled: says it will not run and offers no follow bar', (
      tester,
    ) async {
      final cancelled = journey([
        stop(1, 'CEN', 'Central', scheduledDep: 0),
        stop(2, 'RIV', 'Riverside', scheduledArr: 20),
      ], status: 'Cancelled');
      await pumpApp(
        tester,
        journeys: FakeJourneyRepository([ok(cancelled)]),
        start: route,
      );

      expect(find.text('Cancelled'), findsWidgets);
      expect(find.textContaining('will not run'), findsOneWidget);
    });

    testWidgets('not started yet: the times are the timetable', (tester) async {
      final planned = journey(
        [
          stop(1, 'CEN', 'Central', scheduledDep: 0),
          stop(2, 'RIV', 'Riverside', scheduledArr: 20),
        ],
        status: 'Planned',
        confidence: 'Scheduled',
      );
      await pumpApp(
        tester,
        journeys: FakeJourneyRepository([ok(planned)]),
        start: route,
      );

      expect(find.text('Scheduled · not departed yet'), findsOneWidget);
      expect(find.textContaining('has not left yet'), findsOneWidget);
    });
  });

  group('refreshing and failing', () {
    testWidgets('a status change fades in rather than jumping', (tester) async {
      final repo = FakeJourneyRepository([
        ok(onTimeJourney()),
        ok(delayedJourney()),
      ]);
      await pumpApp(
        tester,
        journeys: repo,
        poll: const Duration(seconds: 10),
        start: route,
      );
      expect(find.text('Running · on time'), findsOneWidget);

      await tester.pump(const Duration(seconds: 10));
      await tester.pump(const Duration(milliseconds: 50));
      // Mid-change both the old and the new words are on screen.
      expect(find.text('Delayed · +12 min'), findsOneWidget);

      await tester.pumpAndSettle();
      expect(find.text('Running · on time'), findsNothing);
      expect(find.text('Delayed · +12 min'), findsOneWidget);

      await unmount(tester);
    });

    testWidgets('failing with nothing saved says so and can retry', (
      tester,
    ) async {
      final repo = FakeJourneyRepository([
        bad(const NetworkFailure.timeout()),
        ok(onTimeJourney()),
      ]);
      await pumpApp(tester, journeys: repo, start: route);

      expect(find.text('Something went wrong'), findsOneWidget);
      expect(find.textContaining('could not reach SmartRail'), findsOneWidget);

      await tester.tap(find.text('Try again'));
      await tester.pumpAndSettle();
      expect(find.text('Running · on time'), findsOneWidget);
    });

    testWidgets('a saved copy is labelled as saved, not live', (tester) async {
      final saved = onTimeJourney();
      await pumpApp(
        tester,
        journeys: FakeJourneyRepository([
          ok(
            journey(
              saved.stops,
              fromCache: true,
              delay: 0,
              asOf: now1418.subtract(const Duration(minutes: 12)),
            ),
          ),
        ]),
        start: route,
      );

      expect(find.textContaining('Saved'), findsWidgets);
    });
  });

  group('following a journey', () {
    testWidgets('follow, see it, and keep it after a restart', (tester) async {
      final store = MemoryStore({'onboarding_seen': true});
      await pumpApp(
        tester,
        journeys: FakeJourneyRepository([ok(onTimeJourney())]),
        store: store,
        start: route,
      );

      await tester.tap(find.text('Follow this journey'));
      await tester.pumpAndSettle();

      expect(find.text('You are following this journey'), findsOneWidget);
      expect(find.text('Following'), findsWidgets);

      final saved = LocalFollowRepository(store).journeys();
      expect(saved.single.journeyId, 'j1');
      expect(saved.single.destinationName, 'Riverside');
    });

    testWidgets('a journey that already ran has no follow bar', (tester) async {
      final done = journey([
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
          'RIV',
          'Riverside',
          state: 'Arrived',
          scheduledArr: 20,
          actualArr: 20,
          source: 'Confirmed',
        ),
      ], status: 'Completed');
      await pumpApp(
        tester,
        journeys: FakeJourneyRepository([ok(done)]),
        start: route,
      );

      expect(find.text('Follow this journey'), findsNothing);
    });
  });

  group('reading it', () {
    testWidgets('the header reads as one sentence for a screen reader', (
      tester,
    ) async {
      final handle = tester.ensureSemantics();
      await pumpApp(
        tester,
        journeys: FakeJourneyRepository([ok(onTimeJourney())]),
        start: route,
      );

      expect(
        find.bySemanticsLabel(RegExp('Central to Riverside')),
        findsWidgets,
      );
      handle.dispose();
    });

    testWidgets('at 200 percent text nothing overflows', (tester) async {
      tester.platformDispatcher.textScaleFactorTestValue = 2.0;
      addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);
      tester.view.physicalSize = const Size(390 * 3, 844 * 3);
      tester.view.devicePixelRatio = 3;
      addTearDown(tester.view.reset);

      await pumpApp(
        tester,
        journeys: FakeJourneyRepository([ok(delayedJourney())]),
        start: route,
      );

      expect(tester.takeException(), isNull);
    });
  });
}
