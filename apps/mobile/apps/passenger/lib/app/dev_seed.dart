import 'dart:convert';
import 'dart:io';

import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';

import '../core/storage/key_value_store.dart';
import '../features/follow/data/local_follow_repository.dart';
import '../features/follow/domain/follow_repository.dart';

/// Debug builds only, once per install: follows a few stations and the running demo trains the backend's development scenarios put on
/// the line, so Home, Departures and Alerts have real trains to show straight away. Release builds never run this.
Future<void> seedDemoFollows(SharedPreferences prefs) async {
  const doneKey = 'dev_seed_follows_v1';
  if (!kDebugMode || prefs.getBool(doneKey) == true) return;

  final repository = LocalFollowRepository(SharedPrefsStore(prefs));
  final client = HttpClient()..connectionTimeout = const Duration(seconds: 3);

  try {
    final stations = <FollowedStation>[];
    final journeys = <FollowedJourney>[];

    for (final code in const ['CEN', 'MIL', 'HIL']) {
      final board = await _get(client, '/api/journeys/boards/$code?count=20');
      if (board is! Map) continue;

      final station = board['station'] as Map?;
      if (station != null) {
        stations.add(
          FollowedStation(
            code: station['code']! as String,
            name: station['name']! as String,
          ),
        );
      }

      // The demo trains are numbered L101, L201 and so on; follow the ones that are running.
      for (final item in (board['items'] as List? ?? const [])) {
        final train = item as Map;
        final number = train['trainNumber'] as String? ?? '';
        final id = train['journeyId'] as String;
        if (!number.startsWith('L') || journeys.any((j) => j.journeyId == id)) {
          continue;
        }
        journeys.add(
          FollowedJourney(
            journeyId: id,
            trainNumber: number,
            originName: (station?['name'] as String?) ?? code,
            destinationName: train['destinationName']! as String,
            scheduledDeparture: DateTime.parse(
              train['scheduledDepartureUtc']! as String,
            ).toUtc(),
          ),
        );
      }
    }

    // Nothing reached: try again next launch, when the backend is up.
    if (stations.isEmpty) return;

    await repository.saveStations(stations);
    await repository.saveJourneys(journeys);
    await prefs.setBool(doneKey, true);
  } on Object {
    // The backend is not running: carry on with an empty Home.
  } finally {
    client.close();
  }
}

Future<Object?> _get(HttpClient client, String path) async {
  final request = await client.getUrl(Uri.parse('${ApiConfig.baseUrl}$path'));
  final response = await request.close();
  if (response.statusCode != 200) return null;
  return jsonDecode(await response.transform(utf8.decoder).join());
}
