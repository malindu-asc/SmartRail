import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';
import 'package:passenger_app/core/storage/key_value_store.dart';
import 'package:passenger_app/features/board/presentation/widgets/departure_row.dart';
import 'package:smartrail_core/smartrail_core.dart';

import 'support/support.dart';

void main() {
  group('the board (AC-5)', () {
    testWidgets(
      'each train shows its timetable time, what it is doing, and a chip with an icon, words and colour',
      (tester) async {
        final repo = FakeBoardRepository([
          ok(
            board([
              dep(
                id: 'a',
                scheduled: 0,
                delay: 0,
                name: 'Riverside',
                platform: '1',
              ),
              dep(
                id: 'b',
                scheduled: 15,
                expected: 27,
                delay: 12,
                name: 'Mill Lane',
                platform: '1',
              ),
              dep(
                id: 'c',
                scheduled: 6,
                status: 'Cancelled',
                name: 'Hill Town',
                platform: '4',
              ),
              dep(
                id: 'd',
                scheduled: 40,
                confidence: 'Unknown',
                ageSeconds: 720,
                name: 'Harbour',
                platform: null,
              ),
              dep(
                id: 'e',
                scheduled: 60,
                delay: 0,
                platformChanged: true,
                platform: '5',
                name: 'Oak Park',
              ),
              dep(
                id: 'f',
                scheduled: 90,
                status: 'Planned',
                confidence: 'Scheduled',
                name: 'Coast',
                platform: null,
              ),
            ]),
          ),
        ]);
        await pumpApp(tester, boards: repo, start: '/home/station/CEN');

        expect(
          find.text('Central'),
          findsOneWidget,
        ); // the app bar names the station

        // On time: 14:25, in 7 minutes, platform 1.
        expect(find.text('14:25'), findsOneWidget);
        expect(find.text('in 7 min · Platform 1'), findsOneWidget);
        expect(find.text('On time'), findsOneWidget);

        // Delayed: the timetable time stays, the expected time is written under it.
        expect(find.text('14:40'), findsOneWidget);
        expect(find.text('Expected 14:52 · Platform 1'), findsOneWidget);
        expect(find.text('+12 min'), findsOneWidget);

        // Cancelled: no expected time (design 07 draws the time as it is, with the Cancelled chip).
        expect(find.text('Cancelled'), findsOneWidget);

        // No live data: only the timetable, and since when.
        expect(find.text('No live data'), findsOneWidget);
        expect(find.text('Scheduled · no update since 14:06'), findsOneWidget);

        // Platform changed, and a train with no live claim at all (no chip).
        expect(find.text('Platform changed'), findsOneWidget);
        expect(find.text('Platform to be announced'), findsWidgets);

        // Status is icon plus words: each chip carries an icon.
        // Every chip is an icon (the file's own SVG) and words, never colour alone.
        for (final chip in tester.widgetList<StatusChip>(
          find.byType(StatusChip),
        )) {
          expect(
            find.descendant(
              of: find.byWidget(chip),
              matching: find.byType(SvgIcon),
            ),
            findsOneWidget,
            reason: chip.label,
          );
        }
        expect(find.byType(StatusChip), findsAtLeastNWidgets(4));
      },
    );

    testWidgets(
      'the freshness line says Live with the time, or Saved when the phone is offline (AC-6)',
      (tester) async {
        await pumpApp(
          tester,
          boards: FakeBoardRepository([
            ok(board([dep()])),
          ]),
          start: '/home/station/CEN',
        );
        expect(find.text('Live · updated 14:18'), findsOneWidget);

        await unmount(tester);
        final saved = board(
          [dep()],
          fromCache: true,
          fetchedAt: DateTime.utc(2026, 10, 14, 8, 36),
        ); // 14:06
        await pumpApp(
          tester,
          boards: FakeBoardRepository([ok(saved)]),
          start: '/home/station/CEN',
        );
        expect(find.text('Saved 14:06'), findsOneWidget);
        expect(find.text('Live · updated 14:18'), findsNothing);
      },
    );

    testWidgets('an empty board says so', (tester) async {
      await pumpApp(
        tester,
        boards: FakeBoardRepository([ok(board([]))]),
        start: '/home/station/CEN',
      );

      expect(find.text('No more trains today'), findsOneWidget);
    });

    testWidgets('the indicative-information note is on the board (AC-16)', (
      tester,
    ) async {
      await pumpApp(
        tester,
        boards: FakeBoardRepository([
          ok(board([dep()])),
        ]),
        start: '/home/station/CEN',
      );

      expect(
        find.text(
          'Times are provided for information. Listen for station announcements.',
        ),
        findsOneWidget,
      );
    });

    testWidgets(
      'the tab bar stays visible on the board and a row opens its journey',
      (tester) async {
        final harness = await pumpApp(
          tester,
          boards: FakeBoardRepository([
            ok(board([dep(id: 'j42')])),
          ]),
          start: '/home/station/CEN',
        );

        expect(find.byType(NavigationBar), findsOneWidget);

        await tester.tap(find.text('Riverside'));
        await tester.pumpAndSettle();

        expect(harness.location, '/home/journey/j42');
        expect(find.byType(NavigationBar), findsOneWidget);
      },
    );
  });

  group('failures and retries', () {
    testWidgets(
      'with nothing to show and no connection it says so in words and can retry',
      (tester) async {
        final repo = FakeBoardRepository([
          bad(const NetworkFailure.unreachable()),
          ok(board([dep()])),
        ]);
        await pumpApp(tester, boards: repo, start: '/home/station/CEN');

        expect(find.text('Something went wrong'), findsOneWidget);
        expect(
          find.text(
            'We could not reach SmartRail. Check your connection and try again.',
          ),
          findsOneWidget,
        );

        await tester.tap(find.text('Try again'));
        await tester.pumpAndSettle();

        expect(find.text('Riverside'), findsOneWidget);
      },
    );
  });

  group('polling (AC-7)', () {
    testWidgets(
      'refreshes every 10 seconds while visible, and stops when the screen is left',
      (tester) async {
        final repo = FakeBoardRepository([
          ok(board([dep()])),
        ]);
        final harness = await pumpApp(
          tester,
          boards: repo,
          poll: const Duration(seconds: 10),
          start: '/home/station/CEN',
        );
        expect(repo.asked, hasLength(1));

        await tester.pump(const Duration(seconds: 10));
        expect(repo.asked, hasLength(2));
        await tester.pump(const Duration(seconds: 10));
        expect(repo.asked, hasLength(3));

        harness.go('/plan');
        await tester.pumpAndSettle();
        await tester.pump(const Duration(seconds: 40));
        expect(
          repo.asked,
          hasLength(3),
          reason: 'nobody is looking at the board, so nothing is asked',
        );

        await unmount(tester);
      },
    );

    testWidgets('pauses in the background and refreshes at once on return', (
      tester,
    ) async {
      final repo = FakeBoardRepository([
        ok(board([dep()])),
      ]);
      await pumpApp(
        tester,
        boards: repo,
        poll: const Duration(seconds: 10),
        start: '/home/station/CEN',
      );
      expect(repo.asked, hasLength(1));

      // The framework only allows real transitions, so step through them as the OS does.
      for (final s in [
        AppLifecycleState.inactive,
        AppLifecycleState.hidden,
        AppLifecycleState.paused,
      ]) {
        tester.binding.handleAppLifecycleStateChanged(s);
      }
      await tester.pump(const Duration(seconds: 40));
      expect(repo.asked, hasLength(1), reason: 'the app is in the background');

      for (final s in [
        AppLifecycleState.hidden,
        AppLifecycleState.inactive,
        AppLifecycleState.resumed,
      ]) {
        tester.binding.handleAppLifecycleStateChanged(s);
      }
      await tester.pump();
      expect(repo.asked, hasLength(2), reason: 'coming back refreshes at once');

      await unmount(tester);
    });

    testWidgets('a refresh that fails keeps the last board on screen', (
      tester,
    ) async {
      final repo = FakeBoardRepository([
        ok(board([dep(name: 'Riverside')])),
        bad(const NetworkFailure.timeout()),
      ]);
      await pumpApp(
        tester,
        boards: repo,
        poll: const Duration(seconds: 10),
        start: '/home/station/CEN',
      );

      await tester.pump(const Duration(seconds: 10));
      await tester.pump();

      expect(repo.asked, hasLength(2));
      expect(find.text('Riverside'), findsOneWidget);
      expect(find.text('Something went wrong'), findsNothing);

      await unmount(tester);
    });

    testWidgets('pull to refresh asks again', (tester) async {
      final repo = FakeBoardRepository([
        ok(board([dep()])),
      ]);
      await pumpApp(tester, boards: repo, start: '/home/station/CEN');
      expect(repo.asked, hasLength(1));

      await tester.fling(
        find.byType(ListView).first,
        const Offset(0, 400),
        1000,
      );
      await tester.pumpAndSettle();

      expect(repo.asked, hasLength(2));
    });
  });

  group('following a station (AC-11)', () {
    testWidgets(
      'follow, see it say Following with an undo, and keep it after a restart',
      (tester) async {
        final store = MemoryStore({'onboarding_seen': true});
        final repo = FakeBoardRepository([
          ok(board([dep()])),
        ]);
        await pumpApp(
          tester,
          boards: repo,
          store: store,
          start: '/home/station/CEN',
        );

        await tester.tap(find.text('Follow station'));
        await tester.pumpAndSettle();
        expect(find.text('Following'), findsOneWidget);
        expect(find.text('You are following this station'), findsOneWidget);
        expect(
          (jsonDecode(store.getString('followed_stations')!) as List)
              .single['code'],
          'CEN',
        );

        await tester.tap(find.text('Undo'));
        await tester.pumpAndSettle();
        expect(find.text('Follow station'), findsOneWidget);
        expect(jsonDecode(store.getString('followed_stations')!), isEmpty);

        await tester.tap(find.text('Follow station'));
        await tester.pumpAndSettle();
        await unmount(tester);

        // A new start over the same storage still follows it.
        await pumpApp(
          tester,
          boards: repo,
          store: store,
          start: '/home/station/CEN',
        );
        expect(find.text('Following'), findsOneWidget);
      },
    );
  });

  group('text size and touch targets (AC-14)', () {
    testWidgets(
      'at 200 percent text the board lays out without overflow and rows are tall enough to touch',
      (tester) async {
        tester.platformDispatcher.textScaleFactorTestValue = 2.0;
        addTearDown(tester.platformDispatcher.clearTextScaleFactorTestValue);
        tester.view.physicalSize = const Size(390 * 3, 844 * 3);
        tester.view.devicePixelRatio = 3;
        addTearDown(tester.view.reset);

        final repo = FakeBoardRepository([
          ok(
            board([
              dep(
                id: 'a',
                scheduled: 15,
                expected: 27,
                delay: 12,
                name: 'Riverside via Mill Lane and Oak Park',
                platformChanged: true,
                platform: '5',
              ),
            ]),
          ),
        ]);
        await pumpApp(tester, boards: repo, start: '/home/station/CEN');

        expect(tester.takeException(), isNull);
        expect(
          tester.getSize(find.byType(DepartureRow).first).height,
          greaterThanOrEqualTo(48),
        );
      },
    );

    testWidgets(
      'a row is read as one sentence: time, destination, what it is doing',
      (tester) async {
        final handle = tester.ensureSemantics();
        await pumpApp(
          tester,
          boards: FakeBoardRepository([
            ok(
              board([
                dep(scheduled: 15, expected: 27, delay: 12, name: 'Mill Lane'),
              ]),
            ),
          ]),
          start: '/home/station/CEN',
        );

        final label = tester
            .getSemantics(find.byType(DepartureRow).first)
            .label;
        expect(label, contains('14:40'));
        expect(label, contains('Mill Lane'));
        expect(label, contains('Expected 14:52'));
        expect(label, contains('+12 min'));
        handle.dispose();
      },
    );
  });
}
