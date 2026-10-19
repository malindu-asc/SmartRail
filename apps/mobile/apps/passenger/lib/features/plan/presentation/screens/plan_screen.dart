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

/// Plan a journey (design 10): From, To, when, then the trains between them.
class PlanScreen extends ConsumerStatefulWidget {
  const PlanScreen({super.key});

  @override
  ConsumerState<PlanScreen> createState() => _PlanScreenState();
}

class _PlanScreenState extends ConsumerState<PlanScreen> {
  Station? _from;
  Station? _to;

  /// The chosen departure as wall-clock time in the railway's zone; null means now.
  DateTime? _when;

  Future<void> _pick({required bool from}) async {
    final picked = await context.push<Station>(AppRoutes.searchPick);
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

  Future<void> _pickWhen() async {
    final picked = await showModalBottomSheet<DateTime?>(
      context: context,
      isScrollControlled: true,
      showDragHandle: true,
      builder: (_) => DateTimeSheet(
        now: _networkNow(),
        initial: _when,
        zone: ref.read(networkClockProvider).zoneId,
      ),
    );
    // A result of the same moment as "now" is Now.
    if (!mounted || picked == null) return;
    setState(
      () => _when = picked.isAtSameMomentAs(DateTime(0)) ? null : picked,
    );
  }

  String _whenLabel(AppLocalizations l10n) {
    final w = _when;
    if (w == null) return l10n.planPickTime;
    final now = _networkNow();
    final time =
        '${w.hour.toString().padLeft(2, '0')}:${w.minute.toString().padLeft(2, '0')}';
    final day = DateTime(
      w.year,
      w.month,
      w.day,
    ).difference(DateTime(now.year, now.month, now.day)).inDays;
    if (day == 0) return l10n.ticketsWhen(time);
    if (day == 1) return l10n.planTomorrowAt(time);
    return l10n.ticketsWhenDate(
      '${w.year}-${w.month.toString().padLeft(2, '0')}-${w.day.toString().padLeft(2, '0')}',
      time,
    );
  }

  /// The chosen moment as a UTC instant: the wall time is the railway's, so its UTC offset is taken off (REQ-NFR-005).
  DateTime? _afterUtc() {
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

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final recent = ref.watch(recentStationsProvider);
    final from = _from ?? (recent.isEmpty ? null : recent.first);

    return SafeArea(
      child: ListView(
        padding: const EdgeInsets.all(SmartRailSpacing.xl),
        children: [
          Text(
            l10n.planTitle,
            style: Theme.of(context).textTheme.headlineLarge,
          ),
          const SizedBox(height: SmartRailSpacing.xl),
          AppearIn(
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
                              PlanField(
                                label: l10n.fromLabel,
                                station: from,
                                placeholder: l10n.chooseStation,
                                onTap: () => _pick(from: true),
                              ),
                              const Divider(),
                              PlanField(
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
                            backgroundColor: SmartRailColors.primarySubtle,
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
                    const SizedBox(height: SmartRailSpacing.md),
                    Align(
                      alignment: Alignment.centerLeft,
                      child: Wrap(
                        spacing: SmartRailSpacing.sm,
                        children: [
                          ChoiceChip(
                            label: Text(l10n.planLeaveNow),
                            selected: _when == null,
                            onSelected: (_) => setState(() => _when = null),
                          ),
                          ChoiceChip(
                            avatar: const Icon(Icons.schedule, size: 18),
                            label: Text(_whenLabel(l10n)),
                            selected: _when != null,
                            onSelected: (_) => _pickWhen(),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: SmartRailSpacing.lg),
                    SmartRailButton(
                      label: l10n.planSearch,
                      icon: Icons.search,
                      onPressed: from != null && _to != null
                          ? () => context.push(
                              AppRoutes.planResults(
                                from.code,
                                _to!.code,
                                after: _afterUtc(),
                              ),
                            )
                          : null,
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

/// A tappable From or To row. Shared by Plan; the station name or the placeholder.
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
    final theme = Theme.of(context);

    return Semantics(
      button: true,
      label: '$label, ${station?.name ?? placeholder}',
      child: ExcludeSemantics(
        child: InkWell(
          onTap: onTap,
          child: ConstrainedBox(
            constraints: const BoxConstraints(minHeight: 56),
            child: Align(
              alignment: Alignment.centerLeft,
              child: Padding(
                padding: const EdgeInsets.symmetric(
                  vertical: SmartRailSpacing.sm,
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      label,
                      style: theme.textTheme.labelSmall?.copyWith(
                        color: SmartRailColors.textSecondary,
                      ),
                    ),
                    Text(
                      station?.name ?? placeholder,
                      style: theme.textTheme.bodyLarge?.copyWith(
                        fontWeight: FontWeight.w600,
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
