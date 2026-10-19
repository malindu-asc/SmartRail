import 'package:flutter/material.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../l10n/app_localizations.dart';
import '../../domain/entities/journey_detail.dart';

/// "14:25 ---- train ---- 14:48, Central, Riverside": the journey at a glance (design 36).
class RouteHeader extends StatelessWidget {
  const RouteHeader({super.key, required this.journey, required this.clock});

  final JourneyDetail journey;
  final NetworkClock clock;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final departure = journey.scheduledDeparture;
    final arrival = journey.lastStop?.scheduledArrival;

    TextStyle time() => theme.textTheme.titleLarge!.copyWith(
      fontSize: 24,
      fontFeatures: SmartRailType.timeFeatures,
    );
    TextStyle name() =>
        theme.textTheme.bodyMedium!.copyWith(fontWeight: FontWeight.w600);

    final l10n = AppLocalizations.of(context);
    final label = l10n.journeyTitle(
      journey.originName,
      journey.destinationName,
    );

    // With very large text the two ends stack instead of fighting for the width.
    if (MediaQuery.textScalerOf(context).scale(16) > 24) {
      return Semantics(
        label: label,
        child: ExcludeSemantics(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                departure == null ? '' : clock.formatTime(departure),
                style: time(),
              ),
              Text(journey.originName, style: name()),
              const SizedBox(height: SmartRailSpacing.md),
              Text(
                arrival == null ? '' : clock.formatTime(arrival),
                style: time(),
              ),
              Text(journey.destinationName, style: name()),
            ],
          ),
        ),
      );
    }

    return Semantics(
      label: label,
      child: ExcludeSemantics(
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  departure == null ? '' : clock.formatTime(departure),
                  style: time(),
                ),
                Text(journey.originName, style: name()),
              ],
            ),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.only(
                  top: 14,
                  left: SmartRailSpacing.sm,
                  right: SmartRailSpacing.sm,
                ),
                child: Row(
                  children: [
                    const Expanded(child: _DottedLine()),
                    const Padding(
                      padding: EdgeInsets.symmetric(
                        horizontal: SmartRailSpacing.sm,
                      ),
                      child: Icon(
                        Icons.train,
                        size: 22,
                        color: SmartRailColors.primary,
                      ),
                    ),
                    const Expanded(child: _DottedLine()),
                  ],
                ),
              ),
            ),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  arrival == null ? '' : clock.formatTime(arrival),
                  style: time(),
                ),
                Text(journey.destinationName, style: name()),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _DottedLine extends StatelessWidget {
  const _DottedLine();

  @override
  Widget build(BuildContext context) => const SizedBox(
    height: 1,
    child: DecoratedBox(
      decoration: BoxDecoration(color: SmartRailColors.borderStrong),
    ),
  );
}

/// The one-line answer: running on time, delayed, no live data, cancelled, not departed yet, finished. Always an icon, words and a
/// colour. When the status changes the card cross-fades (design 08, 09, 23).
class JourneyStatusCard extends StatelessWidget {
  const JourneyStatusCard({
    super.key,
    required this.journey,
    required this.clock,
  });

  final JourneyDetail journey;
  final NetworkClock clock;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final headline = journey.headline;
    final updated = journey.asOf ?? journey.fetchedAt;
    final lastKnown = journey.lastKnownStop;

    final String title;
    final String? detail;
    final IconData icon;
    final Color color;
    final Color background;

    switch (headline) {
      case JourneyHeadline.runningOnTime:
        title = l10n.headlineRunningOnTime;
        detail = lastKnown?.actualDeparture == null
            ? l10n.detailLive(clock.formatTime(updated))
            : l10n.detailLiveDeparted(
                clock.formatTime(updated),
                lastKnown!.stationName,
                clock.formatTime(lastKnown.actualDeparture!),
              );
        icon = Icons.check;
        color = SmartRailColors.primary;
        background = SmartRailColors.primarySubtle;
      case JourneyHeadline.delayed:
        title = l10n.headlineDelayed(journey.delayMinutes ?? 0);
        final next = journey.stops
            .where((s) => s.state == 'Pending' && s.liveTime != null)
            .firstOrNull;
        detail = next == null || next.scheduledTime == null
            ? l10n.detailLive(clock.formatTime(updated))
            : l10n.detailDelayedBody(
                clock.formatTime(next.liveTime!),
                next.stationName,
                clock.formatTime(next.scheduledTime!),
              );
        icon = Icons.schedule;
        color = SmartRailColors.delayedText;
        background = SmartRailColors.delayedBg;
      case JourneyHeadline.noLiveData:
        title = journey.asOf == null
            ? l10n.headlineNoLiveEver
            : l10n.headlineNoLive(clock.formatTime(journey.asOf!));
        detail = lastKnown == null
            ? l10n.detailNoLiveBody
            : '${l10n.detailLastKnown(lastKnown.stationName, clock.formatTime(lastKnown.actualDeparture ?? lastKnown.actualArrival ?? updated))}\n${l10n.detailNoLiveBody}';
        icon = Icons.wifi_off;
        color = SmartRailColors.staleText;
        background = SmartRailColors.staleBg;
      case JourneyHeadline.cancelled:
        title = l10n.headlineCancelled;
        detail = l10n.detailCancelledBody;
        icon = Icons.cancel_outlined;
        color = SmartRailColors.cancelledText;
        background = SmartRailColors.cancelledBg;
      case JourneyHeadline.scheduled:
        title = l10n.headlineScheduled;
        detail = l10n.detailScheduledBody;
        icon = Icons.schedule;
        color = SmartRailColors.textSecondary;
        background = SmartRailColors.bgSoft;
      case JourneyHeadline.completed:
        title = l10n.headlineCompleted;
        detail = null;
        icon = Icons.check;
        color = SmartRailColors.primary;
        background = SmartRailColors.primarySubtle;
    }

    final content = Padding(
      padding: const EdgeInsets.all(14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(icon, size: 22, color: color),
              const SizedBox(width: SmartRailSpacing.sm),
              Expanded(
                child: Text(
                  title,
                  style: SmartRailType.title20.copyWith(color: color),
                ),
              ),
            ],
          ),
          if (detail != null) ...[
            const SizedBox(height: SmartRailSpacing.xs),
            Text(
              detail,
              style: SmartRailType.caption.copyWith(color: color, fontSize: 13),
            ),
          ],
        ],
      ),
    );

    final card = headline == JourneyHeadline.noLiveData
        ? DashedBox(radius: SmartRailRadius.lg, child: content)
        : DecoratedBox(
            decoration: BoxDecoration(
              color: background,
              borderRadius: BorderRadius.circular(SmartRailRadius.lg),
            ),
            child: content,
          );

    return Semantics(
      container: true,
      liveRegion: true,
      label: [title, detail].whereType<String>().join('. '),
      child: ExcludeSemantics(
        child: ChangeSwitcher(
          child: KeyedSubtree(key: ValueKey(headline), child: card),
        ),
      ),
    );
  }
}

/// The platform changed and the train has not left that stop yet (design 08): a dark, impossible-to-miss banner.
class PlatformBanner extends StatelessWidget {
  const PlatformBanner({super.key, required this.stop});

  final JourneyStop stop;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final title = l10n.platformBannerTitle(stop.platform ?? '');
    final body = l10n.platformBannerBody(stop.stationName);

    return Semantics(
      container: true,
      liveRegion: true,
      label: '$title. $body',
      child: ExcludeSemantics(
        child: Container(
          width: double.infinity,
          padding: const EdgeInsets.symmetric(
            horizontal: SmartRailSpacing.lg,
            vertical: 14,
          ),
          decoration: BoxDecoration(
            color: SmartRailColors.textPrimary,
            borderRadius: BorderRadius.circular(SmartRailRadius.md),
          ),
          child: Row(
            children: [
              const Icon(
                Icons.swap_horiz,
                size: 22,
                color: SmartRailColors.onPrimary,
              ),
              const SizedBox(width: SmartRailSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      title,
                      style: SmartRailType.label14.copyWith(
                        fontSize: 16,
                        color: SmartRailColors.onPrimary,
                      ),
                    ),
                    Text(
                      body,
                      style: SmartRailType.caption.copyWith(
                        color: SmartRailColors.border,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Every stop with its scheduled time, what the train did or is expected to do, and a line showing how far it has got. The scheduled
/// time stays visible (struck through) when the live time differs; a stop with no live basis says "Scheduled".
class StopTimeline extends StatelessWidget {
  const StopTimeline({super.key, required this.journey, required this.clock});

  final JourneyDetail journey;
  final NetworkClock clock;

  @override
  Widget build(BuildContext context) {
    final stops = journey.stops;

    return Card(
      child: Padding(
        padding: const EdgeInsets.symmetric(
          horizontal: SmartRailSpacing.lg,
          vertical: SmartRailSpacing.md,
        ),
        child: Column(
          children: [
            for (var i = 0; i < stops.length; i++)
              _StopRow(
                journey: journey,
                stop: stops[i],
                clock: clock,
                isLast: i == stops.length - 1,
                nextHasHappened:
                    i + 1 < stops.length && stops[i + 1].hasHappened,
              ),
          ],
        ),
      ),
    );
  }
}

class _StopRow extends StatelessWidget {
  const _StopRow({
    required this.journey,
    required this.stop,
    required this.clock,
    required this.isLast,
    required this.nextHasHappened,
  });

  final JourneyDetail journey;
  final JourneyStop stop;
  final NetworkClock clock;
  final bool isLast;
  final bool nextHasHappened;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final wording = journey.wordingFor(stop);
    final scheduled = stop.scheduledTime;
    final live = stop.liveTime;
    final noLive =
        wording == StopWording.scheduled || wording == StopWording.skipped;
    final differs =
        scheduled != null &&
        live != null &&
        !noLive &&
        live.difference(scheduled).inSeconds.abs() >= 60;

    final String line = switch (wording) {
      StopWording.departedOnTime => l10n.wordDepartedOnTime(
        _t(stop.actualDeparture),
      ),
      StopWording.departedLate => l10n.wordDepartedLate(
        _t(stop.actualDeparture),
        _late(stop.actualDeparture, stop.scheduledDeparture),
      ),
      StopWording.arrivedOnTime => l10n.wordArrivedOnTime(
        _t(stop.actualArrival),
      ),
      StopWording.arrivedLate => l10n.wordArrivedLate(
        _t(stop.actualArrival),
        _late(stop.actualArrival, stop.scheduledArrival),
      ),
      StopWording.arrivedEarly => l10n.wordArrivedEarly(
        _t(stop.actualArrival),
        -_late(stop.actualArrival, stop.scheduledArrival),
      ),
      StopWording.atPlatform => l10n.wordAtPlatform(_t(stop.actualArrival)),
      StopWording.expectedOnTime => l10n.wordExpectedOnTime(_t(live)),
      StopWording.expected => l10n.wordExpected(_t(live)),
      StopWording.estimate => l10n.wordEstimate,
      StopWording.scheduled => l10n.wordScheduled,
      StopWording.skipped => l10n.wordSkipped,
    };

    final lineColor = switch (wording) {
      StopWording.departedLate ||
      StopWording.arrivedLate ||
      StopWording.expected => SmartRailColors.delayedText,
      _ => SmartRailColors.textSecondary,
    };

    final happened = stop.hasHappened;
    final dotColor = happened
        ? SmartRailColors.textPrimary
        : SmartRailColors.surface;
    final dotBorder = noLive
        ? SmartRailColors.borderStrong
        : SmartRailColors.textPrimary;

    return MergeSemantics(
      child: Semantics(
        label: '${_t(scheduled)}, ${stop.stationName}, $line',
        child: ExcludeSemantics(
          child: IntrinsicHeight(
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                SizedBox(
                  width: 64,
                  child: Padding(
                    padding: const EdgeInsets.only(top: 2),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        if (differs)
                          Text(
                            _t(scheduled),
                            style: theme.textTheme.labelSmall?.copyWith(
                              decoration: TextDecoration.lineThrough,
                              color: SmartRailColors.textTertiary,
                              fontFeatures: SmartRailType.timeFeatures,
                            ),
                          ),
                        Text(
                          _t(differs ? live : scheduled),
                          style: theme.textTheme.bodyLarge?.copyWith(
                            fontWeight: FontWeight.w600,
                            fontFeatures: SmartRailType.timeFeatures,
                            color: noLive
                                ? SmartRailColors.textSecondary
                                : SmartRailColors.textPrimary,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                SizedBox(
                  width: 24,
                  child: Column(
                    children: [
                      const SizedBox(height: 4),
                      AnimatedContainer(
                        duration: SmartRailMotion.of(
                          context,
                          SmartRailMotion.base,
                        ),
                        curve: SmartRailMotion.curve,
                        width: 14,
                        height: 14,
                        decoration: BoxDecoration(
                          shape: BoxShape.circle,
                          color: dotColor,
                          border: Border.all(color: dotBorder, width: 2),
                        ),
                      ),
                      if (!isLast)
                        Expanded(
                          child: AnimatedContainer(
                            duration: SmartRailMotion.of(
                              context,
                              SmartRailMotion.slow,
                            ),
                            curve: SmartRailMotion.curve,
                            width: 2,
                            color: nextHasHappened
                                ? SmartRailColors.textPrimary
                                : SmartRailColors.borderStrong,
                          ),
                        ),
                    ],
                  ),
                ),
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.only(
                      left: SmartRailSpacing.sm,
                      bottom: SmartRailSpacing.lg,
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          stop.stationName,
                          style: theme.textTheme.bodyLarge?.copyWith(
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        Text(
                          line,
                          style: theme.textTheme.labelSmall?.copyWith(
                            color: lineColor,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  String _t(DateTime? time) => time == null ? '' : clock.formatTime(time);

  int _late(DateTime? actual, DateTime? scheduled) =>
      actual == null || scheduled == null
      ? 0
      : (actual.difference(scheduled).inSeconds / 60).round();
}
