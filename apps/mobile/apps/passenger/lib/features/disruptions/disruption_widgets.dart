import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../core/polling/poller.dart';
import '../../core/routing/app_routes.dart';
import '../../core/time/network_clock_provider.dart';
import '../../core/widgets/failure_view.dart';
import '../../l10n/app_localizations.dart';
import '../follow/domain/follow_repository.dart';
import '../follow/presentation/providers/follow_providers.dart';
import 'disruptions.dart';

(StatusKind, String) severityChip(
  AppLocalizations l10n,
  DisruptionSeverity s,
) => switch (s) {
  DisruptionSeverity.minor => (StatusKind.changed, l10n.severityMinor),
  DisruptionSeverity.major => (StatusKind.delayed, l10n.severityMajor),
  DisruptionSeverity.severe => (StatusKind.cancelled, l10n.severitySevere),
};

/// The disruption banners at the top of Home (REQ-PAX-009): scope, severity, message and when it was updated. A retracted
/// disruption disappears on the next refresh.
class DisruptionBanners extends ConsumerWidget {
  const DisruptionBanners({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final disruptions = ref.watch(disruptionsProvider).value ?? const [];
    final clock = ref.watch(networkClockProvider);
    if (disruptions.isEmpty) return const SizedBox.shrink();

    return PollingScope(
      onTick: () => ref.read(disruptionsProvider.notifier).refresh(),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(
          SmartRailSpacing.lg,
          SmartRailSpacing.lg,
          SmartRailSpacing.lg,
          0,
        ),
        child: Column(
          children: [
            for (final d in disruptions.take(3))
              Padding(
                padding: const EdgeInsets.only(bottom: SmartRailSpacing.sm),
                child: InkWell(
                  borderRadius: BorderRadius.circular(SmartRailRadius.md),
                  onTap: () => context.push(AppRoutes.disruption(d.id)),
                  child: InfoBanner(
                    kind: BannerKind.warning,
                    title: d.title,
                    detail:
                        '${severityChip(l10n, d.severity).$2} · ${l10n.disruptionUpdated(clock.formatTime(d.updatedAt))}',
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

/// One disruption (design 34): the headline, severity, message, the stations and trains it touches, and "Follow updates".
class DisruptionScreen extends ConsumerWidget {
  const DisruptionScreen({super.key, required this.id});

  final String id;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final detail = ref.watch(disruptionDetailProvider(id));

    return Scaffold(
      appBar: SmartRailAppBar(title: Text(l10n.disruptionTitle)),
      body: detail.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        // Retracted or resolved while the passenger was looking: it is simply gone.
        error: (error, _) => error is NotFoundFailure
            ? EmptyState(
                icon: Icons.check_circle_outline,
                title: l10n.disruptionGone,
                message: l10n.disruptionGoneBody,
              )
            : FailureView(
                error: error,
                onRetry: () => ref.invalidate(disruptionDetailProvider(id)),
              ),
        data: (data) => _Body(detail: data),
      ),
    );
  }
}

class _Body extends ConsumerWidget {
  const _Body({required this.detail});

  final DisruptionDetail detail;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final d = detail.disruption;
    final clock = ref.watch(networkClockProvider);
    final (kind, label) = severityChip(l10n, d.severity);

    // Design 22: 32 Bold headline, severity chip with "Updated", the message in 16 Regular, two cards, and Follow updates at the end.
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
      children: [
        Text(
          d.title,
          style: SmartRailType.headline.copyWith(
            color: SmartRailColors.textPrimary,
          ),
        ),
        const SizedBox(height: 14),
        Row(
          children: [
            StatusChip(kind: kind, label: label),
            const SizedBox(width: 8),
            Text(
              l10n.disruptionUpdated(clock.formatTime(d.updatedAt)),
              style: SmartRailType.caption.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
          ],
        ),
        const SizedBox(height: 14),
        // Plain text, as the railway wrote it.
        Text(
          d.expectedEnd == null
              ? d.message
              : '${d.message} ${l10n.disruptionExpectedEnd(clock.formatTime(d.expectedEnd!))}',
          style: SmartRailType.body.copyWith(
            color: SmartRailColors.textPrimary,
          ),
        ),
        if (d.networkWide) ...[
          const SizedBox(height: 14),
          InfoBanner(
            kind: BannerKind.message,
            title: l10n.disruptionNetworkWide,
          ),
        ],
        if (d.stations.isNotEmpty) ...[
          const SizedBox(height: 14),
          Text(
            l10n.disruptionStations,
            style: SmartRailType.heading16.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          const SizedBox(height: 14),
          Card(
            child: Column(
              children: [
                for (var i = 0; i < d.stations.length; i++) ...[
                  if (i > 0)
                    const Divider(
                      height: 1,
                      thickness: 1,
                      color: SmartRailColors.border,
                    ),
                  Semantics(
                    button: true,
                    label: d.stations[i].name,
                    child: ExcludeSemantics(
                      child: InkWell(
                        onTap: () =>
                            context.push(AppRoutes.station(d.stations[i].code)),
                        child: Padding(
                          padding: const EdgeInsets.all(14),
                          child: Row(
                            children: [
                              const SvgIcon(
                                AppIcons.pin,
                                size: 18,
                                color: SmartRailColors.textSecondary,
                              ),
                              const SizedBox(width: 10),
                              Expanded(
                                child: Text(
                                  d.stations[i].name,
                                  style: SmartRailType.label14.copyWith(
                                    color: SmartRailColors.textPrimary,
                                  ),
                                ),
                              ),
                              const SvgIcon(
                                AppIcons.right,
                                size: 18,
                                color: SmartRailColors.textTertiary,
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ),
                ],
              ],
            ),
          ),
        ],
        if (detail.journeys.isNotEmpty) ...[
          const SizedBox(height: 14),
          Text(
            l10n.disruptionJourneys,
            style: SmartRailType.heading16.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          const SizedBox(height: 14),
          Card(
            child: Column(
              children: [
                for (var i = 0; i < detail.journeys.length; i++) ...[
                  if (i > 0)
                    const Divider(
                      height: 1,
                      thickness: 1,
                      color: SmartRailColors.border,
                    ),
                  _JourneyRow(journey: detail.journeys[i], clock: clock),
                ],
              ],
            ),
          ),
        ],
        if (d.stations.isNotEmpty) ...[
          const SizedBox(height: 24),
          SmartRailButton(
            label: l10n.disruptionFollow,
            onPressed: () => _follow(context, ref, d),
          ),
        ],
      ],
    );
  }

  Future<void> _follow(
    BuildContext context,
    WidgetRef ref,
    Disruption d,
  ) async {
    final l10n = AppLocalizations.of(context);
    final notifier = ref.read(followProvider.notifier);
    for (final s in d.stations) {
      await notifier.followStation(FollowedStation(code: s.code, name: s.name));
    }
    if (!context.mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(l10n.disruptionFollowed)));
  }
}

class _JourneyRow extends StatelessWidget {
  const _JourneyRow({required this.journey, required this.clock});

  final DisruptionJourney journey;
  final NetworkClock clock;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final late = (journey.delayMinutes ?? 0) >= 2;

    final (kind, label) = journey.isCancelled
        ? (StatusKind.cancelled, l10n.chipCancelled)
        : late
        ? (StatusKind.delayed, l10n.chipDelayed(journey.delayMinutes!))
        : (StatusKind.onTime, l10n.chipOnTime);

    final sub = late
        ? l10n.wordExpected(clock.formatTime(journey.expected))
        : (journey.platform == null ? '' : l10n.platformN(journey.platform!));

    return Semantics(
      button: true,
      label:
          '${clock.formatTime(journey.scheduled)} ${journey.destinationName}, $label',
      child: ExcludeSemantics(
        child: InkWell(
          onTap: () => context.push(AppRoutes.journey(journey.journeyId)),
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${clock.formatTime(journey.scheduled)} ${journey.destinationName}',
                        style: SmartRailType.label14.copyWith(
                          color: SmartRailColors.textPrimary,
                        ),
                      ),
                      if (sub.isNotEmpty) ...[
                        const SizedBox(height: 2),
                        Text(
                          sub,
                          style: SmartRailType.caption.copyWith(
                            color: SmartRailColors.textSecondary,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
                StatusChip(kind: kind, label: label),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
