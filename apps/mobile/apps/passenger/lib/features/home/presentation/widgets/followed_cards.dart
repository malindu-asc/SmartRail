import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/polling/poller.dart';
import '../../../../core/routing/app_routes.dart';
import '../../../../core/time/network_clock_provider.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../board/presentation/providers/board_providers.dart';
import '../../../board/presentation/widgets/departure_row.dart';
import '../../../follow/domain/follow_repository.dart';
import '../../../journeys/domain/entities/journey_detail.dart';
import '../../../journeys/presentation/providers/journey_providers.dart';

/// A followed station: its next departure and what that train is doing. Refreshes like the board itself.
class FollowedStationCard extends ConsumerWidget {
  const FollowedStationCard({super.key, required this.station});

  final FollowedStation station;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final board = ref.watch(boardProvider(station.code));
    final snapshot = board.value;
    final next = snapshot == null || snapshot.items.isEmpty
        ? null
        : snapshot.items.first;
    final clock = snapshot == null
        ? ref.watch(networkClockProvider)
        : clockFor(snapshot.timeZone);
    final now = ref.watch(nowProvider)();

    String subtitle = '';
    if (next != null) {
      final minutes = next.expected.difference(now).inMinutes;
      subtitle =
          '${clock.formatTime(next.scheduled)} ${next.destinationName}${minutes >= 0 && minutes <= 60 ? ' · ${l10n.inMinutes(minutes)}' : ''}';
    } else if (snapshot != null) {
      subtitle = l10n.departuresEmptyTitle;
    }

    final chip = next == null ? null : departureChip(l10n, next);

    return PollingScope(
      onTick: () => ref.read(boardProvider(station.code).notifier).refresh(),
      child: Card(
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: () => context.push(AppRoutes.station(station.code)),
          child: Padding(
            padding: const EdgeInsets.all(SmartRailSpacing.lg),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        station.name,
                        style: theme.textTheme.bodyLarge?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 2),
                      if (board.isLoading && snapshot == null)
                        const Skeleton(width: 140, height: 14)
                      else
                        Text(
                          subtitle,
                          style: theme.textTheme.labelSmall?.copyWith(
                            color: SmartRailColors.textSecondary,
                          ),
                        ),
                    ],
                  ),
                ),
                if (chip != null)
                  ChangeSwitcher(
                    child: StatusChip(
                      key: ValueKey(chip.label),
                      kind: chip.kind,
                      label: chip.label,
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

/// A followed journey: where it goes and what it is doing now.
class FollowedJourneyCard extends ConsumerWidget {
  const FollowedJourneyCard({super.key, required this.journey});

  final FollowedJourney journey;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final detail = ref.watch(journeyProvider(journey.journeyId)).value;
    final clock = detail == null
        ? ref.watch(networkClockProvider)
        : clockFor(detail.timeZone);

    final chip = detail == null ? null : journeyChip(l10n, detail);
    final title = l10n.journeyHeader(
      clock.formatTime(journey.scheduledDeparture),
      journey.destinationName,
    );

    return PollingScope(
      onTick: () =>
          ref.read(journeyProvider(journey.journeyId).notifier).refresh(),
      child: Card(
        clipBehavior: Clip.antiAlias,
        child: InkWell(
          onTap: () => context.push(AppRoutes.journey(journey.journeyId)),
          child: Padding(
            padding: const EdgeInsets.all(SmartRailSpacing.lg),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        style: theme.textTheme.bodyLarge?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      Text(
                        l10n.journeyTitle(
                          journey.originName,
                          journey.destinationName,
                        ),
                        style: theme.textTheme.labelSmall?.copyWith(
                          color: SmartRailColors.textSecondary,
                        ),
                      ),
                    ],
                  ),
                ),
                if (chip != null)
                  ChangeSwitcher(
                    child: StatusChip(
                      key: ValueKey(chip.label),
                      kind: chip.kind,
                      label: chip.label,
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

/// The chip for a journey's headline; none for a train that has not started (there is no live claim to make).
({StatusKind kind, String label})? journeyChip(
  AppLocalizations l10n,
  JourneyDetail journey,
) => switch (journey.headline) {
  JourneyHeadline.cancelled => (
    kind: StatusKind.cancelled,
    label: l10n.chipCancelled,
  ),
  JourneyHeadline.noLiveData => (
    kind: StatusKind.noLiveData,
    label: l10n.chipNoLiveData,
  ),
  JourneyHeadline.delayed => (
    kind: StatusKind.delayed,
    label: l10n.chipDelayed(journey.delayMinutes ?? 0),
  ),
  JourneyHeadline.runningOnTime || JourneyHeadline.completed => (
    kind: StatusKind.onTime,
    label: l10n.chipOnTime,
  ),
  JourneyHeadline.scheduled => null,
};
