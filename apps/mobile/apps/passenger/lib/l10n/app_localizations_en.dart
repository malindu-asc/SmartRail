// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appTitle => 'SmartRail';

  @override
  String get tabHome => 'Home';

  @override
  String get tabTrips => 'Trips';

  @override
  String get tabAccount => 'Account';

  @override
  String get homeTitle => 'Departures';

  @override
  String get homeBody => 'Live departures and journey status will appear here.';

  @override
  String get tripsTitle => 'My trips';

  @override
  String get tripsBody => 'Journeys you follow will appear here.';

  @override
  String get accountTitle => 'Account';

  @override
  String get accountBody => 'Sign in to buy and show tickets.';

  @override
  String get language => 'Language';

  @override
  String get languageEnglish => 'English';

  @override
  String get languageSinhala => 'සිංහල';

  @override
  String get notFoundTitle => 'Page not found';

  @override
  String get notFoundBody => 'That page does not exist.';

  @override
  String get notFoundAction => 'Go to departures';

  @override
  String get offlineTitle => 'No connection';

  @override
  String offlineDetail(String time) {
    return 'Showing the last information from $time';
  }

  @override
  String get offlineNever => 'No information has been received yet.';
}
