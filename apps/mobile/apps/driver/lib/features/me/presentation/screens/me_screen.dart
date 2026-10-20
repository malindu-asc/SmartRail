import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../l10n/app_localizations.dart';
import '../../../auth/presentation/providers/auth_providers.dart';
import '../../../duty/domain/duty_domain.dart';
import '../../../duty/presentation/providers/duty_providers.dart';

/// Me (design 22) and the sign out sheet (23).
class MeScreen extends ConsumerWidget {
  const MeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final profile = ref.watch(profileProvider).value;

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      appBar: SmartRailAppBar(title: Text(l10n.meTitle), showBack: false),
      body: ListView(
        padding: const EdgeInsets.all(SmartRailSpacing.lg),
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 28,
                backgroundColor: SmartRailColors.primarySubtle,
                child: Text(
                  profile?.initials ?? '',
                  style: SmartRailType.title.copyWith(
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
                      style: SmartRailType.title.copyWith(
                        color: SmartRailColors.textPrimary,
                      ),
                    ),
                    Text(
                      profile?.email ?? '',
                      style: SmartRailType.body14.copyWith(
                        color: SmartRailColors.textSecondary,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 24),
          SmartRailButton(
            label: l10n.signOutRow,
            kind: ButtonKind.secondary,
            onPressed: () => _confirm(context, ref),
          ),
        ],
      ),
    );
  }

  Future<void> _confirm(BuildContext context, WidgetRef ref) async {
    final l10n = AppLocalizations.of(context);
    // A driver who is still signed on to a duty signs off first, so control is not left guessing.
    final duty = ref.read(myDutyProvider).value;
    final signedOn =
        duty?.duties.any((d) => d.status == DutyStatus.active) ?? false;

    final confirmed = await showSmartRailSheet<bool>(
      context,
      builder: (context) => SheetBody(
        children: [
          Text(
            l10n.signOutTitle,
            style: SmartRailType.title.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          Text(
            signedOn ? l10n.signOutBlocked : l10n.signOutBody,
            style: SmartRailType.body14.copyWith(
              color: SmartRailColors.textSecondary,
            ),
          ),
          if (!signedOn)
            SmartRailButton(
              label: l10n.signOutConfirm,
              kind: ButtonKind.danger,
              onPressed: () => Navigator.of(context).pop(true),
            ),
          SmartRailButton(
            label: signedOn ? l10n.close : l10n.cancel,
            kind: ButtonKind.secondary,
            onPressed: () => Navigator.of(context).pop(false),
          ),
        ],
      ),
    );

    if (confirmed == true) await ref.read(authProvider.notifier).signOut();
  }
}
