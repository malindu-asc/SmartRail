import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
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
