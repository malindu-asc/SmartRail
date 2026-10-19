import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../core/time/network_clock_provider.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../settings/presentation/providers/settings_providers.dart';
import '../../domain/alerts_domain.dart';
import '../providers/alerts_providers.dart';

/// The Alerts tab (design 33): platform changes, cancellations, delays and corrections for the journeys and stations followed.
class AlertsScreen extends ConsumerWidget {
  const AlertsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final alerts = ref.watch(alertsProvider);
    final on = ref.watch(alertsOnProvider);
    final clock = ref.watch(networkClockProvider);
    final now = ref.watch(nowProvider)();

    final today = clock.formatDate(now);
    final todays = alerts
        .where((a) => clock.formatDate(a.at) == today)
        .toList();
    final earlier = alerts
        .where((a) => clock.formatDate(a.at) != today)
        .toList();

    return SafeArea(
      child: ListView(
        padding: const EdgeInsets.all(SmartRailSpacing.xl),
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  l10n.alertsTitle,
                  style: Theme.of(context).textTheme.headlineLarge,
                ),
              ),
              if (alerts.isNotEmpty)
                TextButton(
                  onPressed: () => ref.read(alertsProvider.notifier).clear(),
                  child: Text(l10n.alertsClear),
                ),
            ],
          ),
          const SizedBox(height: SmartRailSpacing.lg),
          if (!on) InfoBanner(kind: BannerKind.offline, title: l10n.alertsOff),
          if (alerts.isEmpty && on)
            Padding(
              padding: const EdgeInsets.only(top: SmartRailSpacing.xxl),
              child: EmptyState(
                icon: Icons.notifications_none,
                title: l10n.alertsEmptyTitle,
                message: l10n.alertsEmptyBody,
              ),
            ),
          if (todays.isNotEmpty)
            _Group(title: l10n.alertsToday, items: todays, clock: clock),
          if (earlier.isNotEmpty)
            _Group(title: l10n.alertsEarlier, items: earlier, clock: clock),
          const SizedBox(height: SmartRailSpacing.md),
          Text(
            l10n.alertsNote,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
              color: SmartRailColors.textSecondary,
            ),
          ),
        ],
      ),
    );
  }
}

class _Group extends StatelessWidget {
  const _Group({required this.title, required this.items, required this.clock});

  final String title;
  final List<AlertItem> items;
  final NetworkClock clock;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: Theme.of(context).textTheme.labelLarge?.copyWith(
            color: SmartRailColors.textSecondary,
          ),
        ),
        const SizedBox(height: SmartRailSpacing.sm),
        Card(
          clipBehavior: Clip.antiAlias,
          child: Column(
            children: [
              for (var i = 0; i < items.length; i++) ...[
                if (i > 0) const Divider(height: 1),
                AlertRow(alert: items[i], clock: clock),
              ],
            ],
          ),
        ),
        const SizedBox(height: SmartRailSpacing.lg),
      ],
    );
  }
}

/// One alert: an icon and colour for its kind, its title, the time it was raised, and a plain sentence. Opens the journey.
class AlertRow extends StatelessWidget {
  const AlertRow({super.key, required this.alert, required this.clock});

  final AlertItem alert;
  final NetworkClock clock;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);

    final (icon, fg, bg) = switch (alert.kind) {
      AlertKind.platformChanged => (
        Icons.swap_horiz,
        SmartRailColors.changedText,
        SmartRailColors.changedBg,
      ),
      AlertKind.cancelled => (
        Icons.cancel_outlined,
        SmartRailColors.cancelledText,
        SmartRailColors.cancelledBg,
      ),
      AlertKind.delay => (
        Icons.schedule,
        SmartRailColors.delayedText,
        SmartRailColors.delayedBg,
      ),
      AlertKind.correction => (
        Icons.info_outline,
        SmartRailColors.staleText,
        SmartRailColors.staleBg,
      ),
    };

    final scheduled = clock.formatTime(alert.scheduledDeparture);
    final (title, body) = switch (alert.kind) {
      AlertKind.platformChanged => (
        l10n.alertPlatformTitle,
        alert.oldPlatform == null
            ? l10n.alertPlatformBodyNew(
                alert.originName,
                alert.destinationName,
                alert.newPlatform ?? '',
              )
            : l10n.alertPlatformBody(
                alert.originName,
                alert.destinationName,
                alert.newPlatform ?? '',
                alert.oldPlatform!,
              ),
      ),
      AlertKind.cancelled => (
        l10n.alertCancelledTitle,
        l10n.alertCancelledBody(
          scheduled,
          alert.originName,
          alert.destinationName,
        ),
      ),
      AlertKind.delay => (
        l10n.alertDelayTitle,
        l10n.alertDelayBody(
          alert.originName,
          alert.destinationName,
          clock.formatTime(alert.expectedAt ?? alert.scheduledDeparture),
          alert.minutes ?? 0,
        ),
      ),
      AlertKind.correction => (
        l10n.alertCorrectionTitle,
        l10n.alertCorrectionBody(
          scheduled,
          alert.originName,
          alert.destinationName,
        ),
      ),
    };

    return Semantics(
      button: true,
      label: '$title, ${clock.formatTime(alert.at)}. $body',
      child: ExcludeSemantics(
        child: InkWell(
          onTap: () => context.push(AppRoutes.journey(alert.journeyId)),
          child: Padding(
            padding: const EdgeInsets.all(SmartRailSpacing.lg),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                CircleAvatar(
                  radius: 20,
                  backgroundColor: bg,
                  child: Icon(icon, color: fg, size: 22),
                ),
                const SizedBox(width: SmartRailSpacing.md),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              title,
                              style: theme.textTheme.bodyLarge?.copyWith(
                                fontWeight: FontWeight.w700,
                              ),
                            ),
                          ),
                          Text(
                            clock.formatTime(alert.at),
                            style: theme.textTheme.labelSmall?.copyWith(
                              color: SmartRailColors.textSecondary,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 2),
                      Text(
                        body,
                        style: theme.textTheme.bodyMedium?.copyWith(
                          color: SmartRailColors.textSecondary,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
