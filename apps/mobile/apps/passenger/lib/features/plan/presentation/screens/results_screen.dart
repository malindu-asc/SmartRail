import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/polling/poller.dart';
import '../../../../core/routing/app_routes.dart';
import '../../../../core/time/network_clock_provider.dart';
import '../../../../core/widgets/failure_view.dart';
import '../../../../core/widgets/freshness_line.dart';
import '../../../../l10n/app_localizations.dart';
import '../../domain/entities/trip.dart';
import '../providers/plan_providers.dart';

/// The trains between two stations, soonest arrival first (design 11, v2 with the v1 extras): direct or one change, and a plain
/// warning when a change is at risk (REQ-PAX-006).
class ResultsScreen extends ConsumerWidget {
  const ResultsScreen({
    super.key,
    required this.from,
    required this.to,
    this.after,
  });

  final String from;
  final String to;
  final DateTime? after;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final query = TripQuery(from: from, to: to, after: after);
    final search = ref.watch(tripSearchProvider(query));
    final data = search.value;

    return PollingScope(
      onTick: () => ref.read(tripSearchProvider(query).notifier).refresh(),
      child: Scaffold(
        appBar: AppBar(
          title: Text(
            data == null ? '' : l10n.resultsTitle(data.fromName, data.toName),
          ),
        ),
        body: search.when(
          loading: () => const _Loading(),
          error: (error, _) => FailureView(
            error: error,
            onRetry: () => ref.invalidate(tripSearchProvider(query)),
          ),
          data: (result) => RefreshIndicator(
            onRefresh: () =>
                ref.read(tripSearchProvider(query).notifier).refresh(),
            child: _Options(result: result),
          ),
        ),
      ),
    );
  }
}

class _Options extends ConsumerWidget {
  const _Options({required this.result});

  final TripResult result;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = clockFor(result.timeZone);

    if (result.options.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: [
          const SizedBox(height: SmartRailSpacing.xxl),
          EmptyState(
            icon: Icons.train_outlined,
            title: l10n.resultsEmptyTitle,
            message: l10n.resultsEmptyBody,
          ),
        ],
      );
    }

    return ListView(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(
        SmartRailSpacing.lg,
        0,
        SmartRailSpacing.lg,
        SmartRailSpacing.xl,
      ),
      children: [
        Padding(
          padding: const EdgeInsets.only(bottom: SmartRailSpacing.md),
          child: Align(
            alignment: Alignment.centerLeft,
            child: FreshnessLine(
              freshness: result.fromCache ? Freshness.saved : Freshness.live,
              time: result.fromCache ? result.fetchedAt : result.fetchedAt,
              clock: clock,
            ),
          ),
        ),
        for (var i = 0; i < result.options.length; i++)
          Padding(
            padding: const EdgeInsets.only(bottom: SmartRailSpacing.md),
            child: AppearIn(
              index: i,
              child: OptionCard(option: result.options[i], clock: clock),
            ),
          ),
      ],
    );
  }
}

/// One way of getting there. A direct train opens its journey; a change opens out to show both trains and where to change.
class OptionCard extends StatelessWidget {
  const OptionCard({super.key, required this.option, required this.clock});

  final TripOption option;
  final NetworkClock clock;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final transfer = option.transfer;
    final first = option.legs.first;

    final (kind, label) = _state(l10n);
    final summary = [
      '${clock.formatTime(option.expectedDeparture)} to ${clock.formatTime(option.expectedArrival)}',
      l10n.resultsDuration(option.durationMinutes),
      option.isDirect ? l10n.resultsDirect : l10n.resultsChange,
      ?label,
    ].join(', ');

    return Card(
      clipBehavior: Clip.antiAlias,
      child: Semantics(
        container: true,
        label: summary,
        child: Column(
          children: [
            ExcludeSemantics(
              child: InkWell(
                onTap: option.isDirect
                    ? () => context.push(AppRoutes.journey(first.journeyId))
                    : null,
                child: Padding(
                  padding: const EdgeInsets.all(SmartRailSpacing.lg),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          _Times(option: option, clock: clock),
                          const Spacer(),
                          if (label != null)
                            ChangeSwitcher(
                              child: StatusChip(
                                key: ValueKey(label),
                                kind: kind,
                                label: label,
                              ),
                            ),
                        ],
                      ),
                      const SizedBox(height: SmartRailSpacing.sm),
                      Text(
                        [
                          l10n.resultsDuration(option.durationMinutes),
                          option.isDirect
                              ? l10n.resultsDirect
                              : l10n.resultsChangeAt(transfer!.stationName),
                          if (first.platform != null)
                            l10n.platformN(first.platform!),
                        ].join(' · '),
                        style: theme.textTheme.labelSmall?.copyWith(
                          color: SmartRailColors.textSecondary,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            if (option.isDirect)
              Padding(
                padding: const EdgeInsets.fromLTRB(
                  SmartRailSpacing.lg,
                  0,
                  SmartRailSpacing.lg,
                  SmartRailSpacing.md,
                ),
                child: SmartRailButton(
                  label: l10n.ticketsBuy,
                  kind: ButtonKind.tint,
                  icon: Icons.confirmation_number_outlined,
                  onPressed: () => context.push(
                    AppRoutes.chooseTicket(
                      first.fromCode,
                      first.toCode,
                      after: first.departure.subtract(
                        const Duration(minutes: 1),
                      ),
                    ),
                  ),
                ),
              ),
            if (transfer != null) ...[
              if (transfer.atRisk)
                Padding(
                  padding: const EdgeInsets.fromLTRB(
                    SmartRailSpacing.md,
                    0,
                    SmartRailSpacing.md,
                    SmartRailSpacing.md,
                  ),
                  child: InfoBanner(
                    kind: BannerKind.warning,
                    title: l10n.resultsAtRiskTitle,
                    detail: l10n.resultsAtRiskBody(
                      transfer.waitMinutes < 0 ? 0 : transfer.waitMinutes,
                      transfer.stationName,
                    ),
                  ),
                ),
              const Divider(height: 1),
              for (final leg in option.legs)
                _LegRow(leg: leg, clock: clock, transfer: transfer),
            ],
          ],
        ),
      ),
    );
  }

  (StatusKind, String?) _state(AppLocalizations l10n) {
    if (option.transfer?.atRisk ?? false) {
      return (StatusKind.changed, l10n.resultsAtRiskTitle);
    }
    final late = option.legs
        .map((l) => l.delayMinutes ?? 0)
        .fold<int>(0, (a, b) => a > b ? a : b);
    if (late >= 2) return (StatusKind.delayed, l10n.chipDelayed(late));
    if (option.legs.any((l) => l.hasNoLiveData)) {
      return (StatusKind.noLiveData, l10n.chipNoLiveData);
    }
    if (option.legs.every((l) => l.confidence == 'Live')) {
      return (StatusKind.onTime, l10n.chipOnTime);
    }
    return (StatusKind.onTime, null);
  }
}

class _Times extends StatelessWidget {
  const _Times({required this.option, required this.clock});

  final TripOption option;
  final NetworkClock clock;

  @override
  Widget build(BuildContext context) {
    final style = Theme.of(
      context,
    ).textTheme.titleLarge?.copyWith(fontFeatures: SmartRailType.timeFeatures);
    final changed =
        option.expectedDeparture != option.departure ||
        option.expectedArrival != option.arrival;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(clock.formatTime(option.expectedDeparture), style: style),
            const Padding(
              padding: EdgeInsets.symmetric(horizontal: SmartRailSpacing.sm),
              child: Icon(Icons.arrow_forward, size: 18),
            ),
            Text(clock.formatTime(option.expectedArrival), style: style),
          ],
        ),
        if (changed)
          Text(
            '${clock.formatTime(option.departure)} → ${clock.formatTime(option.arrival)}',
            style: Theme.of(context).textTheme.labelSmall?.copyWith(
              color: SmartRailColors.textTertiary,
              decoration: TextDecoration.lineThrough,
            ),
          ),
      ],
    );
  }
}

class _LegRow extends StatelessWidget {
  const _LegRow({
    required this.leg,
    required this.clock,
    required this.transfer,
  });

  final TripLeg leg;
  final NetworkClock clock;
  final TripTransfer transfer;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);

    return Semantics(
      button: true,
      label:
          '${l10n.resultsTrainLeg(leg.trainNumber, leg.fromName, leg.toName)}, ${clock.formatTime(leg.expectedDeparture)}',
      child: ExcludeSemantics(
        child: InkWell(
          onTap: () => context.push(AppRoutes.journey(leg.journeyId)),
          child: ConstrainedBox(
            constraints: const BoxConstraints(minHeight: 56),
            child: Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: SmartRailSpacing.lg,
                vertical: SmartRailSpacing.md,
              ),
              child: Row(
                children: [
                  Text(
                    clock.formatTime(leg.expectedDeparture),
                    style: theme.textTheme.titleMedium?.copyWith(
                      fontFeatures: SmartRailType.timeFeatures,
                    ),
                  ),
                  const SizedBox(width: SmartRailSpacing.md),
                  Expanded(
                    child: Text(
                      l10n.resultsTrainLeg(
                        leg.trainNumber,
                        leg.fromName,
                        leg.toName,
                      ),
                      style: theme.textTheme.bodyMedium,
                    ),
                  ),
                  const Icon(
                    Icons.chevron_right,
                    color: SmartRailColors.textTertiary,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _Loading extends StatelessWidget {
  const _Loading();

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(SmartRailSpacing.lg),
    children: [
      for (var i = 0; i < 3; i++)
        const Padding(
          padding: EdgeInsets.only(bottom: SmartRailSpacing.md),
          child: Skeleton(width: double.infinity, height: 88),
        ),
    ],
  );
}
