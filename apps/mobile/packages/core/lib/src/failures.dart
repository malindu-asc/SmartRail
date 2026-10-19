/// Everything that can go wrong when talking to the platform, as data.
///
/// A failure carries a stable [code] (the API's error code when there is one) and never user-facing text: screens choose the
/// words, so wording and translation stay out of the data layer.
sealed class Failure {
  const Failure(this.code, {this.message});

  /// A stable identifier, for example `Journeys.JourneyNotFound`, or a client-side code such as `network.timeout`.
  final String code;

  /// The API's description. For logs and developers; do not show it to a user.
  final String? message;

  @override
  String toString() => '$runtimeType($code)';

  @override
  bool operator ==(Object other) =>
      other.runtimeType == runtimeType &&
      other is Failure &&
      other.code == code &&
      other.message == message;

  @override
  int get hashCode => Object.hash(runtimeType, code, message);
}

/// No connection, a refused connection or a timeout. The request may not have reached the server.
final class NetworkFailure extends Failure {
  const NetworkFailure(super.code, {super.message});

  const NetworkFailure.timeout() : super('network.timeout');

  const NetworkFailure.unreachable() : super('network.unreachable');
}

/// 401: there is no valid sign-in.
final class UnauthorizedFailure extends Failure {
  const UnauthorizedFailure([super.code = 'auth.unauthorized', String? message])
    : super(message: message);
}

/// 403: signed in, but not allowed.
final class ForbiddenFailure extends Failure {
  const ForbiddenFailure([super.code = 'auth.forbidden', String? message])
    : super(message: message);
}

/// 404: the thing asked for does not exist.
final class NotFoundFailure extends Failure {
  const NotFoundFailure([super.code = 'not_found', String? message])
    : super(message: message);
}

/// 409: the request conflicts with the current state (for example a concurrent change).
final class ConflictFailure extends Failure {
  const ConflictFailure([super.code = 'conflict', String? message])
    : super(message: message);
}

/// 400: the input was refused. [errors] maps a field to its messages when the API sends them.
final class ValidationFailure extends Failure {
  const ValidationFailure([
    super.code = 'validation',
    String? message,
    this.errors = const {},
  ]) : super(message: message);

  final Map<String, List<String>> errors;
}

/// 5xx: the server failed or is unavailable.
final class ServerFailure extends Failure {
  const ServerFailure([super.code = 'server', String? message])
    : super(message: message);
}

/// Anything not covered above. A bug if it happens often.
final class UnknownFailure extends Failure {
  const UnknownFailure([super.code = 'unknown', String? message])
    : super(message: message);
}
