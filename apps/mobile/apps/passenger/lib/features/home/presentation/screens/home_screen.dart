import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../core/time/network_clock_provider.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../follow/presentation/providers/follow_providers.dart';
import '../../../stations/domain/entities/station.dart';
import '../../../stations/presentation/providers/station_providers.dart';
import '../../../disruptions/disruption_widgets.dart';
import '../widgets/followed_cards.dart';

/// Home (design 34 with the trip and followed stations of design 06): a green header with the greeting and the From / To card, then
/// the stations and journeys the passenger follows. There is no bell: alerts have their own tab (walkthrough D1).
class HomeScreen extends ConsumerStatefulWidget {
  const HomeScreen({super.key});

  @override
  ConsumerState<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends ConsumerState<HomeScreen> {
  Station? _from;
  Station? _to;

  Station? _defaultFrom() {
    final recent = ref.read(recentStationsProvider);
    if (recent.isNotEmpty) return recent.first;
    final followed = ref.read(followProvider).stations;
    return followed.isEmpty
        ? null
        : Station(code: followed.first.code, name: followed.first.name);
  }

  Future<void> _pick({required bool from}) async {
    final picked = await context.push<Station>(AppRoutes.searchPick);
    if (picked == null || !mounted) return;
    setState(() => from ? _from = picked : _to = picked);
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final follow = ref.watch(followProvider);
    final from = _from ?? _defaultFrom();
    // The greeting follows the railway's clock, like every other time in the app (REQ-NFR-005).
    final hour = ref
        .watch(networkClockProvider)
        .local(ref.watch(nowProvider)())
        .hour;
    final greeting = hour < 12
        ? l10n.greetingMorning
        : (hour < 17 ? l10n.greetingAfternoon : l10n.greetingEvening);
    final topInset = MediaQuery.paddingOf(context).top;

    return SingleChildScrollView(
      padding: EdgeInsets.zero,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Stack(
            children: [
              Container(
                width: double.infinity,
                padding: EdgeInsets.fromLTRB(
                  SmartRailSpacing.xl,
                  topInset + SmartRailSpacing.lg,
                  SmartRailSpacing.xl,
                  84,
                ),
                decoration: const BoxDecoration(
                  color: SmartRailColors.primary,
                  borderRadius: BorderRadius.vertical(
                    bottom: Radius.circular(SmartRailRadius.sheet),
                  ),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      greeting,
                      style: theme.textTheme.bodyMedium?.copyWith(
                        color: SmartRailColors.onPrimary.withValues(
                          alpha: 0.85,
                        ),
                      ),
                    ),
                    const SizedBox(height: SmartRailSpacing.xs),
                    Text(
                      l10n.homeWhereTo,
                      style: theme.textTheme.headlineLarge?.copyWith(
                        fontSize: 28,
                        color: SmartRailColors.onPrimary,
                      ),
                    ),
                  ],
                ),
              ),
              Padding(
                padding: EdgeInsets.fromLTRB(
                  SmartRailSpacing.lg,
                  topInset + 112,
                  SmartRailSpacing.lg,
                  0,
                ),
                child: AppearIn(
                  index: 0,
                  child: Card(
                    child: Padding(
                      padding: const EdgeInsets.all(SmartRailSpacing.lg),
                      child: Column(
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: Column(
                                  children: [
                                    _StationField(
                                      label: l10n.fromLabel,
                                      station: from,
                                      placeholder: l10n.chooseStation,
                                      onTap: () => _pick(from: true),
                                    ),
                                    const Divider(),
                                    _StationField(
                                      label: l10n.toLabel,
                                      station: _to,
                                      placeholder: l10n.chooseStation,
                                      onTap: () => _pick(from: false),
                                    ),
                                  ],
                                ),
                              ),
                              const SizedBox(width: SmartRailSpacing.sm),
                              IconButton.filledTonal(
                                tooltip: '${l10n.fromLabel} / ${l10n.toLabel}',
                                style: IconButton.styleFrom(
                                  backgroundColor:
                                      SmartRailColors.primarySubtle,
                                  foregroundColor: SmartRailColors.primary,
                                ),
                                icon: const Icon(Icons.swap_vert),
                                onPressed: () => setState(() {
                                  final oldFrom = from;
                                  _from = _to;
                                  _to = oldFrom;
                                }),
                              ),
                            ],
                          ),
                          const SizedBox(height: SmartRailSpacing.lg),
                          SmartRailButton(
                            label: l10n.findTrains,
                            onPressed: from != null && _to != null
                                ? () => context.push(
                                    AppRoutes.planResults(from.code, _to!.code),
                                  )
                                : null,
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            ],
          ),
          const DisruptionBanners(),
          const SizedBox(height: SmartRailSpacing.lg),
          if (follow.journeys.isNotEmpty) ...[
            _SectionTitle(l10n.homeFollowedJourneys),
            for (final journey in follow.journeys)
              Padding(
                padding: const EdgeInsets.fromLTRB(
                  SmartRailSpacing.lg,
                  0,
                  SmartRailSpacing.lg,
                  SmartRailSpacing.md,
                ),
                child: FollowedJourneyCard(journey: journey),
              ),
          ],
          _SectionTitle(l10n.homeFollowedStations),
          if (follow.stations.isEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: SmartRailSpacing.lg,
              ),
              child: Card(
                child: Padding(
                  padding: const EdgeInsets.all(SmartRailSpacing.lg),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        l10n.homeFollowEmptyTitle,
                        style: theme.textTheme.bodyLarge?.copyWith(
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: SmartRailSpacing.xs),
                      Text(
                        l10n.homeFollowEmptyBody,
                        style: theme.textTheme.bodyMedium?.copyWith(
                          color: SmartRailColors.textSecondary,
                        ),
                      ),
                      const SizedBox(height: SmartRailSpacing.lg),
                      SmartRailButton(
                        label: l10n.homeFindStation,
                        kind: ButtonKind.tint,
                        icon: Icons.search,
                        onPressed: () => context.push(AppRoutes.search),
                      ),
                    ],
                  ),
                ),
              ),
            )
          else
            for (var i = 0; i < follow.stations.length; i++)
              Padding(
                padding: const EdgeInsets.fromLTRB(
                  SmartRailSpacing.lg,
                  0,
                  SmartRailSpacing.lg,
                  SmartRailSpacing.md,
                ),
                child: AppearIn(
                  index: i + 1,
                  child: FollowedStationCard(station: follow.stations[i]),
                ),
              ),
          if (follow.stations.isNotEmpty)
            Padding(
              padding: const EdgeInsets.symmetric(
                horizontal: SmartRailSpacing.lg,
              ),
              child: TextButton.icon(
                onPressed: () => context.push(AppRoutes.search),
                icon: const Icon(Icons.search),
                label: Text(l10n.homeFindStation),
              ),
            ),
          const SizedBox(height: SmartRailSpacing.xl),
        ],
      ),
    );
  }
}

class _SectionTitle extends StatelessWidget {
  const _SectionTitle(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(
        SmartRailSpacing.lg,
        SmartRailSpacing.sm,
        SmartRailSpacing.lg,
        SmartRailSpacing.md,
      ),
      child: Text(text, style: Theme.of(context).textTheme.titleMedium),
    );
  }
}

class _StationField extends StatelessWidget {
  const _StationField({
    required this.label,
    required this.station,
    required this.placeholder,
    required this.onTap,
  });

  final String label;
  final Station? station;
  final String placeholder;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final text = station?.name ?? placeholder;

    return Semantics(
      button: true,
      label: '$label, $text',
      child: ExcludeSemantics(
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(SmartRailRadius.sm),
          child: ConstrainedBox(
            constraints: const BoxConstraints(minHeight: 52),
            child: Row(
              children: [
                const Icon(
                  Icons.location_on_outlined,
                  color: SmartRailColors.primary,
                ),
                const SizedBox(width: SmartRailSpacing.md),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Text(
                        label,
                        style: theme.textTheme.labelSmall?.copyWith(
                          color: SmartRailColors.textSecondary,
                        ),
                      ),
                      ChangeSwitcher(
                        child: Text(
                          text,
                          key: ValueKey(text),
                          style: theme.textTheme.bodyLarge?.copyWith(
                            fontWeight: FontWeight.w600,
                            color: station == null
                                ? SmartRailColors.textTertiary
                                : SmartRailColors.textPrimary,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
