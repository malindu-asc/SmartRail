import 'dart:convert';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/storage/key_value_store.dart';
import '../../../stations/domain/entities/station.dart';

/// A trip that was searched: shown under "Recent" on Plan so it can be searched again in one tap.
class RecentTrip {
  const RecentTrip({required this.from, required this.to});

  final Station from;
  final Station to;

  String get key => '${from.code}>${to.code}';

  Map<String, Object?> toJson() => {'from': from.toJson(), 'to': to.toJson()};

  static RecentTrip? fromJson(Map<String, Object?> json) {
    try {
      return RecentTrip(
        from: Station.fromSaved((json['from']! as Map).cast<String, Object?>()),
        to: Station.fromSaved((json['to']! as Map).cast<String, Object?>()),
      );
    } on Object {
      return null;
    }
  }
}

final recentTripsProvider =
    NotifierProvider<RecentTripsNotifier, List<RecentTrip>>(
      RecentTripsNotifier.new,
    );

class RecentTripsNotifier extends Notifier<List<RecentTrip>> {
  static const _key = 'recent_trips';
  static const limit = 5;

  @override
  List<RecentTrip> build() {
    final saved = ref.read(keyValueStoreProvider).getString(_key);
    if (saved == null) return const [];
    try {
      return [
        for (final j in jsonDecode(saved) as List)
          ?RecentTrip.fromJson((j as Map).cast<String, Object?>()),
      ];
    } on Object {
      return const [];
    }
  }

  /// Newest first, no repeats, at most five.
  Future<void> add(Station from, Station to) async {
    final trip = RecentTrip(from: from, to: to);
    state = [
      trip,
      ...state.where((t) => t.key != trip.key),
    ].take(limit).toList();
    await ref
        .read(keyValueStoreProvider)
        .setString(_key, jsonEncode([for (final t in state) t.toJson()]));
  }
}
