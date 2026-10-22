import 'package:smartrail_api_client/smartrail_api_client.dart';

import '../features/auth/presentation/providers/auth_providers.dart';
import 'locale_provider.dart';

/// Wires the app's chosen language into the API client, which sends it as Accept-Language. Used by `main` and by tests.
final appOverrides = [
  authTokenOverride,
  localeCodeProvider.overrideWith(
    (ref) => ref.watch(localeProvider).languageCode,
  ),
];
