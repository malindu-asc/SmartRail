import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_core/smartrail_core.dart';

/// Train times are shown in the network time zone, never the phone's (REQ-NFR-005).
final networkClockProvider = Provider<NetworkClock>(
  (ref) => NetworkClock.colombo(),
);

/// The clock for a zone an API answer names (`timeZone`). Cheap to ask for; the zone data is loaded once.
final _clocks = <String, NetworkClock>{};

NetworkClock clockFor(String zoneId) =>
    _clocks.putIfAbsent(zoneId, () => NetworkClock(zoneId));

/// The current time, so screens can say "in 7 min" and tests can fix it.
final nowProvider = Provider<DateTime Function()>((ref) => DateTime.now);
