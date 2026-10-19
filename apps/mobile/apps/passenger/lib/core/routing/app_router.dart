import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/auth/presentation/screens/account_screen.dart';
import '../../features/board/presentation/screens/home_screen.dart';
import '../../features/journeys/presentation/screens/trips_screen.dart';
import '../widgets/app_shell.dart';
import '../widgets/not_found_screen.dart';

abstract final class AppRoutes {
  static const home = '/home';
  static const trips = '/trips';
  static const account = '/account';
  static const notFound = '/not-found';
}

/// Every route of the passenger app. Ids and codes go in the path, never whole objects.
final appRouterProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    initialLocation: AppRoutes.home,
    errorBuilder: (context, state) => const NotFoundScreen(),
    routes: [
      GoRoute(path: AppRoutes.notFound, builder: (context, state) => const NotFoundScreen()),
      StatefulShellRoute.indexedStack(
        builder: (context, state, shell) => AppShell(shell: shell),
        branches: [
          StatefulShellBranch(routes: [GoRoute(path: AppRoutes.home, builder: (context, state) => const HomeScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: AppRoutes.trips, builder: (context, state) => const TripsScreen())]),
          StatefulShellBranch(routes: [GoRoute(path: AppRoutes.account, builder: (context, state) => const AccountScreen())]),
        ],
      ),
    ],
  );
});
