import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_api_client/smartrail_api_client.dart';

import '../../../../core/storage/key_value_store.dart';
import '../../data/tickets_data.dart';
import '../../domain/tickets_domain.dart';

final ticketsRepositoryProvider = Provider<TicketsRepository>(
  (ref) => RemoteTicketsRepository(
    ref.watch(apiClientProvider),
    ref.watch(keyValueStoreProvider),
  ),
);

/// The trains that can be booked between two stations, with their fare for the travellers chosen.
final offersProvider = FutureProvider.autoDispose
    .family<OffersResult, OfferQuery>((ref, query) async {
      final result = await ref.read(ticketsRepositoryProvider).offers(query);
      return result.match((failure) => throw failure, (offers) => offers);
    });

typedef SeatMapKey = ({String journeyId, String from, String to});

final seatMapProvider = FutureProvider.autoDispose.family<SeatMap, SeatMapKey>((
  ref,
  key,
) async {
  final result = await ref
      .read(ticketsRepositoryProvider)
      .seatMap(journeyId: key.journeyId, from: key.from, to: key.to);
  return result.match((failure) => throw failure, (map) => map);
});

/// My tickets: active (valid, or affected by a cancellation) or past.
final ticketListProvider = FutureProvider.autoDispose
    .family<List<TicketSummary>, bool>((ref, past) async {
      final result = await ref.read(ticketsRepositoryProvider).list(past: past);
      return result.match((failure) => throw failure, (list) => list);
    });

final ticketDetailProvider = FutureProvider.autoDispose
    .family<TicketDetail, String>((ref, id) async {
      final result = await ref.read(ticketsRepositoryProvider).detail(id);
      return result.match((failure) => throw failure, (detail) => detail);
    });

/// The booking being made: the trip, the travellers, the train chosen and the seats held. One booking at a time; it starts over when
/// a new one starts.
class BookingDraft {
  const BookingDraft({
    this.query,
    this.offer,
    this.seats = const [],
    this.hold,
  });

  final OfferQuery? query;
  final TicketOffer? offer;
  final List<Seat> seats;
  final SeatHold? hold;

  Travellers get travellers => query?.travellers ?? const Travellers();

  BookingDraft copyWith({
    OfferQuery? query,
    TicketOffer? offer,
    List<Seat>? seats,
    SeatHold? hold,
    bool clearHold = false,
  }) => BookingDraft(
    query: query ?? this.query,
    offer: offer ?? this.offer,
    seats: seats ?? this.seats,
    hold: clearHold ? null : hold ?? this.hold,
  );
}

final bookingDraftProvider =
    NotifierProvider<BookingDraftNotifier, BookingDraft>(
      BookingDraftNotifier.new,
    );

class BookingDraftNotifier extends Notifier<BookingDraft> {
  @override
  BookingDraft build() => const BookingDraft();

  void start(OfferQuery query) => state = BookingDraft(query: query);

  void setTravellers(Travellers travellers) {
    final q = state.query;
    if (q == null) return;
    state = state.copyWith(
      query: OfferQuery(
        from: q.from,
        to: q.to,
        after: q.after,
        travellers: travellers,
      ),
      seats: const [],
      clearHold: true,
    );
  }

  void choose(TicketOffer offer) =>
      state = state.copyWith(offer: offer, seats: const [], clearHold: true);

  void setSeats(List<Seat> seats) =>
      state = state.copyWith(seats: seats, clearHold: true);

  void setHold(SeatHold hold) => state = state.copyWith(hold: hold);

  void reset() => state = const BookingDraft();
}
