import 'dart:convert';

import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:fpdart/fpdart.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';

import '../domain/auth_domain.dart';

/// Where Keycloak is. Set when the app is built: `--dart-define=KEYCLOAK_URL=https://id.example.org`.
abstract final class AuthConfig {
  static const keycloakUrl = String.fromEnvironment(
    'KEYCLOAK_URL',
    defaultValue: 'http://localhost:8080',
  );
  static const realm = 'smartrail';
  static const clientId = 'smartrail-mobile';
}

class SecureSessionStore implements SessionStore {
  SecureSessionStore([FlutterSecureStorage? storage])
    : _storage = storage ?? const FlutterSecureStorage();

  static const _key = 'session';

  final FlutterSecureStorage _storage;

  @override
  Future<Session?> read() async {
    try {
      final raw = await _storage.read(key: _key);
      if (raw == null) return null;
      final json = jsonDecode(raw);
      return json is Map
          ? Session.fromJson(json.cast<String, Object?>())
          : null;
    } on Object {
      return null;
    }
  }

  @override
  Future<void> write(Session session) async {
    try {
      await _storage.write(key: _key, value: jsonEncode(session.toJson()));
    } on Object {
      // The sign-in still works for this launch; the next launch asks again.
    }
  }

  @override
  Future<void> clear() async {
    try {
      await _storage.delete(key: _key);
    } on Object {
      // Nothing more to do.
    }
  }
}

/// Signs in with Keycloak (the password grant, because the app has its own sign-in form) and uses the API for the account itself.
class KeycloakAuthRepository implements AuthRepository {
  KeycloakAuthRepository(this._api, {Dio? dio, DateTime Function()? now})
    : _dio = dio ?? Dio(),
      _now = now ?? DateTime.now;

  final ApiClient _api;
  final Dio _dio;
  final DateTime Function() _now;

  String get _tokenUrl =>
      '${AuthConfig.keycloakUrl}/realms/${AuthConfig.realm}/protocol/openid-connect/token';

  @override
  Future<Either<Failure, Session>> signIn(String email, String password) =>
      _token({
        'grant_type': 'password',
        'client_id': AuthConfig.clientId,
        'username': email.trim(),
        'password': password,
      });

  @override
  Future<Either<Failure, Session>> refresh(Session session) => _token({
    'grant_type': 'refresh_token',
    'client_id': AuthConfig.clientId,
    'refresh_token': session.refreshToken,
  });

  Future<Either<Failure, Session>> _token(Map<String, String> form) async {
    try {
      final response = await _dio.post<Object?>(
        _tokenUrl,
        data: form,
        options: Options(
          contentType: Headers.formUrlEncodedContentType,
          sendTimeout: ApiConfig.readTimeout,
          receiveTimeout: ApiConfig.readTimeout,
        ),
      );
      final body = response.data;
      if (body is! Map) return left(const UnknownFailure('auth.bad_response'));

      return right(
        Session(
          accessToken: body['access_token']! as String,
          refreshToken: body['refresh_token']! as String,
          expiresAt: _now().add(
            Duration(seconds: (body['expires_in']! as num).toInt()),
          ),
        ),
      );
    } on DioException catch (error) {
      // Keycloak answers a wrong password, a locked account and a dead refresh token with 400 or 401.
      final status = error.response?.statusCode;
      if (status == 400 || status == 401) {
        return left(const UnauthorizedFailure('auth.invalid_credentials'));
      }
      return left(ProblemMapper.fromDio(error));
    } on Object {
      return left(const UnknownFailure('auth.bad_response'));
    }
  }

  @override
  Future<Either<Failure, Unit>> register({
    required String fullName,
    required String email,
    required String password,
    required bool acceptedTerms,
  }) async {
    final result = await _api.post(
      '/api/account/register',
      data: {
        'fullName': fullName,
        'email': email,
        'password': password,
        'acceptedTerms': acceptedTerms,
      },
    );
    return result.map((_) => unit);
  }

  @override
  Future<Either<Failure, Unit>> forgotPassword(String email) async {
    final result = await _api.post(
      '/api/account/forgot-password',
      data: {'email': email},
    );
    return result.map((_) => unit);
  }

  @override
  Future<Either<Failure, Profile>> profile() async {
    final result = await _api.get('/api/account/me');
    return result.flatMap((response) {
      final body = response.body;
      if (body is! Map) {
        return left(const UnknownFailure('account.bad_response'));
      }
      return right(
        Profile(
          id: body['id']! as String,
          fullName: body['fullName']! as String,
          email: body['email']! as String,
        ),
      );
    });
  }

  @override
  Future<Either<Failure, String>> exportData() async {
    final result = await _api.get('/api/account/me/export');
    return result.map(
      (response) => const JsonEncoder.withIndent('  ').convert(response.body),
    );
  }

  @override
  Future<Either<Failure, Unit>> deleteAccount() async {
    final result = await _api.delete('/api/account/me');
    return result.map((_) => unit);
  }
}
