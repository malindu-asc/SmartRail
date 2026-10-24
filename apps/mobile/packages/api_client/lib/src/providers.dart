import 'dart:async';

import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'api_client.dart';

/// The language the app currently shows, as a language code (`en`, `si`). Each app overrides this with its own locale state, and
/// the API client sends it as `Accept-Language`.
final localeCodeProvider = Provider<String>((ref) => 'en');

/// The one API client of the app. Reads the current language each time it makes a request.
final apiClientProvider = Provider<ApiClient>(
  (ref) => ApiClient(
    locale: () => ref.read(localeCodeProvider),
    token: () => ref.read(accessTokenProvider)(),
  ),
);

/// Gives the sign-in token for a request, or null when nobody is signed in. The app replaces it with its sign-in state.
final accessTokenProvider = Provider<Future<String?> Function()>(
  (ref) =>
      () async => null,
);

/// Asks the platform whether it can be reached. The default calls a cheap public read; tests replace it.
abstract interface class ApiProbe {
  Future<bool> isReachable();
}

class HttpApiProbe implements ApiProbe {
  HttpApiProbe(this._client);

  final ApiClient _client;

  @override
  Future<bool> isReachable() async =>
      (await _client.get('/api/network/routes')).isRight();
}

final apiProbeProvider = Provider<ApiProbe>(
  (ref) => HttpApiProbe(ref.watch(apiClientProvider)),
);

/// How often the app asks again while it is open. Zero turns polling off.
final reachabilityIntervalProvider = Provider<Duration>(
  (ref) => const Duration(seconds: 30),
);

/// Whether the API could be reached at the last check, and when it last could (REQ-NFR-006).
class Reachability {
  const Reachability({required this.online, this.lastSuccess});

  final bool online;
  final DateTime? lastSuccess;
}

final reachabilityClockProvider = Provider<DateTime Function()>(
  (ref) => DateTime.now,
);

final reachabilityProvider =
    NotifierProvider<ReachabilityNotifier, Reachability>(
      ReachabilityNotifier.new,
    );

class ReachabilityNotifier extends Notifier<Reachability> {
  @override
  Reachability build() {
    // An interval of zero means no polling (tests, and screens that never need it).
    final interval = ref.watch(reachabilityIntervalProvider);
    if (interval > Duration.zero) {
      final timer = Timer.periodic(interval, (_) => checkNow());
      ref.onDispose(timer.cancel);
    }

    // Not shown as offline until a check has actually failed, so the banner does not flash on start.
    Future.microtask(checkNow);
    return const Reachability(online: true);
  }

  Future<void> checkNow() async {
    final ok = await ref.read(apiProbeProvider).isReachable();
    if (!ref.mounted) return;

    state = ok
        ? Reachability(
            online: true,
            lastSuccess: ref.read(reachabilityClockProvider)(),
          )
        : Reachability(online: false, lastSuccess: state.lastSuccess);
  }
}

/// Checks again when the app comes back to the foreground.
class ReachabilityLifecycle extends ConsumerStatefulWidget {
  const ReachabilityLifecycle({super.key, required this.child});

  final Widget child;

  @override
  ConsumerState<ReachabilityLifecycle> createState() =>
      _ReachabilityLifecycleState();
}

class _ReachabilityLifecycleState extends ConsumerState<ReachabilityLifecycle> {
  late final AppLifecycleListener _listener = AppLifecycleListener(
    onResume: () => ref.read(reachabilityProvider.notifier).checkNow(),
  );

  @override
  void dispose() {
    _listener.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => widget.child;
}
