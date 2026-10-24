import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/providers/auth_providers.dart';
import '../../features/auth/presentation/screens/sign_in_screen.dart';
import '../../features/duty/presentation/screens/duty_screen.dart';
import '../../features/duty/presentation/screens/journey_screen.dart';
import '../../app/locale_provider.dart';
import '../../features/me/presentation/screens/me_screen.dart';
import '../../features/settings/language_screen.dart';
import '../../features/messages/messages_screen.dart';
import '../widgets/app_shell.dart';
import 'app_routes.dart';

/// Every route of the driver app. Signed-out drivers only see sign in; signed-in drivers never see it.
final appRouterProvider = Provider<GoRouter>((ref) {
  // Tells the router to look at the redirect again when the driver signs in or out.
  final changes = ValueNotifier<int>(0);
  ref.listen(
    authProvider.select((s) => s.isSignedIn),
    (_, _) => changes.value++,
  );
  ref.listen(localeProvider, (_, _) => changes.value++);
  ref.onDispose(changes.dispose);

  final router = GoRouter(
    initialLocation: AppRoutes.duty,
    refreshListenable: changes,
    redirect: (context, state) {
      // The first launch asks for a language, once, before anything else.
      final onLanguage = state.matchedLocation == AppRoutes.language;
      if (ref.read(localeProvider) == null) {
        return onLanguage ? null : AppRoutes.language;
      }
      if (onLanguage) return AppRoutes.duty;

      final signedIn = ref.read(authProvider).isSignedIn;
      final onSignIn = state.matchedLocation == AppRoutes.signIn;
      if (!signedIn) return onSignIn ? null : AppRoutes.signIn;
      return onSignIn ? AppRoutes.duty : null;
    },
    routes: [
      GoRoute(
        path: AppRoutes.language,
        builder: (context, state) => const LanguageScreen(),
      ),
      GoRoute(
        path: AppRoutes.signIn,
        builder: (context, state) => const SignInScreen(),
      ),
      GoRoute(
        path: '/journey/:id',
        builder: (context, state) =>
            JourneyScreen(journeyId: state.pathParameters['id']!),
      ),
      StatefulShellRoute.indexedStack(
        builder: (context, state, shell) => AppShell(shell: shell),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.duty,
                builder: (context, state) => const DutyScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.messages,
                builder: (context, state) => const MessagesScreen(),
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.me,
                builder: (context, state) => const MeScreen(),
              ),
            ],
          ),
        ],
      ),
    ],
  );
  ref.onDispose(router.dispose);
  return router;
});
