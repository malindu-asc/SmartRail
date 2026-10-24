import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fpdart/fpdart.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:driver_app/app/driver_app.dart';
import 'package:driver_app/app/locale_provider.dart';
import 'package:driver_app/features/auth/domain/auth_domain.dart';
import 'package:driver_app/features/auth/presentation/providers/auth_providers.dart';
import 'package:driver_app/features/duty/domain/duty_domain.dart';
import 'package:driver_app/features/duty/presentation/providers/duty_providers.dart';
import 'package:driver_app/features/messages/messages.dart';

class _FakeDuty implements DutyRepository {
  @override
  Future<Either<Failure, MyDuty>> myDuty() async => right(
    MyDuty(
      date: '2026-10-20',
      duties: [
        Duty(
          id: 'd1',
          status: DutyStatus.ready,
          from: DateTime.utc(2026, 10, 20, 2),
          to: DateTime.utc(2026, 10, 20, 8),
          unitCode: 'MAIN-F',
          canSignOn: true,
          needsReview: false,
          signedOnAt: null,
          journeys: const [],
        ),
      ],
    ),
  );

  @override
  Future<Either<Failure, List<Duty>>> history() async => right(const []);

  @override
  Future<Either<Failure, Unit>> signOn(String dutyId) async => right(unit);

  @override
  Future<Either<Failure, Unit>> signOff(String dutyId) async => right(unit);

  @override
  Future<Either<Failure, DriverJourney>> journey(String journeyId) async =>
      left(const NotFoundFailure());

  @override
  Future<Either<Failure, Unit>> sendEvent({
    required String journeyId,
    required int stopSequence,
    required String type,
    required int sequence,
    required DateTime occurredAt,
  }) async => right(unit);

  @override
  Future<Either<Failure, Unit>> delayReason(String journeyId, String code) async =>
      right(unit);
}

class _NoMessages implements MessagesRepository {
  @override
  Future<Either<Failure, List<ControlMessage>>> messages() async =>
      right(const []);

  @override
  Future<Either<Failure, Unit>> acknowledge(String id) async => right(unit);

  @override
  Future<Either<Failure, Unit>> requestAssistance({
    String? journeyId,
    String? stationCode,
    String? note,
  }) async => right(unit);
}

void main() {
  testWidgets('the first launch asks for a language, English or Sinhala only', (tester) async {
    await tester.pumpWidget(const ProviderScope(child: DriverApp()));
    await tester.pumpAndSettle();

    expect(find.text('Choose your language'), findsOneWidget);
    expect(find.text('English'), findsOneWidget);
    expect(find.text('සිංහල'), findsOneWidget);
    expect(find.text('Tamil'), findsNothing);

    await tester.tap(find.text('Continue'));
    await tester.pumpAndSettle();
    expect(find.text('Welcome Back'), findsOneWidget);
  });

  testWidgets('a signed-out driver who has chosen a language sees sign in', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [initialLanguageProvider.overrideWithValue(const Locale('en'))],
        child: const DriverApp(),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Welcome Back'), findsOneWidget);
  });

  testWidgets('a signed-in driver sees the duty and can sign on', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          initialLanguageProvider.overrideWithValue(const Locale('en')),
          initialSessionProvider.overrideWithValue(
            Session(
              accessToken: 'a',
              refreshToken: 'r',
              expiresAt: DateTime.now().add(const Duration(hours: 1)),
            ),
          ),
          dutyRepositoryProvider.overrideWithValue(_FakeDuty()),
          messagesRepositoryProvider.overrideWithValue(_NoMessages()),
        ],
        child: const DriverApp(),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Your duty'), findsOneWidget);
    expect(find.text('Sign on to duty'), findsOneWidget);

    // Unmount so the 20 second refresh timers stop before the test ends.
    await tester.pumpWidget(const SizedBox());
    await tester.pump();
  });
}
