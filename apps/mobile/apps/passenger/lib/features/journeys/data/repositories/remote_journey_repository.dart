import 'package:fpdart/fpdart.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';

import '../../domain/entities/journey_detail.dart';
import '../../domain/repositories/journey_repository.dart';
import '../models/journey_model.dart';

class RemoteJourneyRepository implements JourneyRepository {
  RemoteJourneyRepository(this._client);

  final ApiClient _client;

  @override
  Future<Either<Failure, JourneyDetail>> fetch(String journeyId) async {
    final path = '/api/journeys/$journeyId';

    final result = await _client.get(path);

    return result.match((failure) {
      final cached = _client.cachedCopy(path);
      return cached == null ? left(failure) : _parse(cached);
    }, _parse);
  }

  Either<Failure, JourneyDetail> _parse(ApiResponse response) {
    final body = response.body;
    if (body is! Map) return left(const UnknownFailure('journey.bad_response'));

    try {
      return right(
        JourneyModel.fromJson(
          body,
          fetchedAt: response.fetchedAt,
          fromCache: response.fromCache,
        ),
      );
    } on Object {
      return left(const UnknownFailure('journey.bad_response'));
    }
  }
}
