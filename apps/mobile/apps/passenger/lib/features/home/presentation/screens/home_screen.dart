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
    final picked = await context.push<Station>(
      AppRoutes.searchPickExcluding(from ? _to?.code : _from?.code),
    );
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
          // The header of design 34 (v2): brand colour, greeting 14 Regular at 85 percent, "Where to today?" 24 Bold. Under it a
          // 24 high strip where the white sheet starts with 24 radius corners.
          Container(
            width: double.infinity,
            color: SmartRailColors.primary,
            padding: EdgeInsets.fromLTRB(20, topInset + 12, 20, 32),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  greeting,
                  style: SmartRailType.body14.copyWith(
                    color: SmartRailColors.onPrimary.withValues(alpha: 0.85),
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  l10n.homeWhereTo,
                  style: SmartRailType.title24.copyWith(
                    color: SmartRailColors.onPrimary,
                  ),
                ),
              ],
            ),
          ),
          // The white sheet starts with 24 radius corners over the green (design 34). Full width, or the strip collapses to nothing.
          Container(
            width: double.infinity,
            height: 24,
            color: SmartRailColors.primary,
            child: const DecoratedBox(
              decoration: BoxDecoration(
                color: SmartRailColors.page,
                borderRadius: BorderRadius.vertical(
                  top: Radius.circular(SmartRailRadius.sheet),
                ),
              ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 0),
            child: AppearIn(
              index: 0,
              child: DecoratedBox(
                decoration: BoxDecoration(
                  color: SmartRailColors.page,
                  border: Border.all(color: SmartRailColors.border),
                  borderRadius: BorderRadius.circular(SmartRailRadius.lg),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(16),
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
                                const Divider(
                                  height: 1,
                                  thickness: 1,
                                  color: SmartRailColors.border,
                                ),
                                _StationField(
                                  label: l10n.toLabel,
                                  station: _to,
                                  placeholder: l10n.chooseStation,
                                  onTap: () => _pick(from: false),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(width: 12),
                          Semantics(
                            button: true,
                            label: '${l10n.fromLabel} / ${l10n.toLabel}',
                            child: ExcludeSemantics(
                              child: InkWell(
                                customBorder: const CircleBorder(),
                                onTap: () => setState(() {
                                  final oldFrom = from;
                                  _from = _to;
                                  _to = oldFrom;
                                }),
                                child: Container(
                                  width: 44,
                                  height: 44,
                                  alignment: Alignment.center,
                                  decoration: const BoxDecoration(
                                    shape: BoxShape.circle,
                                    color: SmartRailColors.primarySubtle,
                                  ),
                                  child: const SvgIcon(
                                    AppIcons.swapSmall,
                                    size: 20,
                                    color: SmartRailColors.primary,
                                  ),
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 14),
                      SmartRailButton(
                        label: l10n.findTrains,
                        onPressed: from != null && _to != null && from.code != _to!.code
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
          const SizedBox(height: 20),
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
    final text = station?.name ?? placeholder;

    return Semantics(
      button: true,
      label: '$label, $text',
      child: ExcludeSemantics(
        child: InkWell(
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.symmetric(vertical: 10),
            child: Row(
              children: [
                const SvgIcon(
                  AppIcons.pin,
                  size: 20,
                  color: SmartRailColors.primary,
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        label,
                        style: SmartRailType.caption.copyWith(
                          color: SmartRailColors.textSecondary,
                        ),
                      ),
                      ChangeSwitcher(
                        child: Text(
                          text,
                          key: ValueKey(text),
                          style: SmartRailType.heading16.copyWith(
                            color: station == null
                                ? SmartRailColors.textSecondary
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
