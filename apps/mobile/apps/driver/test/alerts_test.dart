import 'package:driver_app/app/locale_provider.dart';
import 'package:driver_app/features/messages/messages.dart';
import 'package:driver_app/features/messages/messages_screen.dart';
import 'package:driver_app/l10n/app_localizations.dart';
import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

Widget _app(List<AlertItem> alerts) => ProviderScope(
  overrides: [
    alertsProvider.overrideWith((ref) => Stream.value(alerts)),
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
    home: const MessagesScreen(),
  ),
);

AlertItem _direct({bool acked = false}) => AlertItem(
  id: 'm1',
  kind: AlertKind.direct,
  title: 'Platform 3 closed until 10:00',
  detail: 'Use platform 2 for all arrivals.',
  at: DateTime.utc(2026, 10, 21, 3),
  needsAck: !acked,
  acknowledgedAt: acked ? DateTime.utc(2026, 10, 21, 3, 5) : null,
);

AlertItem _network(String id, AlertSeverity severity) => AlertItem(
  id: id,
  kind: AlertKind.network,
  title: 'Signal fault at Mill Lane',
  detail: 'Expect delays.',
  at: DateTime.utc(2026, 10, 21, 3),
  severity: severity,
  places: 'Mill Lane',
);

void main() {
  testWidgets('both kinds show, and the switch moves between them', (tester) async {
    await tester.pumpWidget(
      _app([_direct(), _network('n1', AlertSeverity.severe)]),
    );
    await tester.pumpAndSettle();

    expect(find.text('Platform 3 closed until 10:00'), findsOneWidget);
    expect(find.text('Direct to you'), findsOneWidget);

    await tester.tap(find.textContaining('Network'));
    await tester.pumpAndSettle();

    expect(find.text('Signal fault at Mill Lane'), findsOneWidget);
    expect(find.text('Severe'), findsOneWidget);
  });

  testWidgets('a page with nothing in it says so instead of going blank', (tester) async {
    // Only a network alert: the Direct page, shown first, is empty.
    await tester.pumpWidget(_app([_network('n1', AlertSeverity.info)]));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.text('No messages for you from control.'), findsOneWidget);

    await tester.tap(find.textContaining('Network'));
    await tester.pumpAndSettle();
    expect(find.text('Signal fault at Mill Lane'), findsOneWidget);
  });

  testWidgets('nothing at all shows both empty messages', (tester) async {
    await tester.pumpWidget(_app(const []));
    await tester.pumpAndSettle();

    expect(tester.takeException(), isNull);
    expect(find.text('No messages for you from control.'), findsOneWidget);

    await tester.tap(find.textContaining('Network'));
    await tester.pumpAndSettle();
    expect(find.text('No network alerts right now.'), findsOneWidget);
  });
}
