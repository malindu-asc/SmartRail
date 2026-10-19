import 'package:flutter/material.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../l10n/app_localizations.dart';

enum Freshness { live, stale, saved }

/// How old what is on screen is: "Live · updated 14:18", "As of 14:31" or "Saved 14:06" (REQ-PAX-005). Always words and an icon.
class FreshnessLine extends StatelessWidget {
  const FreshnessLine({
    super.key,
    required this.freshness,
    required this.time,
    required this.clock,
  });

  final Freshness freshness;
  final DateTime time;
  final NetworkClock clock;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final at = clock.formatTime(time);

    final (icon, color, text) = switch (freshness) {
      Freshness.live => (
        Icons.check,
        SmartRailColors.primary,
        l10n.freshLive(at),
      ),
      Freshness.stale => (
        Icons.history,
        SmartRailColors.staleText,
        l10n.freshStale(at),
      ),
      Freshness.saved => (
        Icons.wifi_off,
        SmartRailColors.staleText,
        l10n.freshSaved(at),
      ),
    };

    return Semantics(
      label: text,
      child: ExcludeSemantics(
        child: ChangeSwitcher(
          child: Row(
            key: ValueKey(freshness),
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(icon, size: 16, color: color),
              const SizedBox(width: SmartRailSpacing.sm),
              Flexible(
                child: Text(
                  text,
                  style: Theme.of(context).textTheme.labelSmall?.copyWith(
                    color: SmartRailColors.textSecondary,
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
