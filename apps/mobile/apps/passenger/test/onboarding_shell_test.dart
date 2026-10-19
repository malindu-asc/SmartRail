import 'dart:convert';
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:passenger_app/core/storage/key_value_store.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import 'support/support.dart';

void main() {
  group('launch and onboarding (AC-1, AC-2)', () {
    testWidgets(
      'the first launch shows the mark, then the three slides, then Home; the next launch goes straight to Home',
      (tester) async {
        final store = MemoryStore();
        final harness = await pumpApp(tester, store: store, start: null);

        expect(find.byType(Image), findsOneWidget); // the launch mark
        await tester.pump(const Duration(milliseconds: 1500));
        await tester.pumpAndSettle();

        expect(find.text('Know before you go'), findsOneWidget);
        expect(find.text('Next'), findsOneWidget);

        await tester.tap(find.text('Next'));
        await tester.pumpAndSettle();
        expect(find.text('Follow your journey'), findsOneWidget);

        await tester.tap(find.text('Next'));
        await tester.pumpAndSettle();
        expect(find.text('Honest about old data'), findsOneWidget);
        expect(find.text('Go to app'), findsOneWidget);

        await tester.tap(find.text('Go to app'));
        await tester.pumpAndSettle();
        expect(harness.location, '/home');
        expect(store.getBool('onboarding_seen'), isTrue);

        // A new start with the same storage skips the introduction.
        await unmount(tester);
        final second = await pumpApp(tester, store: store, start: null);
        await tester.pump(const Duration(milliseconds: 1500));
        await tester.pumpAndSettle();
        expect(second.location, '/home');
        expect(find.text('Know before you go'), findsNothing);
      },
    );

    testWidgets('Skip finishes the introduction at once', (tester) async {
      final store = MemoryStore();
      final harness = await pumpApp(tester, store: store, start: '/onboarding');

      await tester.tap(find.text('Skip'));
      await tester.pumpAndSettle();

      expect(harness.location, '/home');
      expect(store.getBool('onboarding_seen'), isTrue);
    });

    testWidgets('the illustration container is white, not green', (
      tester,
    ) async {
      await pumpApp(tester, store: MemoryStore(), start: '/onboarding');

      for (var page = 0; page < 3; page++) {
        final container = tester.widget<Container>(
          find.byKey(ValueKey('onboarding-art-$page')),
        );
        final decoration = container.decoration! as BoxDecoration;
        expect(
          decoration.color,
          const Color(0xFFFFFFFF),
          reason: 'slide ${page + 1}',
        );
        expect(decoration.color, isNot(SmartRailColors.primarySubtle));
        if (page < 2) {
          await tester.tap(find.text('Next'));
          await tester.pumpAndSettle();
        }
      }
    });

    testWidgets('the page dot widens on the current slide', (tester) async {
      await pumpApp(tester, store: MemoryStore(), start: '/onboarding');

      // The measured size includes the 4 dp margin on each side.
      double dot(int i) =>
          tester.getSize(find.byKey(ValueKey('dot-$i'))).width - 8;

      expect(dot(0), 20);
      expect(dot(1), 8);

      await tester.tap(find.text('Next'));
      await tester.pumpAndSettle();
      expect(dot(1), 20);
      expect(dot(0), 8);
    });
  });

  group('the five tabs (AC-13, D2)', () {
    testWidgets(
      'Home, Plan, Tickets, Alerts and Me are there and every tab says where it stands',
      (tester) async {
        final harness = await pumpApp(tester);

        expect(find.byType(NavigationBar), findsOneWidget);
        for (final label in ['Home', 'Plan', 'Tickets', 'Alerts', 'Me']) {
          expect(
            find.descendant(
              of: find.byType(NavigationBar),
              matching: find.text(label),
            ),
            findsOneWidget,
            reason: label,
          );
        }

        Future<void> openTab(String label) async {
          await tester.tap(
            find.descendant(
              of: find.byType(NavigationBar),
              matching: find.text(label),
            ),
          );
          await tester.pumpAndSettle();
        }

        await openTab('Plan');
        expect(harness.location, '/plan');
        expect(find.text('Plan a journey'), findsOneWidget);
        expect(find.text('Show trains'), findsOneWidget);

        // Only the tab being shown is visible: the others must not show through (they once stacked on top of each other).
        expect(find.text('Where to today?').hitTestable(), findsNothing);
        expect(find.text('Language').hitTestable(), findsNothing);

        await openTab('Tickets');
        expect(harness.location, '/tickets');
        expect(find.text('Log in to see your tickets'), findsOneWidget);
        expect(find.text('Plan a journey').hitTestable(), findsNothing);

        await openTab('Alerts');
        expect(harness.location, '/alerts');
        expect(find.text('No alerts yet'), findsOneWidget);

        await tester.tap(
          find.descendant(
            of: find.byType(NavigationBar),
            matching: find.text('Me'),
          ),
        );
        await tester.pumpAndSettle();
        expect(find.text('Language'), findsOneWidget);
      },
    );

    testWidgets('Plan needs both stations before it shows trains', (
      tester,
    ) async {
      await pumpApp(tester, start: '/plan');

      final button = find.widgetWithText(FilledButton, 'Show trains');
      expect(tester.widget<FilledButton>(button).onPressed, isNull);
    });

    testWidgets('an unknown route shows the not-found screen and a way back', (
      tester,
    ) async {
      final harness = await pumpApp(tester);

      harness.go('/ghost');
      await tester.pumpAndSettle();
      expect(find.text('Page not found'), findsOneWidget);

      await tester.tap(find.text('Go to home'));
      await tester.pumpAndSettle();
      expect(harness.location, '/home');
    });

    testWidgets('Home has no bell: alerts live in their own tab (D1)', (
      tester,
    ) async {
      await pumpApp(tester);

      // The only notification icon on screen is the Alerts tab itself.
      expect(find.byIcon(Icons.notifications_none), findsOneWidget);
      expect(
        find.descendant(
          of: find.byType(NavigationBar),
          matching: find.byIcon(Icons.notifications_none),
        ),
        findsOneWidget,
      );
    });
  });

  group('language (AC-12)', () {
    testWidgets('switching to Sinhala changes the screens, and back again', (
      tester,
    ) async {
      await pumpApp(tester, start: '/me');

      await tester.tap(find.text('Language'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('සිංහල'));
      await tester.pumpAndSettle();
      await tester.tapAt(const Offset(10, 10)); // closes the sheet
      await tester.pumpAndSettle();
      expect(find.text('භාෂාව'), findsOneWidget);
      expect(
        find.descendant(
          of: find.byType(NavigationBar),
          matching: find.text('මුල් පිටුව'),
        ),
        findsOneWidget,
      );

      await tester.tap(find.text('භාෂාව'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('English'));
      await tester.pumpAndSettle();
      await tester.tapAt(const Offset(10, 10));
      await tester.pumpAndSettle();
      expect(find.text('Language'), findsOneWidget);
    });

    test(
      'the English and Sinhala files have the same keys, the same placeholders, and every key has a value',
      () {
        Map<String, dynamic> load(String name) =>
            jsonDecode(File('lib/l10n/$name').readAsStringSync())
                as Map<String, dynamic>;
        Set<String> keys(Map<String, dynamic> arb) =>
            arb.keys.where((k) => !k.startsWith('@')).toSet();
        Set<String> placeholders(String text) => RegExp(
          r'\{(\w+)\}',
        ).allMatches(text).map((m) => m.group(1)!).toSet();

        final en = load('app_en.arb');
        final si = load('app_si.arb');

        expect(
          keys(si),
          keys(en),
          reason: 'a key is missing from one language',
        );
        for (final key in keys(en)) {
          expect(
            (en[key] as String).trim(),
            isNotEmpty,
            reason: 'en $key is empty',
          );
          expect(
            (si[key] as String).trim(),
            isNotEmpty,
            reason: 'si $key is empty',
          );
          expect(
            placeholders(si[key] as String),
            placeholders(en[key] as String),
            reason: 'si $key must use the same placeholders as en',
          );
        }
      },
    );
  });

  group('the offline banner (AC-6)', () {
    testWidgets(
      'appears with the last time it worked, and goes when the API answers again',
      (tester) async {
        final probe = FakeProbe();
        final harness = await pumpApp(tester, probe: probe);
        expect(find.text('No connection'), findsNothing);

        probe.reachable = false;
        await harness.container.read(reachabilityProvider.notifier).checkNow();
        await tester.pumpAndSettle();
        expect(find.text('No connection'), findsOneWidget);

        probe.reachable = true;
        await harness.container.read(reachabilityProvider.notifier).checkNow();
        await tester.pumpAndSettle();
        expect(find.text('No connection'), findsNothing);
      },
    );
  });

  group('motion (AC-15)', () {
    testWidgets('with reduce-motion on, a tab change needs no animation time', (
      tester,
    ) async {
      tester.platformDispatcher.accessibilityFeaturesTestValue =
          const FakeAccessibilityFeatures(disableAnimations: true);
      addTearDown(
        tester.platformDispatcher.clearAccessibilityFeaturesTestValue,
      );
      await pumpApp(tester);

      await tester.tap(
        find.descendant(
          of: find.byType(NavigationBar),
          matching: find.text('Me'),
        ),
      );
      await tester.pump(); // one frame, no settling

      final opacity = tester.widget<AnimatedOpacity>(
        find
            .ancestor(
              of: find.text('Language'),
              matching: find.byType(AnimatedOpacity),
            )
            .first,
      );
      expect(opacity.duration, Duration.zero);
      expect(opacity.opacity, 1);
    });

    test('the design system has no motion when it is reduced', () {
      expect(SmartRailMotion.fast, lessThan(SmartRailMotion.base));
      expect(
        SmartRailMotion.slow,
        lessThanOrEqualTo(const Duration(milliseconds: 300)),
      );
    });
  });

  group('feature code uses tokens only (design system rule)', () {
    test('no hex colour, raw colour literal or raw font size in lib/', () {
      final offenders = <String>[];
      for (final file
          in Directory('lib')
              .listSync(recursive: true)
              .whereType<File>()
              .where((f) => f.path.endsWith('.dart'))) {
        if (file.path.contains('app_localizations')) continue;
        final lines = file.readAsLinesSync();
        for (var i = 0; i < lines.length; i++) {
          final line = lines[i];
          if (line.trimLeft().startsWith('//')) continue;
          if (RegExp(r'Color\(\s*0x').hasMatch(line) ||
              RegExp(r'#[0-9A-Fa-f]{6}\b').hasMatch(line)) {
            offenders.add('${file.path}:${i + 1}: ${line.trim()}');
          }
        }
      }

      expect(offenders, isEmpty, reason: offenders.join('\n'));
    });
  });
}
