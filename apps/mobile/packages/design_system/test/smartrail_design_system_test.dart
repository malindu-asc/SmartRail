import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

Widget host(Widget child, {ThemeData? theme, double textScale = 1, double width = 360}) => MaterialApp(
  theme: theme ?? SmartRailTheme.passenger(),
  home: MediaQuery(
    data: MediaQueryData(textScaler: TextScaler.linear(textScale), size: Size(width, 800)),
    child: Scaffold(body: SizedBox(width: width, child: Center(child: child))),
  ),
);

void main() {
  group('tokens match the Figma file SmartRail-HighFed (AC-8)', () {
    test('brand, text, background and border colours', () {
      expect(SmartRailColors.primary, const Color(0xFF118167));
      expect(SmartRailColors.primaryPressed, const Color(0xFF0B5F4B));
      expect(SmartRailColors.primarySubtle, const Color(0xFFE3F3EE));
      expect(SmartRailColors.onPrimary, const Color(0xFFFFFFFF));
      expect(SmartRailColors.textPrimary, const Color(0xFF0F1412));
      expect(SmartRailColors.textSecondary, const Color(0xFF4B5563));
      expect(SmartRailColors.textTertiary, const Color(0xFF6B7280));
      expect(SmartRailColors.surface, const Color(0xFFFFFFFF));
      expect(SmartRailColors.borderStrong, const Color(0xFFD1D5DB));
    });

    test('status colours: a text colour and a soft background each', () {
      expect(SmartRailColors.delayedText, const Color(0xFFB45309));
      expect(SmartRailColors.delayedBg, const Color(0xFFFEF3C7));
      expect(SmartRailColors.cancelledText, const Color(0xFFB91C1C));
      expect(SmartRailColors.cancelledBg, const Color(0xFFFEE2E2));
      expect(SmartRailColors.changedText, const Color(0xFF1D4ED8));
      expect(SmartRailColors.changedBg, const Color(0xFFDBEAFE));
      expect(SmartRailColors.staleText, const Color(0xFF4B5563));
      expect(SmartRailColors.staleBg, const Color(0xFFF1F5F9));
      expect(SmartRailColors.staleBorder, const Color(0xFF94A3B8));
    });

    test('radius and spacing: pill 999, md 12, space 12 and 24', () {
      expect(SmartRailRadius.pill, 999);
      expect(SmartRailRadius.md, 12);
      expect(SmartRailSpacing.md, 12);
      expect(SmartRailSpacing.xl, 24);
    });

    test('type styles are Inter with the sizes, weights and line heights of the file', () {
      expect(SmartRailType.fontFamily, 'Inter');
      expect(SmartRailType.button.fontSize, 16);
      expect(SmartRailType.button.fontWeight, FontWeight.w600);
      expect(SmartRailType.title20.fontSize, 20);
      expect(SmartRailType.title20.fontWeight, FontWeight.w700);
      expect(SmartRailType.body.fontSize, 16);
      expect(SmartRailType.body14.fontSize, 14);
      expect(SmartRailType.label14.fontWeight, FontWeight.w600);
      expect(SmartRailType.caption.fontSize, 12);
      expect(SmartRailType.caption.fontWeight, FontWeight.w500);
      expect(SmartRailType.button.height! * SmartRailType.button.fontSize!, closeTo(24, 0.001));
      expect(SmartRailType.caption.height! * SmartRailType.caption.fontSize!, closeTo(16, 0.001));
    });
  });

  group('StatusChip never relies on colour alone (AC-6, REQ-PAX-004)', () {
    for (final kind in StatusKind.values) {
      testWidgets('${kind.name} has an icon, words and a semantic label', (tester) async {
        final handle = tester.ensureSemantics();
        await tester.pumpWidget(host(StatusChip(kind: kind, label: 'label ${kind.name}')));

        expect(find.byIcon(kind.icon), findsOneWidget);
        expect(find.text('label ${kind.name}'), findsOneWidget);
        expect(tester.getSemantics(find.byType(StatusChip)).label, 'label ${kind.name}');
        handle.dispose();
      });
    }

    test('every status has its own icon, so two statuses never differ by colour only', () {
      expect(StatusKind.values.map((k) => k.icon).toSet().length, StatusKind.values.length);
    });

    test('on time is quiet: colour is reserved for exceptions', () {
      expect(StatusKind.onTime.background, isNull);
      expect(StatusKind.onTime.color, SmartRailColors.textPrimary);
      for (final kind in StatusKind.values.where((k) => k != StatusKind.onTime)) {
        expect(kind.background, isNotNull, reason: '${kind.name} is an exception and has a soft background');
      }
    });
  });

  group('buttons (Figma Button and Driver/Action button)', () {
    testWidgets('a passenger button is a 56 dp pill, the driver action is 72 dp', (tester) async {
      await tester.pumpWidget(host(SmartRailButton(label: 'Go', onPressed: () {})));
      expect(tester.getSize(find.byType(FilledButton)).height, 56);
      expect(tester.getSize(find.byType(FilledButton)).width, 360);   // full width

      await tester.pumpWidget(host(SmartRailButton(label: 'Arrived at stop', onPressed: () {}), theme: SmartRailTheme.driver()));
      await tester.pumpAndSettle();   // the theme change is animated
      expect(tester.getSize(find.byType(FilledButton)).height, 72);
    });

    testWidgets('the driver label is Title 20 Bold, the passenger label Button 16 Semi Bold', (tester) async {
      await tester.pumpWidget(host(SmartRailButton(label: 'Go', onPressed: () {})));
      expect(tester.widget<Text>(find.text('Go')).style, isNull);   // the style comes from the button theme
      expect(SmartRailTheme.passengerSpec.buttonLabel, SmartRailType.button);
      expect(SmartRailTheme.driverSpec.buttonLabel, SmartRailType.title20);
    });

    for (final kind in ButtonKind.values) {
      testWidgets('${kind.name} renders its label and calls onPressed', (tester) async {
        var pressed = 0;
        await tester.pumpWidget(host(SmartRailButton(label: kind.name, kind: kind, onPressed: () => pressed++)));

        await tester.tap(find.text(kind.name));
        expect(pressed, 1);
      });
    }

    testWidgets('a disabled button cannot be pressed', (tester) async {
      await tester.pumpWidget(host(const SmartRailButton(label: 'Go', onPressed: null)));

      expect(tester.widget<FilledButton>(find.byType(FilledButton)).onPressed, isNull);
    });
  });

  group('text scaling and minimum sizes (AC-7, REQ-PAX-014)', () {
    testWidgets('shared widgets lay out at 200 percent text on a narrow screen without overflow', (tester) async {
      await tester.pumpWidget(host(
        SingleChildScrollView(
          child: Column(
            children: [
              const StatusChip(kind: StatusKind.changed, label: 'Platform 5 (was 2)'),
              const StatusChip(kind: StatusKind.noLiveData, label: 'No live data'),
              const StalenessLabel(text: 'As of 14:31'),
              const OfflineBanner(title: 'You are offline', detail: 'Actions are saved and will send when signal returns.'),
              const InfoBanner(kind: BannerKind.warning, title: 'Delay ahead', detail: 'Signal fault at Mill Lane.'),
              const SmartRailTextField(label: 'Train number', hint: 'Enter value'),
              const EmptyState(icon: Icons.train, title: 'No departures', message: 'There are no more trains today.'),
              ErrorState(title: 'Something went wrong', message: 'Try again in a moment.', retryLabel: 'Try again', onRetry: () {}),
            ],
          ),
        ),
        textScale: 2,
        width: 320,
      ));

      expect(tester.takeException(), isNull);
    });

    testWidgets('the driver app never shows text under 14', (tester) async {
      await tester.pumpWidget(host(const StatusChip(kind: StatusKind.delayed, label: '+12 min'), theme: SmartRailTheme.driver()));

      expect(tester.widget<Text>(find.text('+12 min')).style!.fontSize, greaterThanOrEqualTo(14));
      expect(SmartRailTheme.driver().textTheme.labelSmall!.fontSize, greaterThanOrEqualTo(14));
    });

    test('the passenger touch target is at least 48 and the driver 72', () {
      expect(SmartRailTheme.passengerSpec.touchTarget, greaterThanOrEqualTo(48));
      expect(SmartRailTheme.driverSpec.touchTarget, 72);
      expect(SmartRailTheme.passengerSpec.buttonHeight, greaterThanOrEqualTo(SmartRailTheme.passengerSpec.touchTarget));
    });
  });

  group('banners, fields and states', () {
    testWidgets('a banner is a live region and reads its title and detail', (tester) async {
      final handle = tester.ensureSemantics();
      await tester.pumpWidget(host(const OfflineBanner(title: 'You are offline', detail: 'Last updated 14:18')));

      expect(tester.getSemantics(find.byType(OfflineBanner)).label, 'You are offline. Last updated 14:18');
      expect(find.byIcon(BannerKind.offline.icon), findsOneWidget);
      handle.dispose();
    });

    test('each banner kind has its own icon and colours', () {
      expect(BannerKind.values.map((k) => k.icon).toSet().length, BannerKind.values.length);
      expect(BannerKind.warning.background, SmartRailColors.delayedBg);
      expect(BannerKind.message.background, SmartRailColors.primarySubtle);
    });

    testWidgets('a text field has its label above it, never a placeholder alone', (tester) async {
      await tester.pumpWidget(host(const SmartRailTextField(label: 'Train number', hint: 'Enter value')));

      final label = tester.getTopLeft(find.text('Train number')).dy;
      final field = tester.getTopLeft(find.byType(TextField)).dy;
      expect(label, lessThan(field));
      expect(tester.getSize(find.byType(TextField)).height, greaterThanOrEqualTo(56));
    });

    testWidgets('the error state offers a retry only when it can retry, and calls it', (tester) async {
      var retried = 0;
      await tester.pumpWidget(host(ErrorState(title: 'Failed', message: 'Oops', retryLabel: 'Try again', onRetry: () => retried++)));

      await tester.tap(find.text('Try again'));
      expect(retried, 1);

      await tester.pumpWidget(host(const ErrorState(title: 'Failed', message: 'Oops')));
      expect(find.byType(FilledButton), findsNothing);
    });

    testWidgets("the staleness label shows the caller's text and a history icon", (tester) async {
      await tester.pumpWidget(host(const StalenessLabel(text: 'As of 14:31')));

      expect(find.text('As of 14:31'), findsOneWidget);
      expect(find.byIcon(Icons.history), findsOneWidget);
    });
  });
}
