import '../../domain/entities/departure.dart';

/// Reads the `GET /api/journeys/boards/{code}` answer.
abstract final class BoardModel {
  static BoardSnapshot fromJson(
    Map<Object?, Object?> json, {
    required DateTime fetchedAt,
    bool fromCache = false,
  }) {
    final station = json['station']! as Map;

    return BoardSnapshot(
      stationCode: station['code']! as String,
      stationName: station['name']! as String,
      timeZone: json['timeZone'] as String? ?? 'Asia/Colombo',
      asOf: DateTime.parse(json['asOfUtc']! as String).toUtc(),
      fetchedAt: fetchedAt.toUtc(),
      fromCache: fromCache,
      items: [
        for (final item in json['items']! as List)
          _departure((item as Map).cast<String, Object?>()),
      ],
    );
  }

  static Departure _departure(Map<String, Object?> item) => Departure(
    journeyId: item['journeyId']! as String,
    trainNumber: item['trainNumber']! as String,
    routeCode: item['routeCode']! as String,
    destinationCode: item['destinationCode']! as String,
    destinationName: item['destinationName']! as String,
    scheduled: DateTime.parse(item['scheduledDepartureUtc']! as String).toUtc(),
    expected: DateTime.parse(item['expectedDepartureUtc']! as String).toUtc(),
    estimateSource: item['estimateSource']! as String,
    delayMinutes: item['delayMinutes'] as int?,
    punctuality: item['punctuality'] as String?,
    platform: item['platform'] as String?,
    platformChanged: item['platformState'] == 'Changed',
    status: item['status']! as String,
    confidence: item['confidence']! as String,
    isStale: item['isStale'] as bool? ?? false,
    ageSeconds: item['ageSeconds'] as int?,
  );
}
