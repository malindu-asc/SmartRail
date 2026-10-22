import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../core/time/clock_providers.dart';
import '../../../../core/widgets/driver_widgets.dart';
import '../../../../core/widgets/failure_view.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../messages/messages.dart';
import '../../domain/duty_domain.dart';
import '../providers/duty_providers.dart';

/// Today's duty (designs 02, 04, 18, 19, 24, 39, 40) with the Active and Past switch.
class DutyScreen extends ConsumerStatefulWidget {
  const DutyScreen({super.key});

  @override
  ConsumerState<DutyScreen> createState() => _DutyScreenState();
}

class _DutyScreenState extends ConsumerState<DutyScreen> {
  bool _past = false;
  bool _busy = false;

  Future<void> _signOn(Duty duty) async {
    setState(() => _busy = true);
    final failure = await ref.read(dutyActionsProvider).signOn(duty.id);
    if (!mounted) return;
    setState(() => _busy = false);
    if (failure != null) _say(failure);
  }

  Future<void> _signOff(Duty duty) async {
    final l10n = AppLocalizations.of(context);
    final allDone = duty.journeys.every((j) => j.status == JourneyStatus.done);

    final confirmed = await showSmartRailSheet<bool>(
      context,
      builder: (sheet) => SheetBody(
        children: [
          SheetText(
            title: l10n.signOffTitle,
            body: allDone ? l10n.signOffBody : l10n.signOffBodyUnfinished,
          ),
          SmartRailButton(
            label: l10n.signOffConfirm,
            onPressed: () => Navigator.of(sheet).pop(true),
          ),
          SmartRailButton(
            label: l10n.staySignedOn,
            kind: ButtonKind.secondary,
            onPressed: () => Navigator.of(sheet).pop(false),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;

    setState(() => _busy = true);
    final failure = await ref.read(dutyActionsProvider).signOff(duty.id);
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
    final now = ref.watch(nowProvider)();
    final duty = ref.watch(myDutyProvider);
    final current = duty.value?.current;

    final roster = current == null
        ? null
        : (current.unitCode == null
              ? l10n.dutyRosterNoUnit(
                  clock.formatTime(current.from),
                  clock.formatTime(current.to),
                )
              : l10n.dutyRoster(
                  clock.formatTime(current.from),
                  clock.formatTime(current.to),
                  current.unitCode!,
                ));

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      body: Column(
        children: [
          const SyncLine(),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (!_past)
                  Text(
                    DateFormat('EEEE d MMMM').format(clock.local(now)),
                    style: SmartRailType.body14.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                Text(
                  l10n.dutyTitle,
                  style: SmartRailType.display.copyWith(
                    color: SmartRailColors.textPrimary,
                  ),
                ),
                if (_past)
                  Text(
                    l10n.dutyHistorySub,
                    style: SmartRailType.body14.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  )
                else if (roster != null)
                  Text(
                    roster,
                    style: SmartRailType.body14.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                const SizedBox(height: 16),
                SegmentedPill(
                  left: l10n.dutyActive,
                  right: l10n.dutyPast,
                  rightSelected: _past,
                  onChanged: (past) => setState(() => _past = past),
                ),
              ],
            ),
          ),
          Expanded(
            child: _past
                ? const _PastDuties()
                : _Today(
                    busy: _busy,
                    onSignOn: _signOn,
                    onSignOff: _signOff,
                  ),
          ),
        ],
      ),
    );
  }
}

/// The Active tab: the notice, the journeys and the one button that moves the duty on.
class _Today extends ConsumerWidget {
  const _Today({
    required this.busy,
    required this.onSignOn,
    required this.onSignOff,
  });

  final bool busy;
  final Future<void> Function(Duty) onSignOn;
  final Future<void> Function(Duty) onSignOff;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.watch(networkClockProvider);
    final async = ref.watch(myDutyProvider);
    final alerts = ref.watch(alertsProvider).value ?? const [];
    final seen = ref.watch(seenAlertsProvider);

    // A first load that fails: say what the driver can do instead (design 18/40). A refresh that fails keeps the last duty.
    final shown = async.value;
    if (shown == null) {
      return async.when(
        data: (_) => const SizedBox.shrink(),
        loading: () => const LoadingList(),
        error: (error, _) => _Frame(
          bottom: SmartRailButton(
            label: l10n.retry,
            onPressed: () => ref.invalidate(myDutyProvider),
          ),
          child: ListView(
            padding: EdgeInsets.zero,
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 0),
                child: InfoBanner(
                  kind: BannerKind.warning,
                  title: l10n.dutyNotCurrentTitle,
                  detail: l10n.dutyNotCurrentBody,
                ),
              ),
              StatusPage(
                icon: Icons.schedule,
                title: l10n.dutyCantShowTitle,
                body: l10n.dutyCantShowBody,
              ),
            ],
          ),
        ),
      );
    }

    final duty = shown.current;
    if (duty == null) {
      return _Frame(
        bottom: SmartRailButton(
          label: l10n.refresh,
          kind: ButtonKind.secondary,
          onPressed: () => ref.invalidate(myDutyProvider),
        ),
        child: StatusPage(
          icon: Icons.assignment_turned_in_outlined,
          title: l10n.dutyNoneTitle,
          body: l10n.dutyNoneBody,
        ),
      );
    }

    // The newest alert that is still new (a message to acknowledge, or a network alert not yet opened) is the notice at the top.
    final notice = alerts
        .where(
          (a) => a.kind == AlertKind.direct ? a.needsAck : !seen.contains(a.id),
        )
        .firstOrNull;
    final signedOn = duty.status == DutyStatus.active;
    final next = duty.journeys
        .where(
          (j) =>
              j.status == JourneyStatus.next ||
              j.status == JourneyStatus.running,
        )
        .firstOrNull;

    Widget? action;
    if (duty.canSignOn) {
      action = SmartRailButton(
        label: l10n.signOnButton,
        onPressed: busy ? null : () => onSignOn(duty),
      );
    } else if (signedOn && next != null) {
      action = SmartRailButton(
        label: l10n.startJourney(clock.formatTime(next.departsAt)),
        onPressed: () => context.push(AppRoutes.journey(next.id)),
      );
    } else if (signedOn) {
      action = SmartRailButton(
        label: l10n.signOffButton,
        onPressed: busy ? null : () => onSignOff(duty),
      );
    }

    return _Frame(
      bottom: action,
      child: RefreshIndicator(
        onRefresh: () async => ref.invalidate(myDutyProvider),
        child: ListView(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
          children: [
            if (signedOn && duty.signedOnAt != null) ...[
              Align(
                alignment: Alignment.centerLeft,
                child: ChipTag(
                  label: l10n.signedOnChip(clock.formatTime(duty.signedOnAt!)),
                  tone: ChipTone.green,
                  icon: Icons.check,
                ),
              ),
              const SizedBox(height: 12),
            ],
            if (async.hasError) ...[
              InfoBanner(
                kind: BannerKind.warning,
                title: l10n.dutyNotCurrentTitle,
                detail: l10n.dutyNotCurrentBody,
              ),
              const SizedBox(height: 12),
            ],
            if (notice != null) ...[
              notice.urgent
                  ? _UrgentNotice(
                      title: notice.title,
                      detail: notice.detail,
                      label: l10n.noticeNetwork,
                    )
                  : InfoBanner(
                      kind: BannerKind.warning,
                      title: notice.kind == AlertKind.direct
                          ? l10n.noticeFromControl
                          : l10n.noticeNetwork,
                      detail: '${notice.title}. ${notice.detail}'.trim(),
                    ),
              const SizedBox(height: 12),
            ],
            if (duty.status == DutyStatus.upcoming) ...[
              InfoBanner(
                kind: BannerKind.message,
                title: l10n.dutyUpcoming(clock.formatTime(duty.from)),
              ),
              const SizedBox(height: 12),
            ],
            if (duty.needsReview) ...[
              InfoBanner(
                kind: BannerKind.warning,
                title: l10n.historyNeedsReview,
              ),
              const SizedBox(height: 12),
            ],
            for (final j in duty.journeys) ...[
              _JourneyCard(journey: j, enabled: signedOn),
              const SizedBox(height: 12),
            ],
          ],
        ),
      ),
    );
  }
}

/// A severe network alert at the top of the duty: red, so it is seen at once.
class _UrgentNotice extends StatelessWidget {
  const _UrgentNotice({
    required this.title,
    required this.detail,
    required this.label,
  });

  final String title;
  final String detail;
  final String label;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: SmartRailColors.cancelledText,
        borderRadius: BorderRadius.circular(SmartRailRadius.md),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Icon(Icons.warning_amber_rounded, color: Colors.white),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: SmartRailType.caption.copyWith(color: Colors.white),
                ),
                const SizedBox(height: 2),
                Text(
                  title,
                  style: SmartRailType.heading16.copyWith(color: Colors.white),
                ),
                if (detail.isNotEmpty)
                  Text(
                    detail,
                    style: SmartRailType.body14.copyWith(color: Colors.white),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

/// A body above a sticky action area, as in the designs (the button sits over the tab bar, under a thin line).
class _Frame extends StatelessWidget {
  const _Frame({required this.child, this.bottom});

  final Widget child;
  final Widget? bottom;

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Expanded(child: child),
        if (bottom != null)
          DecoratedBox(
            decoration: const BoxDecoration(
              color: SmartRailColors.surface,
              border: Border(top: BorderSide(color: SmartRailColors.border)),
            ),
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
              child: bottom,
            ),
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
    final late = journey.delayMinutes ?? 0;
    final arrives = clock.formatTime(journey.arrivesAt);

    final chip = switch (journey.status) {
      // One colour per state, so they are told apart at a glance: next is blue, running is green, done is dark, later is grey.
      JourneyStatus.next => ChipTag(
        label: l10n.journeyNext,
        tone: ChipTone.blue,
        icon: Icons.play_arrow_rounded,
      ),
      JourneyStatus.running => ChipTag(
        label: l10n.journeyRunning,
        tone: ChipTone.green,
        icon: Icons.train,
      ),
      JourneyStatus.done => ChipTag(
        label: l10n.journeyDone,
        tone: ChipTone.dark,
        icon: Icons.check,
      ),
      JourneyStatus.later => ChipTag(
        label: l10n.journeyLater,
        tone: ChipTone.grey,
        icon: Icons.schedule,
      ),
    };

    final line = switch (journey.status) {
      JourneyStatus.done =>
        late > 0
            ? l10n.journeyCardDoneLate(late, arrives)
            : l10n.journeyCardDone(arrives),
      _ =>
        journey.platform == null
            ? l10n.journeyCardNoPlatform(journey.stops, arrives)
            : l10n.journeyCardLine(journey.platform!, journey.stops, arrives),
    };

    return OutlinedCard(
      // Only a duty the driver has signed on to can have events recorded.
      onTap: enabled ? () => context.push(AppRoutes.journey(journey.id)) : null,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(
                  clock.formatTime(journey.departsAt),
                  style: SmartRailType.title24.copyWith(
                    color: SmartRailColors.textPrimary,
                  ),
                ),
              ),
              chip,
            ],
          ),
          const SizedBox(height: 4),
          Text(
            l10n.journeyRoute(journey.destination, journey.origin),
            style: SmartRailType.heading16.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            line,
            style: SmartRailType.body14.copyWith(
              color: SmartRailColors.textSecondary,
            ),
          ),
        ],
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
          return StatusPage(icon: Icons.history, title: l10n.dutyHistoryEmpty);
        }
        return ListView(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
          children: [
            for (final d in duties) ...[
              OutlinedCard(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: Text(
                            DateFormat('EEE d MMMM').format(clock.local(d.from)),
                            style: SmartRailType.heading16.copyWith(
                              color: SmartRailColors.textPrimary,
                            ),
                          ),
                        ),
                        d.needsReview
                            ? ChipTag(
                                label: l10n.historyNeedsReview,
                                tone: ChipTone.amber,
                                icon: Icons.warning_amber_rounded,
                              )
                            : ChipTag(
                                label: l10n.historySignedOff,
                                tone: ChipTone.green,
                                icon: Icons.check,
                              ),
                      ],
                    ),
                    const SizedBox(height: 6),
                    Text(
                      d.unitCode == null
                          ? l10n.dutyRosterNoUnit(
                              clock.formatTime(d.from),
                              clock.formatTime(d.to),
                            )
                          : l10n.dutyRoster(
                              clock.formatTime(d.from),
                              clock.formatTime(d.to),
                              d.unitCode!,
                            ),
                      style: SmartRailType.body14.copyWith(
                        color: SmartRailColors.textSecondary,
                      ),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      l10n.historyLine(d.journeys.length),
                      style: SmartRailType.body14.copyWith(
                        color: SmartRailColors.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 12),
            ],
          ],
        );
      },
    );
  }
}
