import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../features/board/presentation/screens/departures_screen.dart';
import '../../features/home/presentation/screens/home_screen.dart';
import '../../features/journeys/presentation/screens/journey_screen.dart';
import '../../features/me/presentation/screens/me_screen.dart';
import '../../features/onboarding/presentation/screens/launch_screen.dart';
import '../../features/onboarding/presentation/screens/onboarding_screen.dart';
import '../../features/stations/presentation/screens/station_search_screen.dart';
import '../widgets/app_shell.dart';
import '../widgets/not_found_screen.dart';
import '../../features/plan/presentation/screens/plan_screen.dart';
import '../../features/plan/presentation/screens/results_screen.dart';
import '../../features/auth/presentation/screens/create_profile_screen.dart';
import '../../features/auth/presentation/screens/login_screen.dart';
import '../../features/tickets/domain/tickets_domain.dart';
import '../../features/tickets/presentation/screens/choose_ticket_screen.dart';
import '../../features/tickets/presentation/screens/review_screen.dart';
import '../../features/tickets/presentation/screens/seat_screen.dart';
import '../../features/tickets/presentation/screens/ticket_screen.dart';
import '../../features/tickets/presentation/screens/tickets_screen.dart';
import '../../features/alerts/presentation/screens/alerts_screen.dart';
import '../../features/disruptions/disruption_widgets.dart';
import '../../features/food/food.dart';
import 'app_routes.dart';

final _rootKey = GlobalKey<NavigatorState>(debugLabel: 'root');

/// Every route of the passenger app. Departures and journeys live under Home, so the tab bar stays visible.
final appRouterProvider = Provider<GoRouter>((ref) {
  return GoRouter(
    navigatorKey: _rootKey,
    observers: [_ClearMessages()],
    initialLocation: AppRoutes.launch,
    errorBuilder: (context, state) => const NotFoundScreen(),
    routes: [
      GoRoute(
        path: AppRoutes.launch,
        builder: (context, state) => const LaunchScreen(),
      ),
      GoRoute(
        path: AppRoutes.onboarding,
        builder: (context, state) => const OnboardingScreen(),
      ),
      GoRoute(
        path: AppRoutes.login,
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: AppRoutes.createProfile,
        builder: (context, state) => const CreateProfileScreen(),
      ),
      GoRoute(
        path: AppRoutes.notFound,
        builder: (context, state) => const NotFoundScreen(),
      ),
      StatefulShellRoute(
        builder: (context, state, shell) => shell,
        navigatorContainerBuilder: (context, shell, children) => AppShell(
          shell: shell,
          body: FadeBranches(
            currentIndex: shell.currentIndex,
            children: children,
          ),
        ),
        branches: [
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.home,
                builder: (context, state) => const HomeScreen(),
                routes: [
                  GoRoute(
                    path: 'search',
                    builder: (context, state) => StationSearchScreen(
                      pick: state.uri.queryParameters['pick'] == '1',
                      exclude: state.uri.queryParameters['exclude'],
                    ),
                  ),
                  GoRoute(
                    path: 'station/:code',
                    builder: (context, state) => DeparturesScreen(
                      stationCode: state.pathParameters['code']!,
                    ),
                    routes: [
                      GoRoute(
                        path: 'food',
                        builder: (context, state) => FoodScreen(
                          stationCode: state.pathParameters['code']!,
                          subtitle: state.uri.queryParameters['note'],
                        ),
                      ),
                    ],
                  ),
                  GoRoute(
                    path: 'disruption/:id',
                    builder: (context, state) =>
                        DisruptionScreen(id: state.pathParameters['id']!),
                  ),
                  GoRoute(
                    path: 'journey/:id',
                    builder: (context, state) =>
                        JourneyScreen(journeyId: state.pathParameters['id']!),
                  ),
                ],
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.plan,
                builder: (context, state) => const PlanScreen(),
                routes: [
                  GoRoute(
                    path: 'results',
                    builder: (context, state) {
                      final q = state.uri.queryParameters;
                      return ResultsScreen(
                        from: q['from'] ?? '',
                        to: q['to'] ?? '',
                        after: q['after'] == null
                            ? null
                            : DateTime.tryParse(q['after']!),
                        arriveBy: q['arriveBy'] == null
                            ? null
                            : DateTime.tryParse(q['arriveBy']!),
                      );
                    },
                  ),
                ],
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.tickets,
                builder: (context, state) => const TicketsScreen(),
                routes: [
                  GoRoute(
                    path: 'choose',
                    parentNavigatorKey: _rootKey,
                    builder: (context, state) {
                      final q = state.uri.queryParameters;
                      return ChooseTicketScreen(
                        query: OfferQuery(
                          from: q['from'] ?? '',
                          to: q['to'] ?? '',
                          after: q['after'] == null
                              ? null
                              : DateTime.tryParse(q['after']!),
                        ),
                      );
                    },
                  ),
                  GoRoute(
                    path: 'seats',
                    parentNavigatorKey: _rootKey,
                    builder: (context, state) => const SeatScreen(),
                  ),
                  GoRoute(
                    path: 'review',
                    parentNavigatorKey: _rootKey,
                    builder: (context, state) => const ReviewScreen(),
                  ),
                  GoRoute(
                    path: 'ticket/:id',
                    parentNavigatorKey: _rootKey,
                    builder: (context, state) => TicketScreen(
                      ticketId: state.pathParameters['id']!,
                      created: state.uri.queryParameters['new'] == '1',
                    ),
                  ),
                ],
              ),
            ],
          ),
          StatefulShellBranch(
            routes: [
              GoRoute(
                path: AppRoutes.alerts,
                builder: (context, state) => const AlertsScreen(),
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
});

/// A message belongs to the screen that showed it: moving to another screen, or back, takes it away. (Sheets and dialogs are not
/// screens, so a message shown after one closes stays.)
class _ClearMessages extends NavigatorObserver {
  void _clear(Route<dynamic> route) {
    if (route is! PageRoute) return;
    final context = navigator?.context;
    if (context != null) ScaffoldMessenger.maybeOf(context)?.clearSnackBars();
  }

  @override
  void didPush(Route<dynamic> route, Route<dynamic>? previousRoute) =>
      _clear(route);

  @override
  void didPop(Route<dynamic> route, Route<dynamic>? previousRoute) =>
      _clear(route);

  @override
  void didReplace({Route<dynamic>? newRoute, Route<dynamic>? oldRoute}) {
    if (newRoute != null) _clear(newRoute);
  }
}
