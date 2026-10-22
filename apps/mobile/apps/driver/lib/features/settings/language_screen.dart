import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../app/locale_provider.dart';
import '../../core/widgets/driver_widgets.dart';
import '../../l10n/app_localizations.dart';

/// The name of a language in its own words, so a driver can find theirs whatever the app is showing now.
String languageName(AppLocalizations l10n, Locale locale) =>
    locale.languageCode == 'si' ? l10n.languageSinhala : l10n.languageEnglish;

/// Choose your language: shown once, before sign in (design A2, without Tamil). It can be changed later in Me.
class LanguageScreen extends ConsumerStatefulWidget {
  const LanguageScreen({super.key});

  @override
  ConsumerState<LanguageScreen> createState() => _LanguageScreenState();
}

class _LanguageScreenState extends ConsumerState<LanguageScreen> {
  Locale _chosen = supportedLanguages.first;

  @override
  Widget build(BuildContext context) {
    // Shows the words in the language being chosen, so the choice is visible straight away.
    return Localizations.override(
      context: context,
      locale: _chosen,
      child: Builder(
        builder: (context) {
          final l10n = AppLocalizations.of(context);

          return Scaffold(
            backgroundColor: SmartRailColors.page,
            body: Column(
              children: [
                Expanded(
                  child: SafeArea(
                    bottom: false,
                    child: ListView(
                      padding: const EdgeInsets.fromLTRB(24, 56, 24, 24),
                      children: [
                        Text(
                          l10n.chooseLanguageTitle,
                          style: SmartRailType.display.copyWith(
                            color: SmartRailColors.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          l10n.chooseLanguageBody,
                          style: SmartRailType.body14.copyWith(
                            color: SmartRailColors.textSecondary,
                          ),
                        ),
                        const SizedBox(height: 24),
                        for (final locale in supportedLanguages) ...[
                          OptionRow(
                            label: languageName(l10n, locale),
                            selected: _chosen == locale,
                            onTap: () => setState(() => _chosen = locale),
                          ),
                          const SizedBox(height: 8),
                        ],
                      ],
                    ),
                  ),
                ),
                StickyActionBar(
                  children: [
                    SmartRailButton(
                      label: l10n.continueButton,
                      onPressed: () =>
                          ref.read(localeProvider.notifier).select(_chosen),
                    ),
                  ],
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
