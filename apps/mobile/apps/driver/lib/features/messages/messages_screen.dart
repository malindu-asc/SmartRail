import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../core/time/clock_providers.dart';
import '../../core/widgets/driver_widgets.dart';
import '../../core/widgets/failure_view.dart';
import '../../l10n/app_localizations.dart';
import 'messages.dart';

/// Alerts from control (designs 10 and 09), with a switch between the two kinds: **Direct** (a message for this driver, to be
/// acknowledged) and **Network** (a notice control published for everyone). Only the Severe capsule is red.
class MessagesScreen extends ConsumerStatefulWidget {
  const MessagesScreen({super.key});

  @override
  ConsumerState<MessagesScreen> createState() => _MessagesScreenState();
}

class _MessagesScreenState extends ConsumerState<MessagesScreen> {
  // Page 0 is Direct, page 1 is Network. Swiping and the switch above move between them.
  final _pages = PageController();
  bool _network = false;

  @override
  void dispose() {
    _pages.dispose();
    super.dispose();
  }

  void _show(bool network) {
    setState(() => _network = network);
    _pages.animateToPage(
      network ? 1 : 0,
      duration: const Duration(milliseconds: 250),
      curve: Curves.easeOut,
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final alerts = ref.watch(alertsProvider);
    final seen = ref.watch(seenAlertsProvider);

    // How many of each kind are new, shown on the switch.
    final all = alerts.value ?? const <AlertItem>[];
    final newDirect = all.where((a) => a.kind == AlertKind.direct && _isNew(a, seen)).length;
    final newNetwork = all.where((a) => a.kind == AlertKind.network && _isNew(a, seen)).length;
    String label(String text, int count) => count > 0 ? '$text · $count' : text;

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      body: Column(
        children: [
          const SyncLine(),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
            child: Align(
              alignment: Alignment.centerLeft,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    l10n.messagesTitle,
                    style: SmartRailType.display.copyWith(
                      color: SmartRailColors.textPrimary,
                    ),
                  ),
                  Text(
                    l10n.messagesSub,
                    style: SmartRailType.body14.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                ],
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
            child: SegmentedPill(
              left: label(l10n.alertsTabDirect, newDirect),
              right: label(l10n.alertsTabNetwork, newNetwork),
              rightSelected: _network,
              onChanged: _show,
            ),
          ),
          Expanded(
            child: alerts.when(
              loading: () => const LoadingList(),
              error: (error, _) => FailureView(
                error: error,
                onRetry: () => ref.invalidate(alertsProvider),
              ),
              data: (everything) {
                Widget page(AlertKind kind, String empty) {
                  // New ones first, newest first; once read, an alert moves down below the new ones.
                  final items = everything.where((a) => a.kind == kind).toList()
                    ..sort((a, b) {
                      final byNew = (_isNew(b, seen) ? 1 : 0) - (_isNew(a, seen) ? 1 : 0);
                      return byNew != 0 ? byNew : b.at.compareTo(a.at);
                    });
                  // Always a list, so pull to refresh works on an empty page too.
                  return RefreshIndicator(
                    onRefresh: () async => ref.invalidate(alertsProvider),
                    // The empty page is itself a scrollable list, so pull to refresh still works on it.
                    child: items.isEmpty
                        ? StatusPage(
                            icon: Icons.notifications_none,
                            title: empty,
                          )
                        : ListView(
                            padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                            children: [
                              for (final a in items) ...[
                                _AlertCard(alert: a),
                                const SizedBox(height: 12),
                              ],
                            ],
                          ),
                  );
                }

                return PageView(
                  controller: _pages,
                  onPageChanged: (index) =>
                      setState(() => _network = index == 1),
                  children: [
                    page(AlertKind.direct, l10n.alertsEmptyDirect),
                    page(AlertKind.network, l10n.alertsEmptyNetwork),
                  ],
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

/// A direct message that has not been acknowledged, or a network alert that has not been opened.
bool _isNew(AlertItem a, Set<String> seen) =>
    a.kind == AlertKind.direct ? a.needsAck : !seen.contains(a.id);

String _severityLabel(AppLocalizations l10n, AlertSeverity s) => switch (s) {
  AlertSeverity.severe => l10n.severitySevere,
  AlertSeverity.major => l10n.severityMajor,
  AlertSeverity.info => l10n.severityMinor,
};

class _AlertCard extends ConsumerWidget {
  const _AlertCard({required this.alert});

  final AlertItem alert;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.watch(networkClockProvider);
    final seen = ref.watch(seenAlertsProvider);

    final direct = alert.kind == AlertKind.direct;
    final isNew = _isNew(alert, seen);

    // Where it comes from: a tag at the top of every alert.
    final source = direct
        ? ChipTag(
            label: l10n.alertDirect,
            tone: ChipTone.green,
            icon: Icons.person_outline,
          )
        : ChipTag(
            label: l10n.alertNetwork,
            tone: ChipTone.grey,
            icon: Icons.campaign_outlined,
          );

    final status = direct
        ? (alert.acknowledgedAt != null
              ? ChipTag(
                  label: l10n.messageAcked(
                    clock.formatTime(alert.acknowledgedAt!),
                  ),
                  tone: ChipTone.green,
                  icon: Icons.check,
                )
              : ChipTag(
                  label: l10n.messageDelivered,
                  tone: ChipTone.amber,
                  icon: Icons.schedule,
                ))
        : ChipTag(
            label: _severityLabel(l10n, alert.severity),
            tone: switch (alert.severity) {
              AlertSeverity.severe => ChipTone.red,
              AlertSeverity.major => ChipTone.amber,
              AlertSeverity.info => ChipTone.grey,
            },
            icon: alert.urgent
                ? Icons.warning_amber_rounded
                : Icons.info_outline,
          );

    return OutlinedCard(
      highlighted: isNew,
      onTap: () => _open(context, ref),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              source,
              const SizedBox(width: 8),
              Expanded(
                child: Text(
                  clock.formatTime(alert.at),
                  style: SmartRailType.body14.copyWith(
                    color: SmartRailColors.textSecondary,
                  ),
                ),
              ),
              status,
            ],
          ),
          const SizedBox(height: 10),
          // New ones are bold and read ones are regular, as in any inbox.
          Text(
            alert.title,
            style: SmartRailType.heading16.copyWith(
              color: SmartRailColors.textPrimary,
              fontWeight: isNew ? FontWeight.w800 : FontWeight.w400,
            ),
          ),
          if (alert.detail.isNotEmpty) ...[
            const SizedBox(height: 4),
            Text(
              alert.detail,
              maxLines: 2,
              overflow: TextOverflow.ellipsis,
              // Only the heading is bold; the description is always normal text.
              style: SmartRailType.body14.copyWith(
                color: SmartRailColors.textSecondary,
                fontWeight: FontWeight.w400,
              ),
            ),
          ],
        ],
      ),
    );
  }

  Future<void> _open(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.read(networkClockProvider);
    final direct = alert.kind == AlertKind.direct;

    // Opening a network alert is how it stops counting as new.
    if (!direct) ref.read(seenAlertsProvider.notifier).markSeen(alert.id);

    return showSmartRailSheet<void>(
      context,
      builder: (sheet) => SheetBody(
        children: [
          Row(
            children: [
              Icon(
                direct ? Icons.person_outline : Icons.campaign_outlined,
                size: 18,
                color: SmartRailColors.primaryPressed,
              ),
              const SizedBox(width: 8),
              Text(
                direct
                    ? l10n.messageSheetFrom(clock.formatTime(alert.at))
                    : l10n.alertSheetNetwork(clock.formatTime(alert.at)),
                style: SmartRailType.label14.copyWith(
                  color: SmartRailColors.primaryPressed,
                ),
              ),
            ],
          ),
          Text(
            alert.title,
            style: SmartRailType.title24.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          if (alert.detail.isNotEmpty)
            Text(
              alert.detail,
              style: SmartRailType.body.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
          if (!direct)
            Text(
              l10n.alertSheetPlaces(
                alert.places.isEmpty ? l10n.allStations : alert.places,
              ),
              style: SmartRailType.body14.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
          if (direct)
            Text(
              alert.acknowledgedAt != null
                  ? l10n.messageAcked(clock.formatTime(alert.acknowledgedAt!))
                  : l10n.messageWaiting(clock.formatTime(alert.at)),
              style: SmartRailType.body14.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
          if (direct && alert.needsAck)
            SmartRailButton(
              label: l10n.messageAckButton,
              onPressed: () async {
                await ref
                    .read(messagesRepositoryProvider)
                    .acknowledge(alert.id);
                ref.invalidate(alertsProvider);
                if (sheet.mounted) Navigator.of(sheet).pop();
              },
            )
          else
            SmartRailButton(
              label: l10n.close,
              kind: ButtonKind.secondary,
              onPressed: () => Navigator.of(sheet).pop(),
            ),
        ],
      ),
    );
  }
}
