/// The paths of the driver app.
abstract final class AppRoutes {
  static const language = '/language';
  static const signIn = '/sign-in';
  static const duty = '/duty';
  static const messages = '/messages';
  static const me = '/me';

  /// A journey on the driver's duty. Opened from the duty list, over the tab bar.
  static String journey(String id) => '/journey/$id';
}
