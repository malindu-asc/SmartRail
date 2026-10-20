import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../core/time/clock_providers.dart';
import '../../../../core/widgets/failure_view.dart';
import '../../../../l10n/app_localizations.dart';
import '../../domain/duty_domain.dart';
import '../providers/duty_providers.dart';

/// Today's duty (designs 02, 18, 19, 39, 40) with the Active and Past switch.
class DutyScreen extends ConsumerStatefulWidget {
  const DutyScreen({super.key});

  @override
  ConsumerState<DutyScreen> createState() => _DutyScreenState();
}

class _DutyScreenState extends ConsumerState<DutyScreen> {
  bool _past = false;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      appBar: SmartRailAppBar(title: Text(l10n.dutyTitle), showBack: false),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
            child: SegmentedPill(
              left: l10n.dutyActive,
              right: l10n.dutyPast,
              rightSelected: _past,
              onChanged: (past) => setState(() => _past = past),
            ),
          ),
          Expanded(child: _past ? const _PastDuties() : const _TodayDuties()),
        ],
      ),
    );
  }
}

class _TodayDuties extends ConsumerWidget {
  const _TodayDuties();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final duty = ref.watch(myDutyProvider);

    // A refresh that failed keeps the last good duty on screen; only a first load that fails shows the failure.
    final shown = duty.value;
    if (shown == null) {
      return duty.when(
        data: (_) => const SizedBox.shrink(),
        loading: () => const LoadingList(),
        error: (error, _) => FailureView(
          error: error,
          onRetry: () => ref.invalidate(myDutyProvider),
        ),
      );
    }

    final duties = shown.duties
        .where((d) => d.status != DutyStatus.past)
        .toList();

    return RefreshIndicator(
      onRefresh: () async => ref.invalidate(myDutyProvider),
      child: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          if (duty.hasError) ...[
            InfoBanner(
              kind: BannerKind.warning,
              title: l10n.errorTitle,
              detail: l10n.errorNetwork,
            ),
            const SizedBox(height: 12),
          ],
          if (duties.isEmpty)
            EmptyState(
              icon: Icons.event_available_outlined,
              title: l10n.dutyNoneTitle,
              message: l10n.dutyNoneBody,
            ),
          for (final d in duties) ...[
            _DutyCard(duty: d),
            const SizedBox(height: 16),
          ],
        ],
      ),
    );
  }
}

class _DutyCard extends ConsumerStatefulWidget {
  const _DutyCard({required this.duty});

  final Duty duty;

  @override
  ConsumerState<_DutyCard> createState() => _DutyCardState();
}

class _DutyCardState extends ConsumerState<_DutyCard> {
  bool _busy = false;

  Future<void> _signOn() async {
    setState(() => _busy = true);
    final failure = await ref
        .read(dutyActionsProvider)
        .signOn(widget.duty.id);
    if (!mounted) return;
    setState(() => _busy = false);
    if (failure != null) _say(failure);
  }

  Future<void> _signOff() async {
    final l10n = AppLocalizations.of(context);
    final confirmed = await showSmartRailSheet<bool>(
      context,
      builder: (context) => SheetBody(
        children: [
          Text(
            l10n.signOffTitle,
            style: SmartRailType.title.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          Text(
            l10n.signOffBody,
            style: SmartRailType.body14.copyWith(
              color: SmartRailColors.textSecondary,
            ),
          ),
          SmartRailButton(
            label: l10n.signOffConfirm,
            kind: ButtonKind.danger,
            onPressed: () => Navigator.of(context).pop(true),
          ),
          SmartRailButton(
            label: l10n.cancel,
            kind: ButtonKind.secondary,
            onPressed: () => Navigator.of(context).pop(false),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() => _busy = true);
    final failure = await ref
        .read(dutyActionsProvider)
        .signOff(widget.duty.id);
    if (!mounted) return;
    setState(() => _busy = false);
    if (failure != null) _say(failure);
  }

  void _say(Failure failure) {
    final l10n = AppLocalizations.of(context);
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(
            failure is NetworkFailure ? l10n.errorNetwork : l10n.errorGeneric,
          ),
        ),
      );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.watch(networkClockProvider);
    final duty = widget.duty;

    final roster = duty.unitCode == null
        ? l10n.dutyRosterNoUnit(clock.formatTime(duty.from), clock.formatTime(duty.to))
        : l10n.dutyRoster(
            duty.unitCode!,
            clock.formatTime(duty.from),
            clock.formatTime(duty.to),
          );

    final signedOn = duty.status == DutyStatus.active;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Text(
          roster,
          style: SmartRailType.title.copyWith(
            color: SmartRailColors.textPrimary,
          ),
        ),
        if (signedOn && duty.signedOnAt != null)
          Padding(
            padding: const EdgeInsets.only(top: 4),
            child: Text(
              l10n.signedOnAt(clock.formatTime(duty.signedOnAt!)),
              style: SmartRailType.body14.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
          ),
        const SizedBox(height: 12),
        if (duty.needsReview) ...[
          InfoBanner(kind: BannerKind.warning, title: l10n.dutyNeedsReview),
          const SizedBox(height: 12),
        ],
        if (duty.status == DutyStatus.upcoming) ...[
          InfoBanner(
            kind: BannerKind.message,
            title: l10n.dutyUpcoming(clock.formatTime(duty.from)),
          ),
          const SizedBox(height: 12),
        ],
        for (final j in duty.journeys) ...[
          _JourneyCard(journey: j, enabled: signedOn),
          const SizedBox(height: 12),
        ],
        if (duty.canSignOn)
          SmartRailButton(
            label: l10n.signOnButton,
            onPressed: _busy ? null : _signOn,
          ),
        if (signedOn)
          SmartRailButton(
            label: l10n.signOffButton,
            kind: ButtonKind.secondary,
            onPressed: _busy ? null : _signOff,
          ),
      ],
    );
  }
}

class _JourneyCard extends ConsumerWidget {
  const _JourneyCard({required this.journey, required this.enabled});

  final DutyJourney journey;
  final bool enabled;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.watch(networkClockProvider);

    final status = switch (journey.status) {
      JourneyStatus.next => l10n.journeyNext,
      JourneyStatus.later => l10n.journeyLater,
      JourneyStatus.running => l10n.journeyRunning,
      JourneyStatus.done => l10n.journeyDone,
    };
    final arrives = clock.formatTime(journey.arrivesAt);
    final line = journey.platform == null
        ? l10n.journeyCardNoPlatform(journey.stops, arrives)
        : l10n.journeyCardLine(journey.platform!, journey.stops, arrives);
    final delay = journey.delayMinutes ?? 0;

    return Material(
      color: SmartRailColors.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(SmartRailRadius.lg),
        side: const BorderSide(color: SmartRailColors.border),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        // Only a journey of a duty the driver has signed on to can be opened for events.
        onTap: enabled
            ? () => context.push(AppRoutes.journey(journey.id))
            : null,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Text(
                    clock.formatTime(journey.departsAt),
                    style: SmartRailType.title.copyWith(
                      color: SmartRailColors.textPrimary,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      '${journey.origin} → ${journey.destination}',
                      style: SmartRailType.heading16.copyWith(
                        color: SmartRailColors.textPrimary,
                      ),
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  Text(
                    status,
                    style: SmartRailType.label14.copyWith(
                      color: SmartRailColors.primaryPressed,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              Text(
                '${journey.trainNumber} · $line',
                style: SmartRailType.body14.copyWith(
                  color: SmartRailColors.textSecondary,
                ),
              ),
              if (delay > 0) ...[
                const SizedBox(height: 8),
                StatusChip(
                  kind: StatusKind.delayed,
                  label: l10n.delayChip(delay),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}

class _PastDuties extends ConsumerWidget {
  const _PastDuties();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.watch(networkClockProvider);
    final history = ref.watch(dutyHistoryProvider);

    return history.when(
      loading: () => const LoadingList(),
      error: (error, _) => FailureView(
        error: error,
        onRetry: () => ref.invalidate(dutyHistoryProvider),
      ),
      data: (duties) {
        if (duties.isEmpty) {
          return EmptyState(
            icon: Icons.history,
            title: l10n.dutyHistoryEmpty,
          );
        }
        return ListView(
          padding: const EdgeInsets.all(16),
          children: [
            for (final d in duties) ...[
              RowsCard(
                children: [
                  Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '${clock.formatDate(d.from)} · ${d.unitCode == null ? l10n.dutyRosterNoUnit(clock.formatTime(d.from), clock.formatTime(d.to)) : l10n.dutyRoster(d.unitCode!, clock.formatTime(d.from), clock.formatTime(d.to))}',
                          style: SmartRailType.heading16.copyWith(
                            color: SmartRailColors.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          d.journeys
                              .map((j) => j.trainNumber)
                              .join(' · '),
                          style: SmartRailType.body14.copyWith(
                            color: SmartRailColors.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
            ],
          ],
        );
      },
    );
  }
}
