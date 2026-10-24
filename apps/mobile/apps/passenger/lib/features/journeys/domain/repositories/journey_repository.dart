import 'package:fpdart/fpdart.dart';
import 'package:smartrail_core/smartrail_core.dart';

import '../entities/journey_detail.dart';

abstract interface class JourneyRepository {
  /// One journey with its stops. When the platform cannot be reached and an earlier answer exists, that answer comes back marked
  /// `fromCache`, so the screen shows it as old instead of showing nothing.
  Future<Either<Failure, JourneyDetail>> fetch(String journeyId);
}
