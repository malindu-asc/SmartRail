import 'package:flutter/material.dart';

import '../motion/motion.dart';
import '../tokens/tokens.dart';
import 'smartrail_button.dart';

/// "As of 14:31": data that is shown but not fresh. Stale is shown, never hidden (REQ-PAX-005). The text comes from the caller.
class StalenessLabel extends StatelessWidget {
  const StalenessLabel({super.key, required this.text});

  final String text;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      label: text,
      child: ExcludeSemantics(
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.history, size: 14, color: SmartRailColors.staleText),
            const SizedBox(width: SmartRailSpacing.xs),
            Flexible(child: Text(text, style: SmartRailType.caption.copyWith(color: SmartRailColors.staleText))),
          ],
        ),
      ),
    );
  }
}

/// Nothing to show yet, and why.
class EmptyState extends StatelessWidget {
  const EmptyState({super.key, required this.icon, required this.title, this.message});

  final IconData icon;
  final String title;
  final String? message;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(SmartRailSpacing.xl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 48, color: SmartRailColors.textTertiary),
            const SizedBox(height: SmartRailSpacing.lg),
            Text(title, style: SmartRailType.title, textAlign: TextAlign.center),
            if (message != null) ...[
              const SizedBox(height: SmartRailSpacing.sm),
              Text(message!, style: SmartRailType.body.copyWith(color: SmartRailColors.textSecondary), textAlign: TextAlign.center),
            ],
          ],
        ),
      ),
    );
  }
}

/// Something failed. The words come from the caller, which chooses them from the kind of failure.
class ErrorState extends StatelessWidget {
  const ErrorState({super.key, required this.title, required this.message, this.retryLabel, this.onRetry});

  final String title;
  final String message;
  final String? retryLabel;
  final VoidCallback? onRetry;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(SmartRailSpacing.xl),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline, size: 48, color: SmartRailColors.cancelledText),
            const SizedBox(height: SmartRailSpacing.lg),
            Text(title, style: SmartRailType.title, textAlign: TextAlign.center),
            const SizedBox(height: SmartRailSpacing.sm),
            Text(message, style: SmartRailType.body.copyWith(color: SmartRailColors.textSecondary), textAlign: TextAlign.center),
            if (onRetry != null && retryLabel != null) ...[
              const SizedBox(height: SmartRailSpacing.xl),
              SmartRailButton(label: retryLabel!, onPressed: onRetry, icon: Icons.refresh),
            ],
          ],
        ),
      ),
    );
  }
}

/// A soft grey block shown where content is loading. It breathes very slowly so it reads as "loading", and not at all when motion
/// is reduced. It has no meaning for a screen reader.
class Skeleton extends StatefulWidget {
  const Skeleton({super.key, this.width, this.height = 16});

  final double? width;
  final double height;

  @override
  State<Skeleton> createState() => _SkeletonState();
}

class _SkeletonState extends State<Skeleton> with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(vsync: this, duration: const Duration(milliseconds: 1100));

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (SmartRailMotion.reduced(context)) {
      _controller.stop();
    } else if (!_controller.isAnimating) {
      _controller.repeat(reverse: true);
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ExcludeSemantics(
      child: AnimatedBuilder(
        animation: _controller,
        builder: (context, _) => Container(
          width: widget.width,
          height: widget.height,
          decoration: BoxDecoration(
            color: Color.lerp(SmartRailColors.bgSoft, SmartRailColors.border, _controller.value),
            borderRadius: BorderRadius.circular(SmartRailRadius.sm),
          ),
        ),
      ),
    );
  }
}
