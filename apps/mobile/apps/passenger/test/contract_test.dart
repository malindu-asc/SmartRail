import 'dart:convert';
import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:passenger_app/features/board/data/models/board_model.dart';
import 'package:passenger_app/features/disruptions/disruptions.dart';
import 'package:passenger_app/features/food/food.dart';
import 'package:passenger_app/features/journeys/data/models/journey_model.dart';
import 'package:passenger_app/features/plan/data/plan_data.dart';
import 'package:passenger_app/features/tickets/data/tickets_data.dart';
import 'package:passenger_app/features/tickets/domain/tickets_domain.dart';

/// Real answers of the running API, saved by the backend smoke test (test/fixtures). If the API's shape changes, or the app reads a
/// field the API does not send, these fail before a phone does.
Map<String, dynamic> fixture(String name) =>
    jsonDecode(File('test/fixtures/$name.json').readAsStringSync())
        as Map<String, dynamic>;

List<dynamic> fixtureList(String name) =>
    jsonDecode(File('test/fixtures/$name.json').readAsStringSync())
        as List<dynamic>;

final fetched = DateTime.utc(2026, 10, 20, 8);

void main() {
  test('the board answer is read', () {
    final board = BoardModel.fromJson(fixture('board'), fetchedAt: fetched);
    expect(board.stationCode, 'CEN');
    expect(board.items, isNotEmpty);
  });

  test('the journey answer is read', () {
    final journey = JourneyModel.fromJson(
      fixture('journey'),
      fetchedAt: fetched,
    );
    expect(journey.stops.length, greaterThan(1));
  });

  test('direct and one-change search answers are read', () {
    final direct = TripModel.fromJson(
      fixture('search_direct'),
      fetchedAt: fetched,
    );
    expect(direct.options, isNotEmpty);
    expect(direct.options.first.legs, isNotEmpty);

    final change = TripModel.fromJson(
      fixture('search_transfer'),
      fetchedAt: fetched,
    );
    expect(change.options, isNotEmpty);
  });

  test('offers, with and without seats, are read', () {
    final offers = TicketModel.offers(fixture('offers'));
    expect(offers.offers, isNotEmpty);
    expect(offers.offers.first.fare.total, greaterThan(0));

    final seat = TicketModel.offers(fixture('offers_seat'));
    expect(seat.offers.any((o) => o.seatRequired), isTrue);
  });

  test('the seat map is read, and a booked seat is taken for someone else', () {
    final before = TicketModel.seatMap(fixture('seatmap'));
    expect(before.coaches, ['A', 'B', 'C']);
    expect(before.seatsPerCoach, 28);

    final after = TicketModel.seatMap(fixture('seatmap_after'));
    expect(after.taken, contains(const Seat('B', 13)));
  });

  test('a booking, a ticket and its lists are read', () {
    final booked = TicketModel.detail(
      fixture('booking_seat'),
      fetchedAt: fetched,
    );
    expect(booked.summary.seats, ['B13', 'B14']);
    expect(booked.qrPayload, startsWith('SR1.'));
    expect(booked.travellers.needsHelp, isTrue);

    expect(
      TicketModel.detail(
        fixture('booking_free'),
        fetchedAt: fetched,
      ).summary.seats,
      isEmpty,
    );
    expect(
      TicketModel.detail(fixture('ticket'), fetchedAt: fetched).summary.state,
      TicketState.valid,
    );
    expect(
      TicketModel.detail(fixture('claim'), fetchedAt: fetched).bookingCode,
      hasLength(8),
    );

    expect(
      fixtureList('tickets_active').map((s) => TicketModel.summary(s as Map)),
      isNotEmpty,
    );
    expect(fixtureList('tickets_past'), isEmpty);
  });

  test('a seat hold is read', () {
    final hold = fixture('hold');
    expect(hold['holdId'], isA<String>());
    expect(DateTime.parse(hold['expiresAtUtc'] as String), isA<DateTime>());
  });

  test('disruptions are read', () {
    expect(fixtureList('disruptions_empty'), isEmpty);
    final list = fixtureList(
      'disruptions',
    ).map((d) => DisruptionModel.disruption(d as Map)).toList();
    expect(list.single.severity, DisruptionSeverity.major);
    expect(
      list.single.stations.map((s) => s.code),
      containsAll(['MIL', 'OAK']),
    );

    final detail = DisruptionModel.detail(fixture('disruption'));
    expect(detail.disruption.title, 'Signal fault at Mill Lane');
  });

  test('the station menu is read', () {
    final menu = fixture('menu');
    expect(menu['categories'], isNotEmpty);
    expect(menu['orderPhone'], isA<String>());
    expect(menu['currency'], 'LKR');
  });

  test('the profile and the data export have the fields the app shows', () {
    final me = fixture('me');
    expect(me['fullName'], 'Smoke Test');
    expect(me['email'], isA<String>());
    expect(fixture('export').keys, containsAll(['profile', 'data']));
  });

  test('the menu repository model reads the real menu', () async {
    final menu = fixture('menu');
    expect(
      Menu(
        stationCode: menu['stationCode'] as String,
        currency: menu['currency'] as String,
        orderPhone: menu['orderPhone'] as String,
        categories: const [],
      ).price(180),
      'LKR 180',
    );
  });
}
