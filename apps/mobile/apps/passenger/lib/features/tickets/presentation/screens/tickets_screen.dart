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

/// The Tickets tab (designs 24 and 26): 32 Bold title, an Active / Past switch, one card per ticket, and when there are none an
/// empty state with Buy a ticket and "More actions". Tickets belong to a profile, so signed-out people are asked to log in here.
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
      bottom: false,
      child: signedIn
          ? _Signed(
              past: _past,
              onSegment: (past) => setState(() => _past = past),
            )
          : ListView(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
              children: [
                Text(
                  l10n.ticketsTitle,
                  style: SmartRailType.headline.copyWith(
                    color: SmartRailColors.textPrimary,
                  ),
                ),
                const SizedBox(height: 16),
                _Empty(
                  title: l10n.ticketsSignInTitle,
                  body: l10n.ticketsSignInBody,
                ),
                const SizedBox(height: 16),
                SmartRailButton(
                  label: l10n.loginTitle,
                  onPressed: () => context.push(AppRoutes.login),
                ),
                const SizedBox(height: 8),
                Center(
                  child: TextButton(
                    onPressed: () => context.push(AppRoutes.createProfile),
                    child: Text(
                      l10n.loginCreate,
                      style: SmartRailType.label14.copyWith(
                        color: SmartRailColors.primaryPressed,
                      ),
                    ),
                  ),
                ),
              ],
            ),
    );
  }
}

/// The 88 round brand-subtle badge with a ticket, a 20 Bold title and a 16 Regular line (design 24).
class _Empty extends StatelessWidget {
  const _Empty({required this.title, required this.body});

  final String title;
  final String body;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(24),
      child: Column(
        children: [
          Container(
            width: 88,
            height: 88,
            alignment: Alignment.center,
            decoration: const BoxDecoration(
              shape: BoxShape.circle,
              color: SmartRailColors.primarySubtle,
            ),
            child: const SvgIcon(
              AppIcons.ticketBig,
              size: 40,
              color: SmartRailColors.primary,
            ),
          ),
          const SizedBox(height: 12),
          Text(
            title,
            textAlign: TextAlign.center,
            style: SmartRailType.title20.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          const SizedBox(height: 12),
          Text(
            body,
            textAlign: TextAlign.center,
            style: SmartRailType.body.copyWith(
              color: SmartRailColors.textSecondary,
            ),
          ),
        ],
      ),
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
    final active = ref.watch(ticketListProvider(false)).value;

    final title = Text(
      l10n.ticketsTitle,
      style: SmartRailType.headline.copyWith(
        color: SmartRailColors.textPrimary,
      ),
    );

    // Design 24: nobody has a ticket at all, so there is no Active / Past switch, just the empty state and More actions.
    if (!past && active != null && active.isEmpty) {
      return Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 0),
            child: Align(alignment: Alignment.centerLeft, child: title),
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async => ref.invalidate(ticketListProvider),
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 16),
                children: [
                  _Empty(
                    title: l10n.ticketsEmptyTitle,
                    body: l10n.ticketsEmptyBody,
                  ),
                  const SizedBox(height: 16),
                  SmartRailButton(
                    label: l10n.ticketsBuy,
                    onPressed: () => context.go(AppRoutes.plan),
                  ),
                  const SizedBox(height: 16),
                  Text(
                    l10n.ticketsMoreActions,
                    style: SmartRailType.heading16.copyWith(
                      color: SmartRailColors.textPrimary,
                    ),
                  ),
                  const SizedBox(height: 16),
                  RowsCard(
                    children: [
                      ActionRow(
                        icon: AppIcons.plus,
                        label: l10n.ticketsAddWithCode,
                        onTap: () => showAddTicketSheet(context),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      );
    }

    // The title and the Active / Past switch stay at the top; only the tickets below them scroll.
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 8, 16, 14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              title,
              const SizedBox(height: 14),
              SegmentedPill(
                left: l10n.ticketsActive,
                right: l10n.ticketsPast,
                rightSelected: past,
                onChanged: onSegment,
              ),
            ],
          ),
        ),
        Expanded(
          child: RefreshIndicator(
            onRefresh: () async => ref.invalidate(ticketListProvider),
            child: ListView(
              physics: const AlwaysScrollableScrollPhysics(),
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
              children: [
                ...tickets.when(
                  loading: () => [
                    const Skeleton(width: double.infinity, height: 76),
                  ],
                  error: (error, _) => [
                    FailureView(
                      error: error,
                      onRetry: () => ref.invalidate(ticketListProvider(past)),
                    ),
                  ],
                  data: (list) => list.isEmpty
                      ? [
                          _Empty(
                            title: l10n.ticketsEmptyTitle,
                            body: l10n.ticketsNonePast,
                          ),
                        ]
                      : [
                          for (var i = 0; i < list.length; i++) ...[
                            if (i > 0) const SizedBox(height: 14),
                            AppearIn(
                              index: i,
                              child: TicketCard(
                                ticket: list[i],
                                onTap: () => _open(context, list[i]),
                              ),
                            ),
                          ],
                          const SizedBox(height: 16),
                          Center(
                            child: TextButton.icon(
                              onPressed: () => showAddTicketSheet(context),
                              icon: const SvgIcon(
                                AppIcons.plus,
                                size: 18,
                                color: SmartRailColors.primaryPressed,
                              ),
                              label: Text(
                                l10n.ticketsAddWithCode,
                                style: SmartRailType.label14.copyWith(
                                  color: SmartRailColors.primaryPressed,
                                ),
                              ),
                            ),
                          ),
                        ],
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }

  void _open(BuildContext context, TicketSummary ticket) {
    if (ticket.state == TicketState.affected) {
      showCancelledSheet(context, ticket);
    } else {
      context.push(AppRoutes.ticket(ticket.id));
    }
  }
}

/// Design 28 without the money: the train was cancelled, the ticket cannot be used, find another train or call the centre.
Future<void> showCancelledSheet(BuildContext context, TicketSummary ticket) {
  final l10n = AppLocalizations.of(context);

  Widget line(String label, String value) => Row(
    mainAxisAlignment: MainAxisAlignment.spaceBetween,
    children: [
      Text(
        label,
        style: SmartRailType.body14.copyWith(
          color: SmartRailColors.textSecondary,
        ),
      ),
      Text(
        value,
        style: SmartRailType.label14.copyWith(
          color: SmartRailColors.textPrimary,
        ),
      ),
    ],
  );

  return showSmartRailSheet<void>(
    context,
    builder: (sheet) => SheetBody(
      children: [
        Center(
          child: Container(
            width: 56,
            height: 56,
            alignment: Alignment.center,
            decoration: const BoxDecoration(
              shape: BoxShape.circle,
              color: SmartRailColors.cancelledBg,
            ),
            child: const SvgIcon(
              AppIcons.xcBig,
              size: 28,
              color: SmartRailColors.cancelledText,
            ),
          ),
        ),
        Text(
          l10n.ticketAffectedTitle,
          textAlign: TextAlign.center,
          style: SmartRailType.title24.copyWith(
            color: SmartRailColors.textPrimary,
          ),
        ),
        Text(
          l10n.ticketAffectedBody,
          style: SmartRailType.body.copyWith(
            color: SmartRailColors.textSecondary,
          ),
        ),
        line(l10n.ticketStatus, l10n.ticketAffected),
        line(l10n.ticketReference, ticket.reference),
        SmartRailButton(
          label: l10n.ticketFindAnother,
          onPressed: () {
            Navigator.of(sheet).pop();
            context.go(AppRoutes.planResults(ticket.fromCode, ticket.toCode));
          },
        ),
        Center(
          child: TextButton(
            onPressed: () {
              Navigator.of(sheet).pop();
              showCallCentreSheet(context);
            },
            child: Text(
              l10n.meCallCentre,
              style: SmartRailType.label14.copyWith(
                color: SmartRailColors.textPrimary,
              ),
            ),
          ),
        ),
      ],
    ),
  );
}

/// A ticket in the list (design 26): the route in 16 Semi Bold with its state on the right, then when and where in 14 Regular.
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
    final when = ticketWhenAndSeat(l10n, ticket, clock, now);
    final route = l10n.ticketsRoute(ticket.fromName, ticket.toName);

    return Semantics(
      button: true,
      label: '$route, $label, $when',
      child: ExcludeSemantics(
        child: Material(
          color: SmartRailColors.surface,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(SmartRailRadius.lg),
            side: const BorderSide(color: SmartRailColors.border),
          ),
          clipBehavior: Clip.antiAlias,
          child: InkWell(
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
                          route,
                          style: SmartRailType.heading16.copyWith(
                            color: SmartRailColors.textPrimary,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      StatusChip(kind: kind, label: label),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Text(
                    when,
                    style: SmartRailType.body14.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
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
