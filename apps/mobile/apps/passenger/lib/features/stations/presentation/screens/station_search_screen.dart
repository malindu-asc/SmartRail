import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../core/widgets/failure_view.dart';
import '../../../../l10n/app_localizations.dart';
import '../../domain/entities/station.dart';
import '../providers/station_providers.dart';

/// Search for a station by name or code (design 18). With [pick] it returns the chosen station to whoever asked (the From and To
/// fields on Home); otherwise it opens that station's live departures.
class StationSearchScreen extends ConsumerStatefulWidget {
  const StationSearchScreen({super.key, this.pick = false});

  final bool pick;

  @override
  ConsumerState<StationSearchScreen> createState() =>
      _StationSearchScreenState();
}

class _StationSearchScreenState extends ConsumerState<StationSearchScreen> {
  final _controller = TextEditingController();

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _choose(Station station) async {
    await ref.read(recentStationsProvider.notifier).add(station);
    if (!mounted) return;

    if (widget.pick) {
      context.pop(station);
    } else {
      context.push(AppRoutes.station(station.code));
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final results = ref.watch(stationSearchProvider);
    final recent = ref.watch(recentStationsProvider);
    final query = _controller.text.trim();

    return Scaffold(
      appBar: AppBar(title: Text(l10n.stationsTitle)),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(
              SmartRailSpacing.lg,
              SmartRailSpacing.sm,
              SmartRailSpacing.lg,
              SmartRailSpacing.md,
            ),
            child: TextField(
              controller: _controller,
              autofocus: true,
              textInputAction: TextInputAction.search,
              onChanged: (value) {
                ref.read(stationSearchProvider.notifier).queryChanged(value);
                setState(() {});
              },
              style: SmartRailType.body,
              decoration: InputDecoration(
                labelText: l10n.stationsSearchLabel,
                hintText: l10n.stationsSearchHint,
                floatingLabelBehavior: FloatingLabelBehavior.always,
                prefixIcon: const Icon(
                  Icons.search,
                  color: SmartRailColors.textSecondary,
                ),
                suffixIcon: query.isEmpty
                    ? null
                    : IconButton(
                        tooltip: MaterialLocalizations.of(
                          context,
                        ).clearButtonTooltip,
                        icon: const Icon(Icons.close),
                        onPressed: () {
                          _controller.clear();
                          ref
                              .read(stationSearchProvider.notifier)
                              .queryChanged('');
                          setState(() {});
                        },
                      ),
                border: _pill(SmartRailColors.borderStrong),
                enabledBorder: _pill(SmartRailColors.borderStrong),
                focusedBorder: _pill(SmartRailColors.primary),
              ),
            ),
          ),
          Expanded(child: _content(context, l10n, query, results, recent)),
        ],
      ),
    );
  }

  OutlineInputBorder _pill(Color color) => OutlineInputBorder(
    borderRadius: BorderRadius.circular(SmartRailRadius.pill),
    borderSide: BorderSide(color: color, width: 1.5),
  );

  Widget _content(
    BuildContext context,
    AppLocalizations l10n,
    String query,
    AsyncValue<List<Station>> results,
    List<Station> recent,
  ) {
    if (query.isEmpty) {
      if (recent.isEmpty) {
        return EmptyState(
          icon: Icons.search,
          title: l10n.stationsTitle,
          message: l10n.stationsIntro,
        );
      }
      return _StationList(
        title: l10n.stationsRecent,
        stations: recent,
        onTap: _choose,
      );
    }

    return results.when(
      loading: () => const _LoadingList(),
      error: (error, _) => FailureView(
        error: error,
        onRetry: () =>
            ref.read(stationSearchProvider.notifier).queryChanged(query),
      ),
      data: (stations) => stations.isEmpty
          ? EmptyState(
              icon: Icons.search_off,
              title: l10n.stationsEmptyTitle,
              message: l10n.stationsEmptyBody,
            )
          : _StationList(
              title: l10n.stationsResults,
              stations: stations,
              onTap: _choose,
            ),
    );
  }
}

class _StationList extends StatelessWidget {
  const _StationList({
    required this.title,
    required this.stations,
    required this.onTap,
  });

  final String title;
  final List<Station> stations;
  final ValueChanged<Station> onTap;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return ListView(
      padding: const EdgeInsets.fromLTRB(
        SmartRailSpacing.lg,
        0,
        SmartRailSpacing.lg,
        SmartRailSpacing.xl,
      ),
      children: [
        Padding(
          padding: const EdgeInsets.only(bottom: SmartRailSpacing.sm),
          child: Text(
            title,
            style: theme.textTheme.labelLarge?.copyWith(
              color: SmartRailColors.textSecondary,
            ),
          ),
        ),
        Card(
          clipBehavior: Clip.antiAlias,
          child: Column(
            children: [
              for (var i = 0; i < stations.length; i++) ...[
                if (i > 0) const Divider(),
                AppearIn(
                  index: i,
                  child: Semantics(
                    button: true,
                    label: '${stations[i].name}, ${stations[i].code}',
                    child: ExcludeSemantics(
                      child: InkWell(
                        onTap: () => onTap(stations[i]),
                        child: Padding(
                          padding: const EdgeInsets.symmetric(
                            horizontal: SmartRailSpacing.lg,
                            vertical: SmartRailSpacing.md,
                          ),
                          child: Row(
                            children: [
                              const Icon(
                                Icons.train_outlined,
                                color: SmartRailColors.textSecondary,
                              ),
                              const SizedBox(width: SmartRailSpacing.md),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      stations[i].name,
                                      style: theme.textTheme.bodyLarge
                                          ?.copyWith(
                                            fontWeight: FontWeight.w600,
                                          ),
                                    ),
                                    Text(
                                      stations[i].code,
                                      style: theme.textTheme.labelSmall
                                          ?.copyWith(
                                            color:
                                                SmartRailColors.textSecondary,
                                          ),
                                    ),
                                  ],
                                ),
                              ),
                              const Icon(
                                Icons.chevron_right,
                                color: SmartRailColors.textTertiary,
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

class _LoadingList extends StatelessWidget {
  const _LoadingList();

  @override
  Widget build(BuildContext context) {
    return const Padding(
      padding: EdgeInsets.all(SmartRailSpacing.lg),
      child: Column(
        children: [
          Skeleton(height: 52),
          SizedBox(height: SmartRailSpacing.md),
          Skeleton(height: 52),
          SizedBox(height: SmartRailSpacing.md),
          Skeleton(height: 52),
        ],
      ),
    );
  }
}
