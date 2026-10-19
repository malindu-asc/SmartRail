/// Where the API is. The base URL is set when the app is built or run:
/// `flutter run --dart-define=API_BASE_URL=https://api.example.org`.
abstract final class ApiConfig {
  /// The local development API (see the README of the repository).
  static const baseUrl = String.fromEnvironment('API_BASE_URL', defaultValue: 'http://localhost:5080');

  /// How long a read waits for the server.
  static const readTimeout = Duration(seconds: 10);
}
