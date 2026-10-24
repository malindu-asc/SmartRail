import 'package:fpdart/fpdart.dart';
import 'package:smartrail_core/smartrail_core.dart';

import '../entities/departure.dart';

abstract interface class BoardRepository {
  /// The next departures from a station. When the platform cannot be reached and an earlier answer exists, that answer comes back
  /// marked `fromCache`, so the screen shows it as old instead of showing nothing.
  Future<Either<Failure, BoardSnapshot>> fetch(String stationCode);
}
