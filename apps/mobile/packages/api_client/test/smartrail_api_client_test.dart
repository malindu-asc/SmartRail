import 'dart:async';
import 'dart:convert';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';

/// Scripted answers for Dio, so no network is used. Each call takes the next step and records what was sent.
class FakeAdapter implements HttpClientAdapter {
  FakeAdapter(this.steps);

  final List<Object> steps;
  final List<RequestOptions> sent = [];

  @override
  Future<ResponseBody> fetch(RequestOptions options, Stream<Uint8List>? requestStream, Future<void>? cancelFuture) async {
    sent.add(options);
    final step = steps.removeAt(0);
    if (step is DioException) throw step.copyWith(requestOptions: options);
    return step as ResponseBody;
  }

  @override
  void close({bool force = false}) {}
}

ResponseBody json(int status, Object? body, {Map<String, List<String>> headers = const {}}) => ResponseBody.fromString(
  jsonEncode(body),
  status,
  headers: {
    Headers.contentTypeHeader: [status >= 400 ? 'application/problem+json' : 'application/json'],
    ...headers,
  },
);

DioException connectionError() => DioException(requestOptions: RequestOptions(), type: DioExceptionType.connectionError);

DioException timeout() => DioException(requestOptions: RequestOptions(), type: DioExceptionType.receiveTimeout);

({ApiClient client, FakeAdapter adapter, List<String> log}) build(
  List<Object> steps, {
  String locale = 'en',
  String? token,
  DateTime Function()? now,
}) {
  final adapter = FakeAdapter(steps);
  final dio = Dio()..httpClientAdapter = adapter;
  final log = <String>[];
  final client = ApiClient(dio: dio, baseUrl: 'http://api.test', locale: () => locale, token: () async => token, log: log.add, now: now);
  return (client: client, adapter: adapter, log: log);
}

void main() {
  reachabilityTests();

  group('errors become failures, never exceptions (AC-2)', () {
    Future<Failure> failureFor(int status, Object? body) async {
      final t = build([json(status, body)]);
      final result = await t.client.get('/api/journeys/x');
      return result.match((f) => f, (_) => throw StateError('expected a failure'));
    }

    test('400 keeps the code and the field errors', () async {
      final f = await failureFor(400, {
        'code': 'Journeys.InvalidCount',
        'detail': 'The count must be between 1 and 50.',
        'errors': {'count': ['The count must be between 1 and 50.']},
      });

      expect(f, isA<ValidationFailure>());
      expect(f.code, 'Journeys.InvalidCount');
      expect((f as ValidationFailure).errors['count'], ['The count must be between 1 and 50.']);
    });

    test('401, 403, 404 and 409 map to their kinds with the API code', () async {
      expect(await failureFor(401, {'title': 'Unauthorized'}), isA<UnauthorizedFailure>());
      expect(await failureFor(403, {'title': 'Forbidden'}), isA<ForbiddenFailure>());

      final notFound = await failureFor(404, {'code': 'Journeys.JourneyNotFound'});
      expect(notFound, isA<NotFoundFailure>());
      expect(notFound.code, 'Journeys.JourneyNotFound');

      expect(await failureFor(409, {'code': 'Journeys.IllegalTransition'}), isA<ConflictFailure>());
    });

    test('500 and 503 are server failures', () async {
      expect(await failureFor(500, {'title': 'Error'}), isA<ServerFailure>());
      expect(await failureFor(503, null), isA<ServerFailure>());
    });

    test('a timeout and a refused connection are network failures', () async {
      final t1 = build([timeout()]);
      expect((await t1.client.get('/x')).match((f) => f, (_) => throw StateError('no')), const NetworkFailure.timeout());

      final t2 = build([connectionError(), connectionError()]);
      expect((await t2.client.get('/x')).match((f) => f, (_) => throw StateError('no')), const NetworkFailure.unreachable());
    });

    test('a body that is not JSON problem details still gives a failure', () async {
      final t = build([ResponseBody.fromString('<html>bad gateway</html>', 502)]);

      final result = await t.client.get('/x');

      expect(result.match((f) => f, (_) => throw StateError('no')), isA<ServerFailure>());
    });
  });

  group('retries', () {
    test('a read is tried once more after a refused connection', () async {
      final t = build([connectionError(), json(200, {'ok': true})]);

      final result = await t.client.get('/api/network/stations');

      expect(result.isRight(), isTrue);
      expect(t.adapter.sent.length, 2);
    });

    test('a timeout is not retried', () async {
      final t = build([timeout(), json(200, {})]);

      await t.client.get('/x');

      expect(t.adapter.sent.length, 1);
    });

    test('a write is never retried', () async {
      final t = build([connectionError(), json(200, {})]);

      final result = await t.client.post('/api/anything', data: {'a': 1});

      expect(result.isLeft(), isTrue);
      expect(t.adapter.sent.length, 1);
    });
  });

  group('ETag revalidation and stale copies (AC-3)', () {
    test('the next read sends If-None-Match and a 304 returns the remembered body with the original fetch time', () async {
      var clock = DateTime.utc(2026, 10, 19, 8, 0);
      final t = build([
        json(200, {'items': [1, 2]}, headers: {'etag': ['"abc"']}),
        ResponseBody.fromString('', 304),
      ], now: () => clock);

      final first = (await t.client.get('/api/journeys/boards/CEN')).match((_) => throw StateError('no'), (r) => r);
      clock = clock.add(const Duration(minutes: 5));
      final second = (await t.client.get('/api/journeys/boards/CEN')).match((_) => throw StateError('no'), (r) => r);

      expect(t.adapter.sent[0].headers.containsKey('If-None-Match'), isFalse);
      expect(t.adapter.sent[1].headers['If-None-Match'], '"abc"');
      expect(second.body, first.body);
      expect(second.revalidated, isTrue);
      expect(second.fetchedAt, DateTime.utc(2026, 10, 19, 8, 0));   // not the time of the second request
    });

    test('after a failure the caller can still ask for the last good copy, marked as a copy', () async {
      final t = build([json(200, {'items': [1]}, headers: {'etag': ['"v1"']}), connectionError(), connectionError()]);

      await t.client.get('/api/journeys/boards/CEN');
      final failed = await t.client.get('/api/journeys/boards/CEN');
      final copy = t.client.cachedCopy('/api/journeys/boards/CEN');

      expect(failed.isLeft(), isTrue);
      expect(copy, isNotNull);
      expect(copy!.fromCache, isTrue);
      expect(copy.body, {'items': [1]});
      expect(t.client.cachedCopy('/api/never/asked'), isNull);
    });

    test('the language is part of the cache key: another language is a different answer', () async {
      var locale = 'en';
      final adapter = FakeAdapter([json(200, {'name': 'Central'}, headers: {'etag': ['"en"']}), json(200, {'name': 'මධ්‍යම'}, headers: {'etag': ['"si"']})]);
      final client = ApiClient(dio: Dio()..httpClientAdapter = adapter, baseUrl: 'http://api.test', locale: () => locale);

      await client.get('/api/network/stations/CEN');
      locale = 'si';
      await client.get('/api/network/stations/CEN');

      expect(adapter.sent[1].headers.containsKey('If-None-Match'), isFalse);
      expect(client.cachedCopy('/api/network/stations/CEN')!.body, {'name': 'මධ්‍යම'});
    });
  });

  group('headers', () {
    test('sends the language, and a bearer token only when there is one', () async {
      final signedIn = build([json(200, {})], locale: 'si', token: 'abc.def.ghi');
      await signedIn.client.get('/x');
      expect(signedIn.adapter.sent.single.headers['Accept-Language'], 'si');
      expect(signedIn.adapter.sent.single.headers['Authorization'], 'Bearer abc.def.ghi');

      final anonymous = build([json(200, {})]);
      await anonymous.client.get('/x');
      expect(anonymous.adapter.sent.single.headers.containsKey('Authorization'), isFalse);
    });
  });

  group('logging (AC-4, REQ-SEC-001)', () {
    test('only the method, the path and the status are logged: no token, body, query or location', () async {
      final t = build([json(200, {'secret': 'body-value'}), json(500, {'title': 'x'})], token: 'tok-123');

      await t.client.get('/api/journeys/boards/CEN', query: {'lat': '6.9271', 'lng': '79.8612'});
      await t.client.post('/api/anything', data: {'password': 'p4ss'});

      expect(t.log, ['GET /api/journeys/boards/CEN -> 200', 'POST /api/anything -> 500']);
      final all = t.log.join('\n');
      for (final secret in ['tok-123', 'body-value', '6.9271', '79.8612', 'p4ss', 'Bearer']) {
        expect(all.contains(secret), isFalse, reason: 'the log must not contain $secret');
      }
    });
  });
}

// ---- reachability (AC-11) ----

class FakeProbe implements ApiProbe {
  bool reachable = true;
  int calls = 0;

  @override
  Future<bool> isReachable() async {
    calls++;
    return reachable;
  }
}

void reachabilityTests() {
  group('reachability (AC-11, REQ-NFR-006)', () {
    test('is online at start and offline after a failed check, remembering the last success', () async {
      final probe = FakeProbe();
      var now = DateTime.utc(2026, 10, 19, 8, 0);
      final container = ProviderContainer(overrides: [
        apiProbeProvider.overrideWithValue(probe),
        reachabilityClockProvider.overrideWithValue(() => now),
        reachabilityIntervalProvider.overrideWithValue(const Duration(hours: 1)),
      ]);
      addTearDown(container.dispose);

      final notifier = container.read(reachabilityProvider.notifier);
      expect(container.read(reachabilityProvider).online, isTrue);

      await pumpEventQueue();   // the first check starts by itself when the provider is first read
      expect(container.read(reachabilityProvider).lastSuccess, DateTime.utc(2026, 10, 19, 8, 0));

      now = DateTime.utc(2026, 10, 19, 8, 5);
      probe.reachable = false;
      await notifier.checkNow();
      final offline = container.read(reachabilityProvider);
      expect(offline.online, isFalse);
      expect(offline.lastSuccess, DateTime.utc(2026, 10, 19, 8, 0));   // when it last worked, not when it failed

      probe.reachable = true;
      await notifier.checkNow();
      expect(container.read(reachabilityProvider).online, isTrue);
    });

    test('checks again on its own interval', () async {
      final probe = FakeProbe();
      final container = ProviderContainer(overrides: [
        apiProbeProvider.overrideWithValue(probe),
        reachabilityIntervalProvider.overrideWithValue(const Duration(milliseconds: 20)),
      ]);
      addTearDown(container.dispose);

      container.read(reachabilityProvider);
      await Future<void>.delayed(const Duration(milliseconds: 110));

      expect(probe.calls, greaterThanOrEqualTo(3));
    });

    test('the default probe calls a cheap public read and is reachable when it answers', () async {
      final t = build([json(200, [])]);
      final probe = HttpApiProbe(t.client);

      expect(await probe.isReachable(), isTrue);
      expect(t.adapter.sent.single.path, '/api/network/routes');
    });

    test('the default probe is unreachable when the read fails', () async {
      final t = build([connectionError(), connectionError()]);

      expect(await HttpApiProbe(t.client).isReachable(), isFalse);
    });
  });
}
