import 'package:flutter/material.dart';

import '../tokens/tokens.dart';

/// A rounded box with a dashed outline: the look of "no live data" (the Figma status chip and the no-live-data journey card).
class DashedBox extends StatelessWidget {
  const DashedBox({
    super.key,
    required this.child,
    this.color = SmartRailColors.staleBorder,
    this.background = SmartRailColors.staleBg,
    this.radius = SmartRailRadius.md,
    this.padding = EdgeInsets.zero,
  });

  final Widget child;
  final Color color;
  final Color background;
  final double radius;
  final EdgeInsetsGeometry padding;

  @override
  Widget build(BuildContext context) {
    return CustomPaint(
      foregroundPainter: _DashedRRectPainter(color: color, radius: radius),
      child: Container(
        padding: padding,
        decoration: BoxDecoration(
          color: background,
          borderRadius: BorderRadius.circular(radius),
        ),
        child: child,
      ),
    );
  }
}

class _DashedRRectPainter extends CustomPainter {
  _DashedRRectPainter({required this.color, required this.radius});

  final Color color;
  final double radius;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1;
    final path = Path()
      ..addRRect(
        RRect.fromRectAndRadius(Offset.zero & size, Radius.circular(radius)),
      );

    for (final metric in path.computeMetrics()) {
      for (var at = 0.0; at < metric.length; at += 6) {
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
  bool shouldRepaint(_DashedRRectPainter old) =>
      old.color != color || old.radius != radius;
}
