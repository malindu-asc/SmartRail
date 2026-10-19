import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../app/locale_provider.dart';
import '../../../../l10n/app_localizations.dart';

/// Chooses English or Sinhala. Each language is written in itself, so a reader can always find theirs.
class LanguageSwitcher extends ConsumerWidget {
  const LanguageSwitcher({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final current = ref.watch(localeProvider);
    final notifier = ref.read(localeProvider.notifier);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(l10n.language, style: Theme.of(context).textTheme.labelLarge),
        const SizedBox(height: SmartRailSpacing.md),
        SmartRailButton(
          label: l10n.languageEnglish,
          kind: current.languageCode == 'en' ? ButtonKind.tint : ButtonKind.secondary,
          onPressed: () => notifier.select(const Locale('en')),
        ),
        const SizedBox(height: SmartRailSpacing.md),
        SmartRailButton(
          label: l10n.languageSinhala,
          kind: current.languageCode == 'si' ? ButtonKind.tint : ButtonKind.secondary,
          onPressed: () => notifier.select(const Locale('si')),
        ),
      ],
    );
  }
}
