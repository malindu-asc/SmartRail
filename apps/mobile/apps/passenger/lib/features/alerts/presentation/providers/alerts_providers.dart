import 'dart:async';
import 'dart:convert';

import 'package:flutter/widgets.dart' hide Baseline;
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/polling/poller.dart';
import '../../../../core/storage/key_value_store.dart';
import '../../../board/presentation/providers/board_providers.dart';
import '../../../follow/presentation/providers/follow_providers.dart';
import '../../../journeys/domain/entities/journey_detail.dart';
import '../../../journeys/presentation/providers/journey_providers.dart';
import '../../../settings/presentation/providers/settings_providers.dart';
import '../../domain/alerts_domain.dart';

/// How often the followed trains are looked at while the app is open (phone push messages are not part of this version).
final alertsPollIntervalProvider = Provider<Duration>(
  (ref) => const Duration(seconds: 30),
);

/// The alerts raised so far, newest first, kept on the phone.
final alertsProvider = NotifierProvider<AlertsNotifier, List<AlertItem>>(
  AlertsNotifier.new,
);

class AlertsNotifier extends Notifier<List<AlertItem>> {
  static const _alertsKey = 'alerts';
  static const _baselinesKey = 'alert_baselines';
  static const limit = 100;

  int _counter = 0;

  @override
  List<AlertItem> build() {
    final saved = ref.read(keyValueStoreProvider).getString(_alertsKey);
    if (saved == null) return const [];
    try {
      return [
        for (final j in jsonDecode(saved) as List)
          ?AlertItem.fromJson((j as Map).cast<String, Object?>()),
      ];
    } on Object {
      return const [];
    }
  }

  Map<String, Baseline> _baselines() {
    final saved = ref.read(keyValueStoreProvider).getString(_baselinesKey);
    if (saved == null) return {};
    try {
      return {
        for (final e in (jsonDecode(saved) as Map).entries)
          e.key as String: Baseline.fromJson(
            (e.value as Map).cast<String, Object?>(),
          ),
      };
    } on Object {
      return {};
    }
  }

  /// Applies one look at a train. Returns the alerts it raised.
  List<AlertItem> observe(
    String key,
    Observed seen,
    DateTime now,
    Map<String, Baseline> baselines,
  ) {
    final (next, alerts) = AlertRules.detect(
      baseline: baselines[key],
      seen: seen,
      now: now,
      newId: () => '${now.microsecondsSinceEpoch}-${_counter++}',
    );
    baselines[key] = next;

    // The same news from a followed station and a followed journey is told once.
    final fresh = alerts.where(
      (a) => !state.any(
        (old) =>
            old.journeyId == a.journeyId &&
            old.kind == a.kind &&
            old.newPlatform == a.newPlatform &&
            old.minutes == a.minutes &&
            now.difference(old.at) < const Duration(minutes: 10),
      ),
    );
    return fresh.toList();
  }

  Future<void> commit(
    List<AlertItem> added,
    Map<String, Baseline> baselines,
    Set<String> keep,
  ) async {
    final store = ref.read(keyValueStoreProvider);
    baselines.removeWhere((k, _) => !keep.contains(k));
    await store.setString(
      _baselinesKey,
      jsonEncode({for (final e in baselines.entries) e.key: e.value.toJson()}),
    );

    if (added.isEmpty) return;
    state = [...added.reversed, ...state].take(limit).toList();
    await store.setString(
      _alertsKey,
      jsonEncode([for (final a in state) a.toJson()]),
    );
  }

  Future<void> clear() async {
    state = const [];
    await ref.read(keyValueStoreProvider).setString(_alertsKey, '[]');
  }

  /// One round: look at every followed journey and station once. Public so tests can run it without a timer.
  Future<void> checkOnce() async {
    if (!ref.read(alertsOnProvider)) return;

    final follow = ref.read(followProvider);
    if (follow.journeys.isEmpty && follow.stations.isEmpty) return;

    final now = DateTime.now().toUtc();
    final baselines = _baselines();
    final keep = <String>{};
    final added = <AlertItem>[];

    for (final journey in follow.journeys) {
      final result = await ref
          .read(journeyRepositoryProvider)
          .fetch(journey.journeyId);
      final detail = result.toNullable();
      if (detail == null) continue;

      final key = 'j:${journey.journeyId}';
      keep.add(key);
      added.addAll(observe(key, _fromJourney(detail), now, baselines));
    }

    for (final station in follow.stations) {
      final result = await ref
          .read(boardRepositoryProvider)
          .fetch(station.code);
      final board = result.toNullable();
      if (board == null) continue;

      for (final d in board.items) {
        final key = 's:${station.code}:${d.journeyId}';
        keep.add(key);
        added.addAll(
          observe(
            key,
            Observed(
              journeyId: d.journeyId,
              scheduledDeparture: d.scheduled,
              originName: station.name,
              destinationName: d.destinationName,
              cancelled: d.isCancelled,
              delayMinutes: d.delayMinutes ?? 0,
              expectedAt: d.expected,
              platform: d.platform,
            ),
            now,
            baselines,
          ),
        );
      }
    }

    await commit(added, baselines, keep);
  }

  static Observed _fromJourney(JourneyDetail detail) {
    final next = detail.stops.where((s) => s.state != 'Departed').firstOrNull;
    final last = detail.stops.isEmpty ? null : detail.stops.last;
    final scheduled = detail.scheduledDeparture ?? DateTime.now().toUtc();
    final delay = detail.delayMinutes ?? 0;

    return Observed(
      journeyId: detail.journeyId,
      scheduledDeparture: scheduled,
      originName: detail.originName,
      destinationName: detail.destinationName,
      cancelled: detail.status == 'Cancelled',
      delayMinutes: delay,
      expectedAt:
          last?.estimatedArrival ??
          last?.scheduledArrival ??
          scheduled.add(Duration(minutes: delay)),
      platform: next?.platform,
    );
  }
}

/// Looks at the followed trains on a timer while the app is open. `PassengerApp` keeps it alive.
final alertWatcherProvider = Provider<void>((ref) {
  Timer? timer;

  void sync() {
    timer?.cancel();
    timer = null;

    final interval = ref.read(alertsPollIntervalProvider);
    final foreground =
        ref.read(appLifecycleProvider) == AppLifecycleState.resumed;
    if (!foreground || interval <= Duration.zero) return;

    timer = Timer.periodic(
      interval,
      (_) => ref.read(alertsProvider.notifier).checkOnce(),
    );
  }

  ref.listen(appLifecycleProvider, (_, _) => sync());
  ref.onDispose(() => timer?.cancel());
  sync();
});
