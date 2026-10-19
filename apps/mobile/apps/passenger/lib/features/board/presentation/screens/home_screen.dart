import 'package:flutter/material.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../l10n/app_localizations.dart';

/// The Home tab. A placeholder until SPEC-009 builds the departures board and the journey timeline here.
class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return EmptyState(icon: Icons.train_outlined, title: l10n.homeTitle, message: l10n.homeBody);
  }
}
