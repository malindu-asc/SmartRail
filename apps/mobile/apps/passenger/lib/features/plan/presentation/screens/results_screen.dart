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
import '../../../tickets/domain/tickets_domain.dart';
import '../../../tickets/presentation/providers/tickets_providers.dart';
import '../../domain/entities/trip.dart';
import '../providers/plan_providers.dart';

/// What one train of a result costs, for the fare under the option. Null when it cannot be told (no fare shown then).
final legFareProvider = FutureProvider.autoDispose
    .family<
      num?,
      ({String from, String to, String journeyId, DateTime departure})
    >((ref, leg) async {
      final result = await ref
          .read(ticketsRepositoryProvider)
          .offers(
            OfferQuery(
              from: leg.from,
              to: leg.to,
              after: leg.departure.subtract(const Duration(minutes: 1)),
            ),
          );
      return result
          .toNullable()
          ?.offers
          .where((o) => o.journeyId == leg.journeyId)
          .firstOrNull
          ?.fare
          .total;
    });

/// The trains between two stations (design 35, v2): a green header with the route and a filter, a strip of days, and one card for each
/// way of getting there, soonest arrival first. A change that may be missed says so in words (REQ-PAX-006).
class ResultsScreen extends ConsumerStatefulWidget {
  const ResultsScreen({
    super.key,
    required this.from,
    required this.to,
    this.after,
    this.arriveBy,
  });

  final String from;
  final String to;
  final DateTime? after;

  /// "Arrive" mode: only trains that get there by this time (UTC), latest first.
  final DateTime? arriveBy;

  @override
  ConsumerState<ResultsScreen> createState() => _ResultsScreenState();
}

class _ResultsScreenState extends ConsumerState<ResultsScreen> {
  static const days = 14;

  int _day = 0;
  bool _directOnly = false;

  /// The start of day [offset] in the railway's zone as a UTC instant. Today starts "now" (or the time chosen on Plan).
  DateTime? _dayStartUtc(int offset) {
    if (offset == 0) return widget.after;
    final clock = ref.read(networkClockProvider);
    final local = clock.local(ref.read(nowProvider)());
    return DateTime.utc(
      local.year,
      local.month,
      local.day + offset,
    ).subtract(local.timeZoneOffset);
  }

  Future<void> _filter() async {
    final l10n = AppLocalizations.of(context);
    final direct = await showModalBottomSheet<bool>(
      context: context,
      useRootNavigator: true,
      showDragHandle: true,
      builder: (sheet) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(24, 0, 24, 24),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                l10n.resultsFilter,
                style: Theme.of(sheet).textTheme.titleLarge,
              ),
              SwitchListTile(
                contentPadding: EdgeInsets.zero,
                title: Text(l10n.resultsDirectOnly),
                value: _directOnly,
                onChanged: (v) => Navigator.of(sheet).pop(v),
              ),
            ],
          ),
        ),
      ),
    );
    if (direct != null && mounted) setState(() => _directOnly = direct);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.watch(networkClockProvider);
    final now = ref.watch(nowProvider)();

    // Arrive-by looks at the few hours before the wanted time, then keeps what arrives in time.
    final query = TripQuery(
      from: widget.from,
      to: widget.to,
      after:
          widget.arriveBy?.subtract(const Duration(hours: 4)) ??
          _dayStartUtc(_day),
    );
    final search = ref.watch(tripSearchProvider(query));
    final data = search.value;

    return PollingScope(
      onTick: () => ref.read(tripSearchProvider(query).notifier).refresh(),
      child: Scaffold(
        backgroundColor: SmartRailColors.page,
        body: Column(
          children: [
            // Design 35 header: brand colour, a back button, the route in white 18 Semi Bold, and the filter.
            ColoredBox(
              color: SmartRailColors.primary,
              child: SafeArea(
                bottom: false,
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(8, 4, 8, 8),
                  child: Row(
                    children: [
                      IconButton(
                        tooltip: MaterialLocalizations.of(
                          context,
                        ).backButtonTooltip,
                        onPressed: () => Navigator.of(context).maybePop(),
                        icon: const SvgIcon(
                          AppIcons.back,
                          size: 24,
                          color: SmartRailColors.onPrimary,
                        ),
                      ),
                      Expanded(
                        child: Text(
                          data == null
                              ? ''
                              : l10n.resultsTitle(data.fromName, data.toName),
                          style: SmartRailType.title.copyWith(
                            color: SmartRailColors.onPrimary,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      IconButton(
                        tooltip: l10n.resultsFilter,
                        onPressed: _filter,
                        icon: const SvgIcon(
                          AppIcons.filter,
                          size: 24,
                          color: SmartRailColors.onPrimary,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            _DateStrip(
              selected: _day,
              clock: clock,
              now: now,
              onSelect: (d) => setState(() => _day = d),
            ),
            const Divider(
              height: 1,
              thickness: 1,
              color: SmartRailColors.border,
            ),
            Expanded(
              child: search.when(
                loading: () => const _Loading(),
                error: (error, _) => FailureView(
                  error: error,
                  onRetry: () => ref.invalidate(tripSearchProvider(query)),
                ),
                data: (result) => RefreshIndicator(
                  onRefresh: () =>
                      ref.read(tripSearchProvider(query).notifier).refresh(),
                  child: _Options(
                    result: result,
                    arriveBy: widget.arriveBy,
                    directOnly: _directOnly,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// Two weeks from today, scrolling sideways: weekday in 12 Medium, date in 16 Semi Bold, and a 3 high bar under the chosen one.
class _DateStrip extends StatelessWidget {
  const _DateStrip({
    required this.selected,
    required this.clock,
    required this.now,
    required this.onSelect,
  });

  final int selected;
  final NetworkClock clock;
  final DateTime now;
  final ValueChanged<int> onSelect;

  static const _weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

  @override
  Widget build(BuildContext context) {
    final today = clock.local(now);

    return Padding(
      padding: const EdgeInsets.fromLTRB(8, 8, 8, 0),
      child: SingleChildScrollView(
        scrollDirection: Axis.horizontal,
        child: Row(
          children: [
            for (var i = 0; i < _ResultsScreenState.days; i++)
              SizedBox(
                width: 64,
                child: Builder(
                  builder: (context) {
                    final day = DateTime(
                      today.year,
                      today.month,
                      today.day + i,
                    );
                    final chosen = i == selected;
                    return Semantics(
                      button: true,
                      selected: chosen,
                      label: '${_weekdays[day.weekday - 1]} ${day.day}',
                      child: ExcludeSemantics(
                        child: InkWell(
                          onTap: () => onSelect(i),
                          child: Padding(
                            padding: const EdgeInsets.only(top: 8),
                            child: Column(
                              children: [
                                Text(
                                  _weekdays[day.weekday - 1],
                                  style: SmartRailType.caption.copyWith(
                                    color: SmartRailColors.textSecondary,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                Text(
                                  '${day.day}',
                                  style: SmartRailType.heading16.copyWith(
                                    color: chosen
                                        ? SmartRailColors.primary
                                        : SmartRailColors.textPrimary,
                                  ),
                                ),
                                const SizedBox(height: 2),
                                AnimatedContainer(
                                  duration: SmartRailMotion.of(
                                    context,
                                    SmartRailMotion.fast,
                                  ),
                                  height: 3,
                                  width: 40,
                                  decoration: BoxDecoration(
                                    color: chosen
                                        ? SmartRailColors.primary
                                        : Colors.transparent,
                                    borderRadius: BorderRadius.circular(2),
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _Options extends ConsumerWidget {
  const _Options({
    required this.result,
    this.arriveBy,
    this.directOnly = false,
  });

  final TripResult result;
  final DateTime? arriveBy;
  final bool directOnly;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = clockFor(result.timeZone);

    var options = result.options
        .where((o) => !directOnly || o.isDirect)
        .toList();
    if (arriveBy != null) {
      options =
          options.where((o) => !o.expectedArrival.isAfter(arriveBy!)).toList()
            ..sort((a, b) => b.expectedArrival.compareTo(a.expectedArrival));
    }

    if (options.isEmpty) {
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
      padding: const EdgeInsets.all(16),
      children: [
        if (result.fromCache) ...[
          Align(
            alignment: Alignment.centerLeft,
            child: FreshnessLine(
              freshness: Freshness.saved,
              time: result.fetchedAt,
              clock: clock,
            ),
          ),
          const SizedBox(height: 12),
        ],
        for (var i = 0; i < options.length; i++)
          Padding(
            padding: EdgeInsets.only(bottom: i == options.length - 1 ? 0 : 12),
            child: AppearIn(
              index: i,
              child: OptionCard(option: options[i], clock: clock),
            ),
          ),
      ],
    );
  }
}

/// One way of getting there, laid out as the Figma "Journey option": a line with the duration and state, the route with the train
/// between two 20 Bold times, the station names, a hairline, then the platform and the fare. A direct train opens its journey; a
/// change also lists both trains.
class OptionCard extends ConsumerWidget {
  const OptionCard({super.key, required this.option, required this.clock});

  final TripOption option;
  final NetworkClock clock;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final transfer = option.transfer;
    final first = option.legs.first;
    final last = option.legs.last;
    final (kind, label) = _state(l10n);

    // The fare of every train added up; shown once all of them are known.
    final fares = [
      for (final l in option.legs)
        ref.watch(
          legFareProvider((
            from: l.fromCode,
            to: l.toCode,
            journeyId: l.journeyId,
            departure: l.departure,
          )),
        ),
    ];
    final fare = fares.every((f) => f.hasValue && f.value != null)
        ? fares.fold<num>(0, (sum, f) => sum + f.value!)
        : null;

    final duration = option.isDirect
        ? l10n.chooseDirect(option.durationMinutes)
        : '${l10n.resultsDuration(option.durationMinutes)} · ${l10n.resultsChangeAt(transfer!.stationName)}';

    final summary = [
      '${clock.formatTime(option.expectedDeparture)} to ${clock.formatTime(option.expectedArrival)}',
      duration,
      ?label,
    ].join(', ');

    return Semantics(
      container: true,
      label: summary,
      child: Material(
        color: SmartRailColors.page,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(SmartRailRadius.lg),
          side: const BorderSide(color: SmartRailColors.border),
        ),
        clipBehavior: Clip.antiAlias,
        child: Column(
          children: [
            ExcludeSemantics(
              child: InkWell(
                onTap: () => context.push(AppRoutes.journey(first.journeyId)),
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.stretch,
                    children: [
                      Row(
                        children: [
                          Expanded(
                            child: Text(
                              duration,
                              style: SmartRailType.body14.copyWith(
                                color: SmartRailColors.textSecondary,
                              ),
                            ),
                          ),
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
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          Text(
                            clock.formatTime(option.expectedDeparture),
                            style: SmartRailType.title20.copyWith(
                              fontFeatures: SmartRailType.timeFeatures,
                              color: SmartRailColors.textPrimary,
                            ),
                          ),
                          const SizedBox(width: 8),
                          const Expanded(child: _Line()),
                          const SizedBox(width: 8),
                          const SvgIcon(
                            AppIcons.train,
                            size: 24,
                            color: SmartRailColors.primary,
                          ),
                          const SizedBox(width: 8),
                          const Expanded(child: _Line()),
                          const SizedBox(width: 8),
                          Text(
                            clock.formatTime(option.expectedArrival),
                            style: SmartRailType.title20.copyWith(
                              fontFeatures: SmartRailType.timeFeatures,
                              color: SmartRailColors.textPrimary,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 6),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Flexible(
                            child: Text(
                              first.fromName,
                              style: SmartRailType.body14.copyWith(
                                color: SmartRailColors.textPrimary,
                              ),
                            ),
                          ),
                          const SizedBox(width: 8),
                          Flexible(
                            child: Text(
                              last.toName,
                              textAlign: TextAlign.end,
                              style: SmartRailType.body14.copyWith(
                                color: SmartRailColors.textPrimary,
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      const Divider(
                        height: 1,
                        thickness: 1,
                        color: SmartRailColors.border,
                      ),
                      Padding(
                        padding: const EdgeInsets.only(top: 10),
                        child: Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Text(
                              first.platform == null
                                  ? ''
                                  : l10n.platformN(first.platform!),
                              style: SmartRailType.label14.copyWith(
                                color: SmartRailColors.textPrimary,
                              ),
                            ),
                            if (fare != null)
                              Text(
                                'LKR ${fare.round()}',
                                style: SmartRailType.heading16.copyWith(
                                  color: SmartRailColors.primary,
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
            if (option.isDirect)
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                child: SmartRailButton(
                  label: l10n.ticketsBuy,
                  kind: ButtonKind.tint,
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
                  padding: const EdgeInsets.fromLTRB(12, 0, 12, 12),
                  child: InfoBanner(
                    kind: BannerKind.warning,
                    title: l10n.resultsAtRiskTitle,
                    detail: l10n.resultsAtRiskBody(
                      transfer.waitMinutes < 0 ? 0 : transfer.waitMinutes,
                      transfer.stationName,
                    ),
                  ),
                ),
              const Divider(
                height: 1,
                thickness: 1,
                color: SmartRailColors.border,
              ),
              for (final leg in option.legs) _LegRow(leg: leg, clock: clock),
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
    return (StatusKind.onTime, l10n.chipOnTime);
  }
}

class _Line extends StatelessWidget {
  const _Line();

  @override
  Widget build(BuildContext context) => const SizedBox(
    height: 2,
    child: DecoratedBox(
      decoration: BoxDecoration(color: SmartRailColors.borderStrong),
    ),
  );
}

class _LegRow extends StatelessWidget {
  const _LegRow({required this.leg, required this.clock});

  final TripLeg leg;
  final NetworkClock clock;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

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
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              child: Row(
                children: [
                  Text(
                    clock.formatTime(leg.expectedDeparture),
                    style: SmartRailType.heading16.copyWith(
                      fontFeatures: SmartRailType.timeFeatures,
                      color: SmartRailColors.textPrimary,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      l10n.resultsTrainLeg(
                        leg.trainNumber,
                        leg.fromName,
                        leg.toName,
                      ),
                      style: SmartRailType.body14.copyWith(
                        color: SmartRailColors.textPrimary,
                      ),
                    ),
                  ),
                  const SvgIcon(
                    AppIcons.chevron,
                    size: 18,
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
    padding: const EdgeInsets.all(16),
    children: [
      for (var i = 0; i < 3; i++)
        const Padding(
          padding: EdgeInsets.only(bottom: 12),
          child: Skeleton(width: double.infinity, height: 150),
        ),
    ],
  );
}
