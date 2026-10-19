import 'package:smartrail_api_client/smartrail_api_client.dart';

import 'locale_provider.dart';

/// Wires the app's chosen language into the API client, which sends it as Accept-Language. Used by `main` and by tests.
final appOverrides = [
  localeCodeProvider.overrideWith((ref) => ref.watch(localeProvider).languageCode),
];
