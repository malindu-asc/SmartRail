import 'package:flutter/material.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../l10n/app_localizations.dart';

/// A failure shown in words a passenger can use. The words are chosen from the kind of failure, never from the API's own text.
class FailureView extends StatelessWidget {
  const FailureView({super.key, required this.error, required this.onRetry});

  final Object error;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    final message = switch (error) {
      NetworkFailure() => l10n.errorNetwork,
      NotFoundFailure() => l10n.errorNotFound,
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
