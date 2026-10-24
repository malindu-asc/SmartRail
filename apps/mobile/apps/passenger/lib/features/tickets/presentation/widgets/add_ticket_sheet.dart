import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../l10n/app_localizations.dart';
import '../providers/tickets_providers.dart';

/// "Add ticket" (design 25): a 24 Bold title with a close button, the booking number with its help line, the email used at purchase,
/// and Add ticket. It puts a ticket on this profile, for example on a new phone.
Future<void> showAddTicketSheet(BuildContext context) =>
    showSmartRailSheet<void>(context, builder: (_) => const _AddTicket());

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
        ref.invalidate(ticketListProvider);
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

    return SheetBody(
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                l10n.addTitle,
                style: SmartRailType.title24.copyWith(
                  color: SmartRailColors.textPrimary,
                ),
              ),
            ),
            IconButton(
              tooltip: MaterialLocalizations.of(context).closeButtonTooltip,
              onPressed: () => Navigator.of(context).pop(),
              icon: const SvgIcon(
                AppIcons.close,
                size: 22,
                color: SmartRailColors.textPrimary,
              ),
            ),
          ],
        ),
        SmartRailTextField(
          label: l10n.addNumberLabel,
          hint: l10n.addNumberHint,
          controller: _code,
        ),
        Text(
          l10n.addNumberHelp,
          style: SmartRailType.caption.copyWith(
            color: SmartRailColors.textSecondary,
          ),
        ),
        SmartRailTextField(
          label: l10n.addContactLabel,
          hint: l10n.addContactHint,
          controller: _contact,
          keyboardType: TextInputType.emailAddress,
        ),
        if (_error != null)
          InfoBanner(kind: BannerKind.warning, title: _error!),
        SmartRailButton(
          label: l10n.addTitle,
          onPressed: _busy ? null : _submit,
        ),
      ],
    );
  }
}
