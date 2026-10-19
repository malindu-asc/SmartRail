import 'package:dio/dio.dart';
import 'package:fpdart/fpdart.dart';
import 'package:smartrail_core/smartrail_core.dart';

import 'api_config.dart';
import 'problem_mapper.dart';
import 'response_cache.dart';

/// A successful answer. [fetchedAt] is when the server last sent or confirmed this body, so a screen can say how old it is;
/// [revalidated] is true when the server answered `304 Not Modified` and the remembered body was used.
class ApiResponse {
  const ApiResponse({required this.body, required this.fetchedAt, this.revalidated = false, this.fromCache = false});

  final Object? body;
  final DateTime fetchedAt;
  final bool revalidated;

  /// True for a copy handed out by [ApiClient.cachedCopy] after a failure: the data is stale and must be shown as such.
  final bool fromCache;
}

/// The one way the apps talk to the SmartRail API.
///
/// * Errors come back as a [Failure] value, never as a raw exception.
/// * Public reads are revalidated with `ETag` and the last good answer is remembered with its fetch time.
/// * Reads time out after [ApiConfig.readTimeout] and are tried once more after a refused connection; writes are never retried.
/// * Nothing sensitive is logged: only the method, the path (no query) and the status.
class ApiClient {
  ApiClient({
    Dio? dio,
    String? baseUrl,
    required this._locale,
    this._token,
    ResponseCache? cache,
    this._log,
    DateTime Function()? now,
  }) : _dio = dio ?? Dio(),
       _cache = cache ?? MemoryResponseCache(),
       _now = now ?? DateTime.now {
    final options = _dio.options;
    options.baseUrl = baseUrl ?? ApiConfig.baseUrl;
    options.connectTimeout = ApiConfig.readTimeout;
    options.receiveTimeout = ApiConfig.readTimeout;
    options.headers['Accept'] = 'application/json';

    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (request, handler) async {
          request.headers['Accept-Language'] = _locale();
          final bearer = await _token?.call();
          if (bearer != null && bearer.isNotEmpty) {
            request.headers['Authorization'] = 'Bearer $bearer';
          }
          handler.next(request);
        },
        onResponse: (response, handler) {
          _log?.call('${response.requestOptions.method} ${response.requestOptions.path} -> ${response.statusCode}');
          handler.next(response);
        },
        onError: (error, handler) {
          _log?.call('${error.requestOptions.method} ${error.requestOptions.path} -> ${error.response?.statusCode ?? error.type.name}');
          handler.next(error);
        },
      ),
    );
  }

  final Dio _dio;
  final String Function() _locale;
  final Future<String?> Function()? _token;
  final ResponseCache _cache;
  final void Function(String line)? _log;
  final DateTime Function() _now;

  /// A read. Safe to repeat, so it is retried once after a refused connection.
  Future<Either<Failure, ApiResponse>> get(String path, {Map<String, Object?>? query}) async {
    final key = _key(path, query);

    for (var attempt = 1;; attempt++) {
      final cached = _cache.read(key);
      try {
        final response = await _dio.get<Object?>(
          path,
          queryParameters: query,
          options: Options(
            headers: {if (cached?.etag != null) 'If-None-Match': cached!.etag},
            validateStatus: (status) => status != null && (status < 300 || status == 304),
          ),
        );

        if (response.statusCode == 304 && cached != null) {
          return right(ApiResponse(body: cached.body, fetchedAt: cached.fetchedAt, revalidated: true));
        }

        final fetchedAt = _now();
        final etag = response.headers.value('etag');
        if (etag != null) {
          _cache.write(key, CachedResponse(body: response.data, etag: etag, fetchedAt: fetchedAt));
        }

        return right(ApiResponse(body: response.data, fetchedAt: fetchedAt));
      } on DioException catch (error) {
        final failure = ProblemMapper.fromDio(error);
        if (attempt == 1 && failure == const NetworkFailure.unreachable()) continue;
        return left(failure);
      }
    }
  }

  /// A write. Never retried: a repeated write could repeat an action.
  Future<Either<Failure, ApiResponse>> post(String path, {Object? data}) async {
    try {
      final response = await _dio.post<Object?>(path, data: data);
      return right(ApiResponse(body: response.data, fetchedAt: _now()));
    } on DioException catch (error) {
      return left(ProblemMapper.fromDio(error));
    }
  }

  /// The last good answer to a read, for showing stale data when a fresh read failed. Null when there is none.
  ApiResponse? cachedCopy(String path, {Map<String, Object?>? query}) {
    final cached = _cache.read(_key(path, query));
    return cached == null ? null : ApiResponse(body: cached.body, fetchedAt: cached.fetchedAt, fromCache: true);
  }

  // The language is part of the key: the same URL answers differently per language.
  String _key(String path, Map<String, Object?>? query) {
    final params = query == null ? '' : (query.entries.toList()..sort((a, b) => a.key.compareTo(b.key))).map((e) => '${e.key}=${e.value}').join('&');
    return '${_locale()}|$path?$params';
  }
}
