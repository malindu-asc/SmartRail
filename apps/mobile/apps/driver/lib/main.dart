import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app/app_overrides.dart';
import 'app/driver_app.dart';
import 'features/auth/data/auth_data.dart';
import 'features/auth/presentation/providers/auth_providers.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  // Whoever was signed in last time still is, without asking again.
  final session = await SecureSessionStore().read();

  runApp(
    ProviderScope(
      // A failed load shows the failure and the driver's own Try again; the app does not retry silently.
      retry: (retryCount, error) => null,
      overrides: [
        ...appOverrides,
        initialSessionProvider.overrideWithValue(session),
      ],
      child: const DriverApp(),
    ),
  );
}
