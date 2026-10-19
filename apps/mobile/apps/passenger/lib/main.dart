import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'app/app_overrides.dart';
import 'app/passenger_app.dart';

void main() {
  runApp(ProviderScope(overrides: appOverrides, child: const PassengerApp()));
}
