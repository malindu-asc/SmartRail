import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/polling/poller.dart';
import '../../../../core/time/network_clock_provider.dart';
import '../../../../core/widgets/failure_view.dart';
import '../../../../core/widgets/freshness_line.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../follow/domain/follow_repository.dart';
import '../../../follow/presentation/providers/follow_providers.dart';
import '../../domain/entities/journey_detail.dart';
import '../providers/journey_providers.dart';
import '../../../food/food.dart';
import '../widgets/journey_widgets.dart';

/// One journey: the route at a glance, what it is doing, the platform, and every stop (designs 08, 09, 23 in the v2 layout).
/// Refreshes every 10 seconds while on screen; when the phone is offline it keeps the last journey and the shell says so.
class JourneyScreen extends ConsumerWidget {
  const JourneyScreen({super.key, required this.journeyId});

  final String journeyId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final journey = ref.watch(journeyProvider(journeyId));
    final data = journey.value;
    final clock = data == null ? null : clockFor(data.timeZone);

    return PollingScope(
      onTick: () => ref.read(journeyProvider(journeyId).notifier).refresh(),
      child: Scaffold(
        appBar: SmartRailAppBar(
          title: Text(
            data == null || clock == null || data.scheduledDeparture == null
                ? ''
                : l10n.journeyHeader(
                    clock.formatTime(data.scheduledDeparture!),
                    data.destinationName,
                  ),
          ),
        ),
        body: journey.when(
          loading: () => const _LoadingJourney(),
          error: (error, _) => FailureView(
            error: error,
            onRetry: () => ref.invalidate(journeyProvider(journeyId)),
          ),
          data: (detail) => RefreshIndicator(
            onRefresh: () =>
                ref.read(journeyProvider(journeyId).notifier).refresh(),
            child: _JourneyBody(journey: detail),
          ),
        ),
        bottomNavigationBar: data == null || data.isCompleted
            ? null
            : _FollowBar(journey: data),
      ),
    );
  }
}

class _JourneyBody extends StatelessWidget {
  const _JourneyBody({required this.journey});

  final JourneyDetail journey;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final clock = clockFor(journey.timeZone);
    final banner = journey.platformBannerStop;
    final delayed = journey.headline == JourneyHeadline.delayed;
    final next = journey.stops.where((s) => s.state == 'Pending').firstOrNull;

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(
        SmartRailSpacing.lg,
        SmartRailSpacing.sm,
        SmartRailSpacing.lg,
        SmartRailSpacing.xl,
      ),
      children: [
        if (journey.fromCache) ...[
          FreshnessLine(
            freshness: Freshness.saved,
            time: journey.fetchedAt,
            clock: clock,
          ),
          const SizedBox(height: SmartRailSpacing.md),
        ],
        AppearIn(
          index: 0,
          child: RouteHeader(journey: journey, clock: clock),
        ),
        const SizedBox(height: SmartRailSpacing.lg),
        AppearIn(
          index: 1,
          child: JourneyStatusCard(journey: journey, clock: clock),
        ),
        if (banner != null) ...[
          const SizedBox(height: SmartRailSpacing.md),
          PlatformBanner(stop: banner),
        ],
        const SizedBox(height: SmartRailSpacing.md),
        AppearIn(
          index: 2,
          child: StopTimeline(journey: journey, clock: clock),
        ),
        const SizedBox(height: SmartRailSpacing.md),
        JourneyFood(stops: journey.stops, clock: clock),
        if (delayed && next != null) ...[
          const SizedBox(height: SmartRailSpacing.md),
          _Note(
            icon: Icons.info_outline,
            text: l10n.estimatesNote(next.stationName),
          ),
        ],
        const SizedBox(height: SmartRailSpacing.md),
        Text(
          l10n.indicativeNote,
          style: Theme.of(
            context,
          ).textTheme.labelSmall?.copyWith(color: SmartRailColors.textTertiary),
        ),
      ],
    );
  }
}

class _Note extends StatelessWidget {
  const _Note({required this.icon, required this.text});

  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(SmartRailSpacing.md),
      decoration: BoxDecoration(
        color: SmartRailColors.bgSoft,
        borderRadius: BorderRadius.circular(SmartRailRadius.md),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Icon(icon, size: 18, color: SmartRailColors.textSecondary),
          const SizedBox(width: SmartRailSpacing.sm),
          Expanded(
            child: Text(
              text,
              style: Theme.of(context).textTheme.labelSmall?.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _FollowBar extends ConsumerWidget {
  const _FollowBar({required this.journey});

  final JourneyDetail journey;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final followed = ref
        .watch(followProvider)
        .isJourneyFollowed(journey.journeyId);

    return DecoratedBox(
      decoration: const BoxDecoration(
        color: SmartRailColors.surface,
        border: Border(top: BorderSide(color: SmartRailColors.border)),
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
          child: SmartRailButton(
            label: followed ? l10n.followingJourney : l10n.followJourney,
            kind: followed ? ButtonKind.secondary : ButtonKind.primary,
            icon: followed ? Icons.check : null,
            onPressed: () => _toggle(context, ref, followed),
          ),
        ),
      ),
    );
  }

  Future<void> _toggle(
    BuildContext context,
    WidgetRef ref,
    bool followed,
  ) async {
    final l10n = AppLocalizations.of(context);
    final notifier = ref.read(followProvider.notifier);
    final saved = FollowedJourney(
      journeyId: journey.journeyId,
      trainNumber: journey.trainNumber,
      originName: journey.originName,
      destinationName: journey.destinationName,
      scheduledDeparture: journey.scheduledDeparture ?? journey.fetchedAt,
    );

    if (followed) {
      await notifier.unfollowJourney(journey.journeyId);
    } else {
      await notifier.followJourney(saved);
    }

    if (!context.mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(
            followed ? l10n.snackUnfollowedJourney : l10n.snackFollowingJourney,
          ),
          action: SnackBarAction(
            label: l10n.undo,
            onPressed: () => followed
                ? notifier.followJourney(saved)
                : notifier.unfollowJourney(journey.journeyId),
          ),
        ),
      );
  }
}

class _LoadingJourney extends StatelessWidget {
  const _LoadingJourney();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.all(SmartRailSpacing.lg),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Skeleton(height: 44),
          SizedBox(height: SmartRailSpacing.lg),
          Skeleton(height: 76),
          SizedBox(height: SmartRailSpacing.md),
          Skeleton(height: 220),
        ],
      ),
    );
  }
}
