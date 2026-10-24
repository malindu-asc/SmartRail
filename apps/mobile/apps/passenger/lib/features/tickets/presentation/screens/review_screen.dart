import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../core/time/network_clock_provider.dart';
import '../../../../l10n/app_localizations.dart';
import '../providers/tickets_providers.dart';
import '../widgets/ticket_text.dart';

/// Review (design 30, without the payment step): the train, the seats, the fare for information, and Confirm booking. The booking is
/// issued at once as a valid ticket; the fare is not charged in this version.
class ReviewScreen extends ConsumerStatefulWidget {
  const ReviewScreen({super.key});

  @override
  ConsumerState<ReviewScreen> createState() => _ReviewScreenState();
}

class _ReviewScreenState extends ConsumerState<ReviewScreen> {
  bool _busy = false;
  String? _error;

  Future<void> _confirm() async {
    final l10n = AppLocalizations.of(context);
    final draft = ref.read(bookingDraftProvider);
    final offer = draft.offer;
    if (offer == null) return;

    setState(() {
      _busy = true;
      _error = null;
    });

    final result = await ref
        .read(ticketsRepositoryProvider)
        .book(
          journeyId: offer.journeyId,
          from: offer.fromCode,
          to: offer.toCode,
          travellers: draft.travellers,
          holdId: draft.hold?.id,
        );
    if (!mounted) return;

    result.match(
      (failure) => setState(() {
        _busy = false;
        _error = switch (failure) {
          ConflictFailure(:final code) when code == 'Ticketing.HoldExpired' =>
            l10n.reviewExpired,
          ConflictFailure() => l10n.seatGone,
          NetworkFailure() => l10n.errorNetwork,
          _ => l10n.errorGeneric,
        };
      }),
      (ticket) {
        ref.invalidate(ticketListProvider);
        ref.read(bookingDraftProvider.notifier).reset();
        // The ticket opens over the Tickets tab, so back goes to the list.
        context.go(AppRoutes.tickets);
        context.push(AppRoutes.ticket(ticket.summary.id, created: true));
      },
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final draft = ref.watch(bookingDraftProvider);
    final offer = draft.offer;

    if (offer == null) {
      return Scaffold(
        appBar: SmartRailAppBar(),
        body: Center(
          child: SmartRailButton(
            label: l10n.ticketsBuy,
            onPressed: () => context.go(AppRoutes.plan),
          ),
        ),
      );
    }

    final clock = clockFor('Asia/Colombo');
    final now = ref.watch(nowProvider)();
    final fare = offer.fare;
    final seats = seatsText(l10n, draft.seats.map((s) => '$s').toList());
    final hold = draft.hold;

    TextStyle body14(Color c) => SmartRailType.body14.copyWith(color: c);

    Widget row(String label, String value) => Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Flexible(
          child: Text(label, style: body14(SmartRailColors.textSecondary)),
        ),
        const SizedBox(width: 12),
        Text(value, style: body14(SmartRailColors.textPrimary)),
      ],
    );

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      appBar: SmartRailAppBar(title: Text(l10n.reviewTitle)),
      body: Column(
        children: [
          Expanded(
            child: ListView(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
              children: [
                Container(
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: SmartRailColors.surface,
                    border: Border.all(color: SmartRailColors.border),
                    borderRadius: BorderRadius.circular(SmartRailRadius.lg),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${offer.fromCode} → ${offer.toCode}',
                        style: SmartRailType.title.copyWith(
                          color: SmartRailColors.textPrimary,
                        ),
                      ),
                      const SizedBox(height: 6),
                      Text(
                        [
                          '${ticketWhen(l10n, offer.expectedDeparture, clock, now)} → ${clock.formatTime(offer.expectedArrival)}',
                          if (seats.isNotEmpty) seats,
                        ].join(' · '),
                        style: body14(SmartRailColors.textSecondary),
                      ),
                      const SizedBox(height: 6),
                      Text(
                        [
                          travellersText(l10n, draft.travellers),
                          if (offer.platform != null)
                            l10n.platformN(offer.platform!),
                        ].join(' · '),
                        style: body14(SmartRailColors.textSecondary),
                      ),
                    ],
                  ),
                ),
                if (hold != null) ...[
                  const SizedBox(height: 14),
                  InfoNote(l10n.reviewHold(clock.formatTime(hold.expiresAt))),
                ],
                const SizedBox(height: 14),
                Text(
                  l10n.reviewFare,
                  style: SmartRailType.heading16.copyWith(
                    color: SmartRailColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 14),
                for (final line in fare.lines) ...[
                  row(
                    '${line.count} × ${_category(l10n, line.category)}',
                    '${fare.currency} ${line.total.round()}',
                  ),
                  const SizedBox(height: 8),
                ],
                const Divider(
                  height: 1,
                  thickness: 1,
                  color: SmartRailColors.border,
                ),
                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(
                      l10n.reviewTotal,
                      style: SmartRailType.heading16.copyWith(
                        color: SmartRailColors.textPrimary,
                      ),
                    ),
                    Text(
                      fare.label,
                      style: SmartRailType.title.copyWith(
                        color: SmartRailColors.textPrimary,
                      ),
                    ),
                  ],
                ),
                const SizedBox(height: 14),
                Text(
                  l10n.chooseNoPayment,
                  style: SmartRailType.caption.copyWith(
                    color: SmartRailColors.textSecondary,
                  ),
                ),
                if (_error != null) ...[
                  const SizedBox(height: 14),
                  InfoBanner(kind: BannerKind.warning, title: _error!),
                ],
              ],
            ),
          ),
          StickyActionBar(
            children: [
              SmartRailButton(
                label: l10n.reviewConfirm,
                onPressed: _busy ? null : _confirm,
              ),
            ],
          ),
        ],
      ),
    );
  }

  String _category(AppLocalizations l10n, String category) =>
      switch (category) {
        'Child' => l10n.whoChild,
        'Senior' => l10n.whoSenior,
        _ => l10n.whoAdult,
      };
}
