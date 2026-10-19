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

/// Choose a seat (design 28), only for a train that needs one. One seat for each traveller; the seats are held for a few minutes while
/// the booking is finished, so nobody else can take them.
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
                  padding: const EdgeInsets.all(SmartRailSpacing.lg),
                  children: [
                    Wrap(
                      spacing: SmartRailSpacing.sm,
                      children: [
                        for (final c in seats.coaches)
                          ChoiceChip(
                            label: Text(l10n.seatCoach(c)),
                            selected: _coach == c,
                            onSelected: (_) => setState(() => _coach = c),
                          ),
                      ],
                    ),
                    const SizedBox(height: SmartRailSpacing.md),
                    const _Legend(),
                    const SizedBox(height: SmartRailSpacing.md),
                    _Grid(
                      coach: _coach,
                      map: seats,
                      chosen: _chosen,
                      onTap: (seat) => _toggle(seat, needed),
                    ),
                  ],
                ),
              ),
              _Bar(
                label: _chosen.isEmpty
                    ? l10n.seatSelected(0, needed)
                    : (needed == 1
                          ? l10n.ticketsCoachSeat(
                              _chosen.first.coach,
                              '${_chosen.first.number}',
                            )
                          : l10n.seatSelected(_chosen.length, needed)),
                fare: offer.fare.label,
                button: needed == 1 ? l10n.seatConfirm : l10n.seatConfirmMany,
                onPressed: _chosen.length == needed && !_busy
                    ? () => _confirm(offer)
                    : null,
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
            borderRadius: BorderRadius.circular(5),
          ),
        ),
        const SizedBox(width: SmartRailSpacing.xs),
        Text(text, style: Theme.of(context).textTheme.labelSmall),
      ],
    );

    return Wrap(
      spacing: SmartRailSpacing.lg,
      children: [
        item(
          SmartRailColors.surface,
          SmartRailColors.borderStrong,
          l10n.seatFree,
        ),
        item(SmartRailColors.bgSoft, SmartRailColors.border, l10n.seatTaken),
        item(SmartRailColors.primary, SmartRailColors.primary, l10n.seatYours),
      ],
    );
  }
}

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
        return const SizedBox(width: 56, height: 52);
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
              height: 52,
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
                            ? SmartRailColors.border
                            : SmartRailColors.borderStrong),
                ),
                borderRadius: BorderRadius.circular(SmartRailRadius.md),
              ),
              child: Text(
                '$number',
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  fontWeight: FontWeight.w600,
                  color: mine
                      ? SmartRailColors.onPrimary
                      : (taken
                            ? SmartRailColors.textTertiary
                            : SmartRailColors.textPrimary),
                ),
              ),
            ),
          ),
        ),
      );
    }

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(SmartRailSpacing.lg),
        child: Column(
          children: [
            for (var r = 0; r < rows; r++)
              Padding(
                padding: const EdgeInsets.only(bottom: SmartRailSpacing.sm),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    cell(r * 4 + 1),
                    cell(r * 4 + 2),
                    const SizedBox(width: SmartRailSpacing.lg),
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

class _Bar extends StatelessWidget {
  const _Bar({
    required this.label,
    required this.fare,
    required this.button,
    required this.onPressed,
  });

  final String label;
  final String fare;
  final String button;
  final VoidCallback? onPressed;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: const BoxDecoration(
        color: SmartRailColors.surface,
        border: Border(top: BorderSide(color: SmartRailColors.border)),
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.all(SmartRailSpacing.lg),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                children: [
                  Expanded(child: Text(label)),
                  Text(fare, style: Theme.of(context).textTheme.titleLarge),
                ],
              ),
              const SizedBox(height: SmartRailSpacing.md),
              SmartRailButton(label: button, onPressed: onPressed),
            ],
          ),
        ),
      ),
    );
  }
}
