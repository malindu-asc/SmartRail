import 'package:flutter/material.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../l10n/app_localizations.dart';
import '../../../settings/presentation/widgets/language_switcher.dart';

/// The Account tab. Sign-in and tickets come with SPEC-021; for now it holds the language choice (REQ-PAX-013).
class AccountScreen extends StatelessWidget {
  const AccountScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return ListView(
      padding: const EdgeInsets.all(SmartRailSpacing.xl),
      children: [
        Text(l10n.accountTitle, style: Theme.of(context).textTheme.headlineLarge),
        const SizedBox(height: SmartRailSpacing.sm),
        Text(l10n.accountBody),
        const SizedBox(height: SmartRailSpacing.xxl),
        const LanguageSwitcher(),
      ],
    );
  }
}
