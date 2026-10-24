import 'package:fpdart/fpdart.dart';
import 'package:smartrail_core/smartrail_core.dart';

import '../entities/station.dart';

abstract interface class StationRepository {
  /// Stations whose name or code matches [query]; every station when it is empty.
  Future<Either<Failure, List<Station>>> search(String query);
}

/// The last few stations the passenger looked at, newest first, kept on the phone.
abstract interface class RecentStationsRepository {
  List<Station> recent();

  Future<void> add(Station station);
}
