import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';

import '../../data/repositories/remote_journey_repository.dart';
import '../../domain/entities/journey_detail.dart';
import '../../domain/repositories/journey_repository.dart';

final journeyRepositoryProvider = Provider<JourneyRepository>(
  (ref) => RemoteJourneyRepository(ref.watch(apiClientProvider)),
);

/// One journey with its stops, refreshed by the screen while it is visible (same rules as the board).
final journeyProvider = AsyncNotifierProvider.autoDispose
    .family<JourneyNotifier, JourneyDetail, String>(JourneyNotifier.new);

class JourneyNotifier extends AsyncNotifier<JourneyDetail> {
  JourneyNotifier(this.journeyId);

  final String journeyId;

  @override
  Future<JourneyDetail> build() {
    return _load();
  }

  Future<JourneyDetail> _load() async {
    final result = await ref.read(journeyRepositoryProvider).fetch(journeyId);
    return result.match((failure) => throw failure, (journey) => journey);
  }

  Future<void> refresh() async {
    final next = await AsyncValue.guard(_load);
    if (!ref.mounted) return;

    if (next.hasError && state.hasValue) return;
    state = next;
  }
}
