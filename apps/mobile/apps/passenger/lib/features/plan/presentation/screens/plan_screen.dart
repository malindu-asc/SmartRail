import 'package:flutter/cupertino.dart'
    show CupertinoDatePicker, CupertinoDatePickerMode;
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../core/time/network_clock_provider.dart';
import '../../../../l10n/app_localizations.dart';
import '../../../stations/domain/entities/station.dart';
import '../../../stations/presentation/providers/station_providers.dart';
import '../providers/recent_trips_provider.dart';

/// Plan a journey (design 10), laid out from the Figma values: 32 Bold title, a 16 radius card with From and To (18 Semi Bold) and a
/// 48 round swap button, a Depart/Arrive switch, three time chips, the 56 high "Search trains" button, and Recent trips.
class PlanScreen extends ConsumerStatefulWidget {
  const PlanScreen({super.key});

  @override
  ConsumerState<PlanScreen> createState() => _PlanScreenState();
}

class _PlanScreenState extends ConsumerState<PlanScreen> {
  Station? _from;
  Station? _to;

  /// The chosen moment as wall-clock time in the railway's zone; null means now.
  DateTime? _when;
  bool _arrive = false;

  Future<void> _pick({required bool from}) async {
    final picked = await context.push<Station>(
      AppRoutes.searchPickExcluding(from ? _to?.code : _from?.code),
    );
    if (picked == null || !mounted) return;
    setState(() => from ? _from = picked : _to = picked);
  }

  /// The railway's wall clock right now (year, month, day, hour, minute), not the phone's.
  DateTime _networkNow() {
    final local = ref.read(networkClockProvider).local(ref.read(nowProvider)());
    return DateTime(
      local.year,
      local.month,
      local.day,
      local.hour,
      local.minute,
    );
  }

  DateTime _laterToday() {
    final n = _networkNow();
    return DateTime(n.year, n.month, n.day, (n.hour + 3).clamp(0, 23), 0);
  }

  DateTime _tomorrow() {
    final n = _networkNow();
    return DateTime(n.year, n.month, n.day + 1, 8, 0);
  }

  Future<void> _openSheet() async {
    final picked = await showModalBottomSheet<DateTime?>(
      context: context,
      useRootNavigator: true,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => DateTimeSheet(
        now: _networkNow(),
        initial: _when,
        zone: ref.read(networkClockProvider).zoneId,
      ),
    );
    // A result of DateTime(0) means Now.
    if (!mounted || picked == null) return;
    setState(
      () => _when = picked.isAtSameMomentAs(DateTime(0)) ? null : picked,
    );
  }

  String _hhmm(DateTime w) =>
      '${w.hour.toString().padLeft(2, '0')}:${w.minute.toString().padLeft(2, '0')}';

  /// "Today 17:00", "Tomorrow 08:00" or "2026-10-22 08:00".
  String _label(AppLocalizations l10n, DateTime w) {
    final now = _networkNow();
    final day = DateTime(
      w.year,
      w.month,
      w.day,
    ).difference(DateTime(now.year, now.month, now.day)).inDays;
    if (day == 0) return l10n.ticketsWhen(_hhmm(w));
    if (day == 1) return l10n.planTomorrowAt(_hhmm(w));
    return l10n.ticketsWhenDate(
      '${w.year}-${w.month.toString().padLeft(2, '0')}-${w.day.toString().padLeft(2, '0')}',
      _hhmm(w),
    );
  }

  /// The chosen moment as a UTC instant: the wall time is the railway's, so its UTC offset is taken off (REQ-NFR-005).
  DateTime? _momentUtc() {
    final w = _when;
    if (w == null) return null;
    final offset = ref
        .read(networkClockProvider)
        .local(ref.read(nowProvider)())
        .timeZoneOffset;
    return DateTime.utc(
      w.year,
      w.month,
      w.day,
      w.hour,
      w.minute,
    ).subtract(offset);
  }

  void _search(Station from, Station to) {
    ref.read(recentTripsProvider.notifier).add(from, to);
    context.push(
      AppRoutes.planResults(
        from.code,
        to.code,
        after: _arrive ? null : _momentUtc(),
        arriveBy: _arrive ? _momentUtc() : null,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final recentStations = ref.watch(recentStationsProvider);
    final trips = ref.watch(recentTripsProvider);
    final from =
        _from ?? (recentStations.isEmpty ? null : recentStations.first);
    final to = _to;

    final custom =
        _when != null && _when != _laterToday() && _when != _tomorrow();

    return SafeArea(
      bottom: false,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(16, 8, 16, 16),
        children: [
          Text(
            l10n.planTitle,
            style: SmartRailType.headline.copyWith(
              color: SmartRailColors.textPrimary,
            ),
          ),
          const SizedBox(height: 16),
          _FromTo(
            from: from,
            to: to,
            fromLabel: l10n.fromLabel,
            toLabel: l10n.toLabel,
            placeholder: l10n.chooseStation,
            swapLabel: '${l10n.fromLabel} / ${l10n.toLabel}',
            onFrom: () => _pick(from: true),
            onTo: () => _pick(from: false),
            onSwap: () => setState(() {
              final oldFrom = from;
              _from = to;
              _to = oldFrom;
            }),
          ),
          const SizedBox(height: 16),
          _Segmented(
            left: l10n.planDepart,
            right: l10n.planArrive,
            rightSelected: _arrive,
            onChanged: (arrive) => setState(() => _arrive = arrive),
          ),
          const SizedBox(height: 16),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              _TimeChip(
                label: l10n.planLeaveNow,
                selected: _when == null,
                onTap: () => setState(() => _when = null),
              ),
              _TimeChip(
                label: custom
                    ? _label(l10n, _when!)
                    : _label(l10n, _laterToday()),
                selected: _when != null && !(_when == _tomorrow()),
                onTap: custom || _when == null
                    ? _openSheet
                    : () => setState(() => _when = _laterToday()),
                onLongPress: _openSheet,
              ),
              _TimeChip(
                label: l10n.planTomorrow,
                selected: _when == _tomorrow(),
                onTap: () => setState(() => _when = _tomorrow()),
              ),
            ],
          ),
          const SizedBox(height: 16),
          SmartRailButton(
            label: l10n.planSearch,
            onPressed: from != null && to != null
                ? () => _search(from, to)
                : null,
          ),
          if (trips.isNotEmpty) ...[
            const SizedBox(height: 16),
            Text(
              l10n.planRecent,
              style: SmartRailType.heading16.copyWith(
                color: SmartRailColors.textPrimary,
              ),
            ),
            const SizedBox(height: 16),
            DecoratedBox(
              decoration: BoxDecoration(
                color: SmartRailColors.surface,
                border: Border.all(color: SmartRailColors.border),
                borderRadius: BorderRadius.circular(SmartRailRadius.lg),
              ),
              child: ClipRRect(
                borderRadius: BorderRadius.circular(SmartRailRadius.lg),
                child: Column(
                  children: [
                    for (var i = 0; i < trips.length; i++) ...[
                      if (i > 0)
                        const Divider(
                          height: 1,
                          thickness: 1,
                          color: SmartRailColors.border,
                        ),
                      Semantics(
                        button: true,
                        label: l10n.ticketsRoute(
                          trips[i].from.name,
                          trips[i].to.name,
                        ),
                        child: ExcludeSemantics(
                          child: InkWell(
                            onTap: () => setState(() {
                              _from = trips[i].from;
                              _to = trips[i].to;
                            }),
                            child: Padding(
                              padding: const EdgeInsets.all(14),
                              child: Row(
                                children: [
                                  const SvgIcon(
                                    AppIcons.history,
                                    size: 20,
                                    color: SmartRailColors.textSecondary,
                                  ),
                                  const SizedBox(width: 12),
                                  Expanded(
                                    child: Text(
                                      l10n.ticketsRoute(
                                        trips[i].from.name,
                                        trips[i].to.name,
                                      ),
                                      style: SmartRailType.label14.copyWith(
                                        color: SmartRailColors.textPrimary,
                                      ),
                                    ),
                                  ),
                                  const SvgIcon(
                                    AppIcons.right,
                                    size: 18,
                                    color: SmartRailColors.textTertiary,
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      ),
                    ],
                  ],
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _FromTo extends StatelessWidget {
  const _FromTo({
    required this.from,
    required this.to,
    required this.fromLabel,
    required this.toLabel,
    required this.placeholder,
    required this.swapLabel,
    required this.onFrom,
    required this.onTo,
    required this.onSwap,
  });

  final Station? from;
  final Station? to;
  final String fromLabel;
  final String toLabel;
  final String placeholder;
  final String swapLabel;
  final VoidCallback onFrom;
  final VoidCallback onTo;
  final VoidCallback onSwap;

  @override
  Widget build(BuildContext context) {
    return DecoratedBox(
      decoration: BoxDecoration(
        color: SmartRailColors.surface,
        border: Border.all(color: SmartRailColors.border),
        borderRadius: BorderRadius.circular(SmartRailRadius.lg),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  PlanField(
                    label: fromLabel,
                    station: from,
                    placeholder: placeholder,
                    onTap: onFrom,
                  ),
                  const SizedBox(height: 10),
                  PlanField(
                    label: toLabel,
                    station: to,
                    placeholder: placeholder,
                    onTap: onTo,
                  ),
                ],
              ),
            ),
            const SizedBox(width: 12),
            Semantics(
              button: true,
              label: swapLabel,
              child: ExcludeSemantics(
                child: InkWell(
                  customBorder: const CircleBorder(),
                  onTap: onSwap,
                  child: Container(
                    width: 48,
                    height: 48,
                    alignment: Alignment.center,
                    decoration: BoxDecoration(
                      shape: BoxShape.circle,
                      border: Border.all(
                        color: SmartRailColors.borderStrong,
                        width: 1.5,
                      ),
                    ),
                    child: const SvgIcon(
                      AppIcons.swap,
                      size: 22,
                      color: SmartRailColors.primary,
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

/// A tappable From or To: a 12 Medium label over the station in 18 Semi Bold.
class PlanField extends StatelessWidget {
  const PlanField({
    super.key,
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
    return Semantics(
      button: true,
      label: '$label, ${station?.name ?? placeholder}',
      child: ExcludeSemantics(
        child: InkWell(
          onTap: onTap,
          child: ConstrainedBox(
            constraints: const BoxConstraints(minHeight: 44),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(
                  label,
                  style: SmartRailType.caption.copyWith(
                    color: SmartRailColors.textSecondary,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  station?.name ?? placeholder,
                  style: SmartRailType.title.copyWith(
                    color: station == null
                        ? SmartRailColors.textTertiary
                        : SmartRailColors.textPrimary,
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

/// The Depart / Arrive switch: a soft pill with a white pill under the chosen side.
class _Segmented extends StatelessWidget {
  const _Segmented({
    required this.left,
    required this.right,
    required this.rightSelected,
    required this.onChanged,
  });

  final String left;
  final String right;
  final bool rightSelected;
  final ValueChanged<bool> onChanged;

  @override
  Widget build(BuildContext context) {
    Widget segment(String text, bool selected, VoidCallback onTap) => Expanded(
      child: Semantics(
        button: true,
        selected: selected,
        label: text,
        child: ExcludeSemantics(
          child: GestureDetector(
            onTap: onTap,
            child: AnimatedContainer(
              duration: SmartRailMotion.of(context, SmartRailMotion.fast),
              height: 36,
              alignment: Alignment.center,
              decoration: BoxDecoration(
                color: selected ? SmartRailColors.surface : Colors.transparent,
                borderRadius: BorderRadius.circular(SmartRailRadius.pill),
              ),
              child: Text(
                text,
                style: SmartRailType.label14.copyWith(
                  color: selected
                      ? SmartRailColors.textPrimary
                      : SmartRailColors.textSecondary,
                ),
              ),
            ),
          ),
        ),
      ),
    );

    return Container(
      padding: const EdgeInsets.all(4),
      decoration: BoxDecoration(
        color: SmartRailColors.bgSoft,
        borderRadius: BorderRadius.circular(SmartRailRadius.pill),
      ),
      child: Row(
        children: [
          segment(left, !rightSelected, () => onChanged(false)),
          segment(right, rightSelected, () => onChanged(true)),
        ],
      ),
    );
  }
}

/// A time chip: 14 Semi Bold, padded 14 by 8; the chosen one is brand-subtle with a brand hairline.
class _TimeChip extends StatelessWidget {
  const _TimeChip({
    required this.label,
    required this.selected,
    required this.onTap,
    this.onLongPress,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;
  final VoidCallback? onLongPress;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      button: true,
      selected: selected,
      label: label,
      child: ExcludeSemantics(
        child: GestureDetector(
          onTap: onTap,
          onLongPress: onLongPress,
          child: AnimatedContainer(
            duration: SmartRailMotion.of(context, SmartRailMotion.fast),
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
            decoration: BoxDecoration(
              color: selected
                  ? SmartRailColors.primarySubtle
                  : SmartRailColors.surface,
              border: Border.all(
                color: selected
                    ? SmartRailColors.primary
                    : SmartRailColors.borderStrong,
              ),
              borderRadius: BorderRadius.circular(SmartRailRadius.pill),
            ),
            child: Text(
              label,
              style: SmartRailType.label14.copyWith(
                color: selected
                    ? SmartRailColors.primaryPressed
                    : SmartRailColors.textPrimary,
              ),
            ),
          ),
        ),
      ),
    );
  }
}

/// "Date and time" (design 11): Now, Later today and Tomorrow as shortcuts, and a scroll wheel for any day in the next week. The
/// result is a wall-clock time in the railway's zone; [DateTime(0)] means Now.
class DateTimeSheet extends StatefulWidget {
  const DateTimeSheet({
    super.key,
    required this.now,
    required this.initial,
    required this.zone,
  });

  final DateTime now;
  final DateTime? initial;
  final String zone;

  @override
  State<DateTimeSheet> createState() => _DateTimeSheetState();
}

class _DateTimeSheetState extends State<DateTimeSheet> {
  late DateTime _value = widget.initial ?? widget.now;
  bool _now = false;
  int _generation = 0;

  DateTime get _min => DateTime(
    widget.now.year,
    widget.now.month,
    widget.now.day,
    widget.now.hour,
    widget.now.minute,
  );

  void _set(DateTime value, {bool now = false}) => setState(() {
    _value = value.isBefore(_min) ? _min : value;
    _now = now;
    _generation++; // moves the wheel to the new value
  });

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final n = widget.now;
    final laterToday = DateTime(
      n.year,
      n.month,
      n.day,
      (n.hour + 2).clamp(0, 23),
      0,
    );
    final tomorrow = DateTime(n.year, n.month, n.day + 1, 8, 0);

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
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              l10n.planWhenTitle,
              style: Theme.of(context).textTheme.titleLarge,
            ),
            const SizedBox(height: SmartRailSpacing.md),
            Wrap(
              spacing: SmartRailSpacing.sm,
              children: [
                ChoiceChip(
                  label: Text(l10n.planLeaveNow),
                  selected: _now,
                  onSelected: (_) => _set(n, now: true),
                ),
                ActionChip(
                  label: Text(l10n.planLaterToday),
                  onPressed: () => _set(laterToday),
                ),
                ActionChip(
                  label: Text(l10n.planTomorrow),
                  onPressed: () => _set(tomorrow),
                ),
              ],
            ),
            const SizedBox(height: SmartRailSpacing.md),
            Container(
              height: 190,
              decoration: BoxDecoration(
                color: SmartRailColors.bgSoft,
                borderRadius: BorderRadius.circular(SmartRailRadius.md),
              ),
              child: CupertinoDatePicker(
                key: ValueKey(_generation),
                mode: CupertinoDatePickerMode.dateAndTime,
                use24hFormat: true,
                initialDateTime: _value,
                minimumDate: _min,
                maximumDate: DateTime(n.year, n.month, n.day + 7, 23, 59),
                onDateTimeChanged: (v) => _value = v,
              ),
            ),
            const SizedBox(height: SmartRailSpacing.sm),
            Text(
              l10n.planTimeNote(widget.zone),
              style: Theme.of(context).textTheme.bodySmall?.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
            const SizedBox(height: SmartRailSpacing.lg),
            SmartRailButton(
              label: l10n.whoConfirm,
              onPressed: () =>
                  Navigator.of(context).pop(_now ? DateTime(0) : _value),
            ),
          ],
        ),
      ),
    );
  }
}
