import 'dart:async';

import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// The state of the app: in the foreground ([AppLifecycleState.resumed]) or not. Polling pauses when it is not.
final appLifecycleProvider =
    NotifierProvider<AppLifecycleNotifier, AppLifecycleState>(
      AppLifecycleNotifier.new,
    );

class AppLifecycleNotifier extends Notifier<AppLifecycleState> {
  @override
  AppLifecycleState build() {
    final listener = AppLifecycleListener(
      onStateChange: (next) => state = next,
    );
    ref.onDispose(listener.dispose);
    return AppLifecycleState.resumed;
  }
}

/// How often a visible screen refreshes. The API answers from a 5 second cache, so this does not hammer it (SPEC-009 section 5.3).
final pollIntervalProvider = Provider<Duration>(
  (ref) => const Duration(seconds: 10),
);

/// Calls [onTick] every poll interval while its child is **actually on screen** and the app is in the foreground, and not otherwise.
///
/// "On screen" means the screen is not hidden behind another tab or another route (`TickerMode`), because those screens stay built but
/// nobody is looking at them. Coming back to the screen, or back to the app, refreshes at once (SPEC-009 section 5.3).
class PollingScope extends ConsumerStatefulWidget {
  const PollingScope({super.key, required this.onTick, required this.child});

  final VoidCallback onTick;
  final Widget child;

  @override
  ConsumerState<PollingScope> createState() => _PollingScopeState();
}

class _PollingScopeState extends ConsumerState<PollingScope> {
  Timer? _timer;
  bool? _wasActive;

  @override
  void initState() {
    super.initState();
    ref.listenManual(appLifecycleProvider, (previous, next) => _sync());
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    _sync();
  }

  void _sync() {
    if (!mounted) return;

    final active =
        TickerMode.valuesOf(context).enabled &&
        ref.read(appLifecycleProvider) == AppLifecycleState.resumed;

    if (active == _wasActive) return;
    final first = _wasActive == null;
    _wasActive = active;

    _timer?.cancel();
    _timer = null;
    if (!active) return;

    // The first time on screen the data was just loaded; later, coming back refreshes at once.
    if (!first) Future<void>.microtask(widget.onTick);

    final interval = ref.read(pollIntervalProvider);
    if (interval > Duration.zero) {
      _timer = Timer.periodic(interval, (_) => widget.onTick());
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}
