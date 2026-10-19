import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../core/time/network_clock_provider.dart';
import '../../../../core/widgets/failure_view.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../auth/presentation/providers/auth_providers.dart';
import '../../domain/tickets_domain.dart';
import '../providers/tickets_providers.dart';
import '../widgets/ticket_text.dart';

/// Choose ticket (design 29): the trains between the two stations with the fare for who is travelling, the note that a delay never
/// makes a new ticket, and the total with Continue. It needs an account to go on.
class ChooseTicketScreen extends ConsumerStatefulWidget {
  const ChooseTicketScreen({super.key, required this.query});

  final OfferQuery query;

  @override
  ConsumerState<ChooseTicketScreen> createState() => _ChooseTicketScreenState();
}

class _ChooseTicketScreenState extends ConsumerState<ChooseTicketScreen> {
  String? _selected;

  @override
  void initState() {
    super.initState();
    // A new booking starts here.
    Future.microtask(
      () => ref.read(bookingDraftProvider.notifier).start(widget.query),
    );
  }

  OfferQuery get _query => ref.read(bookingDraftProvider).query ?? widget.query;

  Future<void> _travellers() async {
    final current = _query.travellers;
    final picked = await showSmartRailSheet<Travellers>(
      context,
      builder: (_) => _TravellersSheet(initial: current),
    );
    if (picked == null || !mounted) return;
    ref.read(bookingDraftProvider.notifier).setTravellers(picked);
    setState(() => _selected = null);
  }

  Future<void> _continue(TicketOffer offer) async {
    // Tickets belong to a profile, so this is where the passenger is asked to log in (ASM-012).
    if (!ref.read(authProvider).isSignedIn) {
      final signedIn = await context.push<bool>(AppRoutes.login);
      if (signedIn != true || !mounted) return;
    }

    ref.read(bookingDraftProvider.notifier).choose(offer);
    if (!mounted) return;
    context.push(offer.seatRequired ? AppRoutes.seats : AppRoutes.review);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final draft = ref.watch(bookingDraftProvider);
    final query = draft.query ?? widget.query;
    final offers = ref.watch(offersProvider(query));
    final data = offers.value;

    return Scaffold(
      appBar: SmartRailAppBar(
        title: Text(
          data == null ? '' : l10n.chooseTitle(data.fromName, data.toName),
        ),
      ),
      body: offers.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => FailureView(
          error: error,
          onRetry: () => ref.invalidate(offersProvider(query)),
        ),
        data: (result) => _Body(
          result: result,
          travellers: query.travellers,
          selected: _selected,
          onSelect: (id) => setState(() => _selected = id),
          onTravellers: _travellers,
          onContinue: _continue,
        ),
      ),
    );
  }
}

class _Body extends StatelessWidget {
  const _Body({
    required this.result,
    required this.travellers,
    required this.selected,
    required this.onSelect,
    required this.onTravellers,
    required this.onContinue,
  });

  final OffersResult result;
  final Travellers travellers;
  final String? selected;
  final ValueChanged<String> onSelect;
  final VoidCallback onTravellers;
  final ValueChanged<TicketOffer> onContinue;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final clock = clockFor(result.timeZone);
    // The first train is chosen to begin with, as in the design.
    final chosen =
        result.offers.where((o) => o.journeyId == selected).firstOrNull ??
        result.offers.firstOrNull;

    return Column(
      children: [
        Expanded(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
            children: [
              Semantics(
                button: true,
                child: InkWell(
                  onTap: onTravellers,
                  child: Padding(
                    padding: const EdgeInsets.symmetric(vertical: 4),
                    child: Text(
                      l10n.chooseSummary(travellersText(l10n, travellers)),
                      style: SmartRailType.body14.copyWith(
                        color: SmartRailColors.textSecondary,
                      ),
                    ),
                  ),
                ),
              ),
              const SizedBox(height: 8),
              if (result.offers.isEmpty)
                EmptyState(
                  icon: Icons.train_outlined,
                  title: l10n.chooseNoTrains,
                  message: l10n.chooseNoTrainsBody,
                ),
              for (var i = 0; i < result.offers.length; i++)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: AppearIn(
                    index: i,
                    child: _OfferCard(
                      offer: result.offers[i],
                      clock: clock,
                      selected: result.offers[i].journeyId == chosen?.journeyId,
                      onTap: () => onSelect(result.offers[i].journeyId),
                    ),
                  ),
                ),
              if (result.offers.isNotEmpty) ...[
                InfoNote(l10n.chooseNote),
                const SizedBox(height: 8),
                InfoNote(l10n.chooseNoPayment),
              ],
            ],
          ),
        ),
        StickyActionBar(
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    travellersText(l10n, travellers),
                    style: SmartRailType.body14.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                ),
                if (chosen != null)
                  Text(
                    chosen.fare.label,
                    style: SmartRailType.title.copyWith(
                      color: SmartRailColors.textPrimary,
                    ),
                  ),
              ],
            ),
            const SizedBox(height: 8),
            SmartRailButton(
              label: l10n.continueLabel,
              onPressed: chosen == null ? null : () => onContinue(chosen),
            ),
          ],
        ),
      ],
    );
  }
}

/// A train to book: times in 24 Bold with its state on the right, the duration, "Seat required" and the fare. The chosen one has a
/// 2 wide brand outline.
class _OfferCard extends StatelessWidget {
  const _OfferCard({
    required this.offer,
    required this.clock,
    required this.selected,
    required this.onTap,
  });

  final TicketOffer offer;
  final NetworkClock clock;
  final bool selected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    final (kind, label) = (offer.delayMinutes ?? 0) >= 2
        ? (StatusKind.delayed, l10n.chipDelayed(offer.delayMinutes!))
        : offer.confidence == 'Live'
        ? (StatusKind.onTime, l10n.chipOnTime)
        : offer.confidence == 'Unknown'
        ? (StatusKind.noLiveData, l10n.chipNoLiveData)
        : (StatusKind.onTime, null);

    final times =
        '${clock.formatTime(offer.expectedDeparture)} → ${clock.formatTime(offer.expectedArrival)}';

    return Semantics(
      button: true,
      selected: selected,
      label:
          '$times, ${l10n.chooseDirect(offer.durationMinutes)}, ${offer.fare.label}${label == null ? '' : ', $label'}',
      child: ExcludeSemantics(
        child: AnimatedContainer(
          duration: SmartRailMotion.of(context, SmartRailMotion.fast),
          decoration: BoxDecoration(
            color: SmartRailColors.surface,
            borderRadius: BorderRadius.circular(SmartRailRadius.lg),
            border: Border.all(
              color: selected
                  ? SmartRailColors.primary
                  : SmartRailColors.border,
              width: selected ? 2 : 1,
            ),
          ),
          child: InkWell(
            borderRadius: BorderRadius.circular(SmartRailRadius.lg),
            onTap: onTap,
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          times,
                          style: SmartRailType.title24.copyWith(
                            fontFeatures: SmartRailType.timeFeatures,
                            color: SmartRailColors.textPrimary,
                          ),
                        ),
                      ),
                      if (label != null) StatusChip(kind: kind, label: label),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Text(
                    l10n.chooseDirect(offer.durationMinutes),
                    style: SmartRailType.body14.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                  const SizedBox(height: 6),
                  Row(
                    children: [
                      Text(
                        offer.seatRequired
                            ? l10n.chooseSeatRequired
                            : l10n.chooseNoSeat,
                        style: SmartRailType.caption.copyWith(
                          color: SmartRailColors.textSecondary,
                        ),
                      ),
                      const Spacer(),
                      Text(
                        offer.fare.label,
                        style: SmartRailType.heading16.copyWith(
                          color: SmartRailColors.textPrimary,
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// "Who is travelling?" (design 30): Adult, Child and Senior with a minus, the count and a plus, then "I need help at the station".
class _TravellersSheet extends StatefulWidget {
  const _TravellersSheet({required this.initial});

  final Travellers initial;

  @override
  State<_TravellersSheet> createState() => _TravellersSheetState();
}

class _TravellersSheetState extends State<_TravellersSheet> {
  late Travellers _t = widget.initial;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    Widget divider() =>
        const Divider(height: 1, thickness: 1, color: SmartRailColors.border);

    return SheetBody(
      children: [
        Row(
          children: [
            Expanded(
              child: Text(
                l10n.whoTitle,
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
        _Counter(
          label: l10n.whoAdult,
          hint: l10n.whoAdultHint,
          value: _t.adults,
          canDecrease: _t.adults > 0 && _t.total > 1,
          canIncrease: _t.total < 9,
          onChanged: (v) => setState(() => _t = _t.copyWith(adults: v)),
        ),
        divider(),
        _Counter(
          label: l10n.whoChild,
          hint: l10n.whoChildHint,
          value: _t.children,
          canDecrease: _t.children > 0 && _t.total > 1,
          canIncrease: _t.total < 9,
          onChanged: (v) => setState(() => _t = _t.copyWith(children: v)),
        ),
        divider(),
        _Counter(
          label: l10n.whoSenior,
          hint: l10n.whoSeniorHint,
          value: _t.seniors,
          canDecrease: _t.seniors > 0 && _t.total > 1,
          canIncrease: _t.total < 9,
          onChanged: (v) => setState(() => _t = _t.copyWith(seniors: v)),
        ),
        divider(),
        Row(
          children: [
            const SvgIcon(
              AppIcons.accessHelp,
              size: 22,
              color: SmartRailColors.textPrimary,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    l10n.whoHelp,
                    style: SmartRailType.label14.copyWith(
                      color: SmartRailColors.textPrimary,
                    ),
                  ),
                  Text(
                    l10n.whoHelpHint,
                    style: SmartRailType.caption.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                ],
              ),
            ),
            Switch(
              value: _t.needsHelp,
              onChanged: (v) => setState(() => _t = _t.copyWith(needsHelp: v)),
            ),
          ],
        ),
        SmartRailButton(
          label: l10n.whoConfirm,
          onPressed: () => Navigator.of(context).pop(_t),
        ),
      ],
    );
  }
}

class _Counter extends StatelessWidget {
  const _Counter({
    required this.label,
    required this.hint,
    required this.value,
    required this.canDecrease,
    required this.canIncrease,
    required this.onChanged,
  });

  final String label;
  final String hint;
  final int value;
  final bool canDecrease;
  final bool canIncrease;
  final ValueChanged<int> onChanged;

  Widget _button(String icon, String tooltip, bool enabled, VoidCallback tap) =>
      Semantics(
        button: true,
        enabled: enabled,
        label: tooltip,
        child: ExcludeSemantics(
          child: InkWell(
            customBorder: const CircleBorder(),
            onTap: enabled ? tap : null,
            child: Container(
              width: 40,
              height: 40,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                shape: BoxShape.circle,
                border: Border.all(
                  color: enabled
                      ? SmartRailColors.borderStrong
                      : SmartRailColors.border,
                  width: 1.5,
                ),
              ),
              child: SvgIcon(
                icon,
                size: 18,
                color: enabled
                    ? SmartRailColors.textPrimary
                    : SmartRailColors.borderStrong,
              ),
            ),
          ),
        ),
      );

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  label,
                  style: SmartRailType.heading16.copyWith(
                    color: SmartRailColors.textPrimary,
                  ),
                ),
                Text(
                  hint,
                  style: SmartRailType.caption.copyWith(
                    color: SmartRailColors.textSecondary,
                  ),
                ),
              ],
            ),
          ),
          _button(
            AppIcons.minus,
            '− $label',
            canDecrease,
            () => onChanged(value - 1),
          ),
          SizedBox(
            width: 28,
            child: Text(
              '$value',
              textAlign: TextAlign.center,
              style: SmartRailType.title.copyWith(
                color: SmartRailColors.textPrimary,
              ),
            ),
          ),
          _button(
            AppIcons.plusSmall,
            '+ $label',
            canIncrease,
            () => onChanged(value + 1),
          ),
        ],
      ),
    );
  }
}
