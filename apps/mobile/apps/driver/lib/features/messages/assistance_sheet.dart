import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../core/time/clock_providers.dart';
import '../../core/widgets/driver_widgets.dart';
import '../../l10n/app_localizations.dart';
import '../duty/domain/duty_domain.dart';
import 'messages.dart';

/// Request assistance (designs 12 to 14): choose what is happening, confirm, and see that control has it. It does not change the
/// journey, and it is never the safety channel: the sheet says to use the railway radio in an emergency.
Future<void> startAssistance(
  BuildContext context,
  WidgetRef ref,
  DriverJourney journey,
) async {
  final l10n = AppLocalizations.of(context);
  final clock = ref.read(networkClockProvider);

  final types = [
    l10n.assistMedical,
    l10n.assistOvercrowding,
    l10n.assistUnsafe,
    l10n.assistOther,
  ];

  final type = await showSmartRailSheet<String>(
    context,
    builder: (sheet) {
      var chosen = 0;
      return StatefulBuilder(
        builder: (context, setSheet) => SheetBody(
          children: [
            SheetText(title: l10n.assistTitle, body: l10n.assistBody),
            Column(
              children: [
                for (var i = 0; i < types.length; i++) ...[
                  OptionRow(
                    label: types[i],
                    selected: chosen == i,
                    onTap: () => setSheet(() => chosen = i),
                  ),
                  const SizedBox(height: 8),
                ],
              ],
            ),
            SmartRailButton(
              label: l10n.assistContinue,
              onPressed: () => Navigator.of(sheet).pop(types[chosen]),
            ),
            SmartRailButton(
              label: l10n.cancel,
              kind: ButtonKind.secondary,
              onPressed: () => Navigator.of(sheet).pop(),
            ),
          ],
        ),
      );
    },
  );
  if (type == null || !context.mounted) return;

  final first = journey.stops.firstOrNull?.scheduled;
  final name = first == null
      ? journey.trainNumber
      : '${clock.formatTime(first)} ${l10n.journeyRoute(journey.destination, journey.origin)}';

  final send = await showSmartRailSheet<bool>(
    context,
    builder: (sheet) => SheetBody(
      children: [
        SheetText(
          title: l10n.assistConfirmTitle,
          body: l10n.assistConfirmBody(name, type),
        ),
        SmartRailButton(
          label: l10n.assistSend,
          kind: ButtonKind.danger,
          onPressed: () => Navigator.of(sheet).pop(true),
        ),
        SmartRailButton(
          label: l10n.goBack,
          kind: ButtonKind.secondary,
          onPressed: () => Navigator.of(sheet).pop(false),
        ),
      ],
    ),
  );
  if (send != true || !context.mounted) return;

  final result = await ref
      .read(messagesRepositoryProvider)
      .requestAssistance(journeyId: journey.id, note: type);
  if (!context.mounted) return;

  if (result.isLeft()) {
    final failure = result.getLeft().toNullable();
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(
            failure is NetworkFailure ? l10n.errorNetwork : l10n.errorGeneric,
          ),
        ),
      );
    return;
  }

  await Navigator.of(context).push<void>(
    MaterialPageRoute(
      fullscreenDialog: true,
      builder: (_) => _RequestSent(
        type: type,
        journey: name,
        sentAt: clock.formatTime(DateTime.now()),
      ),
    ),
  );
}

class _RequestSent extends StatelessWidget {
  const _RequestSent({
    required this.type,
    required this.journey,
    required this.sentAt,
  });

  final String type;
  final String journey;
  final String sentAt;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    Widget row(String label, String value) => Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Text(
            label,
            style: SmartRailType.body14.copyWith(
              color: SmartRailColors.textSecondary,
            ),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Text(
              value,
              textAlign: TextAlign.end,
              style: SmartRailType.label14.copyWith(
                color: SmartRailColors.textPrimary,
              ),
            ),
          ),
        ],
      ),
    );

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      body: Column(
        children: [
          Expanded(
            child: SafeArea(
              bottom: false,
              child: StatusPage(
                icon: Icons.check,
                positive: true,
                title: l10n.assistSentTitle,
                body: l10n.assistSentBody,
                child: OutlinedCard(
                  child: Column(
                    children: [
                      row(l10n.assistType, type),
                      row(l10n.assistJourney, journey),
                      row(l10n.assistSent, sentAt),
                    ],
                  ),
                ),
              ),
            ),
          ),
          StickyActionBar(
            children: [
              SmartRailButton(
                label: l10n.backToJourney,
                onPressed: () => Navigator.of(context).pop(),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
