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
    final theme = Theme.of(context);
    final fare = offer.fare;
    final seats = seatsText(l10n, draft.seats.map((s) => '$s').toList());
    final hold = draft.hold;

    return Scaffold(
      appBar: SmartRailAppBar(title: Text(l10n.reviewTitle)),
      body: Column(
        children: [
          Expanded(
            child: ListView(
              padding: const EdgeInsets.all(SmartRailSpacing.lg),
              children: [
                Card(
                  child: Padding(
                    padding: const EdgeInsets.all(SmartRailSpacing.lg),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          '${offer.fromCode} → ${offer.toCode}',
                          style: theme.textTheme.titleLarge,
                        ),
                        const SizedBox(height: SmartRailSpacing.xs),
                        Text(
                          [
                            '${ticketWhen(l10n, offer.expectedDeparture, clock, now)} → ${clock.formatTime(offer.expectedArrival)}',
                            if (seats.isNotEmpty) seats,
                          ].join(' · '),
                          style: theme.textTheme.bodyMedium?.copyWith(
                            color: SmartRailColors.textSecondary,
                          ),
                        ),
                        Text(
                          [
                            travellersText(l10n, draft.travellers),
                            if (offer.platform != null)
                              l10n.platformN(offer.platform!),
                          ].join(' · '),
                          style: theme.textTheme.bodyMedium?.copyWith(
                            color: SmartRailColors.textSecondary,
                          ),
                        ),
                      ],
                    ),
                  ),
                ),
                if (hold != null) ...[
                  const SizedBox(height: SmartRailSpacing.md),
                  InfoBanner(
                    kind: BannerKind.message,
                    title: l10n.reviewHold(clock.formatTime(hold.expiresAt)),
                  ),
                ],
                const SizedBox(height: SmartRailSpacing.lg),
                for (final line in fare.lines)
                  Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Row(
                      children: [
                        Expanded(
                          child: Text(
                            '${l10n.reviewFare} · ${line.count} × ${_category(l10n, line.category)}',
                          ),
                        ),
                        Text('${fare.currency} ${line.total.round()}'),
                      ],
                    ),
                  ),
                const Divider(),
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        l10n.reviewTotal,
                        style: theme.textTheme.titleMedium,
                      ),
                    ),
                    Text(fare.label, style: theme.textTheme.titleLarge),
                  ],
                ),
                const SizedBox(height: SmartRailSpacing.md),
                Text(
                  l10n.chooseNoPayment,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: SmartRailColors.textSecondary,
                  ),
                ),
                if (_error != null) ...[
                  const SizedBox(height: SmartRailSpacing.md),
                  InfoBanner(kind: BannerKind.warning, title: _error!),
                ],
              ],
            ),
          ),
          DecoratedBox(
            decoration: const BoxDecoration(
              color: SmartRailColors.surface,
              border: Border(top: BorderSide(color: SmartRailColors.border)),
            ),
            child: SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.all(SmartRailSpacing.lg),
                child: SmartRailButton(
                  label: l10n.reviewConfirm,
                  onPressed: _busy ? null : _confirm,
                ),
              ),
            ),
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
