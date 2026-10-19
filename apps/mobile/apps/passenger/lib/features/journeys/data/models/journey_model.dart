import '../../domain/entities/journey_detail.dart';

/// Reads the `GET /api/journeys/{id}` answer.
abstract final class JourneyModel {
  static JourneyDetail fromJson(
    Map<Object?, Object?> json, {
    required DateTime fetchedAt,
    bool fromCache = false,
  }) {
    final origin = json['origin']! as Map;
    final destination = json['destination']! as Map;

    return JourneyDetail(
      journeyId: json['journeyId']! as String,
      trainNumber: json['trainNumber']! as String,
      routeCode: json['routeCode']! as String,
      serviceDate: json['serviceDate']! as String,
      timeZone: json['timeZone'] as String? ?? 'Asia/Colombo',
      status: json['status']! as String,
      confidence: json['confidence']! as String,
      originCode: origin['code']! as String,
      originName: origin['name']! as String,
      destinationCode: destination['code']! as String,
      destinationName: destination['name']! as String,
      delayMinutes: json['delayMinutes'] as int?,
      punctuality: json['punctuality'] as String?,
      isStale: json['isStale'] as bool? ?? false,
      ageSeconds: json['ageSeconds'] as int?,
      asOf: _time(json['asOfUtc']),
      fetchedAt: fetchedAt.toUtc(),
      fromCache: fromCache,
      stops: [
        for (final stop in json['stops']! as List)
          _stop((stop as Map).cast<String, Object?>()),
      ],
    );
  }

  static JourneyStop _stop(Map<String, Object?> s) => JourneyStop(
    sequence: s['sequence']! as int,
    stationCode: s['stationCode']! as String,
    stationName: s['stationName']! as String,
    state: s['state']! as String,
    estimateSource: s['estimateSource']! as String,
    scheduledArrival: _time(s['scheduledArrivalUtc']),
    scheduledDeparture: _time(s['scheduledDepartureUtc']),
    estimatedArrival: _time(s['estimatedArrivalUtc']),
    estimatedDeparture: _time(s['estimatedDepartureUtc']),
    actualArrival: _time(s['actualArrivalUtc']),
    actualDeparture: _time(s['actualDepartureUtc']),
    platform: s['platform'] as String?,
    platformChanged: s['platformState'] == 'Changed',
  );

  static DateTime? _time(Object? value) =>
      value is String ? DateTime.parse(value).toUtc() : null;
}
