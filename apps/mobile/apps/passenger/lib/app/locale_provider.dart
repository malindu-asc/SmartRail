import 'package:flutter/widgets.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

/// The language the app shows: English or Sinhala (REQ-PAX-013). Tamil is added when it is designed.
final localeProvider = NotifierProvider<LocaleNotifier, Locale>(
  LocaleNotifier.new,
);

class LocaleNotifier extends Notifier<Locale> {
  static const supported = [Locale('en'), Locale('si')];

  @override
  Locale build() => const Locale('en');

  void select(Locale locale) {
    if (supported.contains(locale)) state = locale;
  }
}
