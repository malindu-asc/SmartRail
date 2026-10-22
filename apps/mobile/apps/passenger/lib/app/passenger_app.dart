import 'package:flutter/material.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../core/routing/app_router.dart';
import '../l10n/app_localizations.dart';
import '../features/settings/presentation/providers/settings_providers.dart';
import '../features/alerts/presentation/providers/alerts_providers.dart';
import 'locale_provider.dart';

/// The passenger app: the passenger theme, the chosen language and the router.
class PassengerApp extends ConsumerWidget {
  const PassengerApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final locale = ref.watch(localeProvider);
    final textSize = ref.watch(textSizeProvider);
    // Looks at the followed trains while the app is open, so the Alerts tab has something to say.
    ref.watch(alertWatcherProvider);

    return MaterialApp.router(
      onGenerateTitle: (context) => AppLocalizations.of(context).appTitle,
      theme: SmartRailTheme.passenger(),
      locale: locale,
      supportedLocales: LocaleNotifier.supported,
      localizationsDelegates: const [
        AppLocalizations.delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
      ],
      // "Text size" in Me is on top of the phone's own setting, never instead of it.
      builder: (context, child) => MediaQuery(
        data: MediaQuery.of(context).copyWith(
          textScaler: TextScaler.linear(
            MediaQuery.textScalerOf(context).scale(1) * textSize.factor,
          ),
        ),
        child: child ?? const SizedBox.shrink(),
      ),
      routerConfig: ref.watch(appRouterProvider),
    );
  }
}
