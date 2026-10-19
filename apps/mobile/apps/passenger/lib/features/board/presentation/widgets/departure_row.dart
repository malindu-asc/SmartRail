import 'package:flutter/material.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../l10n/app_localizations.dart';
import '../../domain/entities/departure.dart';

/// The chip for a departure's state: icon, words and colour (REQ-PAX-004). Null for a plain scheduled train, which has no live claim.
({StatusKind kind, String label})? departureChip(
  AppLocalizations l10n,
  Departure departure,
) => switch (departure.state) {
  DepartureState.cancelled => (
    kind: StatusKind.cancelled,
    label: l10n.chipCancelled,
  ),
  DepartureState.delayed => (
    kind: StatusKind.delayed,
    label: l10n.chipDelayed(departure.delayMinutes ?? 0),
  ),
  DepartureState.platformChanged => (
    kind: StatusKind.changed,
    label: l10n.chipPlatformChanged,
  ),
  DepartureState.noLiveData => (
    kind: StatusKind.noLiveData,
    label: l10n.chipNoLiveData,
  ),
  DepartureState.onTime => (kind: StatusKind.onTime, label: l10n.chipOnTime),
  DepartureState.scheduled => null,
};

/// One train on a station board (design 07). The timetable time is the big one; when the live time differs it is written below it,
/// so the scheduled time never disappears (REQ-PAX-002).
class DepartureRow extends StatelessWidget {
  const DepartureRow({
    super.key,
    required this.departure,
    required this.clock,
    required this.now,
    required this.asOf,
    required this.onTap,
  });

  final Departure departure;
  final NetworkClock clock;

  /// The phone's current time, for "in 7 min".
  final DateTime now;

  /// The server's time for this board, so "no update since" can be worked out from the data's age.
  final DateTime asOf;
  final VoidCallback onTap;

  String _subline(AppLocalizations l10n) {
    final parts = <String>[];

    if (departure.hasNoLiveData) {
      final age = departure.ageSeconds;
      parts.add(
        age == null
            ? l10n.scheduledOnly
            : l10n.scheduledNoUpdate(
                clock.formatTime(asOf.subtract(Duration(seconds: age))),
              ),
      );
    } else if (departure.expectedDiffers) {
      parts.add(l10n.expectedAt(clock.formatTime(departure.expected)));
    } else if (!departure.isCancelled) {
      final minutes = departure.expected.difference(now).inMinutes;
      if (minutes >= 0 && minutes <= 60) parts.add(l10n.inMinutes(minutes));
    }

    if (!departure.isCancelled && !departure.hasNoLiveData) {
      parts.add(
        departure.platform == null
            ? l10n.platformTba
            : l10n.platformN(departure.platform!),
      );
    } else if (departure.platform != null && departure.isCancelled) {
      parts.add(l10n.platformN(departure.platform!));
    }

    return parts.join(' · ');
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final chip = departureChip(l10n, departure);
    final subline = _subline(l10n);
    final time = clock.formatTime(departure.scheduled);
    final scaler = MediaQuery.textScalerOf(context);
    // With very large text the chip drops under the destination instead of squeezing it.
    final stacked = scaler.scale(16) > 24;
    final chipWidget = chip == null
        ? null
        : ChangeSwitcher(
            child: StatusChip(
              key: ValueKey(chip.label),
              kind: chip.kind,
              label: chip.label,
            ),
          );

    return MergeSemantics(
      child: Semantics(
        button: true,
        label: [
          time,
          departure.destinationName,
          subline,
          chip?.label,
        ].whereType<String>().where((s) => s.isNotEmpty).join(', '),
        child: ExcludeSemantics(
          child: InkWell(
            onTap: onTap,
            child: Padding(
              // Design 07: 14 padding all round, the time 20 Bold in a 58 wide slot, 12 between the parts.
              padding: const EdgeInsets.all(14),
              child: Row(
                children: [
                  SizedBox(
                    width: scaler.scale(58),
                    child: Text(
                      time,
                      style: SmartRailType.title20.copyWith(
                        fontFeatures: SmartRailType.timeFeatures,
                        color: SmartRailColors.textPrimary,
                      ),
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          departure.destinationName,
                          style: SmartRailType.heading16.copyWith(
                            color: SmartRailColors.textPrimary,
                          ),
                        ),
                        if (subline.isNotEmpty) ...[
                          const SizedBox(height: 2),
                          Text(
                            subline,
                            style: SmartRailType.caption.copyWith(
                              color: SmartRailColors.textSecondary,
                            ),
                          ),
                        ],
                        if (chipWidget != null && stacked)
                          Padding(
                            padding: const EdgeInsets.only(
                              top: SmartRailSpacing.sm,
                            ),
                            child: chipWidget,
                          ),
                      ],
                    ),
                  ),
                  if (chipWidget != null && !stacked) chipWidget,
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
