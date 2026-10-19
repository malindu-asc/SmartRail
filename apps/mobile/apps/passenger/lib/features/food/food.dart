import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:fpdart/fpdart.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';
import 'package:url_launcher/url_launcher.dart';

import '../../core/routing/app_routes.dart';
import '../../l10n/app_localizations.dart';
import '../journeys/domain/entities/journey_detail.dart';

class MenuItem {
  const MenuItem({required this.id, required this.name, required this.price});

  final String id;
  final String name;
  final num price;
}

class MenuCategory {
  const MenuCategory({
    required this.id,
    required this.name,
    required this.items,
  });

  final String id;
  final String name;
  final List<MenuItem> items;
}

/// The food at a station, from the catering provider. Read only: ordering and payment are the provider's, by phone.
class Menu {
  const Menu({
    required this.stationCode,
    required this.currency,
    required this.orderPhone,
    required this.categories,
  });

  final String stationCode;
  final String currency;
  final String orderPhone;
  final List<MenuCategory> categories;

  String price(num value) => '$currency ${value.round()}';
}

abstract interface class MenuRepository {
  /// Right: the menu, or null when the station has no food. Left: the menu is unavailable (the railway carries on).
  Future<Either<Failure, Menu?>> menu(String stationCode);
}

class RemoteMenuRepository implements MenuRepository {
  RemoteMenuRepository(this._client);

  final ApiClient _client;

  @override
  Future<Either<Failure, Menu?>> menu(String stationCode) async {
    final result = await _client.get('/api/network/stations/$stationCode/menu');

    return result.match(
      // A station with no kitchen is normal, not a failure.
      (failure) => failure is NotFoundFailure ? right(null) : left(failure),
      (response) {
        final body = response.body;
        if (body is! Map) {
          return left(const UnknownFailure('menu.bad_response'));
        }
        try {
          return right(
            Menu(
              stationCode: body['stationCode']! as String,
              currency: body['currency']! as String,
              orderPhone: body['orderPhone']! as String,
              categories: [
                for (final c in body['categories']! as List)
                  MenuCategory(
                    id: (c as Map)['id']! as String,
                    name: c['name']! as String,
                    items: [
                      for (final i in c['items']! as List)
                        MenuItem(
                          id: (i as Map)['id']! as String,
                          name: i['name']! as String,
                          price: i['price']! as num,
                        ),
                    ],
                  ),
              ],
            ),
          );
        } on Object {
          return left(const UnknownFailure('menu.bad_response'));
        }
      },
    );
  }
}

final menuRepositoryProvider = Provider<MenuRepository>(
  (ref) => RemoteMenuRepository(ref.watch(apiClientProvider)),
);

/// The menu at a station: null when it has none, an error when the provider is down.
final menuProvider = FutureProvider.autoDispose.family<Menu?, String>((
  ref,
  code,
) async {
  final result = await ref.read(menuRepositoryProvider).menu(code);
  return result.match((failure) => throw failure, (menu) => menu);
});

/// Food at a station (designs 35 and 36). Offline or unavailable it says so and carries on: it never blocks trains or tickets.
class FoodScreen extends ConsumerStatefulWidget {
  const FoodScreen({super.key, required this.stationCode, this.subtitle});

  final String stationCode;

  /// For example "Arrive 14:31 · Platform 1", when the passenger came from a journey.
  final String? subtitle;

  @override
  ConsumerState<FoodScreen> createState() => _FoodScreenState();
}

class _FoodScreenState extends ConsumerState<FoodScreen> {
  String _filter = 'all';

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final menu = ref.watch(menuProvider(widget.stationCode));

    return Scaffold(
      appBar: SmartRailAppBar(
        backgroundColor: SmartRailColors.primary,
        foregroundColor: SmartRailColors.onPrimary,
        title: Text(l10n.foodTitle(widget.stationCode)),
        bottom: widget.subtitle == null
            ? null
            : PreferredSize(
                preferredSize: const Size.fromHeight(32),
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(
                    SmartRailSpacing.xl,
                    0,
                    SmartRailSpacing.xl,
                    SmartRailSpacing.md,
                  ),
                  child: Align(
                    alignment: Alignment.centerLeft,
                    child: Text(
                      widget.subtitle!,
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                        color: SmartRailColors.onPrimary,
                      ),
                    ),
                  ),
                ),
              ),
      ),
      body: menu.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, _) => Column(
          children: [
            Expanded(
              child: EmptyState(
                icon: Icons.wifi_off,
                title: l10n.foodUnavailable,
                message: l10n.foodUnavailableBody,
              ),
            ),
            SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.all(SmartRailSpacing.lg),
                child: SmartRailButton(
                  label: l10n.retry,
                  kind: ButtonKind.secondary,
                  onPressed: () =>
                      ref.invalidate(menuProvider(widget.stationCode)),
                ),
              ),
            ),
          ],
        ),
        data: (data) => data == null
            ? EmptyState(icon: Icons.restaurant_outlined, title: l10n.foodNone)
            : _Menu(
                menu: data,
                filter: _filter,
                onFilter: (f) => setState(() => _filter = f),
              ),
      ),
    );
  }
}

class _Menu extends StatelessWidget {
  const _Menu({
    required this.menu,
    required this.filter,
    required this.onFilter,
  });

  final Menu menu;
  final String filter;
  final ValueChanged<String> onFilter;

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);
    final shown = menu.categories
        .where((c) => filter == 'all' || c.id == filter)
        .toList();

    return Column(
      children: [
        Expanded(
          child: ListView(
            padding: const EdgeInsets.all(SmartRailSpacing.lg),
            children: [
              Wrap(
                spacing: SmartRailSpacing.sm,
                children: [
                  ChoiceChip(
                    label: Text(l10n.foodAll),
                    selected: filter == 'all',
                    onSelected: (_) => onFilter('all'),
                  ),
                  for (final c in menu.categories)
                    ChoiceChip(
                      label: Text(c.name),
                      selected: filter == c.id,
                      onSelected: (_) => onFilter(c.id),
                    ),
                ],
              ),
              const SizedBox(height: SmartRailSpacing.md),
              for (var i = 0; i < shown.length; i++) ...[
                Text(shown[i].name, style: theme.textTheme.titleMedium),
                const SizedBox(height: SmartRailSpacing.sm),
                AppearIn(
                  index: i,
                  child: Card(
                    child: Column(
                      children: [
                        for (var j = 0; j < shown[i].items.length; j++) ...[
                          if (j > 0) const Divider(height: 1),
                          Padding(
                            padding: const EdgeInsets.all(SmartRailSpacing.lg),
                            child: Row(
                              children: [
                                Expanded(
                                  child: Text(
                                    shown[i].items[j].name,
                                    style: theme.textTheme.bodyLarge,
                                  ),
                                ),
                                Text(
                                  menu.price(shown[i].items[j].price),
                                  style: theme.textTheme.bodyMedium?.copyWith(
                                    fontWeight: FontWeight.w700,
                                  ),
                                ),
                              ],
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                ),
                const SizedBox(height: SmartRailSpacing.lg),
              ],
            ],
          ),
        ),
        SafeArea(
          top: false,
          child: Padding(
            padding: const EdgeInsets.all(SmartRailSpacing.lg),
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(
                  l10n.foodOrderNote,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: SmartRailColors.textSecondary,
                  ),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: SmartRailSpacing.md),
                SmartRailButton(
                  label: l10n.foodCall,
                  icon: Icons.phone,
                  onPressed: () => launchUrl(
                    Uri(
                      scheme: 'tel',
                      path: menu.orderPhone.replaceAll(' ', ''),
                    ),
                  ),
                ),
              ],
            ),
          ),
        ),
      ],
    );
  }
}

/// "Food at stop" (design 36): a row under the timeline for the next stop with a menu. A brand-subtle circle with a fork, the station, a
/// line about menu and prices, and a chevron. Stops with no kitchen, or whose menu cannot be reached, simply do not appear: the
/// journey never waits for the caterer.
class JourneyFood extends ConsumerWidget {
  const JourneyFood({super.key, required this.stops, required this.clock});

  final List<JourneyStop> stops;
  final NetworkClock clock;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final l10n = AppLocalizations.of(context);
    final ahead = stops.where((s) => s.state == 'Pending').toList();

    final cards = <Widget>[];
    for (final stop in ahead) {
      if (ref.watch(menuProvider(stop.stationCode)).value == null) continue;

      final arrive =
          stop.estimatedArrival ?? stop.scheduledArrival ?? stop.scheduledTime;
      final note = [
        if (arrive != null) l10n.foodArrive(clock.formatTime(arrive)),
        if (stop.platform != null) l10n.platformN(stop.platform!),
      ].join(' · ');

      cards.add(
        Semantics(
          button: true,
          label: '${l10n.foodTitle(stop.stationName)}. ${l10n.foodMenuHint}',
          child: ExcludeSemantics(
            child: Material(
              color: SmartRailColors.page,
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(SmartRailRadius.lg),
                side: const BorderSide(color: SmartRailColors.border),
              ),
              clipBehavior: Clip.antiAlias,
              child: InkWell(
                onTap: () => context.push(
                  AppRoutes.food(
                    stop.stationCode,
                    subtitle: note.isEmpty ? null : note,
                  ),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(14),
                  child: Row(
                    children: [
                      Container(
                        width: 44,
                        height: 44,
                        alignment: Alignment.center,
                        decoration: const BoxDecoration(
                          shape: BoxShape.circle,
                          color: SmartRailColors.primarySubtle,
                        ),
                        child: const SvgIcon(
                          AppIcons.fork,
                          size: 22,
                          color: SmartRailColors.primary,
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              l10n.foodTitle(stop.stationName),
                              style: SmartRailType.heading16.copyWith(
                                color: SmartRailColors.textPrimary,
                              ),
                            ),
                            const SizedBox(height: 2),
                            Text(
                              l10n.foodMenuHint,
                              style: SmartRailType.body14.copyWith(
                                color: SmartRailColors.textSecondary,
                              ),
                            ),
                          ],
                        ),
                      ),
                      const SvgIcon(
                        AppIcons.chevron,
                        size: 18,
                        color: SmartRailColors.textTertiary,
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
      );
    }

    if (cards.isEmpty) return const SizedBox.shrink();

    return Column(
      children: [
        for (var i = 0; i < cards.length; i++) ...[
          if (i > 0) const SizedBox(height: 12),
          cards[i],
        ],
      ],
    );
  }
}
