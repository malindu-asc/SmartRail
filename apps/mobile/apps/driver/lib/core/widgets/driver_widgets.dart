import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../l10n/app_localizations.dart';
import '../time/clock_providers.dart';

/// The top line of every screen: when the information was last fetched, or that the phone is offline (designs: "Synced 08:42",
/// "Offline · last sync yesterday 17:40"). The phone's own status bar above it shows the time.
class SyncLine extends ConsumerWidget {
  const SyncLine({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final reachability = ref.watch(reachabilityProvider);
    final clock = ref.watch(networkClockProvider);
    final last = reachability.lastSuccess;

    final online = reachability.online;
    final text = online
        ? l10n.syncedAt(clock.formatTime(last ?? DateTime.now()))
        : (last == null
              ? l10n.offlineNever
              : l10n.offlineSynced(clock.formatTime(last)));

    return SafeArea(
      bottom: false,
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 6, 16, 0),
        child: Align(
          alignment: Alignment.centerRight,
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (!online) ...[
                const SvgIcon(
                  AppIcons.wifiOff,
                  size: 14,
                  color: SmartRailColors.textSecondary,
                ),
                const SizedBox(width: 6),
              ],
              Text(
                text,
                style: SmartRailType.caption.copyWith(
                  color: SmartRailColors.textSecondary,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

enum ChipTone { green, amber, grey, red, blue, dark }

/// A small rounded tag: "Next", "Later", "Done", "Delivered", "Signed off". An icon and words, never colour alone.
class ChipTag extends StatelessWidget {
  const ChipTag({
    super.key,
    required this.label,
    required this.tone,
    this.icon,
  });

  final String label;
  final ChipTone tone;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final (fg, bg) = switch (tone) {
      ChipTone.green => (SmartRailColors.primaryPressed, SmartRailColors.primarySubtle),
      ChipTone.amber => (SmartRailColors.delayedText, SmartRailColors.delayedBg),
      ChipTone.grey => (SmartRailColors.textSecondary, SmartRailColors.bgSoft),
      ChipTone.red => (SmartRailColors.cancelledText, SmartRailColors.cancelledBg),
      ChipTone.blue => (SmartRailColors.changedText, SmartRailColors.changedBg),
      ChipTone.dark => (Colors.white, SmartRailColors.textPrimary),
    };

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(SmartRailRadius.pill),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 15, color: fg),
            const SizedBox(width: 5),
          ],
          Text(label, style: SmartRailType.label14.copyWith(color: fg)),
        ],
      ),
    );
  }
}

/// A choice in a sheet: a white row with a ring, or a green one with a tick when it is chosen (delay reason, skipped stop, assistance).
class OptionRow extends StatelessWidget {
  const OptionRow({
    super.key,
    required this.label,
    required this.selected,
    required this.onTap,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      selected: selected,
      label: label,
      child: ExcludeSemantics(
        child: GestureDetector(
          onTap: onTap,
          child: Container(
            constraints: const BoxConstraints(minHeight: 56),
            padding: const EdgeInsets.symmetric(horizontal: 16),
            decoration: BoxDecoration(
              color: selected
                  ? SmartRailColors.primarySubtle
                  : SmartRailColors.surface,
              borderRadius: BorderRadius.circular(SmartRailRadius.md),
              border: Border.all(
                color: selected
                    ? SmartRailColors.primary
                    : SmartRailColors.border,
                width: selected ? 1.5 : 1,
              ),
            ),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    label,
                    style: SmartRailType.label14.copyWith(
                      color: SmartRailColors.textPrimary,
                    ),
                  ),
                ),
                Container(
                  width: 26,
                  height: 26,
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    color: selected ? SmartRailColors.primary : null,
                    border: selected
                        ? null
                        : Border.all(color: SmartRailColors.borderStrong),
                  ),
                  child: selected
                      ? const Icon(Icons.check, size: 16, color: Colors.white)
                      : null,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

/// A full-width tinted button for an action that is not the main one but must be easy to find (not stopping at a stop).
class ToneButton extends StatelessWidget {
  const ToneButton({
    super.key,
    required this.label,
    required this.icon,
    required this.foreground,
    required this.background,
    required this.onPressed,
  });

  final String label;
  final IconData icon;
  final Color foreground;
  final Color background;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    final enabled = onPressed != null;

    return Semantics(
      button: true,
      enabled: enabled,
      label: label,
      child: ExcludeSemantics(
        child: Material(
          color: enabled ? background : SmartRailColors.bgSoft,
          shape: StadiumBorder(
            side: BorderSide(
              color: enabled ? foreground : SmartRailColors.border,
              width: 1.5,
            ),
          ),
          child: InkWell(
            customBorder: const StadiumBorder(),
            onTap: onPressed,
            child: ConstrainedBox(
              constraints: const BoxConstraints(minHeight: 56),
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 12),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Icon(
                      icon,
                      size: 20,
                      color: enabled ? foreground : SmartRailColors.textTertiary,
                    ),
                    const SizedBox(width: 8),
                    Flexible(
                      child: Text(
                        label,
                        textAlign: TextAlign.center,
                        style: SmartRailType.label14.copyWith(
                          color: enabled ? foreground : SmartRailColors.textTertiary,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// A heading and a line of text for a sheet.
class SheetText extends StatelessWidget {
  const SheetText({super.key, required this.title, this.body});

  final String title;
  final String? body;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          title,
          style: SmartRailType.title20.copyWith(
            color: SmartRailColors.textPrimary,
          ),
        ),
        if (body != null) ...[
          const SizedBox(height: 8),
          Text(
            body!,
            style: SmartRailType.body14.copyWith(
              color: SmartRailColors.textSecondary,
            ),
          ),
        ],
      ],
    );
  }
}

/// A centred message with a round icon: no duty, duty not current, request sent, journey complete.
class StatusPage extends StatelessWidget {
  const StatusPage({
    super.key,
    required this.icon,
    required this.title,
    this.body,
    this.child,
    this.positive = false,
  });

  final IconData icon;
  final String title;
  final String? body;
  final Widget? child;

  /// A green circle for something that worked (request sent, journey complete).
  final bool positive;

  @override
  Widget build(BuildContext context) {
    return ListView(
      // Always scrollable, so a screen with this message can still be pulled down to refresh.
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.fromLTRB(24, 40, 24, 24),
      children: [
        Center(
          child: Container(
            width: 96,
            height: 96,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              color: positive ? SmartRailColors.primarySubtle : SmartRailColors.bgSoft,
            ),
            child: Icon(
              icon,
              size: 40,
              color: positive ? SmartRailColors.primary : SmartRailColors.textSecondary,
            ),
          ),
        ),
        const SizedBox(height: 20),
        Text(
          title,
          textAlign: TextAlign.center,
          style: SmartRailType.title20.copyWith(
            color: SmartRailColors.textPrimary,
          ),
        ),
        if (body != null) ...[
          const SizedBox(height: 8),
          Text(
            body!,
            textAlign: TextAlign.center,
            style: SmartRailType.body.copyWith(
              color: SmartRailColors.textSecondary,
            ),
          ),
        ],
        if (child != null) ...[const SizedBox(height: 24), child!],
      ],
    );
  }
}

/// A white rounded card with a thin border, used for journeys and messages.
class OutlinedCard extends StatelessWidget {
  const OutlinedCard({
    super.key,
    required this.child,
    this.onTap,
    this.highlighted = false,
  });

  final Widget child;
  final VoidCallback? onTap;

  /// A green border: something new, or that needs the driver's answer.
  final bool highlighted;


  @override
  Widget build(BuildContext context) {
    return Material(
      color: SmartRailColors.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(SmartRailRadius.lg),
        side: BorderSide(
          color: highlighted ? SmartRailColors.primary : SmartRailColors.border,
          width: highlighted ? 1.5 : 1,
        ),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(padding: const EdgeInsets.all(16), child: child),
      ),
    );
  }
}
