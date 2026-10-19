import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../app/locale_provider.dart';
import '../../../../core/routing/app_routes.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../auth/presentation/providers/auth_providers.dart';
import '../../../settings/presentation/providers/settings_providers.dart';
import '../../../settings/presentation/widgets/language_switcher.dart';
import '../widgets/call_centre_sheet.dart';

/// The Me tab (design 32): who is signed in, the language, text size and notifications, my data, the call centre and sign out.
class MeScreen extends ConsumerWidget {
  const MeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final signedIn = ref.watch(authProvider.select((s) => s.isSignedIn));
    final locale = ref.watch(localeProvider);
    final textSize = ref.watch(textSizeProvider);
    final alertsOn = ref.watch(alertsOnProvider);

    return SafeArea(
      bottom: false,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
        children: [
          Text(
            l10n.meTitle,
            style: SmartRailType.headline.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          const SizedBox(height: 14),
          AppearIn(
            index: 0,
            child: signedIn
                ? const _ProfileCard()
                : _LogInCard(onTap: () => context.push(AppRoutes.login)),
          ),
          const SizedBox(height: 14),
          Card(
            child: Column(
              children: [
                _Row(
                  icon: AppIcons.globe,
                  label: l10n.language,
                  value: locale.languageCode == 'si'
                      ? l10n.languageSinhala
                      : l10n.languageEnglish,
                  onTap: () => _sheet(context, const LanguageSwitcher()),
                ),
                const Divider(
                  height: 1,
                  thickness: 1,
                  color: SmartRailColors.border,
                ),
                _Row(
                  icon: AppIcons.type,
                  label: l10n.meTextSize,
                  value: _sizeLabel(l10n, textSize),
                  onTap: () => _sheet(context, const _TextSizeChoice()),
                ),
                const Divider(
                  height: 1,
                  thickness: 1,
                  color: SmartRailColors.border,
                ),
                _Row(
                  icon: AppIcons.bellRow,
                  label: l10n.meNotifications,
                  value: alertsOn ? l10n.stateOn : l10n.stateOff,
                  onTap: () =>
                      ref.read(alertsOnProvider.notifier).set(!alertsOn),
                ),
              ],
            ),
          ),
          const SizedBox(height: 14),
          Card(
            child: Column(
              children: [
                if (signedIn) ...[
                  _Row(
                    icon: AppIcons.download,
                    label: l10n.meDownloadData,
                    onTap: () => _download(context, ref),
                  ),
                  const Divider(
                    height: 1,
                    thickness: 1,
                    color: SmartRailColors.border,
                  ),
                  _Row(
                    icon: AppIcons.trash,
                    label: l10n.meDeleteData,
                    danger: true,
                    onTap: () => _confirmDelete(context, ref),
                  ),
                  const Divider(
                    height: 1,
                    thickness: 1,
                    color: SmartRailColors.border,
                  ),
                ],
                _Row(
                  icon: AppIcons.phone,
                  label: l10n.meCallCentre,
                  onTap: () => showCallCentreSheet(context),
                ),
              ],
            ),
          ),
          if (signedIn) ...[
            const SizedBox(height: 14),
            Card(
              child: _Row(
                icon: AppIcons.logout,
                label: l10n.meSignOut,
                onTap: () => ref.read(authProvider.notifier).signOut(),
              ),
            ),
          ],
          const SizedBox(height: SmartRailSpacing.xl),
          Text(
            l10n.meAbout,
            style: Theme.of(context).textTheme.bodySmall?.copyWith(
              color: SmartRailColors.textSecondary,
            ),
          ),
        ],
      ),
    );
  }

  static String _sizeLabel(AppLocalizations l10n, TextSize size) =>
      switch (size) {
        TextSize.normal => l10n.textSizeNormal,
        TextSize.large => l10n.textSizeLarge,
        TextSize.larger => l10n.textSizeLarger,
      };

  Future<void> _sheet(BuildContext context, Widget child) =>
      showModalBottomSheet<void>(
        context: context,
        useRootNavigator: true,
        showDragHandle: true,
        builder: (context) => SafeArea(
          child: Padding(
            padding: const EdgeInsets.fromLTRB(
              SmartRailSpacing.xl,
              0,
              SmartRailSpacing.xl,
              SmartRailSpacing.xl,
            ),
            child: child,
          ),
        ),
      );

  Future<void> _download(BuildContext context, WidgetRef ref) async {
    final l10n = AppLocalizations.of(context);
    final result = await ref.read(authRepositoryProvider).exportData();
    if (!context.mounted) return;

    result.match(
      (failure) => ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(l10n.downloadFailed))),
      (json) => _sheet(
        context,
        Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              l10n.downloadTitle,
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: SmartRailSpacing.md),
            Flexible(
              child: SingleChildScrollView(
                child: SelectableText(
                  json,
                  style: Theme.of(
                    context,
                  ).textTheme.bodySmall?.copyWith(fontFamily: 'monospace'),
                ),
              ),
            ),
            const SizedBox(height: SmartRailSpacing.lg),
            SmartRailButton(
              label: l10n.downloadCopy,
              icon: Icons.copy,
              onPressed: () async {
                await Clipboard.setData(ClipboardData(text: json));
                if (!context.mounted) return;
                Navigator.of(context).pop();
                ScaffoldMessenger.of(
                  context,
                ).showSnackBar(SnackBar(content: Text(l10n.downloadCopied)));
              },
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _confirmDelete(BuildContext context, WidgetRef ref) async {
    final l10n = AppLocalizations.of(context);
    final confirmed = await showModalBottomSheet<bool>(
      context: context,
      useRootNavigator: true,
      showDragHandle: true,
      builder: (context) => SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(
            SmartRailSpacing.xl,
            0,
            SmartRailSpacing.xl,
            SmartRailSpacing.xl,
          ),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const CircleAvatar(
                radius: 28,
                backgroundColor: SmartRailColors.cancelledBg,
                child: Icon(
                  Icons.delete_outline,
                  color: SmartRailColors.cancelledText,
                ),
              ),
              const SizedBox(height: SmartRailSpacing.md),
              Text(
                l10n.deleteTitle,
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: SmartRailSpacing.sm),
              Text(l10n.deleteBody, textAlign: TextAlign.center),
              const SizedBox(height: SmartRailSpacing.lg),
              SmartRailButton(
                label: l10n.deleteConfirm,
                kind: ButtonKind.danger,
                onPressed: () => Navigator.of(context).pop(true),
              ),
              TextButton(
                onPressed: () => Navigator.of(context).pop(false),
                child: Text(l10n.cancel),
              ),
            ],
          ),
        ),
      ),
    );
    if (confirmed != true || !context.mounted) return;

    final failure = await ref.read(authProvider.notifier).deleteAccount();
    if (!context.mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(
        SnackBar(
          content: Text(failure == null ? l10n.deleteDone : l10n.errorGeneric),
        ),
      );
  }
}

class _LogInCard extends StatelessWidget {
  const _LogInCard({required this.onTap});

  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(SmartRailSpacing.lg),
          child: Row(
            children: [
              const CircleAvatar(
                radius: 24,
                backgroundColor: SmartRailColors.primarySubtle,
                child: Icon(
                  Icons.person_outline,
                  color: SmartRailColors.primary,
                ),
              ),
              const SizedBox(width: SmartRailSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      l10n.meLogIn,
                      style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    Text(
                      l10n.meLogInBody,
                      style: Theme.of(context).textTheme.bodySmall?.copyWith(
                        color: SmartRailColors.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
              const Icon(Icons.chevron_right),
            ],
          ),
        ),
      ),
    );
  }
}

class _ProfileCard extends ConsumerWidget {
  const _ProfileCard();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profile = ref.watch(profileProvider).value;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          children: [
            Container(
              width: 48,
              height: 48,
              alignment: Alignment.center,
              decoration: const BoxDecoration(
                shape: BoxShape.circle,
                color: SmartRailColors.primarySubtle,
              ),
              child: Text(
                profile?.initials ?? '',
                style: SmartRailType.label14.copyWith(
                  color: SmartRailColors.primaryPressed,
                ),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: profile == null
                  ? const Skeleton(width: 160, height: 16)
                  : Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          profile.fullName,
                          style: SmartRailType.heading16.copyWith(
                            color: SmartRailColors.textPrimary,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          profile.email,
                          style: SmartRailType.body14.copyWith(
                            color: SmartRailColors.textSecondary,
                          ),
                        ),
                      ],
                    ),
            ),
            const SvgIcon(
              AppIcons.right,
              size: 18,
              color: SmartRailColors.textTertiary,
            ),
          ],
        ),
      ),
    );
  }
}

class _Row extends StatelessWidget {
  const _Row({
    required this.icon,
    required this.label,
    required this.onTap,
    this.value,
    this.danger = false,
  });

  final String icon;
  final String label;
  final String? value;
  final bool danger;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final color = danger
        ? SmartRailColors.cancelledText
        : SmartRailColors.textPrimary;

    return Semantics(
      button: true,
      label: value == null ? label : '$label, $value',
      child: ExcludeSemantics(
        child: InkWell(
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Row(
              children: [
                SvgIcon(
                  icon,
                  size: 20,
                  color: danger
                      ? SmartRailColors.cancelledText
                      : SmartRailColors.textSecondary,
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    label,
                    style: SmartRailType.label14.copyWith(color: color),
                  ),
                ),
                if (value != null) ...[
                  Text(
                    value!,
                    style: SmartRailType.body14.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                  const SizedBox(width: 12),
                ],
                const SvgIcon(
                  AppIcons.rightSmall,
                  size: 16,
                  color: SmartRailColors.textTertiary,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _TextSizeChoice extends ConsumerWidget {
  const _TextSizeChoice();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final current = ref.watch(textSizeProvider);

    return Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(l10n.meTextSize, style: Theme.of(context).textTheme.labelLarge),
        for (final size in TextSize.values) ...[
          const SizedBox(height: SmartRailSpacing.md),
          SmartRailButton(
            label: MeScreen._sizeLabel(l10n, size),
            kind: current == size ? ButtonKind.tint : ButtonKind.secondary,
            onPressed: () => ref.read(textSizeProvider.notifier).set(size),
          ),
        ],
      ],
    );
  }
}
