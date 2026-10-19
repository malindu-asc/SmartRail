import 'package:dio/dio.dart';
import 'package:smartrail_core/smartrail_core.dart';

/// Turns whatever went wrong with a request into a [Failure]. The platform answers errors as `application/problem+json`
/// with a stable `code` (for example `Journeys.JourneyNotFound`).
abstract final class ProblemMapper {
  static Failure fromDio(DioException error) {
    switch (error.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
      case DioExceptionType.transformTimeout:
        return const NetworkFailure.timeout();
      case DioExceptionType.connectionError:
        return const NetworkFailure.unreachable();
      case DioExceptionType.badResponse:
        return fromStatus(error.response?.statusCode ?? 0, error.response?.data);
      case DioExceptionType.cancel:
      case DioExceptionType.badCertificate:
      case DioExceptionType.unknown:
        return const UnknownFailure('unknown');
    }
  }

  static Failure fromStatus(int status, Object? body) {
    final problem = body is Map ? body : const <String, Object?>{};
    final code = problem['code'] is String ? problem['code'] as String : null;
    final detail = problem['detail'] is String ? problem['detail'] as String : (problem['title'] is String ? problem['title'] as String : null);

    return switch (status) {
      400 => ValidationFailure(code ?? 'validation', detail, _fieldErrors(problem['errors'])),
      401 => UnauthorizedFailure(code ?? 'auth.unauthorized', detail),
      403 => ForbiddenFailure(code ?? 'auth.forbidden', detail),
      404 => NotFoundFailure(code ?? 'not_found', detail),
      409 => ConflictFailure(code ?? 'conflict', detail),
      >= 500 && < 600 => ServerFailure(code ?? 'server', detail),
      _ => UnknownFailure(code ?? 'unknown', detail),
    };
  }

  static Map<String, List<String>> _fieldErrors(Object? errors) {
    if (errors is! Map) return const {};
    return {
      for (final entry in errors.entries)
        entry.key.toString(): [if (entry.value is List) ...(entry.value as List).map((e) => e.toString()) else entry.value.toString()],
    };
  }
}
