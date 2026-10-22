import 'dart:async';
import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:fpdart/fpdart.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';

/// A message from control to this driver, and the driver's own help requests (`/api/driver/messages`, `/api/driver/assistance`).
class ControlMessage {
  const ControlMessage({
    required this.id,
    required this.body,
    required this.requiresAck,
    required this.sentAt,
    required this.acknowledgedAt,
    required this.expired,
  });

  final String id;
  final String body;
  final bool requiresAck;
  final DateTime sentAt;
  final DateTime? acknowledgedAt;
  final bool expired;

  bool get needsAck => requiresAck && acknowledgedAt == null && !expired;

  static ControlMessage fromJson(Map<String, Object?> json) => ControlMessage(
    id: json['id']! as String,
    body: json['body']! as String,
    requiresAck: json['requiresAck'] == true,
    sentAt: DateTime.parse(json['sentAtUtc']! as String),
    acknowledgedAt: json['acknowledgedAtUtc'] is String
        ? DateTime.parse(json['acknowledgedAtUtc']! as String)
        : null,
    expired: json['expired'] == true,
  );
}

abstract interface class MessagesRepository {
  Future<Either<Failure, List<ControlMessage>>> messages();

  Future<Either<Failure, Unit>> acknowledge(String id);

  Future<Either<Failure, Unit>> requestAssistance({
    String? journeyId,
    String? stationCode,
    String? note,
  });
}

class ApiMessagesRepository implements MessagesRepository {
  ApiMessagesRepository(this._api);

  final ApiClient _api;

  @override
  Future<Either<Failure, List<ControlMessage>>> messages() async {
    final result = await _api.get('/api/driver/messages');
    return result.flatMap((r) {
      final body = r.body;
      if (body is! List) return left(const UnknownFailure('bad_response'));
      return right([
        for (final m in body)
          ControlMessage.fromJson((m as Map).cast<String, Object?>()),
      ]);
    });
  }

  @override
  Future<Either<Failure, Unit>> acknowledge(String id) async =>
      (await _api.post('/api/driver/messages/$id/acknowledge')).map((_) => unit);

  @override
  Future<Either<Failure, Unit>> requestAssistance({
    String? journeyId,
    String? stationCode,
    String? note,
  }) async => (await _api.post(
    '/api/driver/assistance',
    data: {'journeyId': journeyId, 'stationCode': stationCode, 'note': note},
  )).map((_) => unit);
}

final messagesRepositoryProvider = Provider<MessagesRepository>(
  (ref) => ApiMessagesRepository(ref.watch(apiClientProvider)),
);

/// The driver's messages, newest first, asked for again every 20 seconds so a message from control arrives while the app is open.
final messagesProvider = StreamProvider.autoDispose<List<ControlMessage>>((ref) {
  final controller = StreamController<List<ControlMessage>>();

  Future<void> tick({required bool first}) async {
    final result = await ref.read(messagesRepositoryProvider).messages();
    if (controller.isClosed) return;
    result.match(
      (failure) {
        // A refresh that fails keeps the list on screen; only the first load reports its failure.
        if (first) controller.addError(failure);
      },
      controller.add,
    );
  }

  final timer = Timer.periodic(
    const Duration(seconds: 20),
    (_) => tick(first: false),
  );
  ref.onDispose(() {
    timer.cancel();
    controller.close();
  });
  tick(first: true);

  return controller.stream;
});

/// How many messages still need the driver's acknowledgement (the badge on the Messages tab).
final unreadCountProvider = Provider.autoDispose<int>((ref) {
  final messages = ref.watch(messagesProvider).value ?? const [];
  return messages.where((m) => m.needsAck).length;
});

// ---- alerts: what control sends to this driver, and what control publishes to everyone ----

enum AlertKind { direct, network }

enum AlertSeverity { info, major, severe }

/// One line of the Alerts tab. A **direct** alert is a message from control to this driver, and needs an acknowledgement. A **network**
/// alert is a notice control published for the whole network (a disruption): the driver reads it, nothing more is asked.
class AlertItem {
  const AlertItem({
    required this.id,
    required this.kind,
    required this.title,
    required this.detail,
    required this.at,
    this.severity = AlertSeverity.info,
    this.needsAck = false,
    this.acknowledgedAt,
    this.places = '',
  });

  final String id;
  final AlertKind kind;
  final String title;
  final String detail;
  final DateTime at;
  final AlertSeverity severity;
  final bool needsAck;
  final DateTime? acknowledgedAt;

  /// Where a network alert applies, for example "Central, Mill Lane" or "All stations".
  final String places;

  /// Severe network notices are the ones that must be seen at once.
  bool get urgent => kind == AlertKind.network && severity == AlertSeverity.severe;

  /// The first line of a message is its heading; the rest is the detail.
  static (String, String) splitMessage(String body) {
    final lines = body.trim().split('\n');
    if (lines.length > 1) return (lines.first, lines.skip(1).join(' ').trim());
    final dot = body.indexOf('. ');
    if (dot > 0) return (body.substring(0, dot), body.substring(dot + 2).trim());
    return (body.trim(), '');
  }

  static AlertItem fromMessage(ControlMessage m) {
    final (title, detail) = splitMessage(m.body);
    return AlertItem(
      id: m.id,
      kind: AlertKind.direct,
      title: title,
      detail: detail,
      at: m.sentAt,
      needsAck: m.needsAck,
      acknowledgedAt: m.acknowledgedAt,
    );
  }

  static AlertItem? fromDisruption(Map<String, Object?> d) {
    // A notice for works that start later is not an alert yet.
    if (d['planned'] == true) return null;
    final stations = [
      for (final s in (d['stations'] as List? ?? const []))
        (s as Map)['name'] as String,
    ];
    return AlertItem(
      id: d['id']! as String,
      kind: AlertKind.network,
      title: d['title']! as String,
      detail: d['message']! as String,
      at: DateTime.parse(d['updatedAtUtc']! as String),
      severity: switch (d['severity']) {
        'Severe' => AlertSeverity.severe,
        'Major' => AlertSeverity.major,
        _ => AlertSeverity.info,
      },
      places: d['networkWide'] == true ? '' : stations.join(', '),
    );
  }
}

/// The notices control has published, from the public disruptions list.
Future<Either<Failure, List<AlertItem>>> loadNetworkAlerts(ApiClient api) async {
  final result = await api.get('/api/operations/disruptions');
  return result.flatMap((r) {
    final body = r.body;
    if (body is! List) return left(const UnknownFailure('bad_response'));
    return right([
      for (final d in body)
        ?AlertItem.fromDisruption((d as Map).cast<String, Object?>()),
    ]);
  });
}

/// Direct messages and network alerts together, newest first, asked for again every 20 seconds. If one of the two cannot be read the
/// other is still shown.
final alertsProvider = StreamProvider.autoDispose<List<AlertItem>>((ref) {
  final controller = StreamController<List<AlertItem>>();

  Future<void> tick({required bool first}) async {
    final direct = await ref.read(messagesRepositoryProvider).messages();
    final network = await loadNetworkAlerts(ref.read(apiClientProvider));
    if (controller.isClosed) return;

    if (direct.isLeft() && network.isLeft()) {
      // A refresh that fails keeps the list on screen; only the first load reports its failure.
      if (first) controller.addError(direct.getLeft().toNullable()!);
      return;
    }

    final items = [
      ...direct.getOrElse((_) => const []).map(AlertItem.fromMessage),
      ...network.getOrElse((_) => const []),
    ]..sort((a, b) => b.at.compareTo(a.at));
    controller.add(items);
  }

  final timer = Timer.periodic(
    const Duration(seconds: 20),
    (_) => tick(first: false),
  );
  ref.onDispose(() {
    timer.cancel();
    controller.close();
  });
  tick(first: true);

  return controller.stream;
});

/// The network alerts this driver has opened. A driver is asked to acknowledge direct messages; network alerts only count as new
/// until they have been opened once.
final seenAlertsProvider = NotifierProvider<SeenAlerts, Set<String>>(
  SeenAlerts.new,
);

class SeenAlerts extends Notifier<Set<String>> {
  static const _key = 'driver_seen_alerts';
  static const _storage = FlutterSecureStorage();

  @override
  Set<String> build() {
    _load();
    return const {};
  }

  Future<void> _load() async {
    try {
      final raw = await _storage.read(key: _key);
      if (raw == null) return;
      state = {...state, ...(jsonDecode(raw) as List).cast<String>()};
    } on Object {
      // No stored list: every network alert is new once.
    }
  }

  void markSeen(String id) {
    if (state.contains(id)) return;
    state = {...state, id};
    _storage.write(key: _key, value: jsonEncode(state.toList())).ignore();
  }
}

/// A message that has not been acknowledged, or a network alert that has not been opened: the number on the Alerts tab.
final unreadAlertsProvider = Provider.autoDispose<int>((ref) {
  final seen = ref.watch(seenAlertsProvider);
  final alerts = ref.watch(alertsProvider).value ?? const [];
  return alerts
      .where(
        (a) => a.kind == AlertKind.direct ? a.needsAck : !seen.contains(a.id),
      )
      .length;
});
