import 'dart:convert';

import '../../../core/storage/key_value_store.dart';
import '../domain/follow_repository.dart';

class LocalFollowRepository implements FollowRepository {
  LocalFollowRepository(this._store);

  static const _stationsKey = 'followed_stations';
  static const _journeysKey = 'followed_journeys';

  final KeyValueStore _store;

  @override
  List<FollowedStation> stations() =>
      _read(_stationsKey, FollowedStation.fromJson);

  @override
  List<FollowedJourney> journeys() =>
      _read(_journeysKey, FollowedJourney.fromJson);

  @override
  Future<void> saveStations(List<FollowedStation> stations) => _store.setString(
    _stationsKey,
    jsonEncode([for (final s in stations) s.toJson()]),
  );

  @override
  Future<void> saveJourneys(List<FollowedJourney> journeys) => _store.setString(
    _journeysKey,
    jsonEncode([for (final j in journeys) j.toJson()]),
  );

  List<T> _read<T>(String key, T Function(Map<String, Object?>) fromJson) {
    final raw = _store.getString(key);
    if (raw == null) return const [];

    try {
      return [
        for (final item in jsonDecode(raw) as List)
          fromJson((item as Map).cast<String, Object?>()),
      ];
    } on Object {
      return const []; // a damaged value is the same as none
    }
  }
}
