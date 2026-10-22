import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../l10n/app_localizations.dart';
import '../time/network_clock_provider.dart';

/// The frame around the tabs: the offline banner when the API cannot be reached (REQ-NFR-006) and the five-tab bar.
/// Tabs fade into one another; the bar stays on the departures and journey screens too (walkthrough D2).
class AppShell extends ConsumerWidget {
  const AppShell({super.key, required this.shell, required this.body});

  final StatefulNavigationShell shell;

  /// The tab bodies, already arranged by [FadeBranches].
  final Widget body;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final reachability = ref.watch(reachabilityProvider);
    final clock = ref.watch(networkClockProvider);
    final lastSuccess = reachability.lastSuccess;

    return ReachabilityLifecycle(
      child: Scaffold(
        body: Column(
          children: [
            // The offline banner sits under the status bar and slides in, so the screens below do not jump.
            AnimatedSize(
              duration: SmartRailMotion.of(context, SmartRailMotion.base),
              curve: SmartRailMotion.curve,
              alignment: Alignment.topCenter,
              child: reachability.online
                  ? const SizedBox(width: double.infinity)
                  : SafeArea(
                      bottom: false,
                      child: Padding(
                        padding: const EdgeInsets.all(SmartRailSpacing.lg),
                        child: OfflineBanner(
                          title: l10n.offlineTitle,
                          detail: lastSuccess == null
                              ? l10n.offlineNever
                              : l10n.offlineDetail(
                                  clock.formatTime(lastSuccess),
                                ),
                        ),
                      ),
                    ),
            ),
            Expanded(child: body),
          ],
        ),
        bottomNavigationBar: NavigationBar(
          selectedIndex: shell.currentIndex,
          onDestinationSelected: (index) => shell.goBranch(
            index,
            initialLocation: index == shell.currentIndex,
          ),
          destinations: [
            NavigationDestination(
              icon: const Icon(Icons.home_outlined),
              selectedIcon: const Icon(Icons.home),
              label: l10n.tabHome,
            ),
            NavigationDestination(
              icon: const Icon(Icons.route_outlined),
              selectedIcon: const Icon(Icons.route),
              label: l10n.tabPlan,
            ),
            NavigationDestination(
              icon: const Icon(Icons.confirmation_number_outlined),
              selectedIcon: const Icon(Icons.confirmation_number),
              label: l10n.tabTickets,
            ),
            NavigationDestination(
              icon: const Icon(Icons.notifications_none),
              selectedIcon: const Icon(Icons.notifications),
              label: l10n.tabAlerts,
            ),
            NavigationDestination(
              icon: const Icon(Icons.person_outline),
              selectedIcon: const Icon(Icons.person),
              label: l10n.tabMe,
            ),
          ],
        ),
      ),
    );
  }
}

/// Shows the current tab and fades between tabs (200 ms, none when motion is reduced). Every tab keeps its state, and the hidden ones
/// take no touches and are invisible to a screen reader.
class FadeBranches extends StatelessWidget {
  const FadeBranches({
    super.key,
    required this.currentIndex,
    required this.children,
  });

  final int currentIndex;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Stack(
      fit: StackFit.expand,
      children: [
        for (var i = 0; i < children.length; i++)
          // The fade sits outside the TickerMode: a tab that is leaving must still be allowed to run its fade-out, only its own
          // content stops animating.
          IgnorePointer(
            ignoring: i != currentIndex,
            child: ExcludeSemantics(
              excluding: i != currentIndex,
              child: AnimatedOpacity(
                opacity: i == currentIndex ? 1 : 0,
                duration: SmartRailMotion.of(context, SmartRailMotion.base),
                curve: SmartRailMotion.curve,
                child: TickerMode(
                  enabled: i == currentIndex,
                  child: children[i],
                ),
              ),
            ),
          ),
      ],
    );
  }
}
