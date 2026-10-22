import 'package:fpdart/fpdart.dart';
import 'package:smartrail_core/smartrail_core.dart';

import '../entities/trip.dart';

abstract interface class PlanRepository {
  /// Trains from one station to another, direct or with one change. When the platform cannot be reached and an earlier answer
  /// exists it comes back marked `fromCache`.
  Future<Either<Failure, TripResult>> search(TripQuery query);
}
