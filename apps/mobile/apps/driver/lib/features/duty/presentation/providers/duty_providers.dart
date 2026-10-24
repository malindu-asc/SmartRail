import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';

import '../../data/duty_data.dart';
import '../../domain/duty_domain.dart';

final dutyRepositoryProvider = Provider<DutyRepository>(
  (ref) => ApiDutyRepository(ref.watch(apiClientProvider)),
);

/// How often a screen that shows live data asks again while it is open.
const _refreshEvery = Duration(seconds: 20);

Stream<T> _polling<T>(Ref ref, Future<T> Function() load) {
  final controller = StreamController<T>();

  Future<void> tick({required bool first}) async {
    try {
      final value = await load();
      if (!controller.isClosed) controller.add(value);
    } on Object catch (error) {
      // A refresh that fails keeps what is on screen; only the first load reports its failure.
      if (first && !controller.isClosed) controller.addError(error);
    }
  }

  // A timer, not a stream loop, so closing the screen stops it at once.
  final timer = Timer.periodic(_refreshEvery, (_) => tick(first: false));
  ref.onDispose(() {
    timer.cancel();
    controller.close();
  });
  tick(first: true);

  return controller.stream;
}

/// Today's duty, refreshed every 20 seconds while the Duty tab is open.
final myDutyProvider = StreamProvider.autoDispose<MyDuty>(
  (ref) => _polling(ref, () async {
    final result = await ref.read(dutyRepositoryProvider).myDuty();
    return result.match((failure) => throw failure, (duty) => duty);
  }),
);

final dutyHistoryProvider = FutureProvider.autoDispose<List<Duty>>((ref) async {
  final result = await ref.read(dutyRepositoryProvider).history();
  return result.match((failure) => throw failure, (duties) => duties);
});

/// One journey on the driver's duty, refreshed every 20 seconds so a change by control shows.
final driverJourneyProvider = StreamProvider.autoDispose
    .family<DriverJourney, String>(
      (ref, id) => _polling(ref, () async {
        final result = await ref.read(dutyRepositoryProvider).journey(id);
        return result.match((failure) => throw failure, (j) => j);
      }),
    );

/// The app's own counter for event messages. It only goes up (the server drops a lower number), also across restarts, because it
/// starts from the clock.
class EventSequence {
  int _last = 0;

  int next() {
    final now = DateTime.now().millisecondsSinceEpoch;
    _last = now > _last ? now : _last + 1;
    return _last;
  }
}

final eventSequenceProvider = Provider<EventSequence>((ref) => EventSequence());

/// Does something to the duty or a journey, then reloads what changed. Returns the failure, or null when it worked.
class DutyActions {
  DutyActions(this._ref);

  final Ref _ref;

  DutyRepository get _repo => _ref.read(dutyRepositoryProvider);

  Future<Failure?> signOn(String dutyId) async {
    final result = await _repo.signOn(dutyId);
    _ref.invalidate(myDutyProvider);
    return result.getLeft().toNullable();
  }

  Future<Failure?> signOff(String dutyId) async {
    final result = await _repo.signOff(dutyId);
    _ref.invalidate(myDutyProvider);
    _ref.invalidate(dutyHistoryProvider);
    return result.getLeft().toNullable();
  }

  Future<Failure?> event(
    String journeyId,
    int stopSequence,
    String type,
  ) async {
    final result = await _repo.sendEvent(
      journeyId: journeyId,
      stopSequence: stopSequence,
      type: type,
      sequence: _ref.read(eventSequenceProvider).next(),
      occurredAt: DateTime.now(),
    );
    _ref.invalidate(driverJourneyProvider(journeyId));
    _ref.invalidate(myDutyProvider);
    return result.getLeft().toNullable();
  }

  Future<Failure?> delayReason(String journeyId, String code) async {
    final result = await _repo.delayReason(journeyId, code);
    _ref.invalidate(driverJourneyProvider(journeyId));
    return result.getLeft().toNullable();
  }
}

final dutyActionsProvider = Provider<DutyActions>(DutyActions.new);
