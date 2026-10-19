import 'package:timezone/data/latest_10y.dart' as tzdata;
import 'package:timezone/timezone.dart' as tz;

/// Shows UTC instants in the network time zone (REQ-NFR-005).
///
/// The server sends UTC and names the zone in the response (`timeZone`, for example `Asia/Colombo`). The app never uses the phone's
/// own zone for train times: a traveller abroad still sees the times of the railway.
class NetworkClock {
  NetworkClock(String zoneId) : _zone = _locate(zoneId), zoneId = zoneId;

  /// The zone the demo network runs in.
  factory NetworkClock.colombo() => NetworkClock(defaultZoneId);

  static const defaultZoneId = 'Asia/Colombo';

  static bool _loaded = false;

  final tz.Location _zone;
  final String zoneId;

  static tz.Location _locate(String zoneId) {
    if (!_loaded) {
      tzdata.initializeTimeZones();
      _loaded = true;
    }
    return tz.getLocation(zoneId);
  }

  tz.TZDateTime local(DateTime instant) =>
      tz.TZDateTime.from(instant.toUtc(), _zone);

  /// `HH:mm` in the network zone, for example `14:25`.
  String formatTime(DateTime instant) {
    final t = local(instant);
    return '${_two(t.hour)}:${_two(t.minute)}';
  }

  /// The calendar date in the network zone as `yyyy-MM-dd`.
  String formatDate(DateTime instant) {
    final t = local(instant);
    return '${t.year.toString().padLeft(4, '0')}-${_two(t.month)}-${_two(t.day)}';
  }

  /// How many calendar days after the service date this instant falls in the network zone. A service that leaves at 23:50 and
  /// arrives at 00:35 belongs to the earlier service date, so the arrival is `1` day after it (REQ-LIVE-009).
  int daysAfterServiceDate(DateTime instant, String serviceDate) {
    final service = DateTime.parse(serviceDate);
    final t = local(instant);
    return DateTime.utc(
      t.year,
      t.month,
      t.day,
    ).difference(DateTime.utc(service.year, service.month, service.day)).inDays;
  }

  /// The time with a day marker when it is not on the service date, for example `00:35 +1`.
  String formatTimeForService(DateTime instant, String serviceDate) {
    final days = daysAfterServiceDate(instant, serviceDate);
    final time = formatTime(instant);
    return days == 0 ? time : '$time +$days';
  }

  static String _two(int value) => value.toString().padLeft(2, '0');
}
