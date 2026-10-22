import 'package:fpdart/fpdart.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';

import '../domain/duty_domain.dart';

/// The driver endpoints of the API (`/api/driver/...`).
class ApiDutyRepository implements DutyRepository {
  ApiDutyRepository(this._api);

  final ApiClient _api;

  @override
  Future<Either<Failure, MyDuty>> myDuty() async {
    final result = await _api.get('/api/driver/duty');
    return result.flatMap(
      (r) => _map(r.body, (json) => MyDuty.fromJson(json)),
    );
  }

  @override
  Future<Either<Failure, List<Duty>>> history() async {
    final result = await _api.get('/api/driver/duty/history');
    return result.flatMap((r) {
      final body = r.body;
      if (body is! List) return left(const UnknownFailure('bad_response'));
      return right([
        for (final d in body) Duty.fromJson((d as Map).cast<String, Object?>()),
      ]);
    });
  }

  @override
  Future<Either<Failure, Unit>> signOn(String dutyId) async =>
      (await _api.post('/api/driver/duty/$dutyId/sign-on')).map((_) => unit);

  @override
  Future<Either<Failure, Unit>> signOff(String dutyId) async =>
      (await _api.post('/api/driver/duty/$dutyId/sign-off')).map((_) => unit);

  @override
  Future<Either<Failure, DriverJourney>> journey(String journeyId) async {
    final result = await _api.get('/api/driver/journeys/$journeyId');
    return result.flatMap(
      (r) => _map(r.body, (json) => DriverJourney.fromJson(json)),
    );
  }

  @override
  Future<Either<Failure, Unit>> sendEvent({
    required String journeyId,
    required int stopSequence,
    required String type,
    required int sequence,
    required DateTime occurredAt,
  }) async {
    final result = await _api.post(
      '/api/driver/journeys/$journeyId/events',
      data: {
        'stopSequence': stopSequence,
        'type': type,
        'sequence': sequence,
        'occurredAtUtc': occurredAt.toUtc().toIso8601String(),
      },
    );
    return result.map((_) => unit);
  }

  @override
  Future<Either<Failure, Unit>> delayReason(String journeyId, String code) async =>
      (await _api.post(
        '/api/driver/journeys/$journeyId/delay-reason',
        data: {'reasonCode': code},
      )).map((_) => unit);

  Either<Failure, T> _map<T>(
    Object? body,
    T Function(Map<String, Object?> json) parse,
  ) {
    if (body is! Map) return left(const UnknownFailure('bad_response'));
    try {
      return right(parse(body.cast<String, Object?>()));
    } on Object {
      return left(const UnknownFailure('bad_response'));
    }
  }
}
