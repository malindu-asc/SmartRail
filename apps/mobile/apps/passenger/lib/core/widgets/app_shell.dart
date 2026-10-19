import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../l10n/app_localizations.dart';
import '../time/network_clock_provider.dart';

/// The frame around the tabs: the offline banner when the API cannot be reached (REQ-NFR-006) and the tab bar.
class AppShell extends ConsumerWidget {
  const AppShell({super.key, required this.shell});

  final StatefulNavigationShell shell;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final reachability = ref.watch(reachabilityProvider);
    final clock = ref.watch(networkClockProvider);

    final lastSuccess = reachability.lastSuccess;

    return ReachabilityLifecycle(
      child: Scaffold(
        body: SafeArea(
          child: Column(
            children: [
              if (!reachability.online)
                Padding(
                  padding: const EdgeInsets.all(SmartRailSpacing.lg),
                  child: OfflineBanner(
                    title: l10n.offlineTitle,
                    detail: lastSuccess == null ? l10n.offlineNever : l10n.offlineDetail(clock.formatTime(lastSuccess)),
                  ),
                ),
              Expanded(child: shell),
            ],
          ),
        ),
        bottomNavigationBar: NavigationBar(
          selectedIndex: shell.currentIndex,
          onDestinationSelected: (index) => shell.goBranch(index, initialLocation: index == shell.currentIndex),
          destinations: [
            NavigationDestination(icon: const Icon(Icons.home_outlined), selectedIcon: const Icon(Icons.home), label: l10n.tabHome),
            NavigationDestination(icon: const Icon(Icons.route_outlined), selectedIcon: const Icon(Icons.route), label: l10n.tabTrips),
            NavigationDestination(icon: const Icon(Icons.person_outline), selectedIcon: const Icon(Icons.person), label: l10n.tabAccount),
          ],
        ),
      ),
    );
  }
}
