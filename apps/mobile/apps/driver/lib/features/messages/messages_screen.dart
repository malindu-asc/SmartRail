import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../core/time/clock_providers.dart';
import '../../core/widgets/failure_view.dart';
import '../../l10n/app_localizations.dart';
import 'messages.dart';

/// Messages from control (design 10) and the message sheet with Acknowledge (design 09).
class MessagesScreen extends ConsumerWidget {
  const MessagesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.watch(networkClockProvider);
    final messages = ref.watch(messagesProvider);

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      appBar: SmartRailAppBar(title: Text(l10n.messagesTitle), showBack: false),
      body: messages.when(
        loading: () => const LoadingList(),
        error: (error, _) => FailureView(
          error: error,
          onRetry: () => ref.invalidate(messagesProvider),
        ),
        data: (items) {
          if (items.isEmpty) {
            return EmptyState(
              icon: Icons.chat_bubble_outline,
              title: l10n.messagesEmpty,
            );
          }
          return RefreshIndicator(
            onRefresh: () async => ref.invalidate(messagesProvider),
            child: ListView(
              padding: const EdgeInsets.all(16),
              children: [
                for (final m in items) ...[
                  _MessageCard(message: m, sent: clock.formatTime(m.sentAt)),
                  const SizedBox(height: 12),
                ],
              ],
            ),
          );
        },
      ),
    );
  }
}

class _MessageCard extends ConsumerWidget {
  const _MessageCard({required this.message, required this.sent});

  final ControlMessage message;
  final String sent;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);

    return Material(
      color: SmartRailColors.surface,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(SmartRailRadius.lg),
        side: BorderSide(
          color: message.needsAck
              ? SmartRailColors.primary
              : SmartRailColors.border,
        ),
      ),
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: () => _open(context, ref),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                l10n.messageSent(sent),
                style: SmartRailType.caption.copyWith(
                  color: SmartRailColors.textSecondary,
                ),
              ),
              const SizedBox(height: 6),
              Text(
                message.body,
                maxLines: 3,
                overflow: TextOverflow.ellipsis,
                style: SmartRailType.body.copyWith(
                  color: SmartRailColors.textPrimary,
                ),
              ),
              if (message.needsAck) ...[
                const SizedBox(height: 8),
                Text(
                  l10n.messageNeedsAck,
                  style: SmartRailType.label14.copyWith(
                    color: SmartRailColors.primaryPressed,
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Future<void> _open(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = ref.read(networkClockProvider);

    return showSmartRailSheet<void>(
      context,
      builder: (sheet) => SheetBody(
        children: [
          Text(
            l10n.messageFromControl,
            style: SmartRailType.title.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          Text(
            message.body,
            style: SmartRailType.body.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          if (message.acknowledgedAt != null)
            Text(
              l10n.messageAcked(clock.formatTime(message.acknowledgedAt!)),
              style: SmartRailType.body14.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
          if (message.needsAck)
            SmartRailButton(
              label: l10n.messageAckButton,
              onPressed: () async {
                await ref
                    .read(messagesRepositoryProvider)
                    .acknowledge(message.id);
                ref.invalidate(messagesProvider);
                if (sheet.mounted) Navigator.of(sheet).pop();
              },
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
}
