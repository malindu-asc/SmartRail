import 'package:fpdart/fpdart.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';

import '../../domain/entities/departure.dart';
import '../../domain/repositories/board_repository.dart';
import '../models/board_model.dart';

class RemoteBoardRepository implements BoardRepository {
  RemoteBoardRepository(this._client);

  static const rows = 20;

  final ApiClient _client;

  @override
  Future<Either<Failure, BoardSnapshot>> fetch(String stationCode) async {
    final path = '/api/journeys/boards/$stationCode';
    final query = {'count': rows};

    final result = await _client.get(path, query: query);

    return result.match((failure) {
      // The phone could not reach the platform. An earlier answer is shown as old, never hidden.
      final cached = _client.cachedCopy(path, query: query);
      return cached == null ? left(failure) : _parse(cached);
    }, _parse);
  }

  Either<Failure, BoardSnapshot> _parse(ApiResponse response) {
    final body = response.body;
    if (body is! Map) return left(const UnknownFailure('board.bad_response'));

    try {
      return right(
        BoardModel.fromJson(
          body,
          fetchedAt: response.fetchedAt,
          fromCache: response.fromCache,
        ),
      );
    } on Object {
      return left(const UnknownFailure('board.bad_response'));
    }
  }
}
