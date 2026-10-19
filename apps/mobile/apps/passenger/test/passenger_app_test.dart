import 'dart:convert';
import 'dart:io';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:passenger_app/app/app_overrides.dart';
import 'package:passenger_app/app/passenger_app.dart';
import 'package:passenger_app/core/routing/app_router.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

class FakeProbe implements ApiProbe {
  bool reachable = true;

  @override
  Future<bool> isReachable() async => reachable;
}

Future<ProviderContainer> pumpApp(WidgetTester tester, {FakeProbe? probe, DateTime Function()? now}) async {
  final container = ProviderContainer(overrides: [
    ...appOverrides,
    apiProbeProvider.overrideWithValue(probe ?? FakeProbe()),
    reachabilityIntervalProvider.overrideWithValue(Duration.zero),
    if (now != null) reachabilityClockProvider.overrideWithValue(now),
  ]);
  addTearDown(container.dispose);

  await tester.pumpWidget(UncontrolledProviderScope(container: container, child: const PassengerApp()));
  await tester.pumpAndSettle();
  return container;
}

void main() {
  group('the app boots to its home shell (AC-9)', () {
    testWidgets('shows the Home tab with the passenger theme', (tester) async {
      await pumpApp(tester);

      expect(find.text('Departures'), findsOneWidget);
      expect(find.byType(NavigationBar), findsOneWidget);
      expect(Theme.of(tester.element(find.byType(NavigationBar))).extension<SmartRailSpec>()!.buttonHeight, 56);
    });

    testWidgets('the three tabs navigate', (tester) async {
      await pumpApp(tester);

      await tester.tap(find.text('Trips'));
      await tester.pumpAndSettle();
      expect(find.text('My trips'), findsOneWidget);

      await tester.tap(find.text('Account'));
      await tester.pumpAndSettle();
      expect(find.text('Sign in to buy and show tickets.'), findsOneWidget);

      await tester.tap(find.text('Home'));
      await tester.pumpAndSettle();
      expect(find.text('Departures'), findsOneWidget);
    });

    testWidgets('an unknown route shows the not-found screen and a way back', (tester) async {
      final container = await pumpApp(tester);

      container.read(appRouterProvider).go('/ghost');
      await tester.pumpAndSettle();
      expect(find.text('Page not found'), findsOneWidget);

      await tester.tap(find.text('Go to departures'));
      await tester.pumpAndSettle();
      expect(find.text('Departures'), findsOneWidget);
    });
  });

  group('localization (AC-10, REQ-PAX-013)', () {
    testWidgets('switching to Sinhala changes the visible text, and back again', (tester) async {
      await pumpApp(tester);

      await tester.tap(find.text('Account'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('සිංහල'));
      await tester.pumpAndSettle();

      expect(find.text('ටිකට්පත් මිලදී ගැනීමට සහ පෙන්වීමට පිවිසෙන්න.'), findsOneWidget);
      expect(find.text('Sign in to buy and show tickets.'), findsNothing);

      await tester.tap(find.text('English'));
      await tester.pumpAndSettle();
      expect(find.text('Sign in to buy and show tickets.'), findsOneWidget);
    });

    test('the English and Sinhala files have the same keys and every key has a value', () {
      Map<String, dynamic> load(String name) => jsonDecode(File('lib/l10n/$name').readAsStringSync()) as Map<String, dynamic>;
      Set<String> keys(Map<String, dynamic> arb) => arb.keys.where((k) => !k.startsWith('@')).toSet();

      final en = load('app_en.arb');
      final si = load('app_si.arb');

      expect(keys(si), keys(en), reason: 'a key is missing from one language');
      for (final key in keys(en)) {
        expect((en[key] as String).trim(), isNotEmpty, reason: 'en $key is empty');
        expect((si[key] as String).trim(), isNotEmpty, reason: 'si $key is empty');
      }
    });
  });

  group('the offline banner (AC-11, REQ-NFR-006)', () {
    testWidgets('appears with the last time it worked when the API cannot be reached, and goes when it can', (tester) async {
      final probe = FakeProbe();
      // 02:00 UTC is 07:30 in the network time zone.
      final container = await pumpApp(tester, probe: probe, now: () => DateTime.utc(2026, 10, 19, 2, 0));
      expect(find.text('No connection'), findsNothing);

      probe.reachable = false;
      await container.read(reachabilityProvider.notifier).checkNow();
      await tester.pumpAndSettle();

      expect(find.text('No connection'), findsOneWidget);
      expect(find.text('Showing the last information from 07:30'), findsOneWidget);

      probe.reachable = true;
      await container.read(reachabilityProvider.notifier).checkNow();
      await tester.pumpAndSettle();
      expect(find.text('No connection'), findsNothing);
    });

    testWidgets('says that nothing has been received yet when the API never answered', (tester) async {
      final probe = FakeProbe()..reachable = false;
      final container = await pumpApp(tester, probe: probe);

      await container.read(reachabilityProvider.notifier).checkNow();
      await tester.pumpAndSettle();
      expect(find.text('No connection'), findsOneWidget);
      expect(find.text('No information has been received yet.'), findsOneWidget);
    });
  });

  group('feature code uses tokens only (AC-8)', () {
    test('no hex colour, raw colour literal or raw font size in lib/', () {
      final offenders = <String>[];
      for (final file in Directory('lib').listSync(recursive: true).whereType<File>().where((f) => f.path.endsWith('.dart'))) {
        // The generated localization files are not feature code.
        if (file.path.contains('app_localizations')) continue;
        final lines = file.readAsLinesSync();
        for (var i = 0; i < lines.length; i++) {
          final line = lines[i];
          if (line.trimLeft().startsWith('//')) continue;
          if (RegExp(r'Color\(\s*0x').hasMatch(line) || RegExp(r'#[0-9A-Fa-f]{6}\b').hasMatch(line) || line.contains('fontSize:')) {
            offenders.add('${file.path}:${i + 1}: ${line.trim()}');
          }
        }
      }

      expect(offenders, isEmpty, reason: offenders.join('\n'));
    });
  });
}
