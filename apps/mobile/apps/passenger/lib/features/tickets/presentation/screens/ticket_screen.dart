import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:qr_flutter/qr_flutter.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../core/time/network_clock_provider.dart';
import '../../../../core/widgets/failure_view.dart';
import '../../../../l10n/app_localizations.dart';
import '../../domain/tickets_domain.dart';
import '../providers/tickets_providers.dart';
import '../widgets/ticket_text.dart';

/// One ticket (design 31): the route, the time and seat, and the QR code. The QR is drawn on the phone from the signed text, so it
/// shows without a connection; the note says when it was last checked with SmartRail.
class TicketScreen extends ConsumerWidget {
  const TicketScreen({super.key, required this.ticketId, this.created = false});

  final String ticketId;

  /// True right after booking, so the screen says the booking is confirmed.
  final bool created;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final ticket = ref.watch(ticketDetailProvider(ticketId));

    return Scaffold(
      appBar: AppBar(title: Text(l10n.ticketTitle)),
      body: ticket.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => FailureView(
          error: error,
          onRetry: () => ref.invalidate(ticketDetailProvider(ticketId)),
        ),
        data: (detail) => _Ticket(detail: detail, created: created),
      ),
    );
  }
}

class _Ticket extends ConsumerWidget {
  const _Ticket({required this.detail, required this.created});

  final TicketDetail detail;
  final bool created;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final s = detail.summary;
    final clock = clockFor(s.timeZone);
    final now = ref.watch(nowProvider)();
    final affected = s.state == TicketState.affected;

    final (kind, label) = switch (s.state) {
      TicketState.valid => (StatusKind.onTime, l10n.ticketValid),
      TicketState.affected => (StatusKind.cancelled, l10n.ticketAffected),
      TicketState.past => (StatusKind.noLiveData, l10n.ticketPast),
    };

    return ListView(
      padding: const EdgeInsets.all(SmartRailSpacing.lg),
      children: [
        if (created) ...[
          InfoBanner(kind: BannerKind.message, title: l10n.ticketBooked),
          const SizedBox(height: SmartRailSpacing.md),
        ],
        Card(
          child: Padding(
            padding: const EdgeInsets.all(SmartRailSpacing.lg),
            child: Column(
              children: [
                Text(
                  l10n.ticketsRoute(s.fromName, s.toName),
                  style: theme.textTheme.titleLarge,
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: SmartRailSpacing.xs),
                Text(
                  ticketWhenAndSeat(l10n, s, clock, now),
                  style: theme.textTheme.bodyMedium?.copyWith(
                    color: SmartRailColors.textSecondary,
                  ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: SmartRailSpacing.lg),
                Semantics(
                  label: '${l10n.ticketTitle} ${s.reference}',
                  image: true,
                  child: Container(
                    padding: const EdgeInsets.all(SmartRailSpacing.lg),
                    decoration: BoxDecoration(
                      color: SmartRailColors.surface,
                      border: Border.all(color: SmartRailColors.borderStrong),
                      borderRadius: BorderRadius.circular(SmartRailRadius.md),
                    ),
                    child: Opacity(
                      opacity: affected || s.state == TicketState.past
                          ? 0.35
                          : 1,
                      child: QrImageView(
                        data: detail.qrPayload,
                        size: 200,
                        backgroundColor: SmartRailColors.surface,
                        errorCorrectionLevel: QrErrorCorrectLevel.M,
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: SmartRailSpacing.md),
                StatusChip(kind: kind, label: label),
                const SizedBox(height: SmartRailSpacing.sm),
                Text(
                  s.reference,
                  style: theme.textTheme.labelMedium?.copyWith(
                    color: SmartRailColors.textSecondary,
                  ),
                ),
                Text(
                  l10n.ticketBookingNumber(detail.bookingCode),
                  style: theme.textTheme.labelSmall?.copyWith(
                    color: SmartRailColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: SmartRailSpacing.md),
        if (affected) ...[
          InfoBanner(
            kind: BannerKind.warning,
            title: l10n.ticketAffectedTitle,
            detail: l10n.ticketAffectedBody,
          ),
          const SizedBox(height: SmartRailSpacing.md),
          SmartRailButton(
            label: l10n.ticketFindAnother,
            onPressed: () =>
                context.go(AppRoutes.planResults(s.fromCode, s.toCode)),
          ),
        ] else
          InfoBanner(
            kind: BannerKind.message,
            title: l10n.ticketOffline(clock.formatTime(detail.fetchedAt)),
          ),
      ],
    );
  }
}
