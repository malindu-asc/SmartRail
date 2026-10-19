import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../l10n/app_localizations.dart';
import '../routing/app_router.dart';

/// Shown for any route the app does not have.
class NotFoundScreen extends StatelessWidget {
  const NotFoundScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: Padding(
            padding: const EdgeInsets.all(SmartRailSpacing.xl),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                const Icon(Icons.explore_off_outlined, size: 48, color: SmartRailColors.textTertiary),
                const SizedBox(height: SmartRailSpacing.lg),
                Text(l10n.notFoundTitle, style: Theme.of(context).textTheme.titleMedium, textAlign: TextAlign.center),
                const SizedBox(height: SmartRailSpacing.sm),
                Text(l10n.notFoundBody, textAlign: TextAlign.center),
                const SizedBox(height: SmartRailSpacing.xl),
                SmartRailButton(label: l10n.notFoundAction, onPressed: () => context.go(AppRoutes.home)),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
