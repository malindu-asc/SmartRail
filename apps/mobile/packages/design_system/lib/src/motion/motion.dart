import 'package:flutter/material.dart';

/// Motion is small and purposeful: it shows where something came from or that something changed, never decoration.
/// Everything here switches off when the phone's reduce-motion setting is on (`MediaQuery.disableAnimations`).
abstract final class SmartRailMotion {
  static const fast = Duration(milliseconds: 120);
  static const base = Duration(milliseconds: 200);
  static const slow = Duration(milliseconds: 300);

  static const curve = Curves.easeOutCubic;

  /// Whether the user asked for no motion.
  static bool reduced(BuildContext context) => MediaQuery.maybeDisableAnimationsOf(context) ?? false;

  /// [duration], or no time at all when motion is reduced.
  static Duration of(BuildContext context, Duration duration) => reduced(context) ? Duration.zero : duration;
}

/// A tap target that gives a very small press response (0.98) so touch feels alive. No effect when motion is reduced.
class PressableScale extends StatefulWidget {
  const PressableScale({super.key, required this.child, this.enabled = true});

  final Widget child;
  final bool enabled;

  @override
  State<PressableScale> createState() => _PressableScaleState();
}

class _PressableScaleState extends State<PressableScale> {
  bool _down = false;

  void _set(bool value) {
    if (widget.enabled && _down != value) setState(() => _down = value);
  }

  @override
  Widget build(BuildContext context) {
    final scale = _down && !SmartRailMotion.reduced(context) ? 0.98 : 1.0;
    return Listener(
      onPointerDown: (_) => _set(true),
      onPointerUp: (_) => _set(false),
      onPointerCancel: (_) => _set(false),
      child: AnimatedScale(
        scale: scale,
        duration: SmartRailMotion.of(context, const Duration(milliseconds: 100)),
        curve: SmartRailMotion.curve,
        child: widget.child,
      ),
    );
  }
}

/// A one-time fade and short rise for the first screenful of a list. Each item waits a little longer than the one before, up to a
/// limit, so the first load feels calm. A refresh must not use it.
class AppearIn extends StatefulWidget {
  const AppearIn({super.key, required this.index, required this.child});

  final int index;
  final Widget child;

  @override
  State<AppearIn> createState() => _AppearInState();
}

class _AppearInState extends State<AppearIn> with SingleTickerProviderStateMixin {
  late final AnimationController _controller = AnimationController(vsync: this, duration: SmartRailMotion.slow);
  late final Animation<double> _opacity = CurvedAnimation(parent: _controller, curve: SmartRailMotion.curve);
  bool _started = false;

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (_started) return;
    _started = true;

    if (SmartRailMotion.reduced(context)) {
      _controller.value = 1;
      return;
    }

    final delay = Duration(milliseconds: 30 * widget.index.clamp(0, 8));
    Future<void>.delayed(delay, () {
      if (mounted) _controller.forward();
    });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return FadeTransition(
      opacity: _opacity,
      child: AnimatedBuilder(
        animation: _opacity,
        builder: (context, child) => Transform.translate(offset: Offset(0, 8 * (1 - _opacity.value)), child: child),
        child: widget.child,
      ),
    );
  }
}

/// Cross-fades when [child] is replaced (a status that changes from on time to delayed), so the change is noticed, not announced.
class ChangeSwitcher extends StatelessWidget {
  const ChangeSwitcher({super.key, required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    return AnimatedSwitcher(
      duration: SmartRailMotion.of(context, SmartRailMotion.base),
      switchInCurve: SmartRailMotion.curve,
      child: child,
    );
  }
}
