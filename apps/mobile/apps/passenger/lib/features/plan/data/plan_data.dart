import 'package:fpdart/fpdart.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';

import '../domain/entities/trip.dart';
import '../domain/repositories/plan_repository.dart';

/// Reads the `GET /api/journeys/search` answer.
abstract final class TripModel {
  static TripResult fromJson(
    Map<Object?, Object?> json, {
    required DateTime fetchedAt,
    bool fromCache = false,
  }) {
    return TripResult(
      timeZone: json['timeZone']! as String,
      fromName: (json['from']! as Map)['name']! as String,
      toName: (json['to']! as Map)['name']! as String,
      fetchedAt: fetchedAt.toUtc(),
      fromCache: fromCache,
      options: [
        for (final o in json['options']! as List) _option((o as Map).cast()),
      ],
    );
  }

  static TripOption _option(Map<String, Object?> o) {
    final transfer = o['transfer'] as Map?;
    return TripOption(
      departure: _time(o['departureUtc']),
      expectedDeparture: _time(o['expectedDepartureUtc']),
      arrival: _time(o['arrivalUtc']),
      expectedArrival: _time(o['expectedArrivalUtc']),
      durationMinutes: o['durationMinutes']! as int,
      transfer: transfer == null
          ? null
          : TripTransfer(
              stationCode: transfer['stationCode']! as String,
              stationName: transfer['stationName']! as String,
              waitMinutes: transfer['waitMinutes']! as int,
              atRisk: transfer['connectionAtRisk']! as bool,
            ),
      legs: [for (final l in o['legs']! as List) _leg((l as Map).cast())],
    );
  }

  static TripLeg _leg(Map<String, Object?> l) => TripLeg(
    journeyId: l['journeyId']! as String,
    trainNumber: l['trainNumber']! as String,
    fromCode: l['fromCode']! as String,
    fromName: l['fromName']! as String,
    toCode: l['toCode']! as String,
    toName: l['toName']! as String,
    departure: _time(l['departureUtc']),
    expectedDeparture: _time(l['expectedDepartureUtc']),
    arrival: _time(l['arrivalUtc']),
    expectedArrival: _time(l['expectedArrivalUtc']),
    platform: l['platform'] as String?,
    status: l['status']! as String,
    confidence: l['confidence']! as String,
    delayMinutes: l['delayMinutes'] as int?,
  );

  static DateTime _time(Object? value) =>
      DateTime.parse(value! as String).toUtc();
}

class RemotePlanRepository implements PlanRepository {
  RemotePlanRepository(this._client);

  final ApiClient _client;

  @override
  Future<Either<Failure, TripResult>> search(TripQuery query) async {
    const path = '/api/journeys/search';
    final params = <String, Object?>{
      'from': query.from,
      'to': query.to,
      if (query.after != null) 'after': query.after!.toUtc().toIso8601String(),
    };

    final result = await _client.get(path, query: params);

    return result.match((failure) {
      final cached = _client.cachedCopy(path, query: params);
      return cached == null ? left(failure) : _parse(cached);
    }, _parse);
  }

  Either<Failure, TripResult> _parse(ApiResponse response) {
    final body = response.body;
    if (body is! Map) return left(const UnknownFailure('search.bad_response'));

    try {
      return right(
        TripModel.fromJson(
          body,
          fetchedAt: response.fetchedAt,
          fromCache: response.fromCache,
        ),
      );
    } on Object {
      return left(const UnknownFailure('search.bad_response'));
    }
  }
}
