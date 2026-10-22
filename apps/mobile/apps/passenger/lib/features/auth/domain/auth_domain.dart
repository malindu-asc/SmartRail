import 'package:fpdart/fpdart.dart';
import 'package:smartrail_core/smartrail_core.dart';

/// A sign-in: the short-lived access token the API checks, and the longer-lived refresh token that gets a new one.
class Session {
  const Session({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresAt,
  });

  final String accessToken;
  final String refreshToken;
  final DateTime expiresAt;

  /// True when the access token is about to run out (or has), so it is renewed before a request uses it.
  bool expiresSoon(DateTime now) =>
      !now.isBefore(expiresAt.subtract(const Duration(seconds: 30)));

  Map<String, Object?> toJson() => {
    'access': accessToken,
    'refresh': refreshToken,
    'expires': expiresAt.toUtc().toIso8601String(),
  };

  static Session? fromJson(Map<String, Object?> json) {
    final access = json['access'];
    final refresh = json['refresh'];
    final expires = json['expires'];
    if (access is! String || refresh is! String || expires is! String) {
      return null;
    }
    final at = DateTime.tryParse(expires);
    return at == null
        ? null
        : Session(accessToken: access, refreshToken: refresh, expiresAt: at);
  }
}

class Profile {
  const Profile({
    required this.id,
    required this.fullName,
    required this.email,
  });

  final String id;
  final String fullName;
  final String email;

  /// "JP" for Jane Perera: the first letters of the first and last name.
  String get initials {
    final parts = fullName
        .trim()
        .split(RegExp(r'\s+'))
        .where((p) => p.isNotEmpty)
        .toList();
    if (parts.isEmpty) return '?';
    if (parts.length == 1) return parts.first[0].toUpperCase();
    return (parts.first[0] + parts.last[0]).toUpperCase();
  }
}

/// Everything about the passenger's account. Passwords go to Keycloak only; the API never sees them on sign-in.
abstract interface class AuthRepository {
  Future<Either<Failure, Session>> signIn(String email, String password);

  /// Gets a new access token. Fails with [UnauthorizedFailure] when the refresh token is no longer good.
  Future<Either<Failure, Session>> refresh(Session session);

  Future<Either<Failure, Unit>> register({
    required String fullName,
    required String email,
    required String password,
    required bool acceptedTerms,
  });

  Future<Either<Failure, Unit>> forgotPassword(String email);

  Future<Either<Failure, Profile>> profile();

  /// "Download my data": everything held about the person, as JSON text.
  Future<Either<Failure, String>> exportData();

  Future<Either<Failure, Unit>> deleteAccount();
}

/// Where the session is kept between launches. The real one uses the platform's secure storage.
abstract interface class SessionStore {
  Future<Session?> read();

  Future<void> write(Session session);

  Future<void> clear();
}

class MemorySessionStore implements SessionStore {
  MemorySessionStore([this.session]);

  Session? session;

  @override
  Future<Session?> read() async => session;

  @override
  Future<void> write(Session session) async => this.session = session;

  @override
  Future<void> clear() async => session = null;
}
