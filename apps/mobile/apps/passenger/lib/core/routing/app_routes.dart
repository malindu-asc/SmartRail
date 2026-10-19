/// The paths of the passenger app. Ids and codes go in the path, never whole objects.
abstract final class AppRoutes {
  static const launch = '/launch';
  static const onboarding = '/onboarding';

  static const home = '/home';
  static const plan = '/plan';
  static const tickets = '/tickets';
  static const alerts = '/alerts';
  static const me = '/me';
  static const notFound = '/not-found';

  /// Log in and Create profile open over the tabs. Only tickets need them (REQ-ACC, ASM-012).
  static const login = '/login';
  static const createProfile = '/create-profile';

  /// The station search. With `?pick=1` it returns the chosen station to the screen that asked (the From and To fields).
  static const search = '/home/search';
  static const searchPick = '/home/search?pick=1';

  /// The trains between two stations. `after` is a UTC instant; leave it out for "now".
  static String planResults(String from, String to, {DateTime? after}) => Uri(
    path: '/plan/results',
    queryParameters: {
      'from': from,
      'to': to,
      if (after != null) 'after': after.toUtc().toIso8601String(),
    },
  ).toString();

  /// The booking funnel opens over the tab bar (no escape routes that leave half a booking), see decision D2.
  static String chooseTicket(String from, String to, {DateTime? after}) => Uri(
    path: '/tickets/choose',
    queryParameters: {
      'from': from,
      'to': to,
      if (after != null) 'after': after.toUtc().toIso8601String(),
    },
  ).toString();

  static const seats = '/tickets/seats';
  static const review = '/tickets/review';

  static String ticket(String id, {bool created = false}) =>
      '/tickets/ticket/$id${created ? '?new=1' : ''}';

  static String station(String code) => '/home/station/$code';

  static String food(String code, {String? subtitle}) => Uri(
    path: '/home/station/$code/food',
    queryParameters: {'note': ?subtitle},
  ).toString();

  static String disruption(String id) => '/home/disruption/$id';

  static String journey(String id) => '/home/journey/$id';
}
