import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../l10n/app_localizations.dart';
import 'messages.dart';

/// Request assistance (designs 12 to 14): a note is optional, one tap sends, and the sheet then says control has it.
Future<void> showAssistanceSheet(
  BuildContext context, {
  String? journeyId,
}) {
  return showSmartRailSheet<void>(
    context,
    builder: (context) => _AssistanceSheet(journeyId: journeyId),
  );
}

class _AssistanceSheet extends ConsumerStatefulWidget {
  const _AssistanceSheet({this.journeyId});

  final String? journeyId;

  @override
  ConsumerState<_AssistanceSheet> createState() => _AssistanceSheetState();
}

class _AssistanceSheetState extends ConsumerState<_AssistanceSheet> {
  final _note = TextEditingController();
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
    final result = await ref
        .read(messagesRepositoryProvider)
        .requestAssistance(
          journeyId: widget.journeyId,
          note: _note.text.trim().isEmpty ? null : _note.text.trim(),
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

    if (_sent) {
      return SheetBody(
        children: [
          Text(
            l10n.assistSentTitle,
            style: SmartRailType.title.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          Text(
            l10n.assistSentBody,
            style: SmartRailType.body14.copyWith(
              color: SmartRailColors.textSecondary,
            ),
          ),
          SmartRailButton(
            label: l10n.close,
            onPressed: () => Navigator.of(context).pop(),
          ),
        ],
      );
    }

    return SheetBody(
      children: [
        Text(
          l10n.assistTitle,
          style: SmartRailType.title.copyWith(
            color: SmartRailColors.textPrimary,
          ),
        ),
        Text(
          l10n.assistBody,
          style: SmartRailType.body14.copyWith(
            color: SmartRailColors.textSecondary,
          ),
        ),
        SmartRailTextField(
          label: l10n.assistNoteHint,
          controller: _note,
        ),
        if (_failed)
          InfoBanner(kind: BannerKind.warning, title: l10n.errorNetwork),
        SmartRailButton(
          label: l10n.assistSend,
          kind: ButtonKind.danger,
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
