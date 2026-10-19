import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../l10n/app_localizations.dart';
import '../providers/tickets_providers.dart';

/// "Add ticket" (design 26): a booking number and the email used at purchase put a ticket on this profile, for example on a new phone.
Future<void> showAddTicketSheet(BuildContext context) =>
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => const _AddTicket(),
    );

class _AddTicket extends ConsumerStatefulWidget {
  const _AddTicket();

  @override
  ConsumerState<_AddTicket> createState() => _AddTicketState();
}

class _AddTicketState extends ConsumerState<_AddTicket> {
  final _code = TextEditingController();
  final _contact = TextEditingController();
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _code.dispose();
    _contact.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final l10n = AppLocalizations.of(context);
    setState(() {
      _busy = true;
      _error = null;
    });

    final result = await ref
        .read(ticketsRepositoryProvider)
        .claim(bookingCode: _code.text, contact: _contact.text);
    if (!mounted) return;

    result.match(
      (failure) => setState(() {
        _busy = false;
        _error = l10n.addNotFound;
      }),
      (_) {
        ref.invalidate(ticketListProvider(false));
        Navigator.of(context).pop();
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(l10n.addDone)));
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Padding(
      padding: EdgeInsets.fromLTRB(
        SmartRailSpacing.xl,
        0,
        SmartRailSpacing.xl,
        SmartRailSpacing.xl + MediaQuery.viewInsetsOf(context).bottom,
      ),
      child: SingleChildScrollView(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(l10n.addTitle, style: Theme.of(context).textTheme.titleLarge),
            const SizedBox(height: SmartRailSpacing.lg),
            SmartRailTextField(
              label: l10n.addNumberLabel,
              hint: l10n.addNumberHint,
              controller: _code,
            ),
            Padding(
              padding: const EdgeInsets.symmetric(
                vertical: SmartRailSpacing.sm,
              ),
              child: Text(
                l10n.addNumberHelp,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: SmartRailColors.textSecondary,
                ),
              ),
            ),
            SmartRailTextField(
              label: l10n.addContactLabel,
              hint: l10n.addContactHint,
              controller: _contact,
              keyboardType: TextInputType.emailAddress,
            ),
            const SizedBox(height: SmartRailSpacing.lg),
            if (_error != null) ...[
              InfoBanner(kind: BannerKind.warning, title: _error!),
              const SizedBox(height: SmartRailSpacing.md),
            ],
            SmartRailButton(
              label: l10n.addTitle,
              onPressed: _busy ? null : _submit,
            ),
          ],
        ),
      ),
    );
  }
}
