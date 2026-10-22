import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/time/clock_providers.dart';
import '../../../../core/widgets/driver_widgets.dart';
import '../../../../core/widgets/failure_view.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../messages/assistance_sheet.dart';
import '../../../messages/messages.dart';
import '../../domain/duty_domain.dart';
import '../providers/duty_providers.dart';

/// The journey (designs 05, 06, 07, 08, 20, 35): the next stop, one tap for arrived or departed, the delay reason and help.
class JourneyScreen extends ConsumerStatefulWidget {
  const JourneyScreen({super.key, required this.journeyId});

  final String journeyId;

  @override
  ConsumerState<JourneyScreen> createState() => _JourneyScreenState();
}

class _JourneyScreenState extends ConsumerState<JourneyScreen> {
  bool _busy = false;

  static const _reasonForControl = {
    'signal_fault': 'Signal fault',
    'door_fault': 'Door fault',
    'crew': 'Crew',
    'passenger_incident': 'Passenger incident',
    'track': 'Track',
    'other': 'Other',
  };

  String _eventLabel(AppLocalizations l10n, String type, String station) =>
      switch (type) {
        'arrived' => l10n.eventArrived(station),
        'departed' => l10n.eventDeparted(station),
        _ => l10n.eventSkipped(station),
      };

  Future<void> _send(DriverJourney journey, NextEvent event) async {
    final l10n = AppLocalizations.of(context);
    setState(() => _busy = true);
    final failure = await ref
        .read(dutyActionsProvider)
        .event(journey.id, event.stopSequence, event.type);
    if (!mounted) return;
    setState(() => _busy = false);

    if (failure == null) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(
          SnackBar(
            content: Text(
              l10n.eventSaved(_eventLabel(l10n, event.type, event.stationName)),
            ),
          ),
        );
      return;
    }
    await _explain(failure, journey, event);
  }

  Future<void> _explain(
    Failure failure,
    DriverJourney before,
    NextEvent attempted,
  ) async {
    final l10n = AppLocalizations.of(context);

    // The tap was for a stop the train is not at (design 35). Look at the journey again, say what is true, and offer the right tap.
    if (failure is ConflictFailure && failure.code == stopDoesNotMatchCode) {
      final fresh = (await ref.read(dutyRepositoryProvider).journey(before.id))
          .toNullable();
      if (!mounted) return;
      ref.invalidate(driverJourneyProvider(before.id));

      final here = fresh?.atStop ?? fresh?.nextStop;
      final right = fresh?.next;
      final attempt = _eventLabel(l10n, attempted.type, attempted.stationName);

      final record = await showSmartRailSheet<bool>(
        context,
        builder: (sheet) => SheetBody(
          children: [
            Row(
              children: [
                const Icon(
                  Icons.warning_amber_rounded,
                  size: 18,
                  color: SmartRailColors.delayedText,
                ),
                const SizedBox(width: 6),
                Text(
                  l10n.mismatchLabel,
                  style: SmartRailType.label14.copyWith(
                    color: SmartRailColors.delayedText,
                  ),
                ),
              ],
            ),
            SheetText(
              title: l10n.mismatchTitle,
              body: l10n.mismatchBody(attempt, here?.name ?? ''),
            ),
            if (right != null)
              SmartRailButton(
                label: l10n.mismatchRecord(
                  _eventLabel(l10n, right.type, right.stationName),
                ),
                onPressed: () => Navigator.of(sheet).pop(true),
              ),
            SmartRailButton(
              label: l10n.mismatchHold,
              kind: ButtonKind.secondary,
              onPressed: () => Navigator.of(sheet).pop(false),
            ),
          ],
        ),
      );
      if (record == true && right != null && fresh != null && mounted) {
        await _send(fresh, right);
      }
      return;
    }

    final text = failure is NetworkFailure
        ? l10n.errorNetwork
        : (failure is ForbiddenFailure
              ? l10n.notSignedOn
              : (failure is NotFoundFailure
                    ? l10n.journeyNotMine
                    : l10n.errorGeneric));
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(text)));
  }

  Future<void> _delayReason(DriverJourney journey) async {
    final l10n = AppLocalizations.of(context);
    final reasons = {
      'signal_fault': l10n.reasonSignalFault,
      'door_fault': l10n.reasonDoorFault,
      'crew': l10n.reasonCrew,
      'passenger_incident': l10n.reasonPassengerIncident,
      'track': l10n.reasonTrack,
      'other': l10n.reasonOther,
    };

    final code = await showSmartRailSheet<String>(
      context,
      builder: (sheet) {
        var chosen = journey.delayReason;
        return StatefulBuilder(
          builder: (context, setSheet) => SheetBody(
            children: [
              SheetText(
                title: l10n.delayReasonTitle,
                body: l10n.delayReasonBody,
              ),
              Column(
                children: [
                  for (final entry in reasons.entries) ...[
                    OptionRow(
                      label: entry.value,
                      selected: chosen == entry.key,
                      onTap: () => setSheet(() => chosen = entry.key),
                    ),
                    const SizedBox(height: 8),
                  ],
                ],
              ),
              SmartRailButton(
                label: l10n.saveReason,
                onPressed: chosen == null
                    ? null
                    : () => Navigator.of(sheet).pop(chosen),
              ),
            ],
          ),
        );
      },
    );
    if (code == null || !mounted) return;

    final failure = await ref
        .read(dutyActionsProvider)
        .delayReason(journey.id, code);
    if (!mounted) return;
    if (failure != null) {
      await _explain(
        failure,
        journey,
        journey.next ?? NextEvent(stopSequence: 0, stationName: '', type: ''),
      );
    } else {
      // Control reads this in "Driver reports" and decides whether passengers should be told. Always in English, as control reads it.
      await ref
          .read(messagesRepositoryProvider)
          .requestAssistance(
            journeyId: journey.id,
            note: 'Delay reason: ${_reasonForControl[code] ?? code}',
          );
      if (!mounted) return;
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(l10n.actionSaved)));
    }
  }

  Future<void> _skip(DriverJourney journey) async {
    final l10n = AppLocalizations.of(context);
    final next = journey.next;
    if (next == null) return;

    final confirmed = await showSmartRailSheet<bool>(
      context,
      builder: (sheet) {
        var reason = 0;
        final reasons = [
          l10n.skipPlatformClosed,
          l10n.skipInstructed,
          l10n.skipSafety,
        ];
        return StatefulBuilder(
          builder: (context, setSheet) => SheetBody(
            children: [
              SheetText(
                title: l10n.skipTitle(next.stationName),
                body: l10n.skipBody,
              ),
              Column(
                children: [
                  for (var i = 0; i < reasons.length; i++) ...[
                    OptionRow(
                      label: reasons[i],
                      selected: reason == i,
                      onTap: () => setSheet(() => reason = i),
                    ),
                    const SizedBox(height: 8),
                  ],
                ],
              ),
              SmartRailButton(
                label: l10n.skipConfirm,
                kind: ButtonKind.danger,
                onPressed: () => Navigator.of(sheet).pop(true),
              ),
              SmartRailButton(
                label: l10n.cancel,
                kind: ButtonKind.secondary,
                onPressed: () => Navigator.of(sheet).pop(false),
              ),
            ],
          ),
        );
      },
    );
    if (confirmed == true && mounted) {
      await _send(
        journey,
        NextEvent(
          stopSequence: next.stopSequence,
          stationName: next.stationName,
          type: 'skipped',
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.watch(networkClockProvider);
    final async = ref.watch(driverJourneyProvider(widget.journeyId));
    final journey = async.value;

    if (journey == null) {
      return Scaffold(
        backgroundColor: SmartRailColors.page,
        body: Column(
          children: [
            const SyncLine(),
            const _TopBar(title: '', onHelp: null),
            Expanded(
              child: async.when(
                data: (_) => const SizedBox.shrink(),
                loading: () => const LoadingList(),
                error: (error, _) => FailureView(
                  error: error,
                  onRetry: () =>
                      ref.invalidate(driverJourneyProvider(widget.journeyId)),
                ),
              ),
            ),
          ],
        ),
      );
    }

    final firstTime = journey.stops.firstOrNull?.scheduled;
    final title = firstTime == null
        ? journey.trainNumber
        : l10n.journeyTitle(journey.destination, clock.formatTime(firstTime));

    if (journey.isComplete) {
      return _Complete(journey: journey, title: title);
    }

    final next = journey.next;
    final atStop = journey.atStop;
    final focus = atStop ?? journey.nextStop;
    final delay = journey.delayMinutes ?? 0;

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      body: Column(
        children: [
          const SyncLine(),
          _TopBar(
            title: title,
            onHelp: () => startAssistance(context, ref, journey),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async =>
                  ref.invalidate(driverJourneyProvider(widget.journeyId)),
              child: ListView(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                children: [
                  if (!journey.canAct) ...[
                    InfoBanner(
                      kind: BannerKind.warning,
                      title: l10n.notSignedOn,
                    ),
                    const SizedBox(height: 12),
                  ],
                  if (focus != null)
                    _FocusCard(stop: focus, atStop: atStop != null, delay: delay),
                  const SizedBox(height: 16),
                  _Timeline(stops: journey.stops),
                ],
              ),
            ),
          ),
          StickyActionBar(
            children: [
              if (next != null)
                SmartRailButton(
                  label: next.isArrival
                      ? l10n.arrivedAt(next.stationName)
                      : l10n.departedFrom(next.stationName),
                  onPressed: journey.canAct && !_busy
                      ? () => _send(journey, next)
                      : null,
                ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: SmartRailButton(
                      label: l10n.delayReasonButton,
                      kind: ButtonKind.secondary,
                      onPressed: journey.canAct
                          ? () => _delayReason(journey)
                          : null,
                    ),
                  ),
                  if (next != null) ...[
                    const SizedBox(width: 8),
                    // Not stopping at this stop: a button of its own, not a hidden menu item.
                    Expanded(
                      child: ToneButton(
                        label: l10n.skipButton,
                        icon: Icons.block,
                        foreground: SmartRailColors.delayedText,
                        background: SmartRailColors.delayedBg,
                        onPressed: journey.canAct
                            ? () => _skip(journey)
                            : null,
                      ),
                    ),
                  ],
                ],
              ),
            ],
          ),
        ],
      ),
    );
  }
}

/// Back, the name of the journey and Help.
class _TopBar extends StatelessWidget {
  const _TopBar({
    required this.title,
    required this.onHelp,
  });

  final String title;
  final VoidCallback? onHelp;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Padding(
      padding: const EdgeInsets.fromLTRB(4, 4, 8, 8),
      child: Row(
        children: [
          IconButton(
            onPressed: () => context.pop(),
            icon: const Icon(Icons.arrow_back),
          ),
          Expanded(
            child: Text(
              title,
              overflow: TextOverflow.ellipsis,
              style: SmartRailType.heading16.copyWith(
                color: SmartRailColors.textPrimary,
              ),
            ),
          ),
          if (onHelp != null)
            // Help is a solid blue button, so it is the first thing a driver in trouble finds.
            Material(
              color: SmartRailColors.changedText,
              shape: const StadiumBorder(),
              child: InkWell(
                customBorder: const StadiumBorder(),
                onTap: onHelp,
                child: Padding(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 14,
                    vertical: 12,
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(
                        Icons.support,
                        size: 20,
                        color: Colors.white,
                      ),
                      const SizedBox(width: 6),
                      Text(
                        l10n.helpButton,
                        style: SmartRailType.label14.copyWith(
                          color: Colors.white,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

/// The green card: the stop the train is heading for, or standing at, and how long until it arrives or leaves.
class _FocusCard extends ConsumerWidget {
  const _FocusCard({
    required this.stop,
    required this.atStop,
    required this.delay,
  });

  final DriverStop stop;
  final bool atStop;
  final int delay;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.watch(networkClockProvider);
    final now = ref.watch(nowProvider)();

    final due = stop.scheduled;
    final expected = stop.expected ?? stop.scheduled;

    final headline = atStop
        ? (expected == null ? '' : l10n.departsAt(clock.formatTime(expected)))
        : () {
            if (expected == null) return '';
            final minutes = (expected.difference(now).inSeconds / 60).ceil();
            return minutes <= 0
                ? l10n.arrivingNow
                : l10n.arrivingIn(minutes);
          }();

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: SmartRailColors.primarySubtle,
        borderRadius: BorderRadius.circular(SmartRailRadius.lg),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            atStop ? l10n.atStopLabel : l10n.nextStopLabel,
            style: SmartRailType.caption.copyWith(
              color: SmartRailColors.primaryPressed,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            stop.name,
            style: SmartRailType.display.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          if (headline.isNotEmpty) ...[
            const SizedBox(height: 4),
            Text(
              headline,
              style: SmartRailType.title20.copyWith(
                color: SmartRailColors.primaryPressed,
              ),
            ),
          ],
          const SizedBox(height: 10),
          Wrap(
            spacing: 10,
            runSpacing: 6,
            crossAxisAlignment: WrapCrossAlignment.center,
            children: [
              if (due != null)
                Text(
                  atStop && stop.actual != null
                      ? l10n.arrivedDue(
                          clock.formatTime(due),
                          clock.formatTime(stop.actual!),
                        )
                      : l10n.scheduledAt(clock.formatTime(due)),
                  style: SmartRailType.label14.copyWith(
                    color: SmartRailColors.textPrimary,
                  ),
                ),
              if (delay > 0)
                ChipTag(
                  label: l10n.delayChip(delay),
                  tone: ChipTone.amber,
                  icon: Icons.schedule,
                ),
            ],
          ),
          if (stop.platform != null) ...[
            const SizedBox(height: 10),
            Text(
              l10n.platformLine(stop.platform!),
              style: SmartRailType.body14.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

/// The stops down a line: a filled dot for a stop the train has left, a green ring for the one it is at, hollow rings ahead.
class _Timeline extends ConsumerWidget {
  const _Timeline({required this.stops});

  final List<DriverStop> stops;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.watch(networkClockProvider);

    return Container(
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
      decoration: BoxDecoration(
        color: SmartRailColors.surface,
        borderRadius: BorderRadius.circular(SmartRailRadius.lg),
        border: Border.all(color: SmartRailColors.border),
      ),
      child: Column(
        children: [
          for (var i = 0; i < stops.length; i++)
            _TimelineRow(
              stop: stops[i],
              first: i == 0,
              last: i == stops.length - 1,
              times: _times(stops[i], clock),
              detail: _detail(l10n, stops[i], clock),
            ),
        ],
      ),
    );
  }

  /// Scheduled time over the expected one when they differ; one bold time otherwise.
  (String?, String) _times(DriverStop s, NetworkClock clock) {
    final scheduled = s.scheduled == null ? null : clock.formatTime(s.scheduled!);
    final actual = s.actual ?? s.expected ?? s.scheduled;
    final main = actual == null ? '' : clock.formatTime(actual);
    return (scheduled != null && scheduled != main ? scheduled : null, main);
  }

  String _detail(AppLocalizations l10n, DriverStop s, NetworkClock clock) {
    return switch (s.state) {
      StopState.departed => l10n.stopDeparted(
        clock.formatTime(s.actual ?? s.scheduled ?? DateTime.now()),
      ),
      StopState.arrived =>
        s.platform == null
            ? l10n.stopHere
            : l10n.stopPlatformHere(s.platform!),
      StopState.skipped => l10n.stopSkipped,
      StopState.cancelled => l10n.stopCancelled,
      StopState.pending =>
        s.isNext && s.platform != null
            ? l10n.stopPlatformExpected(s.platform!)
            : l10n.stopEstimate,
    };
  }
}

class _TimelineRow extends StatelessWidget {
  const _TimelineRow({
    required this.stop,
    required this.first,
    required this.last,
    required this.times,
    required this.detail,
  });

  final DriverStop stop;
  final bool first;
  final bool last;
  final (String?, String) times;
  final String detail;

  @override
  Widget build(BuildContext context) {
    final departed = stop.state == StopState.departed;
    final here = stop.state == StopState.arrived;
    const line = SmartRailColors.textPrimary;

    Widget dot() {
      if (departed) {
        return Container(
          width: 16,
          height: 16,
          decoration: const BoxDecoration(shape: BoxShape.circle, color: line),
        );
      }
      return Container(
        width: 16,
        height: 16,
        decoration: BoxDecoration(
          shape: BoxShape.circle,
          color: SmartRailColors.surface,
          border: Border.all(
            color: here ? SmartRailColors.primary : line,
            width: here ? 4 : 2,
          ),
        ),
      );
    }

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(
            width: 48,
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  if (times.$1 != null)
                    Text(
                      times.$1!,
                      style: SmartRailType.caption.copyWith(
                        color: SmartRailColors.textSecondary,
                      ),
                    ),
                  Text(
                    times.$2,
                    style: SmartRailType.label14.copyWith(
                      color: SmartRailColors.textPrimary,
                    ),
                  ),
                ],
              ),
            ),
          ),
          SizedBox(
            width: 28,
            child: Column(
              children: [
                Container(
                  width: 2,
                  height: 10,
                  color: first ? Colors.transparent : line,
                ),
                dot(),
                Expanded(
                  child: Container(
                    width: 2,
                    color: last ? Colors.transparent : line,
                  ),
                ),
              ],
            ),
          ),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    stop.name,
                    style: SmartRailType.body.copyWith(
                      color: SmartRailColors.textPrimary,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  Text(
                    detail,
                    style: SmartRailType.body14.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// Journey complete (design 20): what was sent, and the way back to the duty.
class _Complete extends ConsumerWidget {
  const _Complete({required this.journey, required this.title});

  final DriverJourney journey;
  final String title;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.watch(networkClockProvider);
    final messages = ref.watch(messagesProvider).value ?? const [];

    final stops = journey.stops;
    final last = stops.lastOrNull;
    final arrived = last?.actual ?? last?.scheduled;
    final late = journey.delayMinutes ?? 0;
    final start = stops.firstOrNull?.scheduled;

    // First stop: one event (departed). Last stop: one (arrived). Every stop between: two.
    var sent = 0;
    for (var i = 0; i < stops.length; i++) {
      final s = stops[i];
      if (s.state == StopState.departed) {
        sent += i == 0 ? 1 : 2;
      } else if (s.state == StopState.arrived || s.state == StopState.skipped) {
        sent += 1;
      }
    }
    final total = stops.length < 2 ? 0 : 2 * (stops.length - 1);

    final needAck = messages.where((m) => m.requiresAck).toList();
    final acked = needAck.where((m) => m.acknowledgedAt != null).length;

    final route = l10n.journeyRoute(journey.destination, journey.origin);
    final body = arrived == null
        ? route
        : (late > 0
              ? l10n.journeyCompleteBodyLate(
                  clock.formatTime(arrived),
                  late,
                  route,
                  start == null ? '' : clock.formatTime(start),
                )
              : l10n.journeyCompleteBody(
                  clock.formatTime(arrived),
                  route,
                  start == null ? '' : clock.formatTime(start),
                ));

    Widget row(String label, String value) => Padding(
      padding: const EdgeInsets.symmetric(vertical: 6),
      child: Row(
        children: [
          Expanded(
            child: Text(
              label,
              style: SmartRailType.body14.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
          ),
          Text(
            value,
            style: SmartRailType.label14.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
        ],
      ),
    );

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      body: Column(
        children: [
          const SyncLine(),
          Expanded(
            child: StatusPage(
              icon: Icons.check,
              positive: true,
              title: l10n.journeyCompleteTitle,
              body: body,
              child: OutlinedCard(
                child: Column(
                  children: [
                    row(l10n.completeEvents, '$sent of $total'),
                    row(
                      l10n.completeReasons,
                      journey.delayReason == null ? '0' : '1',
                    ),
                    row(
                      l10n.completeAcks,
                      '$acked of ${needAck.length}',
                    ),
                  ],
                ),
              ),
            ),
          ),
          StickyActionBar(
            children: [
              SmartRailButton(
                label: l10n.backToDuty,
                onPressed: () => context.pop(),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
