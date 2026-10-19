// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Sinhala Sinhalese (`si`).
class AppLocalizationsSi extends AppLocalizations {
  AppLocalizationsSi([String locale = 'si']) : super(locale);

  @override
  String get appTitle => 'SmartRail';

  @override
  String get tabHome => 'මුල් පිටුව';

  @override
  String get tabTrips => 'ගමන්';

  @override
  String get tabAccount => 'ගිණුම';

  @override
  String get homeTitle => 'පිටත්වීම්';

  @override
  String get homeBody => 'සජීවී පිටත්වීම් සහ ගමන් තත්ත්වය මෙහි පෙන්වනු ඇත.';

  @override
  String get tripsTitle => 'මගේ ගමන්';

  @override
  String get tripsBody => 'ඔබ අනුගමනය කරන ගමන් මෙහි පෙන්වනු ඇත.';

  @override
  String get accountTitle => 'ගිණුම';

  @override
  String get accountBody => 'ටිකට්පත් මිලදී ගැනීමට සහ පෙන්වීමට පිවිසෙන්න.';

  @override
  String get language => 'භාෂාව';

  @override
  String get languageEnglish => 'English';

  @override
  String get languageSinhala => 'සිංහල';

  @override
  String get notFoundTitle => 'පිටුව හමු නොවීය';

  @override
  String get notFoundBody => 'එම පිටුව නොපවතී.';

  @override
  String get notFoundAction => 'පිටත්වීම් වෙත යන්න';

  @override
  String get offlineTitle => 'සම්බන්ධතාවයක් නැත';

  @override
  String offlineDetail(String time) {
    return '$time ට ලැබුණු අවසාන තොරතුරු පෙන්වමින්';
  }

  @override
  String get offlineNever => 'තවමත් තොරතුරු ලැබී නැත.';
}
