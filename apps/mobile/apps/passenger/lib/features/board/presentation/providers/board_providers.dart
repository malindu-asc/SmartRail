import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';

import '../../data/repositories/remote_board_repository.dart';
import '../../domain/entities/departure.dart';
import '../../domain/repositories/board_repository.dart';

final boardRepositoryProvider = Provider<BoardRepository>(
  (ref) => RemoteBoardRepository(ref.watch(apiClientProvider)),
);

/// The live board of one station. A screen refreshes it every 10 seconds while it is actually visible (`PollingScope`, SPEC-009 section 5.3). A failed refresh
/// keeps the last good board on screen.
final boardProvider = AsyncNotifierProvider.autoDispose
    .family<BoardNotifier, BoardSnapshot, String>(BoardNotifier.new);

class BoardNotifier extends AsyncNotifier<BoardSnapshot> {
  BoardNotifier(this.stationCode);

  final String stationCode;

  @override
  Future<BoardSnapshot> build() {
    return _load();
  }

  Future<BoardSnapshot> _load() async {
    final result = await ref.read(boardRepositoryProvider).fetch(stationCode);
    return result.match((failure) => throw failure, (snapshot) => snapshot);
  }

  Future<void> refresh() async {
    final next = await AsyncValue.guard(_load);
    if (!ref.mounted) return;

    // Nothing could be fetched and nothing is cached: keep what is on screen rather than replacing a board with an error.
    if (next.hasError && state.hasValue) return;
    state = next;
  }
}
