import '../features/auth/presentation/providers/auth_providers.dart';

/// Sends the API client the signed-in driver's token. Used by `main` and by tests.
final appOverrides = [authTokenOverride];
