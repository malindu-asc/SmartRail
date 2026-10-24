import 'package:fpdart/fpdart.dart';

import 'failures.dart';

/// One operation of a feature. It returns either a [Failure] or the result, so errors are values, not exceptions.
abstract interface class UseCase<Output, Params> {
  Future<Either<Failure, Output>> call(Params params);
}

/// For use cases that need no input.
final class NoParams {
  const NoParams();
}
