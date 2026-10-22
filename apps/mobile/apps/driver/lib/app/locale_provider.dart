import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// The languages of the driver app: English and Sinhala.
const supportedLanguages = [Locale('en'), Locale('si')];

/// Remembers the chosen language between launches.
abstract interface class LanguageStore {
  Future<Locale?> read();

  Future<void> write(Locale locale);
}

class SecureLanguageStore implements LanguageStore {
  SecureLanguageStore([FlutterSecureStorage? storage])
    : _storage = storage ?? const FlutterSecureStorage();

  static const _key = 'driver_language';

  final FlutterSecureStorage _storage;

  @override
  Future<Locale?> read() async {
    final code = await _storage.read(key: _key);
    return supportedLanguages.where((l) => l.languageCode == code).firstOrNull;
  }

  @override
  Future<void> write(Locale locale) =>
      _storage.write(key: _key, value: locale.languageCode);
}

final languageStoreProvider = Provider<LanguageStore>(
  (ref) => SecureLanguageStore(),
);

/// The language found at launch, read by `main` before the first frame. Null means the driver has not chosen one yet.
final initialLanguageProvider = Provider<Locale?>((ref) => null);

/// The language the app shows. Null until the driver chooses one on the first launch; then the app asks no more.
final localeProvider = NotifierProvider<LocaleNotifier, Locale?>(
  LocaleNotifier.new,
);

class LocaleNotifier extends Notifier<Locale?> {
  @override
  Locale? build() => ref.read(initialLanguageProvider);

  void select(Locale locale) {
    if (!supportedLanguages.contains(locale)) return;
    state = locale;
    ref.read(languageStoreProvider).write(locale);
  }
}
