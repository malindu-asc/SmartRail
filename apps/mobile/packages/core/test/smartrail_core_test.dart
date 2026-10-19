import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_core/smartrail_core.dart';

void main() {
  group('Failure', () {
    test('a failure is a value: same kind, code and message are equal', () {
      expect(
        const NotFoundFailure('Journeys.JourneyNotFound'),
        const NotFoundFailure('Journeys.JourneyNotFound'),
      );
      expect(const NotFoundFailure('a') == const ConflictFailure('a'), isFalse);
    });

    test('carries a stable code and no user-facing text of its own', () {
      expect(const NetworkFailure.timeout().code, 'network.timeout');
      expect(const NetworkFailure.unreachable().code, 'network.unreachable');
      expect(const ServerFailure().message, isNull);
    });

    test('a validation failure keeps the field errors', () {
      const failure = ValidationFailure('validation', 'bad', {
        'count': ['The count must be between 1 and 50.'],
      });
      expect(failure.errors['count'], ['The count must be between 1 and 50.']);
    });

    test('every failure kind can be told apart in a switch', () {
      String kind(Failure f) => switch (f) {
        NetworkFailure() => 'network',
        UnauthorizedFailure() => 'unauthorized',
        ForbiddenFailure() => 'forbidden',
        NotFoundFailure() => 'not_found',
        ConflictFailure() => 'conflict',
        ValidationFailure() => 'validation',
        ServerFailure() => 'server',
        UnknownFailure() => 'unknown',
      };

      expect(kind(const ConflictFailure()), 'conflict');
      expect(kind(const UnknownFailure()), 'unknown');
    });
  });

  group('NetworkClock (REQ-NFR-005)', () {
    final clock = NetworkClock.colombo();

    test('shows UTC instants in the network time zone, not the phone zone', () {
      expect(
        clock.formatTime(DateTime.utc(2026, 10, 14, 2, 0)),
        '07:30',
      ); // UTC+5:30
      expect(clock.formatTime(DateTime.utc(2026, 10, 14, 18, 20)), '23:50');
    });

    test('an instant after local midnight is on the next calendar day', () {
      final arrival = DateTime.utc(
        2026,
        10,
        13,
        19,
        5,
      ); // 00:35 on 14 October, local

      expect(clock.formatTime(arrival), '00:35');
      expect(clock.formatDate(arrival), '2026-10-14');
      expect(clock.daysAfterServiceDate(arrival, '2026-10-13'), 1);
    });

    test(
      'a service that crosses midnight shows its late stops with a day marker',
      () {
        const serviceDate = '2026-10-13';

        expect(
          clock.formatTimeForService(
            DateTime.utc(2026, 10, 13, 18, 20),
            serviceDate,
          ),
          '23:50',
        );
        expect(
          clock.formatTimeForService(
            DateTime.utc(2026, 10, 13, 19, 5),
            serviceDate,
          ),
          '00:35 +1',
        );
      },
    );

    test(
      'a local instant is converted to UTC first, whatever zone it was created in',
      () {
        final local = DateTime.parse('2026-10-14T07:30:00+05:30');

        expect(clock.formatTime(local), '07:30');
      },
    );
  });
}
