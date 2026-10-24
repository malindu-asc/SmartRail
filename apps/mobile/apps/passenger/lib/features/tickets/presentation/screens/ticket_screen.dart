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
      backgroundColor: SmartRailColors.page,
      appBar: SmartRailAppBar(title: Text(l10n.ticketTitle)),
      // Right after buying, the person can go home from here; the back arrow alone only went back through the booking.
      bottomNavigationBar: created
          ? StickyActionBar(
              children: [
                SmartRailButton(
                  label: l10n.ticketBackHome,
                  onPressed: () => context.go(AppRoutes.home),
                ),
              ],
            )
          : null,
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
    final s = detail.summary;
    final clock = clockFor(s.timeZone);
    final now = ref.watch(nowProvider)();
    final affected = s.state == TicketState.affected;

    final (kind, label) = switch (s.state) {
      TicketState.valid => (StatusKind.onTime, l10n.ticketValid),
      TicketState.affected => (StatusKind.cancelled, l10n.ticketAffected),
      TicketState.past => (StatusKind.noLiveData, l10n.ticketPast),
    };

    final faded = affected || s.state == TicketState.past;

    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
      children: [
        if (created) ...[
          InfoNote(l10n.ticketBooked),
          const SizedBox(height: 14),
        ],
        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: SmartRailColors.surface,
            border: Border.all(color: SmartRailColors.border),
            borderRadius: BorderRadius.circular(SmartRailRadius.lg),
          ),
          child: Column(
            children: [
              Text(
                l10n.ticketsRoute(s.fromName, s.toName),
                textAlign: TextAlign.center,
                style: SmartRailType.title20.copyWith(
                  color: SmartRailColors.textPrimary,
                ),
              ),
              const SizedBox(height: 14),
              Text(
                ticketWhenAndSeat(l10n, s, clock, now),
                textAlign: TextAlign.center,
                style: SmartRailType.body14.copyWith(
                  color: SmartRailColors.textSecondary,
                ),
              ),
              const SizedBox(height: 14),
              Semantics(
                label: '${l10n.ticketTitle} ${s.reference}',
                image: true,
                child: Container(
                  width: 220,
                  height: 220,
                  alignment: Alignment.center,
                  decoration: BoxDecoration(
                    color: SmartRailColors.surface,
                    border: Border.all(
                      color: SmartRailColors.borderStrong,
                      width: 1.5,
                    ),
                    borderRadius: BorderRadius.circular(SmartRailRadius.md),
                  ),
                  child: Opacity(
                    opacity: faded ? 0.35 : 1,
                    child: QrImageView(
                      data: detail.qrPayload,
                      size: 170,
                      padding: EdgeInsets.zero,
                      backgroundColor: SmartRailColors.surface,
                      errorCorrectionLevel: QrErrorCorrectLevel.M,
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 14),
              StatusChip(kind: kind, label: label),
              const SizedBox(height: 14),
              Text(
                s.reference,
                style: SmartRailType.caption.copyWith(
                  color: SmartRailColors.textTertiary,
                ),
              ),
              Text(
                l10n.ticketBookingNumber(detail.bookingCode),
                style: SmartRailType.caption.copyWith(
                  color: SmartRailColors.textTertiary,
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),
        if (affected) ...[
          InfoBanner(
            kind: BannerKind.warning,
            title: l10n.ticketAffectedTitle,
            detail: l10n.ticketAffectedBody,
          ),
          const SizedBox(height: 14),
          SmartRailButton(
            label: l10n.ticketFindAnother,
            onPressed: () =>
                context.go(AppRoutes.planResults(s.fromCode, s.toCode)),
          ),
        ] else
          InfoNote(l10n.ticketOffline(clock.formatTime(detail.fetchedAt))),
      ],
    );
  }
}
