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

/// Choose ticket (design 27): the trains between the two stations with the fare for who is travelling. It needs an account to go on.
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
    final picked = await showModalBottomSheet<Travellers>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
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

class _Body extends ConsumerWidget {
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
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = clockFor(result.timeZone);
    final chosen = result.offers
        .where((o) => o.journeyId == selected)
        .firstOrNull;

    return Column(
      children: [
        Expanded(
          child: ListView(
            padding: const EdgeInsets.all(SmartRailSpacing.lg),
            children: [
              Semantics(
                button: true,
                child: InkWell(
                  onTap: onTravellers,
                  child: Padding(
                    padding: const EdgeInsets.symmetric(
                      vertical: SmartRailSpacing.sm,
                    ),
                    child: Row(
                      children: [
                        Expanded(
                          child: Text(
                            l10n.chooseSummary(
                              travellersText(l10n, travellers),
                            ),
                            style: Theme.of(context).textTheme.bodyMedium
                                ?.copyWith(
                                  color: SmartRailColors.textSecondary,
                                ),
                          ),
                        ),
                        const Icon(Icons.expand_more),
                      ],
                    ),
                  ),
                ),
              ),
              if (result.offers.isEmpty)
                EmptyState(
                  icon: Icons.train_outlined,
                  title: l10n.chooseNoTrains,
                  message: l10n.chooseNoTrainsBody,
                ),
              for (var i = 0; i < result.offers.length; i++)
                Padding(
                  padding: const EdgeInsets.only(bottom: SmartRailSpacing.md),
                  child: AppearIn(
                    index: i,
                    child: _OfferCard(
                      offer: result.offers[i],
                      clock: clock,
                      selected: result.offers[i].journeyId == selected,
                      onTap: () => onSelect(result.offers[i].journeyId),
                    ),
                  ),
                ),
              if (result.offers.isNotEmpty) ...[
                InfoBanner(kind: BannerKind.message, title: l10n.chooseNote),
                const SizedBox(height: SmartRailSpacing.sm),
                InfoBanner(
                  kind: BannerKind.message,
                  title: l10n.chooseNoPayment,
                ),
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
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Row(
                    children: [
                      Expanded(child: Text(travellersText(l10n, travellers))),
                      if (chosen != null)
                        Text(
                          chosen.fare.label,
                          style: Theme.of(context).textTheme.titleLarge,
                        ),
                    ],
                  ),
                  const SizedBox(height: SmartRailSpacing.md),
                  SmartRailButton(
                    label: l10n.continueLabel,
                    onPressed: chosen == null ? null : () => onContinue(chosen),
                  ),
                ],
              ),
            ),
          ),
        ),
      ],
    );
  }
}

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
    final theme = Theme.of(context);

    final (kind, label) = (offer.delayMinutes ?? 0) >= 2
        ? (StatusKind.delayed, l10n.chipDelayed(offer.delayMinutes!))
        : offer.confidence == 'Live'
        ? (StatusKind.onTime, l10n.chipOnTime)
        : offer.confidence == 'Unknown'
        ? (StatusKind.noLiveData, l10n.chipNoLiveData)
        : (StatusKind.onTime, null);

    return Semantics(
      button: true,
      selected: selected,
      label:
          '${clock.formatTime(offer.expectedDeparture)} to ${clock.formatTime(offer.expectedArrival)}, ${l10n.chooseDirect(offer.durationMinutes)}, ${offer.fare.label}${label == null ? '' : ', $label'}',
      child: ExcludeSemantics(
        child: AnimatedContainer(
          duration: SmartRailMotion.of(context, SmartRailMotion.fast),
          decoration: BoxDecoration(
            color: SmartRailColors.surface,
            borderRadius: BorderRadius.circular(SmartRailRadius.md),
            border: Border.all(
              color: selected
                  ? SmartRailColors.primary
                  : SmartRailColors.border,
              width: selected ? 2 : 1,
            ),
          ),
          child: InkWell(
            borderRadius: BorderRadius.circular(SmartRailRadius.md),
            onTap: onTap,
            child: Padding(
              padding: const EdgeInsets.all(SmartRailSpacing.lg),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Text(
                        '${clock.formatTime(offer.expectedDeparture)} → ${clock.formatTime(offer.expectedArrival)}',
                        style: theme.textTheme.titleLarge?.copyWith(
                          fontFeatures: SmartRailType.timeFeatures,
                        ),
                      ),
                      const Spacer(),
                      if (label != null) StatusChip(kind: kind, label: label),
                    ],
                  ),
                  const SizedBox(height: SmartRailSpacing.xs),
                  Text(
                    l10n.chooseDirect(offer.durationMinutes),
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                  const SizedBox(height: SmartRailSpacing.sm),
                  Row(
                    children: [
                      Text(
                        offer.seatRequired
                            ? l10n.chooseSeatRequired
                            : l10n.chooseNoSeat,
                        style: theme.textTheme.labelSmall,
                      ),
                      const Spacer(),
                      Text(
                        offer.fare.label,
                        style: theme.textTheme.titleMedium,
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

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(
          SmartRailSpacing.xl,
          0,
          SmartRailSpacing.xl,
          SmartRailSpacing.xl,
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    l10n.whoTitle,
                    style: Theme.of(context).textTheme.titleLarge,
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => Navigator.of(context).pop(),
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
            const Divider(),
            _Counter(
              label: l10n.whoChild,
              hint: l10n.whoChildHint,
              value: _t.children,
              canDecrease: _t.children > 0 && _t.total > 1,
              canIncrease: _t.total < 9,
              onChanged: (v) => setState(() => _t = _t.copyWith(children: v)),
            ),
            const Divider(),
            _Counter(
              label: l10n.whoSenior,
              hint: l10n.whoSeniorHint,
              value: _t.seniors,
              canDecrease: _t.seniors > 0 && _t.total > 1,
              canIncrease: _t.total < 9,
              onChanged: (v) => setState(() => _t = _t.copyWith(seniors: v)),
            ),
            const Divider(),
            SwitchListTile(
              contentPadding: EdgeInsets.zero,
              secondary: const Icon(Icons.accessible),
              title: Text(l10n.whoHelp),
              subtitle: Text(l10n.whoHelpHint),
              value: _t.needsHelp,
              onChanged: (v) => setState(() => _t = _t.copyWith(needsHelp: v)),
            ),
            const SizedBox(height: SmartRailSpacing.md),
            SmartRailButton(
              label: l10n.whoConfirm,
              onPressed: () => Navigator.of(context).pop(_t),
            ),
          ],
        ),
      ),
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

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Row(
      children: [
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: theme.textTheme.bodyLarge?.copyWith(
                  fontWeight: FontWeight.w600,
                ),
              ),
              Text(
                hint,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: SmartRailColors.textSecondary,
                ),
              ),
            ],
          ),
        ),
        IconButton.outlined(
          tooltip: '− $label',
          onPressed: canDecrease ? () => onChanged(value - 1) : null,
          icon: const Icon(Icons.remove),
        ),
        SizedBox(
          width: 40,
          child: Text(
            '$value',
            textAlign: TextAlign.center,
            style: theme.textTheme.titleMedium,
          ),
        ),
        IconButton.outlined(
          tooltip: '+ $label',
          onPressed: canIncrease ? () => onChanged(value + 1) : null,
          icon: const Icon(Icons.add),
        ),
      ],
    );
  }
}
