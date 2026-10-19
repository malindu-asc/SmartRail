import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../core/time/network_clock_provider.dart';
import '../../../../core/widgets/failure_view.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../auth/presentation/providers/auth_providers.dart';
import '../../../me/presentation/widgets/call_centre_sheet.dart';
import '../../domain/tickets_domain.dart';
import '../providers/tickets_providers.dart';
import '../widgets/add_ticket_sheet.dart';
import '../widgets/ticket_text.dart';

/// The Tickets tab (designs 24, 25, 38): my tickets, active or past; an empty state that leads to buying one; and a way to add a
/// ticket bought elsewhere. Tickets belong to a profile, so signed-out people are asked to log in here and nowhere else.
class TicketsScreen extends ConsumerStatefulWidget {
  const TicketsScreen({super.key});

  @override
  ConsumerState<TicketsScreen> createState() => _TicketsScreenState();
}

class _TicketsScreenState extends ConsumerState<TicketsScreen> {
  bool _past = false;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final signedIn = ref.watch(authProvider.select((s) => s.isSignedIn));

    return SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(
          SmartRailSpacing.xl,
          SmartRailSpacing.xl,
          SmartRailSpacing.xl,
          0,
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Expanded(
                  child: Text(
                    l10n.ticketsTitle,
                    style: Theme.of(context).textTheme.headlineLarge,
                  ),
                ),
                if (signedIn)
                  TextButton.icon(
                    onPressed: () => showAddTicketSheet(context),
                    icon: const Icon(Icons.add),
                    label: Text(l10n.ticketsAdd),
                  ),
              ],
            ),
            const SizedBox(height: SmartRailSpacing.lg),
            Expanded(
              child: signedIn
                  ? _Signed(
                      past: _past,
                      onSegment: (past) => setState(() => _past = past),
                    )
                  : _SignedOut(),
            ),
          ],
        ),
      ),
    );
  }
}

class _SignedOut extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Column(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        EmptyState(
          icon: Icons.confirmation_number_outlined,
          title: l10n.ticketsSignInTitle,
          message: l10n.ticketsSignInBody,
        ),
        const SizedBox(height: SmartRailSpacing.lg),
        SmartRailButton(
          label: l10n.loginTitle,
          onPressed: () => context.push(AppRoutes.login),
        ),
        TextButton(
          onPressed: () => context.push(AppRoutes.createProfile),
          child: Text(l10n.loginCreate),
        ),
      ],
    );
  }
}

class _Signed extends ConsumerWidget {
  const _Signed({required this.past, required this.onSegment});

  final bool past;
  final ValueChanged<bool> onSegment;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final tickets = ref.watch(ticketListProvider(past));

    return Column(
      children: [
        SizedBox(
          width: double.infinity,
          child: SegmentedButton<bool>(
            showSelectedIcon: false,
            segments: [
              ButtonSegment(value: false, label: Text(l10n.ticketsActive)),
              ButtonSegment(value: true, label: Text(l10n.ticketsPast)),
            ],
            selected: {past},
            onSelectionChanged: (s) => onSegment(s.first),
          ),
        ),
        const SizedBox(height: SmartRailSpacing.lg),
        Expanded(
          child: tickets.when(
            loading: () => const Align(
              alignment: Alignment.topCenter,
              child: Skeleton(width: double.infinity, height: 72),
            ),
            error: (error, _) => FailureView(
              error: error,
              onRetry: () => ref.invalidate(ticketListProvider(past)),
            ),
            data: (list) => RefreshIndicator(
              onRefresh: () async => ref.invalidate(ticketListProvider(past)),
              child: list.isEmpty
                  ? ListView(
                      children: [
                        const SizedBox(height: SmartRailSpacing.xxl),
                        EmptyState(
                          icon: Icons.confirmation_number_outlined,
                          title: l10n.ticketsEmptyTitle,
                          message: l10n.ticketsEmptyBody,
                        ),
                        const SizedBox(height: SmartRailSpacing.lg),
                        if (!past)
                          SmartRailButton(
                            label: l10n.ticketsBuy,
                            onPressed: () => context.go(AppRoutes.plan),
                          ),
                      ],
                    )
                  : ListView(
                      children: [
                        for (var i = 0; i < list.length; i++)
                          Padding(
                            padding: const EdgeInsets.only(
                              bottom: SmartRailSpacing.md,
                            ),
                            child: AppearIn(
                              index: i,
                              child: TicketCard(
                                ticket: list[i],
                                onTap: () => _open(context, list[i]),
                              ),
                            ),
                          ),
                      ],
                    ),
            ),
          ),
        ),
      ],
    );
  }

  void _open(BuildContext context, TicketSummary ticket) {
    if (ticket.state == TicketState.affected) {
      _affectedSheet(context, ticket);
    } else {
      context.push(AppRoutes.ticket(ticket.id));
    }
  }

  void _affectedSheet(BuildContext context, TicketSummary ticket) {
    final l10n = AppLocalizations.of(context);

    showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (sheet) => SafeArea(
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
              const CircleAvatar(
                radius: 28,
                backgroundColor: SmartRailColors.cancelledBg,
                child: Icon(
                  Icons.cancel_outlined,
                  color: SmartRailColors.cancelledText,
                ),
              ),
              const SizedBox(height: SmartRailSpacing.md),
              Text(
                l10n.ticketAffectedTitle,
                style: Theme.of(sheet).textTheme.titleLarge,
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: SmartRailSpacing.sm),
              Text(l10n.ticketAffectedBody, textAlign: TextAlign.center),
              const SizedBox(height: SmartRailSpacing.md),
              Row(
                children: [
                  Expanded(child: Text(l10n.ticketStatus)),
                  Text(
                    l10n.ticketAffected,
                    style: const TextStyle(fontWeight: FontWeight.w700),
                  ),
                ],
              ),
              const SizedBox(height: SmartRailSpacing.lg),
              SmartRailButton(
                label: l10n.ticketFindAnother,
                onPressed: () {
                  Navigator.of(sheet).pop();
                  context.go(
                    AppRoutes.planResults(ticket.fromCode, ticket.toCode),
                  );
                },
              ),
              TextButton(
                onPressed: () {
                  Navigator.of(sheet).pop();
                  showCallCentreSheet(context);
                },
                child: Text(l10n.meCallCentre),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// A ticket in the list: where, when, the seat and its state in words and an icon.
class TicketCard extends ConsumerWidget {
  const TicketCard({super.key, required this.ticket, required this.onTap});

  final TicketSummary ticket;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final clock = clockFor(ticket.timeZone);
    final now = ref.watch(nowProvider)();

    final (kind, label) = switch (ticket.state) {
      TicketState.valid => (StatusKind.onTime, l10n.ticketValid),
      TicketState.affected => (StatusKind.cancelled, l10n.ticketAffected),
      TicketState.past => (StatusKind.noLiveData, l10n.ticketPast),
    };

    return Card(
      clipBehavior: Clip.antiAlias,
      child: InkWell(
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(SmartRailSpacing.lg),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(
                      l10n.ticketsRoute(ticket.fromName, ticket.toName),
                      style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ),
                  StatusChip(kind: kind, label: label),
                ],
              ),
              const SizedBox(height: SmartRailSpacing.xs),
              Text(
                ticketWhenAndSeat(l10n, ticket, clock, now),
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: SmartRailColors.textSecondary,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
