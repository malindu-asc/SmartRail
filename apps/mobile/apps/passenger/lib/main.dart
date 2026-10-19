import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'app/app_overrides.dart';
import 'app/passenger_app.dart';
import 'core/storage/key_value_store.dart';
import 'features/auth/data/auth_data.dart';
import 'features/auth/presentation/providers/auth_providers.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final prefs = await SharedPreferences.getInstance();
  // Whoever was signed in last time still is, without asking again.
  final session = await SecureSessionStore().read();

  runApp(
    ProviderScope(
      // A failed load shows the failure and the passenger's own Try again; the app does not silently retry in the background.
      retry: (retryCount, error) => null,
      overrides: [
        ...appOverrides,
        keyValueStoreProvider.overrideWithValue(SharedPrefsStore(prefs)),
        initialSessionProvider.overrideWithValue(session),
      ],
      child: const PassengerApp(),
    ),
  );
}
