import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/time/clock_providers.dart';
import '../../../../core/widgets/failure_view.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../messages/assistance_sheet.dart';
import '../../domain/duty_domain.dart';
import '../providers/duty_providers.dart';

/// The journey (designs 05, 06, 20): the next stop, one tap for arrived or departed, the delay reason, skip and help.
class JourneyScreen extends ConsumerStatefulWidget {
  const JourneyScreen({super.key, required this.journeyId});

  final String journeyId;

  @override
  ConsumerState<JourneyScreen> createState() => _JourneyScreenState();
}

class _JourneyScreenState extends ConsumerState<JourneyScreen> {
  bool _busy = false;

  Future<void> _send(DriverJourney journey, int stopSequence, String type) async {
    setState(() => _busy = true);
    final failure = await ref
        .read(dutyActionsProvider)
        .event(journey.id, stopSequence, type);
    if (!mounted) return;
    setState(() => _busy = false);
    if (failure != null) await _explain(failure);
  }

  Future<void> _explain(Failure failure) async {
    final l10n = AppLocalizations.of(context);

    // The server says which stop it expected; show that, then refresh.
    if (failure is ConflictFailure && failure.code == stopDoesNotMatchCode) {
      await showSmartRailSheet<void>(
        context,
        builder: (sheet) => SheetBody(
          children: [
            Text(
              l10n.mismatchTitle,
              style: SmartRailType.title.copyWith(
                color: SmartRailColors.textPrimary,
              ),
            ),
            Text(
              failure.message ?? '',
              style: SmartRailType.body14.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
            SmartRailButton(
              label: l10n.mismatchRefresh,
              onPressed: () => Navigator.of(sheet).pop(),
            ),
          ],
        ),
      );
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
              Text(
                l10n.delayReasonTitle,
                style: SmartRailType.title.copyWith(
                  color: SmartRailColors.textPrimary,
                ),
              ),
              Text(
                l10n.delayReasonBody,
                style: SmartRailType.body14.copyWith(
                  color: SmartRailColors.textSecondary,
                ),
              ),
              RowsCard(
                children: [
                  for (final entry in reasons.entries)
                    ListTile(
                      title: Text(entry.value),
                      trailing: chosen == entry.key
                          ? const Icon(
                              Icons.check,
                              color: SmartRailColors.primary,
                            )
                          : null,
                      onTap: () => setSheet(() => chosen = entry.key),
                    ),
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
      await _explain(failure);
    } else {
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
      builder: (sheet) => SheetBody(
        children: [
          Text(
            l10n.skipTitle(next.stationName),
            style: SmartRailType.title.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          Text(
            l10n.skipBody,
            style: SmartRailType.body14.copyWith(
              color: SmartRailColors.textSecondary,
            ),
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
    if (confirmed == true && mounted) {
      await _send(journey, next.stopSequence, 'skipped');
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
        appBar: const SmartRailAppBar(),
        body: async.when(
          data: (_) => const SizedBox.shrink(),
          loading: () => const LoadingList(),
          error: (error, _) => FailureView(
            error: error,
            onRetry: () =>
                ref.invalidate(driverJourneyProvider(widget.journeyId)),
          ),
        ),
      );
    }

    final firstTime = journey.stops.firstOrNull?.scheduled;
    final title = firstTime == null
        ? journey.trainNumber
        : l10n.journeyTitle(clock.formatTime(firstTime), journey.destination);

    if (journey.isComplete) {
      return Scaffold(
        backgroundColor: SmartRailColors.page,
        appBar: SmartRailAppBar(title: Text(title)),
        body: EmptyState(
          icon: Icons.check_circle_outline,
          title: l10n.journeyCompleteTitle,
          message: l10n.journeyCompleteBody(journey.destination),
        ),
        bottomNavigationBar: StickyActionBar(
          children: [
            SmartRailButton(
              label: l10n.backToDuty,
              onPressed: () => context.pop(),
            ),
          ],
        ),
      );
    }

    final next = journey.next;
    final atStop = journey.atStop;
    final focus = atStop ?? journey.nextStop;
    final delay = journey.delayMinutes ?? 0;

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      appBar: SmartRailAppBar(
        title: Text(title),
        actions: [
          TextButton(
            onPressed: () =>
                showAssistanceSheet(context, journeyId: journey.id),
            child: Text(l10n.helpButton),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async =>
            ref.invalidate(driverJourneyProvider(widget.journeyId)),
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (!journey.canAct) ...[
              InfoBanner(kind: BannerKind.warning, title: l10n.notSignedOn),
              const SizedBox(height: 12),
            ],
            if (focus != null)
              _NextStopCard(
                label: atStop != null ? l10n.atStop : l10n.nextStop,
                stop: focus,
                delay: delay,
              ),
            const SizedBox(height: 16),
            RowsCard(
              children: [
                for (final s in journey.stops) _StopRow(stop: s),
              ],
            ),
            if (next != null) ...[
              const SizedBox(height: 16),
              SmartRailButton(
                label: l10n.moreSkipStop,
                kind: ButtonKind.secondary,
                onPressed: journey.canAct && !_busy
                    ? () => _skip(journey)
                    : null,
              ),
            ],
          ],
        ),
      ),
      bottomNavigationBar: StickyActionBar(
        children: [
          if (next != null)
            SmartRailButton(
              label: next.isArrival
                  ? l10n.arrivedAt(next.stationName)
                  : l10n.departedFrom(next.stationName),
              onPressed: journey.canAct && !_busy
                  ? () => _send(journey, next.stopSequence, next.type)
                  : null,
            ),
          const SizedBox(height: 8),
          SmartRailButton(
            label: l10n.delayReasonButton,
            kind: ButtonKind.tint,
            onPressed: journey.canAct && delay > 0
                ? () => _delayReason(journey)
                : null,
          ),
        ],
      ),
    );
  }
}

class _NextStopCard extends ConsumerWidget {
  const _NextStopCard({
    required this.label,
    required this.stop,
    required this.delay,
  });

  final String label;
  final DriverStop stop;
  final int delay;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.watch(networkClockProvider);

    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: SmartRailColors.surface,
        borderRadius: BorderRadius.circular(SmartRailRadius.lg),
        border: Border.all(color: SmartRailColors.primary, width: 1.5),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: SmartRailType.caption.copyWith(
              color: SmartRailColors.textSecondary,
            ),
          ),
          const SizedBox(height: 6),
          Text(
            stop.name,
            style: SmartRailType.title24.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          const SizedBox(height: 6),
          if (stop.scheduled != null)
            Text(
              l10n.scheduledAt(clock.formatTime(stop.scheduled!)),
              style: SmartRailType.body14.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
          if (stop.expected != null && delay > 0)
            Text(
              l10n.expectedAt(clock.formatTime(stop.expected!)),
              style: SmartRailType.label14.copyWith(
                color: SmartRailColors.delayedText,
              ),
            ),
          if (stop.platform != null)
            Text(
              l10n.platformCoach(stop.platform!),
              style: SmartRailType.body14.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
          if (delay > 0) ...[
            const SizedBox(height: 10),
            StatusChip(kind: StatusKind.delayed, label: l10n.delayChip(delay)),
          ],
        ],
      ),
    );
  }
}

class _StopRow extends ConsumerWidget {
  const _StopRow({required this.stop});

  final DriverStop stop;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.watch(networkClockProvider);

    final detail = switch (stop.state) {
      StopState.departed => l10n.stopDeparted(
        clock.formatTime(stop.actual ?? stop.scheduled ?? DateTime.now()),
      ),
      StopState.arrived => l10n.stopHere,
      StopState.skipped => l10n.stopSkipped,
      StopState.cancelled => l10n.stopCancelled,
      StopState.pending =>
        stop.expected == null ? '' : clock.formatTime(stop.expected!),
    };

    final done =
        stop.state == StopState.departed ||
        stop.state == StopState.skipped ||
        stop.state == StopState.cancelled;

    return ListTile(
      leading: Icon(
        stop.state == StopState.arrived
            ? Icons.train
            : (done ? Icons.check_circle : Icons.radio_button_unchecked),
        color: done ? SmartRailColors.textSecondary : SmartRailColors.primary,
      ),
      title: Text(
        stop.name,
        style: SmartRailType.body.copyWith(
          color: done
              ? SmartRailColors.textSecondary
              : SmartRailColors.textPrimary,
          fontWeight: stop.isNext ? FontWeight.w600 : FontWeight.w400,
        ),
      ),
      trailing: Text(
        detail,
        style: SmartRailType.body14.copyWith(
          color: SmartRailColors.textSecondary,
        ),
      ),
    );
  }
}
