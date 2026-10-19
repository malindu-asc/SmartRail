import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';

import '../../../../core/storage/key_value_store.dart';
import '../../data/repositories/station_repositories.dart';
import '../../domain/entities/station.dart';
import '../../domain/repositories/station_repository.dart';

final stationRepositoryProvider = Provider<StationRepository>(
  (ref) => RemoteStationRepository(ref.watch(apiClientProvider)),
);

final recentStationsRepositoryProvider = Provider<RecentStationsRepository>(
  (ref) => LocalRecentStationsRepository(ref.watch(keyValueStoreProvider)),
);

/// The last 5 stations looked at, newest first.
final recentStationsProvider =
    NotifierProvider<RecentStationsNotifier, List<Station>>(
      RecentStationsNotifier.new,
    );

class RecentStationsNotifier extends Notifier<List<Station>> {
  @override
  List<Station> build() => ref.read(recentStationsRepositoryProvider).recent();

  Future<void> add(Station station) async {
    final repository = ref.read(recentStationsRepositoryProvider);
    await repository.add(station);
    state = repository.recent();
  }
}

/// Searches stations as the passenger types, waiting a moment after the last key so the API is not asked for every letter.
final stationSearchProvider =
    NotifierProvider.autoDispose<
      StationSearchNotifier,
      AsyncValue<List<Station>>
    >(StationSearchNotifier.new);

class StationSearchNotifier extends Notifier<AsyncValue<List<Station>>> {
  static const debounce = Duration(milliseconds: 250);

  Timer? _timer;
  int _generation = 0;

  @override
  AsyncValue<List<Station>> build() {
    ref.onDispose(() => _timer?.cancel());
    // Nothing typed yet: every station is listed, so the passenger can simply pick one.
    Future.microtask(() => queryChanged(''));
    return const AsyncLoading<List<Station>>();
  }

  /// An empty query lists every station; typing narrows the list a moment after the last key.
  void queryChanged(String query) {
    _timer?.cancel();
    final trimmed = query.trim();
    final generation = ++_generation;

    Future<void> load() async {
      // The API wants two letters; one letter is matched here against the full list.
      final one = trimmed.length == 1;
      final result = await ref
          .read(stationRepositoryProvider)
          .search(one ? '' : trimmed);
      // A newer query has been typed since: this answer is out of date.
      if (!ref.mounted || generation != _generation) return;
      final needle = trimmed.toLowerCase();
      state = result.match(
        (failure) => AsyncError<List<Station>>(failure, StackTrace.current),
        (stations) => AsyncData(
          one
              ? stations
                    .where(
                      (s) =>
                          s.name.toLowerCase().contains(needle) ||
                          s.code.toLowerCase().startsWith(needle),
                    )
                    .toList()
              : stations,
        ),
      );
    }

    if (trimmed.isEmpty) {
      load();
      return;
    }

    state = const AsyncLoading<List<Station>>();
    _timer = Timer(debounce, load);
  }
}
