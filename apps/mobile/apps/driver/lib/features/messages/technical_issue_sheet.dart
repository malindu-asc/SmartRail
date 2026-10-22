import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../core/widgets/driver_widgets.dart';
import '../../l10n/app_localizations.dart';
import 'messages.dart';

/// Report a technical issue: the app, the phone or the train's equipment. It is sent to control at once, not saved on the phone, and
/// control reads it in "Driver reports". The words sent are always English, because control reads them.
Future<void> showTechnicalIssueSheet(BuildContext context, WidgetRef ref) {
  return showSmartRailSheet<void>(
    context,
    builder: (sheet) => _TechnicalIssue(ref: ref),
  );
}

const _typesForControl = [
  'App problem',
  'Phone or signal problem',
  'Train equipment fault',
  'Other',
];

class _TechnicalIssue extends StatefulWidget {
  const _TechnicalIssue({required this.ref});

  final WidgetRef ref;

  @override
  State<_TechnicalIssue> createState() => _TechnicalIssueState();
}

class _TechnicalIssueState extends State<_TechnicalIssue> {
  final _note = TextEditingController();
  int _chosen = 0;
  bool _busy = false;
  bool _sent = false;
  bool _failed = false;

  @override
  void dispose() {
    _note.dispose();
    super.dispose();
  }

  Future<void> _send() async {
    setState(() {
      _busy = true;
      _failed = false;
    });
    final detail = _note.text.trim();
    final result = await widget.ref
        .read(messagesRepositoryProvider)
        .requestAssistance(
          note:
              'Technical issue · ${_typesForControl[_chosen]}'
              '${detail.isEmpty ? '' : ': $detail'}',
        );
    if (!mounted) return;
    setState(() {
      _busy = false;
      _sent = result.isRight();
      _failed = result.isLeft();
    });
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final labels = [
      l10n.techApp,
      l10n.techPhone,
      l10n.techTrain,
      l10n.techOther,
    ];

    if (_sent) {
      return SheetBody(
        children: [
          SheetText(title: l10n.techSentTitle, body: l10n.techSentBody),
          SmartRailButton(
            label: l10n.close,
            onPressed: () => Navigator.of(context).pop(),
          ),
        ],
      );
    }

    return SheetBody(
      children: [
        SheetText(title: l10n.techIssueTitle, body: l10n.techIssueBody),
        Column(
          children: [
            for (var i = 0; i < labels.length; i++) ...[
              OptionRow(
                label: labels[i],
                selected: _chosen == i,
                onTap: () => setState(() => _chosen = i),
              ),
              const SizedBox(height: 8),
            ],
          ],
        ),
        SmartRailTextField(label: l10n.techNoteHint, controller: _note),
        if (_failed)
          InfoBanner(kind: BannerKind.warning, title: l10n.errorNetwork),
        SmartRailButton(
          label: l10n.techSend,
          onPressed: _busy ? null : _send,
        ),
        SmartRailButton(
          label: l10n.cancel,
          kind: ButtonKind.secondary,
          onPressed: () => Navigator.of(context).pop(),
        ),
      ],
    );
  }
}

/// The emergency numbers, as text. In an emergency the railway radio comes first; these are the national services.
Future<void> showEmergencyContactsSheet(BuildContext context) {
  final l10n = AppLocalizations.of(context);
  final contacts = [
    (l10n.contactPolice, '119'),
    (l10n.contactAmbulance, '1990'),
    (l10n.contactFire, '110'),
  ];

  return showSmartRailSheet<void>(
    context,
    builder: (sheet) => SheetBody(
      children: [
        SheetText(title: l10n.emergencyTitle, body: l10n.emergencyBody),
        RowsCard(
          children: [
            for (final (name, number) in contacts)
              ListTile(
                title: Text(name),
                trailing: Text(
                  number,
                  style: SmartRailType.title20.copyWith(
                    color: SmartRailColors.textPrimary,
                  ),
                ),
              ),
          ],
        ),
        SmartRailButton(
          label: l10n.close,
          kind: ButtonKind.secondary,
          onPressed: () => Navigator.of(sheet).pop(),
        ),
      ],
    ),
  );
}
