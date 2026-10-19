import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/polling/poller.dart';
import '../../../food/food.dart';
import '../../../../core/routing/app_routes.dart';
import '../../../../core/time/network_clock_provider.dart';
import '../../../../core/widgets/failure_view.dart';
import '../../../../core/widgets/freshness_line.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../follow/domain/follow_repository.dart';
import '../../../follow/presentation/providers/follow_providers.dart';
import '../../domain/entities/departure.dart';
import '../providers/board_providers.dart';
import '../widgets/departure_row.dart';

/// The live departures of one station (design 07). Refreshes every 10 seconds while it is on screen, and keeps showing the last board,
/// marked as saved, when the phone cannot reach the platform.
class DeparturesScreen extends ConsumerWidget {
  const DeparturesScreen({super.key, required this.stationCode});

  final String stationCode;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final board = ref.watch(boardProvider(stationCode));
    final snapshot = board.value;
    final follow = ref.watch(followProvider);
    final followed = follow.isStationFollowed(stationCode);

    return PollingScope(
      onTick: () => ref.read(boardProvider(stationCode).notifier).refresh(),
      child: Scaffold(
        appBar: SmartRailAppBar(
          title: Text(snapshot?.stationName ?? stationCode),
          actions: [
            if (ref.watch(menuProvider(stationCode)).value != null)
              IconButton(
                tooltip: l10n.foodLink,
                icon: const Icon(Icons.restaurant_outlined),
                onPressed: () => context.push(AppRoutes.food(stationCode)),
              ),
            if (snapshot != null)
              Padding(
                padding: const EdgeInsets.only(right: SmartRailSpacing.sm),
                child: _FollowButton(
                  followed: followed,
                  label: followed ? l10n.followingStation : l10n.followStation,
                  onPressed: () =>
                      _toggleFollow(context, ref, snapshot, followed),
                ),
              ),
          ],
        ),
        body: board.when(
          loading: () => const _LoadingBoard(),
          error: (error, _) => FailureView(
            error: error,
            onRetry: () => ref.invalidate(boardProvider(stationCode)),
          ),
          data: (data) => RefreshIndicator(
            onRefresh: () =>
                ref.read(boardProvider(stationCode).notifier).refresh(),
            child: _Board(snapshot: data),
          ),
        ),
      ),
    );
  }

  Future<void> _toggleFollow(
    BuildContext context,
    WidgetRef ref,
    BoardSnapshot snapshot,
    bool followed,
  ) async {
    final l10n = AppLocalizations.of(context);
    final notifier = ref.read(followProvider.notifier);
    final station = FollowedStation(
      code: snapshot.stationCode,
      name: snapshot.stationName,
    );

    if (followed) {
      await notifier.unfollowStation(station.code);
    } else {
      await notifier.followStation(station);
    }

    if (!context.mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(
            followed ? l10n.snackUnfollowedStation : l10n.snackFollowingStation,
          ),
          action: SnackBarAction(
            label: l10n.undo,
            onPressed: () => followed
                ? notifier.followStation(station)
                : notifier.unfollowStation(station.code),
          ),
        ),
      );
  }
}

class _Board extends ConsumerWidget {
  const _Board({required this.snapshot});

  final BoardSnapshot snapshot;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = clockFor(snapshot.timeZone);
    final now = ref.watch(nowProvider)();

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.only(bottom: SmartRailSpacing.xl),
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(
            SmartRailSpacing.lg,
            0,
            SmartRailSpacing.lg,
            SmartRailSpacing.sm,
          ),
          child: Align(
            alignment: Alignment.centerLeft,
            child: FreshnessLine(
              freshness: snapshot.fromCache ? Freshness.saved : Freshness.live,
              time: snapshot.fromCache ? snapshot.fetchedAt : snapshot.asOf,
              clock: clock,
            ),
          ),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(
            SmartRailSpacing.lg,
            SmartRailSpacing.sm,
            SmartRailSpacing.lg,
            SmartRailSpacing.sm,
          ),
          child: Text(
            l10n.departuresToday,
            style: SmartRailType.label14.copyWith(
              color: SmartRailColors.textSecondary,
            ),
          ),
        ),
        if (snapshot.items.isEmpty)
          Padding(
            padding: const EdgeInsets.only(top: SmartRailSpacing.xxl),
            child: EmptyState(
              icon: Icons.train_outlined,
              title: l10n.departuresEmptyTitle,
              message: l10n.departuresEmptyBody,
            ),
          )
        else
          Padding(
            padding: const EdgeInsets.symmetric(
              horizontal: SmartRailSpacing.lg,
            ),
            child: Card(
              clipBehavior: Clip.antiAlias,
              child: Column(
                children: [
                  for (var i = 0; i < snapshot.items.length; i++) ...[
                    if (i > 0) const Divider(),
                    AppearIn(
                      index: i,
                      child: DepartureRow(
                        key: ValueKey(snapshot.items[i].journeyId),
                        departure: snapshot.items[i],
                        clock: clock,
                        now: now,
                        asOf: snapshot.asOf,
                        onTap: () => context.push(
                          AppRoutes.journey(snapshot.items[i].journeyId),
                        ),
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ),
        Padding(
          padding: const EdgeInsets.fromLTRB(
            SmartRailSpacing.lg,
            SmartRailSpacing.lg,
            SmartRailSpacing.lg,
            0,
          ),
          child: Text(
            l10n.indicativeNote,
            style: Theme.of(context).textTheme.labelSmall?.copyWith(
              color: SmartRailColors.textTertiary,
            ),
          ),
        ),
      ],
    );
  }
}

class _LoadingBoard extends StatelessWidget {
  const _LoadingBoard();

  @override
  Widget build(BuildContext context) {
    return ListView(
      physics: const NeverScrollableScrollPhysics(),
      padding: const EdgeInsets.all(SmartRailSpacing.lg),
      children: [
        const Skeleton(width: 160, height: 14),
        const SizedBox(height: SmartRailSpacing.lg),
        for (var i = 0; i < 5; i++) ...[
          const Row(
            children: [
              Skeleton(width: 56, height: 24),
              SizedBox(width: SmartRailSpacing.lg),
              Expanded(child: Skeleton(height: 24)),
            ],
          ),
          const SizedBox(height: SmartRailSpacing.xl),
        ],
      ],
    );
  }
}

/// Words and a bell; with very large text only the bell is left (its words stay as the tooltip and for screen readers).
class _FollowButton extends StatelessWidget {
  const _FollowButton({
    required this.followed,
    required this.label,
    required this.onPressed,
  });

  final bool followed;
  final String label;
  final VoidCallback onPressed;

  @override
  Widget build(BuildContext context) {
    final icon = Icon(
      followed ? Icons.notifications_active : Icons.notifications_none,
      size: 20,
    );

    if (MediaQuery.textScalerOf(context).scale(14) > 22) {
      return IconButton(tooltip: label, onPressed: onPressed, icon: icon);
    }

    return TextButton.icon(
      onPressed: onPressed,
      icon: icon,
      label: ChangeSwitcher(child: Text(label, key: ValueKey(followed))),
    );
  }
}
