import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/storage/key_value_store.dart';
import '../../data/local_follow_repository.dart';
import '../../domain/follow_repository.dart';

final followRepositoryProvider = Provider<FollowRepository>(
  (ref) => LocalFollowRepository(ref.watch(keyValueStoreProvider)),
);

class FollowState {
  const FollowState({this.stations = const [], this.journeys = const []});

  final List<FollowedStation> stations;
  final List<FollowedJourney> journeys;

  bool isStationFollowed(String code) => stations.any((s) => s.code == code);

  bool isJourneyFollowed(String journeyId) =>
      journeys.any((j) => j.journeyId == journeyId);
}

/// What the passenger follows. Works without an account and survives a restart; notifications for it come with SPEC-020.
final followProvider = NotifierProvider<FollowNotifier, FollowState>(
  FollowNotifier.new,
);

class FollowNotifier extends Notifier<FollowState> {
  @override
  FollowState build() {
    final repository = ref.read(followRepositoryProvider);
    return FollowState(
      stations: repository.stations(),
      journeys: repository.journeys(),
    );
  }

  Future<void> followStation(FollowedStation station) async {
    if (state.isStationFollowed(station.code)) return;
    final next = [...state.stations, station];
    state = FollowState(stations: next, journeys: state.journeys);
    await ref.read(followRepositoryProvider).saveStations(next);
  }

  Future<void> unfollowStation(String code) async {
    final next = state.stations.where((s) => s.code != code).toList();
    state = FollowState(stations: next, journeys: state.journeys);
    await ref.read(followRepositoryProvider).saveStations(next);
  }

  Future<void> followJourney(FollowedJourney journey) async {
    if (state.isJourneyFollowed(journey.journeyId)) return;
    final next = [...state.journeys, journey];
    state = FollowState(stations: state.stations, journeys: next);
    await ref.read(followRepositoryProvider).saveJourneys(next);
  }

  Future<void> unfollowJourney(String journeyId) async {
    final next = state.journeys.where((j) => j.journeyId != journeyId).toList();
    state = FollowState(stations: state.stations, journeys: next);
    await ref.read(followRepositoryProvider).saveJourneys(next);
  }
}
