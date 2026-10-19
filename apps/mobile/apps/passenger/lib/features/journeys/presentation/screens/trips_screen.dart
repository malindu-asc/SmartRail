import 'package:flutter/material.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../l10n/app_localizations.dart';

/// The Trips tab. A placeholder until followed journeys (SPEC-020) and tickets (SPEC-022) are built.
class TripsScreen extends StatelessWidget {
  const TripsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    return EmptyState(icon: Icons.route_outlined, title: l10n.tripsTitle, message: l10n.tripsBody);
  }
}
