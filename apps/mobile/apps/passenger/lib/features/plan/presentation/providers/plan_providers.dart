import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';

import '../../data/plan_data.dart';
import '../../domain/entities/trip.dart';
import '../../domain/repositories/plan_repository.dart';

final planRepositoryProvider = Provider<PlanRepository>(
  (ref) => RemotePlanRepository(ref.watch(apiClientProvider)),
);

/// The trains between two stations. A failure is thrown so the screen shows it with Try again; a screen refreshes it while visible.
final tripSearchProvider = AsyncNotifierProvider.autoDispose
    .family<TripSearchNotifier, TripResult, TripQuery>(TripSearchNotifier.new);

class TripSearchNotifier extends AsyncNotifier<TripResult> {
  TripSearchNotifier(this.query);

  final TripQuery query;

  @override
  Future<TripResult> build() => _load();

  Future<TripResult> _load() async {
    final result = await ref.read(planRepositoryProvider).search(query);
    return result.match((failure) => throw failure, (trip) => trip);
  }

  Future<void> refresh() async {
    final next = await AsyncValue.guard(_load);
    if (!ref.mounted) return;
    if (next.hasError && state.hasValue) return;
    state = next;
  }
}
