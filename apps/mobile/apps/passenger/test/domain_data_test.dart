import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:passenger_app/core/storage/key_value_store.dart';
import 'package:passenger_app/features/board/data/models/board_model.dart';
import 'package:passenger_app/features/board/domain/entities/departure.dart';
import 'package:passenger_app/features/follow/data/local_follow_repository.dart';
import 'package:passenger_app/features/follow/domain/follow_repository.dart';
import 'package:passenger_app/features/journeys/data/models/journey_model.dart';
import 'package:passenger_app/features/journeys/domain/entities/journey_detail.dart';
import 'package:passenger_app/features/stations/data/repositories/station_repositories.dart';
import 'package:passenger_app/features/stations/domain/entities/station.dart';

import 'support/support.dart';

// The shapes below are copied from the real API answers (SPEC-005): GET /api/journeys/boards/{code} and GET /api/journeys/{id}.
const boardJson = '''
{"timeZone":"Asia/Colombo","asOfUtc":"2026-10-14T08:48:00Z","station":{"code":"CEN","name":"Central"},"items":[
 {"journeyId":"11111111-1111-1111-1111-111111111111","trainNumber":"1001","routeCode":"MAIN","destinationCode":"RIV","destinationName":"Riverside",
  "scheduledDepartureUtc":"2026-10-14T08:55:00Z","expectedDepartureUtc":"2026-10-14T09:07:00Z","estimateSource":"Live","delayMinutes":12,"punctuality":"MajorDelay",
  "platform":"5","platformState":"Changed","status":"Running","confidence":"Live","isStale":false,"ageSeconds":20}]}
''';

const journeyJson = '''
{"journeyId":"22222222-2222-2222-2222-222222222222","serviceDate":"2026-10-14","timeZone":"Asia/Colombo","trainNumber":"1001","routeCode":"MAIN","status":"Running",
 "confidence":"Live","origin":{"code":"CEN","name":"Central"},"destination":{"code":"RIV","name":"Riverside"},"delayMinutes":2,"punctuality":"MinorDelay","isStale":true,
 "ageSeconds":75,"asOfUtc":"2026-10-14T08:47:00Z","stops":[
 {"sequence":1,"stationCode":"CEN","stationName":"Central","state":"Departed","scheduledArrivalUtc":null,"scheduledDepartureUtc":"2026-10-14T08:55:00Z",
  "estimatedArrivalUtc":null,"estimatedDepartureUtc":null,"actualArrivalUtc":null,"actualDepartureUtc":"2026-10-14T08:57:00Z","estimateSource":"Confirmed","platform":"2","platformState":"Assigned"},
 {"sequence":2,"stationCode":"RIV","stationName":"Riverside","state":"Pending","scheduledArrivalUtc":"2026-10-14T09:17:00Z","scheduledDepartureUtc":null,
  "estimatedArrivalUtc":"2026-10-14T09:19:00Z","estimatedDepartureUtc":null,"actualArrivalUtc":null,"actualDepartureUtc":null,"estimateSource":"Live","platform":null,"platformState":"TBA"}]}
''';

void main() {
  group('the board answer is read as the API sends it', () {
    final snapshot = BoardModel.fromJson(
      jsonDecode(boardJson) as Map,
      fetchedAt: DateTime.utc(2026, 10, 14, 8, 48),
    );
    final first = snapshot.items.single;

    test('station, time zone, server time and the row', () {
      expect(snapshot.stationCode, 'CEN');
      expect(snapshot.stationName, 'Central');
      expect(snapshot.timeZone, 'Asia/Colombo');
      expect(snapshot.asOf, DateTime.utc(2026, 10, 14, 8, 48));
      expect(first.destinationName, 'Riverside');
      expect(first.scheduled, DateTime.utc(2026, 10, 14, 8, 55));
      expect(first.expected, DateTime.utc(2026, 10, 14, 9, 7));
      expect(first.platform, '5');
      expect(first.platformChanged, isTrue);
      expect(first.delayMinutes, 12);
    });

    test(
      'a delayed train with a changed platform is shown as delayed first',
      () {
        expect(first.state, DepartureState.delayed);
        expect(first.expectedDiffers, isTrue);
      },
    );
  });

  group('a departure is one state, in the order that matters', () {
    test(
      'cancelled beats everything, then delayed, platform changed, no live data, on time, scheduled',
      () {
        expect(
          dep(status: 'Cancelled', delay: 20, platformChanged: true).state,
          DepartureState.cancelled,
        );
        expect(
          dep(delay: 12, platformChanged: true).state,
          DepartureState.delayed,
        );
        expect(
          dep(delay: 0, platformChanged: true).state,
          DepartureState.platformChanged,
        );
        expect(dep(confidence: 'Unknown').state, DepartureState.noLiveData);
        expect(dep(delay: 0).state, DepartureState.onTime);
        expect(
          dep(status: 'Planned', confidence: 'Scheduled').state,
          DepartureState.scheduled,
        );
      },
    );

    test('a cancelled train shows no expected time and no live claim', () {
      final cancelled = dep(status: 'Cancelled', scheduled: 0, expected: 5);
      expect(cancelled.expectedDiffers, isFalse);
      expect(cancelled.hasNoLiveData, isFalse);
    });

    test('a train with no live data shows only its timetable', () {
      final unknown = dep(confidence: 'Unknown', scheduled: 0, expected: 0);
      expect(unknown.hasNoLiveData, isTrue);
      expect(unknown.expectedDiffers, isFalse);
    });
  });

  group('the journey answer is read as the API sends it', () {
    final detail = JourneyModel.fromJson(
      jsonDecode(journeyJson) as Map,
      fetchedAt: DateTime.utc(2026, 10, 14, 8, 48),
    );

    test('header, status, stops and times', () {
      expect(detail.trainNumber, '1001');
      expect(detail.originName, 'Central');
      expect(detail.destinationName, 'Riverside');
      expect(detail.status, 'Running');
      expect(detail.delayMinutes, 2);
      expect(detail.isStale, isTrue);
      expect(detail.asOf, DateTime.utc(2026, 10, 14, 8, 47));
      expect(detail.stops, hasLength(2));
      expect(
        detail.stops.first.actualDeparture,
        DateTime.utc(2026, 10, 14, 8, 57),
      );
      expect(
        detail.stops.last.estimatedArrival,
        DateTime.utc(2026, 10, 14, 9, 19),
      );
      expect(
        detail.stops.last.scheduledTime,
        DateTime.utc(2026, 10, 14, 9, 17),
      ); // the last stop shows its arrival
    });
  });

  group('how a journey is worded', () {
    test(
      'on time: running, with the platform banner on the stop that changed',
      () {
        final j = onTimeJourney();
        expect(j.headline, JourneyHeadline.runningOnTime);
        expect(j.platformBannerStop?.stationName, 'Oak Park');
        expect(j.wordingFor(j.stops[0]), StopWording.departedOnTime);
        expect(j.wordingFor(j.stops[1]), StopWording.expectedOnTime);
      },
    );

    test(
      'delayed: the late departures are worded as late, the next stop is expected, later ones are estimates',
      () {
        final j = delayedJourney();
        expect(j.headline, JourneyHeadline.delayed);
        expect(j.wordingFor(j.stops[0]), StopWording.departedLate);
        expect(j.wordingFor(j.stops[1]), StopWording.departedLate);
        expect(j.wordingFor(j.stops[2]), StopWording.expected);
        expect(j.wordingFor(j.stops[3]), StopWording.estimate);
      },
    );

    test(
      'no live data: every stop ahead is just the timetable and the last known stop is the one it left',
      () {
        final j = noLiveJourney();
        expect(j.headline, JourneyHeadline.noLiveData);
        expect(j.wordingFor(j.stops[1]), StopWording.scheduled);
        expect(j.wordingFor(j.stops[3]), StopWording.scheduled);
        expect(j.lastKnownStop?.stationName, 'Central');
      },
    );

    test('cancelled and not yet started', () {
      expect(
        journey([
          stop(1, 'CEN', 'Central', scheduledDep: 0),
          stop(2, 'RIV', 'Riverside', scheduledArr: 20),
        ], status: 'Cancelled').headline,
        JourneyHeadline.cancelled,
      );
      expect(
        journey(
          [
            stop(1, 'CEN', 'Central', scheduledDep: 0),
            stop(2, 'RIV', 'Riverside', scheduledArr: 20),
          ],
          status: 'Planned',
          confidence: 'Scheduled',
        ).headline,
        JourneyHeadline.scheduled,
      );
    });

    test(
      'a train 1 minute behind is on time (ASM-021 bands start at 2 minutes)',
      () {
        final j = journey([
          stop(
            1,
            'CEN',
            'Central',
            state: 'Departed',
            scheduledDep: 0,
            actualDep: 1,
            source: 'Confirmed',
          ),
          stop(
            2,
            'RIV',
            'Riverside',
            scheduledArr: 20,
            estArr: 21,
            source: 'Live',
          ),
        ], delay: 1);
        expect(j.headline, JourneyHeadline.runningOnTime);
        expect(j.wordingFor(j.stops[0]), StopWording.departedOnTime);
      },
    );
  });

  group('what stays on the phone', () {
    test(
      'recent stations: newest first, no duplicates, at most five',
      () async {
        final repo = LocalRecentStationsRepository(MemoryStore());
        for (final s in stations) {
          await repo.add(s);
        }
        await repo.add(stations[1]); // seen again: moves to the front

        final recent = repo.recent();
        expect(recent, hasLength(5));
        expect(recent.first.code, 'OAK');
        expect(recent.map((s) => s.code).toSet(), hasLength(5));
      },
    );

    test('a damaged saved value is the same as nothing saved', () {
      final store = MemoryStore({
        'recent_stations': 'not json',
        'followed_stations': '{',
        'followed_journeys': '[1,2',
      });
      expect(LocalRecentStationsRepository(store).recent(), isEmpty);
      expect(LocalFollowRepository(store).stations(), isEmpty);
      expect(LocalFollowRepository(store).journeys(), isEmpty);
    });

    test('follows survive a restart', () async {
      final store = MemoryStore();
      final first = LocalFollowRepository(store);
      await first.saveStations(const [
        FollowedStation(code: 'CEN', name: 'Central'),
      ]);
      await first.saveJourneys([
        FollowedJourney(
          journeyId: 'j1',
          trainNumber: '1001',
          originName: 'Central',
          destinationName: 'Riverside',
          scheduledDeparture: t1425,
        ),
      ]);

      final restarted = LocalFollowRepository(
        store,
      ); // a new repository over the same storage
      expect(restarted.stations().single.name, 'Central');
      expect(restarted.journeys().single.destinationName, 'Riverside');
      expect(restarted.journeys().single.scheduledDeparture, t1425);
    });

    test('a station keeps only its code and name when saved', () {
      expect(
        const Station(
          code: 'CEN',
          name: 'Central',
          facilities: ['toilets'],
        ).toJson(),
        {'code': 'CEN', 'name': 'Central'},
      );
    });
  });
}
