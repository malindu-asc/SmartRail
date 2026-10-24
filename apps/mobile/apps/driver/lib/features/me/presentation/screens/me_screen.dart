import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../app/locale_provider.dart';
import '../../../../core/widgets/driver_widgets.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../auth/presentation/providers/auth_providers.dart';
import '../../../messages/technical_issue_sheet.dart';
import '../../../settings/language_screen.dart';

/// Me (design 22): who is signed in, the language, help, and sign out. The sign out sheet is design 23.
class MeScreen extends ConsumerWidget {
  const MeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final profile = ref.watch(profileProvider).value;
    final locale = ref.watch(localeProvider) ?? supportedLanguages.first;

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      body: Column(
        children: [
          const SyncLine(),
          Expanded(
            child: ListView(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
              children: [
                Text(
                  l10n.meTitle,
                  style: SmartRailType.display.copyWith(
                    color: SmartRailColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 16),
                OutlinedCard(
                  child: Row(
                    children: [
                      CircleAvatar(
                        radius: 28,
                        backgroundColor: SmartRailColors.primarySubtle,
                        child: Text(
                          profile?.initials ?? '',
                          style: SmartRailType.heading16.copyWith(
                            color: SmartRailColors.primaryPressed,
                          ),
                        ),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              profile?.name ?? l10n.meRole,
                              style: SmartRailType.heading16.copyWith(
                                color: SmartRailColors.textPrimary,
                              ),
                            ),
                            Text(
                              profile?.email ?? '',
                              overflow: TextOverflow.ellipsis,
                              style: SmartRailType.body14.copyWith(
                                color: SmartRailColors.textSecondary,
                              ),
                            ),
                            Text(
                              l10n.meRole,
                              style: SmartRailType.body14.copyWith(
                                color: SmartRailColors.textSecondary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),
                RowsCard(
                  children: [
                    _MeRow(
                      label: l10n.meLanguage,
                      value: languageName(l10n, locale),
                      onTap: () => _chooseLanguage(context, ref, locale),
                    ),
                    _MeRow(
                      label: l10n.techIssueRow,
                      onTap: () => showTechnicalIssueSheet(context, ref),
                    ),
                    _MeRow(
                      label: l10n.emergencyRow,
                      onTap: () => showEmergencyContactsSheet(context),
                    ),
                  ],
                ),
                const SizedBox(height: 16),
                InfoBanner(
                  kind: BannerKind.message,
                  title: l10n.sharedPhoneTitle,
                  detail: l10n.sharedPhoneBody,
                ),
                const SizedBox(height: 16),
                SmartRailButton(
                  label: l10n.signOutRow,
                  kind: ButtonKind.secondary,
                  onPressed: () => _confirm(context, ref),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _chooseLanguage(
    BuildContext context,
    WidgetRef ref,
    Locale current,
  ) {
    final l10n = AppLocalizations.of(context);

    return showSmartRailSheet<void>(
      context,
      builder: (sheet) => SheetBody(
        children: [
          SheetText(title: l10n.meLanguage),
          Column(
            children: [
              for (final locale in supportedLanguages) ...[
                OptionRow(
                  label: languageName(l10n, locale),
                  selected: locale == current,
                  onTap: () {
                    ref.read(localeProvider.notifier).select(locale);
                    Navigator.of(sheet).pop();
                  },
                ),
                const SizedBox(height: 8),
              ],
            ],
          ),
        ],
      ),
    );
  }

  Future<void> _confirm(BuildContext context, WidgetRef ref) async {
    final l10n = AppLocalizations.of(context);

    final confirmed = await showSmartRailSheet<bool>(
      context,
      builder: (sheet) => SheetBody(
        children: [
          SheetText(title: l10n.signOutTitle, body: l10n.signOutBody),
          SmartRailButton(
            label: l10n.signOutConfirm,
            kind: ButtonKind.danger,
            onPressed: () => Navigator.of(sheet).pop(true),
          ),
          SmartRailButton(
            label: l10n.cancel,
            kind: ButtonKind.secondary,
            onPressed: () => Navigator.of(sheet).pop(false),
          ),
        ],
      ),
    );

    if (confirmed == true) await ref.read(authProvider.notifier).signOut();
  }
}

class _MeRow extends StatelessWidget {
  const _MeRow({required this.label, required this.onTap, this.value});

  final String label;
  final String? value;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      minTileHeight: 56,
      onTap: onTap,
      title: Text(
        label,
        style: SmartRailType.label14.copyWith(
          color: SmartRailColors.textPrimary,
        ),
      ),
      trailing: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (value != null)
            Text(
              value!,
              style: SmartRailType.body14.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
          const Icon(Icons.chevron_right, color: SmartRailColors.textSecondary),
        ],
      ),
    );
  }
}
