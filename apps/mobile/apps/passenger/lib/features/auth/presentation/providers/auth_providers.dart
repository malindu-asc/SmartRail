import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fpdart/fpdart.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';

import '../../data/auth_data.dart';
import '../../domain/auth_domain.dart';

final authRepositoryProvider = Provider<AuthRepository>(
  (ref) => KeycloakAuthRepository(ref.watch(apiClientProvider)),
);

final sessionStoreProvider = Provider<SessionStore>(
  (ref) => SecureSessionStore(),
);

/// The session found at launch. `main` reads it before the first frame so the app knows at once whether someone is signed in.
final initialSessionProvider = Provider<Session?>((ref) => null);

/// What the app knows about who is signed in. Browsing never needs it (REQ-ACC-001, ASM-012).
class AuthState {
  const AuthState({this.session});

  final Session? session;

  bool get isSignedIn => session != null;
}

final authProvider = NotifierProvider<AuthNotifier, AuthState>(
  AuthNotifier.new,
);

class AuthNotifier extends Notifier<AuthState> {
  Future<Either<Failure, Session>>? _refreshing;

  @override
  AuthState build() => AuthState(session: ref.read(initialSessionProvider));

  /// A token for a request. Renews it first when it is about to run out; signs out when it cannot be renewed.
  Future<String?> accessToken() async {
    final session = state.session;
    if (session == null) return null;
    if (!session.expiresSoon(DateTime.now())) return session.accessToken;

    // One renewal at a time, so several requests at once do not each use up the refresh token.
    final result = await (_refreshing ??= ref
        .read(authRepositoryProvider)
        .refresh(session));
    _refreshing = null;

    return result.match(
      (failure) {
        // Only a refused refresh signs the person out; a missing connection keeps the sign-in for later.
        if (failure is UnauthorizedFailure) signOut();
        return failure is UnauthorizedFailure ? null : session.accessToken;
      },
      (next) {
        _keep(next);
        return next.accessToken;
      },
    );
  }

  Future<Failure?> signIn(String email, String password) async {
    final result = await ref
        .read(authRepositoryProvider)
        .signIn(email, password);
    return result.match((failure) => failure, (session) {
      _keep(session);
      return null;
    });
  }

  /// Creates the profile, then signs in with it.
  Future<Failure?> register({
    required String fullName,
    required String email,
    required String password,
    required bool acceptedTerms,
  }) async {
    final created = await ref
        .read(authRepositoryProvider)
        .register(
          fullName: fullName,
          email: email,
          password: password,
          acceptedTerms: acceptedTerms,
        );
    if (created.isLeft()) return created.getLeft().toNullable();
    return signIn(email, password);
  }

  Future<void> signOut() async {
    state = const AuthState();
    await ref.read(sessionStoreProvider).clear();
  }

  /// Deletes the account on the server, then forgets the sign-in on this phone.
  Future<Failure?> deleteAccount() async {
    final result = await ref.read(authRepositoryProvider).deleteAccount();
    if (result.isLeft()) return result.getLeft().toNullable();
    await signOut();
    return null;
  }

  void _keep(Session session) {
    state = AuthState(session: session);
    ref.read(sessionStoreProvider).write(session);
  }
}

/// The signed-in person's name and email, or null when signed out. Reads again after a sign-in.
final profileProvider = FutureProvider.autoDispose<Profile?>((ref) async {
  final signedIn = ref.watch(authProvider.select((s) => s.isSignedIn));
  if (!signedIn) return null;

  final result = await ref.read(authRepositoryProvider).profile();
  return result.match((failure) => throw failure, (profile) => profile);
});

/// Sends the API client the token of whoever is signed in.
final authTokenOverride = accessTokenProvider.overrideWith(
  (ref) =>
      () => ref.read(authProvider.notifier).accessToken(),
);
