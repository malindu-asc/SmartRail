import 'dart:convert';

import 'package:fpdart/fpdart.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';

import '../../../../core/storage/key_value_store.dart';
import '../../domain/entities/station.dart';
import '../../domain/repositories/station_repository.dart';

class RemoteStationRepository implements StationRepository {
  RemoteStationRepository(this._client);

  final ApiClient _client;

  @override
  Future<Either<Failure, List<Station>>> search(String query) async {
    final trimmed = query.trim();
    final result = await _client.get(
      '/api/network/stations',
      query: {if (trimmed.isNotEmpty) 'q': trimmed},
    );

    return result.flatMap((response) {
      final body = response.body;
      if (body is! Map || body['items'] is! List) {
        return left(const UnknownFailure('stations.bad_response'));
      }

      return right([
        for (final item in body['items'] as List)
          if (item is Map)
            Station(
              code: item['code'] as String,
              name: item['name'] as String,
              facilities: [
                for (final f in (item['facilities'] as List? ?? const []))
                  f.toString(),
              ],
            ),
      ]);
    });
  }
}

class LocalRecentStationsRepository implements RecentStationsRepository {
  LocalRecentStationsRepository(this._store);

  static const _key = 'recent_stations';
  static const _limit = 5;

  final KeyValueStore _store;

  @override
  List<Station> recent() {
    final raw = _store.getString(_key);
    if (raw == null) return const [];

    try {
      return [
        for (final item in jsonDecode(raw) as List)
          Station.fromSaved((item as Map).cast<String, Object?>()),
      ];
    } on Object {
      return const []; // a damaged value is the same as none
    }
  }

  @override
  Future<void> add(Station station) async {
    final next = [
      station,
      ...recent().where((s) => s.code != station.code),
    ].take(_limit).toList();
    await _store.setString(
      _key,
      jsonEncode([for (final s in next) s.toJson()]),
    );
  }
}
