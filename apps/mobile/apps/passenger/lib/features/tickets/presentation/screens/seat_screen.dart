import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../core/widgets/failure_view.dart';
import '../../../../l10n/app_localizations.dart';
import '../../domain/tickets_domain.dart';
import '../providers/tickets_providers.dart';

/// Choose a seat (design 31), only for a train that needs one: a note about holding the seats, Coach A / B / C, the legend, and the
/// seats in rows of two, an aisle and two. One seat for each traveller; they are held for a few minutes while the booking is finished.
class SeatScreen extends ConsumerStatefulWidget {
  const SeatScreen({super.key});

  @override
  ConsumerState<SeatScreen> createState() => _SeatScreenState();
}

class _SeatScreenState extends ConsumerState<SeatScreen> {
  String _coach = 'A';
  List<Seat> _chosen = [];
  bool _busy = false;
  bool _seeded = false;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final draft = ref.watch(bookingDraftProvider);
    final offer = draft.offer;
    final query = draft.query;

    if (offer == null || query == null) {
      // Opened without a booking (for example after a restart): back to the start.
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

    final needed = query.travellers.total;
    final map = ref.watch(
      seatMapProvider((
        journeyId: offer.journeyId,
        from: offer.fromCode,
        to: offer.toCode,
      )),
    );

    return Scaffold(
      appBar: SmartRailAppBar(
        title: Text(needed == 1 ? l10n.seatTitle : l10n.seatTitleMany(needed)),
      ),
      body: map.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => FailureView(
          error: error,
          onRetry: () => ref.invalidate(seatMapProvider),
        ),
        data: (seats) {
          if (!_seeded) {
            // Seats this person already holds start chosen, so coming back does not lose them.
            _seeded = true;
            _chosen = seats.yours.take(needed).toList();
          }

          return Column(
            children: [
              Expanded(
                child: ListView(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
                  children: [
                    DecoratedBox(
                      decoration: BoxDecoration(
                        color: SmartRailColors.primarySubtle,
                        borderRadius: BorderRadius.circular(SmartRailRadius.md),
                      ),
                      child: Padding(
                        padding: const EdgeInsets.all(12),
                        child: Row(
                          children: [
                            const SvgIcon(
                              AppIcons.clockHold,
                              size: 18,
                              color: SmartRailColors.primaryPressed,
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: Text(
                                l10n.seatHoldNote,
                                style: SmartRailType.label14.copyWith(
                                  color: SmartRailColors.primaryPressed,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),
                    Wrap(
                      spacing: 8,
                      children: [
                        for (final c in seats.coaches)
                          Semantics(
                            button: true,
                            selected: _coach == c,
                            label: l10n.seatCoach(c),
                            child: ExcludeSemantics(
                              child: GestureDetector(
                                onTap: () => setState(() => _coach = c),
                                child: Container(
                                  padding: const EdgeInsets.symmetric(
                                    horizontal: 18,
                                    vertical: 8,
                                  ),
                                  decoration: BoxDecoration(
                                    color: _coach == c
                                        ? SmartRailColors.textPrimary
                                        : SmartRailColors.surface,
                                    border: Border.all(
                                      color: _coach == c
                                          ? SmartRailColors.textPrimary
                                          : SmartRailColors.borderStrong,
                                    ),
                                    borderRadius: BorderRadius.circular(
                                      SmartRailRadius.pill,
                                    ),
                                  ),
                                  child: Text(
                                    l10n.seatCoach(c),
                                    style: SmartRailType.label14.copyWith(
                                      color: _coach == c
                                          ? SmartRailColors.surface
                                          : SmartRailColors.textPrimary,
                                    ),
                                  ),
                                ),
                              ),
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 14),
                    const _Legend(),
                    const SizedBox(height: 14),
                    _Grid(
                      coach: _coach,
                      map: seats,
                      chosen: _chosen,
                      onTap: (seat) => _toggle(seat, needed),
                    ),
                  ],
                ),
              ),
              StickyActionBar(
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          _chosen.isEmpty
                              ? l10n.seatSelected(0, needed)
                              : (needed == 1
                                    ? l10n.ticketsCoachSeat(
                                        _chosen.first.coach,
                                        '${_chosen.first.number}',
                                      )
                                    : l10n.seatSelected(
                                        _chosen.length,
                                        needed,
                                      )),
                          style: SmartRailType.body14.copyWith(
                            color: SmartRailColors.textSecondary,
                          ),
                        ),
                      ),
                      Text(
                        offer.fare.label,
                        style: SmartRailType.title.copyWith(
                          color: SmartRailColors.textPrimary,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  SmartRailButton(
                    label: needed == 1
                        ? l10n.seatConfirm
                        : l10n.seatConfirmMany,
                    onPressed: _chosen.length == needed && !_busy
                        ? () => _confirm(offer)
                        : null,
                  ),
                ],
              ),
            ],
          );
        },
      ),
    );
  }

  void _toggle(Seat seat, int needed) {
    setState(() {
      if (_chosen.contains(seat)) {
        _chosen = [..._chosen]..remove(seat);
      } else if (needed == 1) {
        _chosen = [seat];
      } else if (_chosen.length < needed) {
        _chosen = [..._chosen, seat];
      }
    });
  }

  Future<void> _confirm(TicketOffer offer) async {
    final l10n = AppLocalizations.of(context);
    setState(() => _busy = true);

    final result = await ref
        .read(ticketsRepositoryProvider)
        .holdSeats(
          journeyId: offer.journeyId,
          from: offer.fromCode,
          to: offer.toCode,
          seats: _chosen,
        );
    if (!mounted) return;

    result.match(
      (failure) {
        setState(() => _busy = false);
        if (failure is ConflictFailure) {
          // Someone was faster: show the map again as it is now.
          _seeded = false;
          ref.invalidate(seatMapProvider);
          setState(() => _chosen = []);
          ScaffoldMessenger.of(context)
            ..hideCurrentSnackBar()
            ..showSnackBar(SnackBar(content: Text(l10n.seatGone)));
        } else {
          ScaffoldMessenger.of(context)
            ..hideCurrentSnackBar()
            ..showSnackBar(SnackBar(content: Text(l10n.errorGeneric)));
        }
      },
      (hold) {
        setState(() => _busy = false);
        final draft = ref.read(bookingDraftProvider.notifier);
        draft.setSeats(_chosen);
        draft.setHold(hold);
        context.push(AppRoutes.review);
      },
    );
  }
}

class _Legend extends StatelessWidget {
  const _Legend();

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    Widget item(Color fill, Color border, String text) => Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 18,
          height: 18,
          decoration: BoxDecoration(
            color: fill,
            border: Border.all(color: border),
            borderRadius: BorderRadius.circular(SmartRailRadius.sm),
          ),
        ),
        const SizedBox(width: 6),
        Text(
          text,
          style: SmartRailType.caption.copyWith(
            color: SmartRailColors.textSecondary,
          ),
        ),
      ],
    );

    return Wrap(
      spacing: 14,
      runSpacing: 8,
      children: [
        item(
          SmartRailColors.surface,
          SmartRailColors.borderStrong,
          l10n.seatFree,
        ),
        item(SmartRailColors.bgSoft, SmartRailColors.bgSoft, l10n.seatTaken),
        item(SmartRailColors.primary, SmartRailColors.primary, l10n.seatYours),
      ],
    );
  }
}

/// The seats: cells of 56 by 44 with 8 between, two seats, a 24 aisle and two seats, in a hairline card with 14 padding.
class _Grid extends StatelessWidget {
  const _Grid({
    required this.coach,
    required this.map,
    required this.chosen,
    required this.onTap,
  });

  final String coach;
  final SeatMap map;
  final List<Seat> chosen;
  final ValueChanged<Seat> onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final rows = (map.seatsPerCoach / 4).ceil();

    Widget cell(int number) {
      if (number > map.seatsPerCoach) {
        return const SizedBox(width: 56, height: 44);
      }

      final seat = Seat(coach, number);
      final taken = map.taken.contains(seat);
      final mine = chosen.contains(seat);

      return Semantics(
        button: !taken,
        selected: mine,
        label: taken
            ? l10n.seatLabelTaken(number)
            : (mine ? l10n.seatLabelYours(number) : l10n.seatLabelFree(number)),
        child: ExcludeSemantics(
          child: GestureDetector(
            onTap: taken ? null : () => onTap(seat),
            child: AnimatedContainer(
              duration: SmartRailMotion.of(context, SmartRailMotion.fast),
              width: 56,
              height: 44,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: mine
                    ? SmartRailColors.primary
                    : (taken
                          ? SmartRailColors.bgSoft
                          : SmartRailColors.surface),
                border: Border.all(
                  color: mine
                      ? SmartRailColors.primary
                      : (taken
                            ? SmartRailColors.bgSoft
                            : SmartRailColors.borderStrong),
                  width: 1.5,
                ),
                borderRadius: BorderRadius.circular(SmartRailRadius.sm),
              ),
              child: Text(
                '$number',
                style: SmartRailType.label14.copyWith(
                  color: mine
                      ? SmartRailColors.onPrimary
                      : (taken
                            ? SmartRailColors.borderStrong
                            : SmartRailColors.textSecondary),
                ),
              ),
            ),
          ),
        ),
      );
    }

    return DecoratedBox(
      decoration: BoxDecoration(
        border: Border.all(color: SmartRailColors.border),
        borderRadius: BorderRadius.circular(SmartRailRadius.lg),
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          children: [
            for (var r = 0; r < rows; r++)
              Padding(
                padding: EdgeInsets.only(bottom: r == rows - 1 ? 0 : 8),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    cell(r * 4 + 1),
                    cell(r * 4 + 2),
                    const SizedBox(width: 24),
                    cell(r * 4 + 3),
                    cell(r * 4 + 4),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
}
