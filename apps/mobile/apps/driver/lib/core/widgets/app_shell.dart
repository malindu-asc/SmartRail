import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../features/messages/messages.dart';
import '../../l10n/app_localizations.dart';

/// The frame around the tabs: the offline banner when the API cannot be reached, and the Duty, Messages and Me tabs.
class AppShell extends ConsumerWidget {
  const AppShell({super.key, required this.shell});

  final StatefulNavigationShell shell;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final online = ref.watch(reachabilityProvider).online;
    final unread = ref.watch(unreadCountProvider);

    return ReachabilityLifecycle(
      child: Scaffold(
        body: Column(
          children: [
            AnimatedSize(
              duration: SmartRailMotion.of(context, SmartRailMotion.base),
              curve: SmartRailMotion.curve,
              alignment: Alignment.topCenter,
              child: online
                  ? const SizedBox(width: double.infinity)
                  : SafeArea(
                      bottom: false,
                      child: Padding(
                        padding: const EdgeInsets.all(SmartRailSpacing.lg),
                        child: OfflineBanner(
                          title: l10n.offlineTitle,
                          detail: l10n.offlineDetail,
                        ),
                      ),
                    ),
            ),
            Expanded(child: shell),
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
              icon: const Icon(Icons.train_outlined),
              selectedIcon: const Icon(Icons.train),
              label: l10n.tabDuty,
            ),
            NavigationDestination(
              icon: Badge(
                isLabelVisible: unread > 0,
                label: Text('$unread'),
                child: const Icon(Icons.chat_bubble_outline),
              ),
              selectedIcon: Badge(
                isLabelVisible: unread > 0,
                label: Text('$unread'),
                child: const Icon(Icons.chat_bubble),
              ),
              label: l10n.tabMessages,
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
