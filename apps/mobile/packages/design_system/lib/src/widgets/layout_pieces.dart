import 'package:flutter/material.dart';

import '../motion/motion.dart';
import '../tokens/tokens.dart';
import 'svg_icon.dart';

/// The grey note with an info icon (Figma: stale-bg, 12 radius, 12 padding, 18 icon, 12 Medium text).
class InfoNote extends StatelessWidget {
  const InfoNote(this.text, {super.key});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      container: true,
      label: text,
      child: ExcludeSemantics(
        child: DecoratedBox(
          decoration: BoxDecoration(
            color: SmartRailColors.staleBg,
            borderRadius: BorderRadius.circular(SmartRailRadius.md),
          ),
          child: Padding(
            padding: const EdgeInsets.all(12),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const SvgIcon(
                  AppIcons.info,
                  size: 18,
                  color: SmartRailColors.staleText,
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    text,
                    style: SmartRailType.caption.copyWith(
                      color: SmartRailColors.staleText,
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
}

/// The Figma "Sticky action": white, a hairline on top, 12 above and 28 below (the phone's inset counts towards it), 16 at the sides.
class StickyActionBar extends StatelessWidget {
  const StickyActionBar({super.key, required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    final bottom = MediaQuery.paddingOf(context).bottom;
    return DecoratedBox(
      decoration: const BoxDecoration(
        color: SmartRailColors.surface,
        border: Border(top: BorderSide(color: SmartRailColors.border)),
      ),
      child: Padding(
        padding: EdgeInsets.fromLTRB(16, 12, 16, bottom > 28 ? bottom : 28),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: children,
        ),
      ),
    );
  }
}

/// A two-sided switch: a soft pill with a white pill under the chosen side (Depart / Arrive, Active / Past).
class SegmentedPill extends StatelessWidget {
  const SegmentedPill({
    super.key,
    required this.left,
    required this.right,
    required this.rightSelected,
    required this.onChanged,
  });

  final String left;
  final String right;
  final bool rightSelected;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    Widget segment(String text, bool selected, VoidCallback onTap) => Expanded(
      child: Semantics(
        button: true,
        selected: selected,
        label: text,
        child: ExcludeSemantics(
          child: GestureDetector(
            onTap: onTap,
            child: AnimatedContainer(
              duration: SmartRailMotion.of(context, SmartRailMotion.fast),
              constraints: const BoxConstraints(minHeight: 36),
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: selected ? SmartRailColors.surface : Colors.transparent,
                borderRadius: BorderRadius.circular(SmartRailRadius.pill),
              ),
              child: Text(
                text,
                style: SmartRailType.label14.copyWith(
                  color: selected
                      ? SmartRailColors.textPrimary
                      : SmartRailColors.textSecondary,
                ),
              ),
            ),
          ),
        ),
      ),
    );

    return Container(
      padding: const EdgeInsets.all(4),
      decoration: BoxDecoration(
        color: SmartRailColors.bgSoft,
        borderRadius: BorderRadius.circular(SmartRailRadius.pill),
      ),
      child: Row(
        children: [
          segment(left, !rightSelected, () => onChanged(false)),
          segment(right, rightSelected, () => onChanged(true)),
        ],
      ),
    );
  }
}

/// A white card with the default hairline and 16 radius, holding rows split by hairlines (Figma list cards).
class RowsCard extends StatelessWidget {
  const RowsCard({super.key, required this.children});

  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Material(
      color: SmartRailColors.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(SmartRailRadius.lg),
        side: const BorderSide(color: SmartRailColors.border),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          for (var i = 0; i < children.length; i++) ...[
            if (i > 0)
              const Divider(
                height: 1,
                thickness: 1,
                color: SmartRailColors.border,
              ),
            children[i],
          ],
        ],
      ),
    );
  }
}

/// One row of a list card: an icon (20), a 14 Semi Bold label, an optional value, and a chevron (18). 14 padding, 12 gaps.
class ActionRow extends StatelessWidget {
  const ActionRow({
    super.key,
    required this.icon,
    required this.label,
    required this.onTap,
    this.value,
    this.color,
  });

  final String icon;
  final String label;
  final String? value;
  final Color? color;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final c = color ?? SmartRailColors.textPrimary;
    return Semantics(
      button: true,
      label: value == null ? label : '$label, $value',
      child: ExcludeSemantics(
        child: InkWell(
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Row(
              children: [
                SvgIcon(
                  icon,
                  size: 20,
                  color: color ?? SmartRailColors.textSecondary,
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    label,
                    style: SmartRailType.label14.copyWith(color: c),
                  ),
                ),
                if (value != null) ...[
                  Text(
                    value!,
                    style: SmartRailType.body14.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                  const SizedBox(width: 12),
                ],
                const SvgIcon(
                  AppIcons.right,
                  size: 18,
                  color: SmartRailColors.textTertiary,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
