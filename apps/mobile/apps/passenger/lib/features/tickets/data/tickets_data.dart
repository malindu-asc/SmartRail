import 'dart:convert';

import 'package:fpdart/fpdart.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';
import 'package:smartrail_core/smartrail_core.dart';

import '../../../core/storage/key_value_store.dart';
import '../domain/tickets_domain.dart';

abstract final class TicketModel {
  static DateTime _time(Object? v) => DateTime.parse(v! as String).toUtc();

  static Fare _fare(Map f) => Fare(
    currency: f['currency']! as String,
    total: f['total']! as num,
    lines: [
      for (final l in f['lines']! as List)
        FareLine(
          category: (l as Map)['category']! as String,
          count: l['count']! as int,
          each: l['each']! as num,
          total: l['total']! as num,
        ),
    ],
  );

  static OffersResult offers(Map json) => OffersResult(
    timeZone: json['timeZone']! as String,
    fromName: json['fromName']! as String,
    toName: json['toName']! as String,
    offers: [
      for (final o in json['offers']! as List)
        TicketOffer(
          journeyId: (o as Map)['journeyId']! as String,
          trainNumber: o['trainNumber']! as String,
          fromCode: o['fromCode']! as String,
          toCode: o['toCode']! as String,
          departure: _time(o['departureUtc']),
          expectedDeparture: _time(o['expectedDepartureUtc']),
          arrival: _time(o['arrivalUtc']),
          expectedArrival: _time(o['expectedArrivalUtc']),
          durationMinutes: o['durationMinutes']! as int,
          platform: o['platform'] as String?,
          status: o['status']! as String,
          confidence: o['confidence']! as String,
          delayMinutes: o['delayMinutes'] as int?,
          seatRequired: o['seatRequired']! as bool,
          fare: _fare(o['fare']! as Map),
        ),
    ],
  );

  static Set<Seat> _seats(Object? list) => {
    for (final s in list! as List)
      Seat((s as Map)['coach']! as String, s['number']! as int),
  };

  static SeatMap seatMap(Map json) => SeatMap(
    coaches: [for (final c in json['coaches']! as List) c as String],
    seatsPerCoach: json['seatsPerCoach']! as int,
    taken: _seats(json['taken']),
    yours: _seats(json['yours']),
  );

  static TicketSummary summary(Map s) => TicketSummary(
    id: s['id']! as String,
    reference: s['reference']! as String,
    fromCode: s['fromCode']! as String,
    fromName: s['fromName']! as String,
    toCode: s['toCode']! as String,
    toName: s['toName']! as String,
    departure: _time(s['departureUtc']),
    arrival: _time(s['arrivalUtc']),
    timeZone: s['timeZone']! as String,
    state: switch (s['state']) {
      'Affected' => TicketState.affected,
      'Past' => TicketState.past,
      _ => TicketState.valid,
    },
    passengers: s['passengers']! as int,
    seats: [for (final x in s['seats']! as List) x as String],
  );

  static TicketDetail detail(
    Map d, {
    required DateTime fetchedAt,
    bool fromCache = false,
  }) => TicketDetail(
    summary: summary(d['summary']! as Map),
    bookingCode: d['bookingCode']! as String,
    trainNumber: d['trainNumber']! as String,
    journeyId: d['journeyId']! as String,
    travellers: Travellers(
      adults: d['adults']! as int,
      children: d['children']! as int,
      seniors: d['seniors']! as int,
      needsHelp: d['needsHelp']! as bool,
    ),
    currency: d['currency']! as String,
    fareTotal: d['fareTotal']! as num,
    qrPayload: d['qrPayload']! as String,
    fetchedAt: fetchedAt,
    fromCache: fromCache,
  );
}

class RemoteTicketsRepository implements TicketsRepository {
  RemoteTicketsRepository(this._client, this._store, {DateTime Function()? now})
    : _now = now ?? DateTime.now;

  final ApiClient _client;
  final KeyValueStore _store;
  final DateTime Function() _now;

  Either<Failure, T> _read<T>(ApiResponse response, T Function(Map) parse) {
    final body = response.body;
    if (body is! Map) return left(const UnknownFailure('tickets.bad_response'));
    try {
      return right(parse(body));
    } on Object {
      return left(const UnknownFailure('tickets.bad_response'));
    }
  }

  @override
  Future<Either<Failure, OffersResult>> offers(OfferQuery query) async {
    final result = await _client.get(
      '/api/ticketing/offers',
      query: {
        'from': query.from,
        'to': query.to,
        if (query.after != null)
          'after': query.after!.toUtc().toIso8601String(),
        'adults': query.travellers.adults,
        'children': query.travellers.children,
        'seniors': query.travellers.seniors,
      },
    );
    return result.flatMap((r) => _read(r, TicketModel.offers));
  }

  @override
  Future<Either<Failure, SeatMap>> seatMap({
    required String journeyId,
    required String from,
    required String to,
  }) async {
    final result = await _client.get(
      '/api/ticketing/journeys/$journeyId/seats',
      query: {'from': from, 'to': to},
    );
    return result.flatMap((r) => _read(r, TicketModel.seatMap));
  }

  @override
  Future<Either<Failure, SeatHold>> holdSeats({
    required String journeyId,
    required String from,
    required String to,
    required List<Seat> seats,
  }) async {
    final result = await _client.post(
      '/api/ticketing/seat-holds',
      data: {
        'journeyId': journeyId,
        'from': from,
        'to': to,
        'seats': [
          for (final s in seats) {'coach': s.coach, 'number': s.number},
        ],
      },
    );
    return result.flatMap(
      (r) => _read(
        r,
        (b) => SeatHold(
          id: b['holdId']! as String,
          expiresAt: DateTime.parse(b['expiresAtUtc']! as String).toUtc(),
        ),
      ),
    );
  }

  @override
  Future<Either<Failure, TicketDetail>> book({
    required String journeyId,
    required String from,
    required String to,
    required Travellers travellers,
    String? holdId,
  }) async {
    final result = await _client.post(
      '/api/ticketing/bookings',
      data: {
        'journeyId': journeyId,
        'from': from,
        'to': to,
        'adults': travellers.adults,
        'children': travellers.children,
        'seniors': travellers.seniors,
        'needsHelp': travellers.needsHelp,
        'holdId': holdId,
      },
    );
    return result.flatMap(
      (r) => _read(
        r,
        (b) => _keep(TicketModel.detail(b, fetchedAt: r.fetchedAt), b),
      ),
    );
  }

  @override
  Future<Either<Failure, List<TicketSummary>>> list({
    required bool past,
  }) async {
    final path = '/api/ticketing/tickets';
    final query = {'scope': past ? 'past' : 'active'};
    final result = await _client.get(path, query: query);

    return result.match(
      (failure) {
        // Offline: the list as last seen on this phone.
        final saved = _store.getString('tickets_${past ? 'past' : 'active'}');
        if (saved == null) return left(failure);
        try {
          return right([
            for (final s in jsonDecode(saved) as List)
              TicketModel.summary(s as Map),
          ]);
        } on Object {
          return left(failure);
        }
      },
      (response) {
        final body = response.body;
        if (body is! List) {
          return left(const UnknownFailure('tickets.bad_response'));
        }
        try {
          _store.setString(
            'tickets_${past ? 'past' : 'active'}',
            jsonEncode(body),
          );
          return right([for (final s in body) TicketModel.summary(s as Map)]);
        } on Object {
          return left(const UnknownFailure('tickets.bad_response'));
        }
      },
    );
  }

  @override
  Future<Either<Failure, TicketDetail>> detail(String id) async {
    final result = await _client.get('/api/ticketing/tickets/$id');

    return result.match(
      (failure) {
        // The ticket still works offline: show the copy kept when it was last checked with SmartRail.
        final saved = _store.getString('ticket_$id');
        if (saved == null || failure is! NetworkFailure) return left(failure);
        try {
          final json = jsonDecode(saved) as Map;
          return right(
            TicketModel.detail(
              json['body']! as Map,
              fetchedAt: DateTime.parse(json['at']! as String),
              fromCache: true,
            ),
          );
        } on Object {
          return left(failure);
        }
      },
      (response) => _read(
        response,
        (b) => _keep(TicketModel.detail(b, fetchedAt: response.fetchedAt), b),
      ),
    );
  }

  @override
  Future<Either<Failure, TicketDetail>> claim({
    required String bookingCode,
    required String contact,
  }) async {
    final result = await _client.post(
      '/api/ticketing/tickets/claim',
      data: {'bookingCode': bookingCode, 'contact': contact},
    );
    return result.flatMap(
      (r) => _read(
        r,
        (b) => _keep(TicketModel.detail(b, fetchedAt: r.fetchedAt), b),
      ),
    );
  }

  TicketDetail _keep(TicketDetail detail, Map body) {
    _store.setString(
      'ticket_${detail.summary.id}',
      jsonEncode({'at': _now().toUtc().toIso8601String(), 'body': body}),
    );
    return detail;
  }
}
