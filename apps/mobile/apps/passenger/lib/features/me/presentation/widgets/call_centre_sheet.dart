import 'package:flutter/material.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../l10n/app_localizations.dart';

/// The railway's call centre number, set when the app is built: `--dart-define=CALL_CENTRE_NUMBER=...`. Empty until it is known.
const callCentreNumber = String.fromEnvironment('CALL_CENTRE_NUMBER');

/// A sheet with the call centre's details. Used by Me and by a ticket the railway cancelled.
Future<void> showCallCentreSheet(BuildContext context) {
  final l10n = AppLocalizations.of(context);

  return showModalBottomSheet<void>(
    context: context,
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
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              l10n.callCentreTitle,
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: SmartRailSpacing.sm),
            Text(l10n.callCentreBody),
            const SizedBox(height: SmartRailSpacing.sm),
            Text(
              callCentreNumber.isEmpty
                  ? l10n.callCentreNoNumber
                  : l10n.callCentreNumber(callCentreNumber),
              style: Theme.of(
                context,
              ).textTheme.bodyLarge?.copyWith(fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: SmartRailSpacing.lg),
            SmartRailButton(
              label: l10n.close,
              kind: ButtonKind.secondary,
              onPressed: () => Navigator.of(context).pop(),
            ),
          ],
        ),
      ),
    ),
  );
}
