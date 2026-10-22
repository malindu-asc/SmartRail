import 'package:flutter/material.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../l10n/app_localizations.dart';

/// A tab whose screens are built in a later update. It says what is coming and never leaves the passenger at a dead end.
class ComingSoonScreen extends StatelessWidget {
  const ComingSoonScreen({
    super.key,
    required this.icon,
    required this.title,
    required this.body,
    this.actionLabel,
    this.onAction,
  });

  final IconData icon;
  final String title;
  final String body;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.all(SmartRailSpacing.xl),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title, style: Theme.of(context).textTheme.headlineLarge),
            const Spacer(),
            Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  CircleAvatar(
                    radius: 44,
                    backgroundColor: SmartRailColors.primarySubtle,
                    child: Icon(icon, size: 40, color: SmartRailColors.primary),
                  ),
                  const SizedBox(height: SmartRailSpacing.lg),
                  Chip(
                    label: Text(l10n.comingSoon),
                    backgroundColor: SmartRailColors.bgSoft,
                    side: BorderSide.none,
                  ),
                  const SizedBox(height: SmartRailSpacing.md),
                  Text(
                    body,
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                ],
              ),
            ),
            const Spacer(),
            if (actionLabel != null)
              SmartRailButton(label: actionLabel!, onPressed: onAction),
          ],
        ),
      ),
    );
  }
}
