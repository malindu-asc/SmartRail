import 'package:flutter/material.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../l10n/app_localizations.dart';

/// A failure in words a driver can use. The words are chosen from the kind of failure, never from the API's own text.
class FailureView extends StatelessWidget {
  const FailureView({super.key, required this.error, required this.onRetry});

  final Object error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    final message = switch (error) {
      NetworkFailure() => l10n.errorNetwork,
      ForbiddenFailure() => l10n.errorForbidden,
      _ => l10n.errorGeneric,
    };

    return ErrorState(
      title: l10n.errorTitle,
      message: message,
      retryLabel: l10n.retry,
      onRetry: onRetry,
    );
  }
}

/// A loading screen for the first load of a list: grey bars the shape of the content, never a bare spinner.
class LoadingList extends StatelessWidget {
  const LoadingList({super.key, this.rows = 3});

  final int rows;

  @override
  Widget build(BuildContext context) {
    return ListView(
      physics: const NeverScrollableScrollPhysics(),
      padding: const EdgeInsets.all(SmartRailSpacing.lg),
      children: [
        for (var i = 0; i < rows; i++) ...[
          const Skeleton(height: 112),
          const SizedBox(height: SmartRailSpacing.md),
        ],
      ],
    );
  }
}
