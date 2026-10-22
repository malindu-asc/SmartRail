import 'package:flutter/material.dart';

import '../theme/smartrail_theme.dart';
import '../tokens/tokens.dart';
import 'svg_icon.dart';

/// What a journey is doing, as far as a passenger needs to know (state-models.md, REQ-PAX-004). The Figma "Status chip" has the
/// four exceptions; "On time" is a quiet state: colour is reserved for exceptions.
enum StatusKind {
  onTime(AppIcons.check, SmartRailColors.textPrimary, null),
  delayed(
    AppIcons.clock,
    SmartRailColors.delayedText,
    SmartRailColors.delayedBg,
  ),
  cancelled(
    AppIcons.xc,
    SmartRailColors.cancelledText,
    SmartRailColors.cancelledBg,
  ),
  changed(
    AppIcons.swapChip,
    SmartRailColors.changedText,
    SmartRailColors.changedBg,
  ),
  noLiveData(
    AppIcons.wifiOff,
    SmartRailColors.staleText,
    SmartRailColors.staleBg,
  );

  const StatusKind(this.icon, this.color, this.background);

  final String icon;
  final Color color;

  /// Null for the quiet on-time state.
  final Color? background;
}

/// A status shown by an icon, **words** and a colour. Colour is never the only signal (REQ-PAX-004), and a screen reader reads
/// the label. The text comes from the caller, so wording and translation stay in the app.
class StatusChip extends StatelessWidget {
  const StatusChip({super.key, required this.kind, required this.label});

  final StatusKind kind;
  final String label;

  @override
  Widget build(BuildContext context) {
    final minimum = SmartRailSpec.of(context).minTextSize;
    final size = SmartRailType.caption.fontSize! < minimum
        ? minimum
        : SmartRailType.caption.fontSize!;
    final text = Text(
      label,
      style: SmartRailType.caption.copyWith(color: kind.color, fontSize: size),
    );

    return Semantics(
      container: true,
      label: label,
      child: ExcludeSemantics(
        child: kind == StatusKind.onTime
            ? Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  SvgIcon(kind.icon, size: 16, color: kind.color),
                  const SizedBox(width: SmartRailSpacing.xs),
                  Flexible(child: text),
                ],
              )
            : _Pill(
                kind: kind,
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    SvgIcon(kind.icon, size: 14, color: kind.color),
                    const SizedBox(width: SmartRailSpacing.xs),
                    Flexible(child: text),
                  ],
                ),
              ),
      ),
    );
  }
}

// Padding 10 by 4 and a pill. "No live data" has a dashed outline instead of a plain fill.
class _Pill extends StatelessWidget {
  const _Pill({required this.kind, required this.child});

  final StatusKind kind;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final content = Padding(
      padding: const EdgeInsets.symmetric(
        horizontal: 10,
        vertical: SmartRailSpacing.xs,
      ),
      child: child,
    );
    final decorated = DecoratedBox(
      decoration: BoxDecoration(
        color: kind.background,
        borderRadius: BorderRadius.circular(SmartRailRadius.pill),
      ),
      child: content,
    );

    if (kind != StatusKind.noLiveData) return decorated;

    return CustomPaint(
      foregroundPainter: _DashedPillPainter(SmartRailColors.staleBorder),
      child: decorated,
    );
  }
}

class _DashedPillPainter extends CustomPainter {
  _DashedPillPainter(this.color);

  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1;
    final path = Path()
      ..addRRect(
        RRect.fromRectAndRadius(
          Offset.zero & size,
          Radius.circular(size.height / 2),
        ),
      );

    for (final metric in path.computeMetrics()) {
      for (var at = 0.0; at < metric.length; at += 5) {
        canvas.drawPath(
          metric.extractPath(
            at,
            at + 3 > metric.length ? metric.length : at + 3,
          ),
          paint,
        );
      }
    }
  }

  @override
  bool shouldRepaint(_DashedPillPainter old) => old.color != color;
}
