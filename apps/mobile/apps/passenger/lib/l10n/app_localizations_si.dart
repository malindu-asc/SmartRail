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
  String get tabPlan => 'සැලසුම';

  @override
  String get tabTickets => 'ටිකට්පත්';

  @override
  String get tabAlerts => 'දැනුම්දීම්';

  @override
  String get tabMe => 'මම';

  @override
  String get onbSkip => 'මඟ හරින්න';

  @override
  String get onbNext => 'ඊළඟ';

  @override
  String get onbDone => 'යෙදුමට යන්න';

  @override
  String get onb1Title => 'පිටත් වීමට පෙර දැනගන්න';

  @override
  String get onb1Body =>
      'සෑම ස්ථානයකටම සජීවී පිටත්වීම්, ප්‍රමාද සහ වේදිකා වෙනස්කම්.';

  @override
  String get onb2Title => 'ඔබේ ගමන අනුගමනය කරන්න';

  @override
  String get onb2Body =>
      'ඔබේ වේදිකාව වෙනස් වූ විට හෝ දුම්රිය ප්‍රමාද වූ විට දැනුම්දීමක් ලබාගන්න.';

  @override
  String get onb3Title => 'පරණ දත්ත ගැන අවංකයි';

  @override
  String get onb3Body =>
      'වේලාවක් තහවුරු කළ නොහැකි විට අපි අනුමාන නොකර ඒ බව පවසමු.';

  @override
  String get greetingMorning => 'සුභ උදෑසනක්';

  @override
  String get greetingAfternoon => 'සුභ දහවලක්';

  @override
  String get greetingEvening => 'සුභ සන්ධ්‍යාවක්';

  @override
  String get homeWhereTo => 'අද කොහෙටද?';

  @override
  String get fromLabel => 'සිට';

  @override
  String get toLabel => 'දක්වා';

  @override
  String get chooseStation => 'ස්ථානයක් තෝරන්න';

  @override
  String get findTrains => 'දුම්රිය සොයන්න';

  @override
  String get homeFollowedStations => 'අනුගමනය කරන ස්ථාන';

  @override
  String get homeFollowEmptyTitle => 'තවම ස්ථාන අනුගමනය කර නැත';

  @override
  String get homeFollowEmptyBody =>
      'ස්ථානයක ඊළඟ දුම්රිය මෙහි බැලීමට එය අනුගමනය කරන්න.';

  @override
  String get homeFindStation => 'ස්ථානයක් සොයන්න';

  @override
  String get homeFollowedJourneys => 'අනුගමනය කරන ගමන්';

  @override
  String get stationsTitle => 'ස්ථාන';

  @override
  String get stationsSearchLabel => 'ස්ථානයේ නම හෝ කේතය';

  @override
  String get stationsSearchHint => 'උදාහරණ: Central හෝ CEN';

  @override
  String get stationsResults => 'ප්‍රතිඵල';

  @override
  String get stationsRecent => 'මෑත';

  @override
  String get stationsEmptyTitle => 'ගැළපෙන ස්ථාන නැත';

  @override
  String get stationsEmptyBody => 'නම හෝ කෙටි කේතය උත්සාහ කරන්න, උදාහරණ CEN.';

  @override
  String get stationsIntro => 'සජීවී පිටත්වීම් බැලීමට ස්ථානයක් සොයන්න.';

  @override
  String freshLive(String time) {
    return 'සජීවී · $time යාවත්කාලීන කළා';
  }

  @override
  String freshStale(String time) {
    return '$time වන විට';
  }

  @override
  String freshSaved(String time) {
    return '$time සුරැකිණි';
  }

  @override
  String get departuresToday => 'අද';

  @override
  String get departuresEmptyTitle => 'අදට තවත් දුම්රිය නැත';

  @override
  String get departuresEmptyBody =>
      'හෙට නැවත බලන්න, නැතහොත් වෙනත් ස්ථානයක් බලන්න.';

  @override
  String inMinutes(int n) {
    return 'මිනිත්තු $nකින්';
  }

  @override
  String platformN(String p) {
    return 'වේදිකාව $p';
  }

  @override
  String get platformTba => 'වේදිකාව පසුව දැනුම් දෙනු ලැබේ';

  @override
  String get chipOnTime => 'නියමිත වේලාවට';

  @override
  String chipDelayed(int n) {
    return '+මිනි. $n';
  }

  @override
  String get chipCancelled => 'අවලංගුයි';

  @override
  String get chipPlatformChanged => 'වේදිකාව වෙනස් විය';

  @override
  String get chipNoLiveData => 'සජීවී දත්ත නැත';

  @override
  String get scheduledOnly => 'කාලසටහන අනුව';

  @override
  String scheduledNoUpdate(String time) {
    return 'කාලසටහන අනුව · $time සිට යාවත්කාලීන නැත';
  }

  @override
  String expectedAt(String time) {
    return '$time අපේක්ෂිතයි';
  }

  @override
  String get followStation => 'ස්ථානය අනුගමනය කරන්න';

  @override
  String get followingStation => 'අනුගමනය කරයි';

  @override
  String get indicativeNote =>
      'වේලාවන් තොරතුරු සඳහා පමණි. ස්ථානයේ නිවේදන අසන්න.';

  @override
  String trainNumber(String n) {
    return 'දුම්රිය $n';
  }

  @override
  String journeyHeader(String time, String destination) {
    return '$time $destination වෙත';
  }

  @override
  String journeyTitle(String origin, String destination) {
    return '$origin සිට $destination දක්වා';
  }

  @override
  String get headlineRunningOnTime => 'ධාවනය වේ · නියමිත වේලාවට';

  @override
  String headlineDelayed(int n) {
    return 'ප්‍රමාදයි · +මිනි. $n';
  }

  @override
  String headlineNoLive(String time) {
    return '$time සිට සජීවී දත්ත නැත';
  }

  @override
  String get headlineNoLiveEver => 'තවම සජීවී දත්ත නැත';

  @override
  String get headlineCancelled => 'අවලංගුයි';

  @override
  String get headlineScheduled => 'කාලසටහන අනුව · තවම පිටත් වී නැත';

  @override
  String get headlineCompleted => 'ගමන අවසන්';

  @override
  String detailLive(String time) {
    return 'සජීවී · $time යාවත්කාලීන කළා';
  }

  @override
  String detailLiveDeparted(String updated, String station, String time) {
    return 'සජීවී · $updated යාවත්කාලීන කළා · $station සිට $time ට පිටත් විය';
  }

  @override
  String detailLastKnown(String station, String time) {
    return 'අවසන් දන්නා: $station සිට $time ට පිටත් විය';
  }

  @override
  String get detailNoLiveBody =>
      'කාලසටහන පෙන්වයි. වේලාවන් සජීවී නොවේ. ස්ථානයේ නිවේදන අසන්න.';

  @override
  String detailDelayedBody(String expected, String station, String scheduled) {
    return '$scheduled වෙනුවට $station හි $expected අපේක්ෂිතයි.';
  }

  @override
  String get detailCancelledBody =>
      'මෙම දුම්රිය ධාවනය නොවේ. වෙනත් දුම්රියක් බලන්න හෝ ඇමතුම් මධ්‍යස්ථානය අමතන්න.';

  @override
  String get detailScheduledBody =>
      'මෙම දුම්රිය තවම පිටත් වී නැත. වේලාවන් කාලසටහනට අනුවයි.';

  @override
  String platformBannerTitle(String p) {
    return 'වේදිකාව $p';
  }

  @override
  String platformBannerBody(String station) {
    return '$station හි වේදිකාව වෙනස් විය';
  }

  @override
  String wordDepartedOnTime(String time) {
    return '$time ට පිටත් විය · නියමිත වේලාවට';
  }

  @override
  String wordDepartedLate(String time, int n) {
    return '$time ට පිටත් විය · මිනිත්තු $n ප්‍රමාදයි';
  }

  @override
  String wordArrivedOnTime(String time) {
    return '$time ට පැමිණියේය · නියමිත වේලාවට';
  }

  @override
  String wordArrivedLate(String time, int n) {
    return '$time ට පැමිණියේය · මිනිත්තු $n ප්‍රමාදයි';
  }

  @override
  String wordArrivedEarly(String time, int n) {
    return '$time ට පැමිණියේය · මිනිත්තු $n කල් ඉක්මනින්';
  }

  @override
  String wordAtPlatform(String time) {
    return '$time සිට වේදිකාවේ';
  }

  @override
  String wordExpectedOnTime(String time) {
    return '$time අපේක්ෂිතයි · නියමිත වේලාවට';
  }

  @override
  String wordExpected(String time) {
    return '$time අපේක්ෂිතයි';
  }

  @override
  String get wordEstimate => 'ඇස්තමේන්තුවකි · වෙනස් විය හැක';

  @override
  String get wordScheduled => 'කාලසටහන අනුව';

  @override
  String get wordSkipped => 'නතර නොවේ';

  @override
  String estimatesNote(String station) {
    return '$station ට පසු වේලාවන් ඇස්තමේන්තු වන අතර වෙනස් විය හැක.';
  }

  @override
  String get followJourney => 'මෙම ගමන අනුගමනය කරන්න';

  @override
  String get followingJourney => 'අනුගමනය කරයි';

  @override
  String get snackFollowingJourney => 'ඔබ මෙම ගමන අනුගමනය කරයි';

  @override
  String get snackUnfollowedJourney => 'ඔබ මෙම ගමන අනුගමනය කිරීම නැවැත්වීය';

  @override
  String get snackFollowingStation => 'ඔබ මෙම ස්ථානය අනුගමනය කරයි';

  @override
  String get snackUnfollowedStation => 'ඔබ මෙම ස්ථානය අනුගමනය කිරීම නැවැත්වීය';

  @override
  String get undo => 'ආපසු';

  @override
  String get errorTitle => 'යම් දෙයක් වැරදුණා';

  @override
  String get errorNetwork =>
      'අපට SmartRail වෙත සම්බන්ධ විය නොහැකි විය. ඔබේ සම්බන්ධතාවය පරීක්ෂා කර නැවත උත්සාහ කරන්න.';

  @override
  String get errorNotFound =>
      'අපට එය සොයාගත නොහැකි විය. එය තවදුරටත් නොපවතින්නට පුළුවන.';

  @override
  String get errorGeneric => 'කරුණාකර මද වේලාවකින් නැවත උත්සාහ කරන්න.';

  @override
  String get retry => 'නැවත උත්සාහ කරන්න';

  @override
  String get comingTitlePlan => 'ගමනක් සැලසුම් කරන්න';

  @override
  String get comingBodyPlan =>
      'ස්ථාන දෙකක් තෝරා ඒ අතර දුම්රිය බලන්න, අවශ්‍ය නම් මාරුවක් සමඟ. මෙය ඊළඟ යාවත්කාලීනයේ පැමිණේ. ඒ වන තෙක් ඔබට ඕනෑම ස්ථානයක සජීවී පිටත්වීම් බැලිය හැක.';

  @override
  String get comingTitleTickets => 'ටිකට්පත්';

  @override
  String get comingBodyTickets =>
      'ඔබේ ටිකට්පත් මෙහි තිබෙනු ඇත, සම්බන්ධතාවයක් නැතිව ද ඒවා ක්‍රියා කරයි. මෙය ඉදිරි යාවත්කාලීනයක පැමිණේ.';

  @override
  String get comingTitleAlerts => 'දැනුම්දීම්';

  @override
  String get comingBodyAlerts =>
      'ඔබ අනුගමනය කරන ගමන් සහ ස්ථාන සඳහා වේදිකා වෙනස්කම්, ප්‍රමාද සහ අවලංගු කිරීම් මෙහි පෙන්වයි. මෙය ඉදිරි යාවත්කාලීනයක පැමිණේ.';

  @override
  String get comingSoon => 'ඉක්මනින් එයි';

  @override
  String get comingSeeDepartures => 'සජීවී පිටත්වීම් බලන්න';

  @override
  String get meTitle => 'මම';

  @override
  String get language => 'භාෂාව';

  @override
  String get languageEnglish => 'English';

  @override
  String get languageSinhala => 'සිංහල';

  @override
  String get meAbout =>
      'SmartRail සජීවී දුම්රිය තොරතුරු පෙන්වයි. වේලාවන් තොරතුරු සඳහා පමණි.';

  @override
  String get notFoundTitle => 'පිටුව හමු නොවීය';

  @override
  String get notFoundBody => 'එම පිටුව නොපවතී.';

  @override
  String get notFoundAction => 'මුල් පිටුවට යන්න';

  @override
  String get offlineTitle => 'සම්බන්ධතාවයක් නැත';

  @override
  String offlineDetail(String time) {
    return '$time ට ලැබුණු අවසාන තොරතුරු පෙන්වමින්';
  }

  @override
  String get offlineNever => 'තවමත් තොරතුරු ලැබී නැත.';

  @override
  String get planTitle => 'ගමනක් සැලසුම් කරන්න';

  @override
  String get planLeaveNow => 'දැන් පිටත් වන්න';

  @override
  String planLeaveAt(String time) {
    return '$time ට පිටත් වන්න';
  }

  @override
  String get planPickTime => 'වේලාවක් තෝරන්න';

  @override
  String get planSearch => 'දුම්රිය පෙන්වන්න';

  @override
  String resultsTitle(String from, String to) {
    return '$from සිට $to දක්වා';
  }

  @override
  String get resultsDirect => 'සෘජු';

  @override
  String get resultsChange => 'මාරුවක් 1';

  @override
  String resultsDuration(int n) {
    return 'මිනිත්තු $n';
  }

  @override
  String resultsChangeAt(String station) {
    return '$station හිදී මාරු වන්න';
  }

  @override
  String resultsWait(int n) {
    return 'මාරු වීමට මිනිත්තු $n';
  }

  @override
  String get resultsAtRiskTitle => 'සම්බන්ධතාවය අවදානමේ';

  @override
  String resultsAtRiskBody(int n, String station) {
    return '$station හිදී මාරු වීමට ඇත්තේ මිනිත්තු $n ක් පමණි. දෙවන දුම්රිය මග හැරිය හැක.';
  }

  @override
  String resultsTrainLeg(String number, String from, String to) {
    return 'දුම්රිය $number · $from සිට $to දක්වා';
  }

  @override
  String resultsArrives(String time) {
    return '$time ට ළඟා වේ';
  }

  @override
  String get resultsEmptyTitle => 'දුම්රිය හමු නොවීය';

  @override
  String get resultsEmptyBody =>
      'ඉදිරි පැය කිහිපයේ සෘජු හෝ එක් මාරුවක් සහිත දුම්රිය නැත. පසුව වේලාවක් උත්සාහ කරන්න.';

  @override
  String get resultsLater => 'පසුව උත්සාහ කරන්න';

  @override
  String get loginTitle => 'ඇතුල් වන්න';

  @override
  String get loginWelcome => 'නැවත සාදරයෙන් පිළිගනිමු';

  @override
  String get emailLabel => 'විද්‍යුත් තැපෑල';

  @override
  String get emailHint => 'ඔබේ විද්‍යුත් තැපෑල ඇතුළත් කරන්න';

  @override
  String get passwordLabel => 'මුරපදය';

  @override
  String get passwordHint => 'ඔබේ මුරපදය ඇතුළත් කරන්න';

  @override
  String get forgotPassword => 'මුරපදය අමතකද?';

  @override
  String get forgotSent =>
      'එම විද්‍යුත් තැපෑලට ගිණුමක් ඇත්නම්, මුරපදය යළි සැකසීමට සබැඳියක් එවා ඇත.';

  @override
  String get loginNoAccountNeeded =>
      'පිටත්වීම්, ප්‍රමාද සහ වේදිකා වෙනස්කම් බැලීමට ගිණුමක් අවශ්‍ය නැත.';

  @override
  String get loginNew => 'අලුත්ද?';

  @override
  String get loginCreate => 'පැතිකඩ සාදන්න';

  @override
  String get loginBadCredentials =>
      'එම විද්‍යුත් තැපෑල සහ මුරපදය නොගැළපේ, නැතහොත් ගිණුම මිනිත්තු කිහිපයකට අගුළු දමා ඇත.';

  @override
  String get createTitle => 'පැතිකඩ සාදන්න';

  @override
  String get fullNameLabel => 'සම්පූර්ණ නම';

  @override
  String get fullNameHint => 'ජේන් පෙරේරා';

  @override
  String get passwordNewHint => 'අවම වශයෙන් අක්ෂර 8ක්';

  @override
  String get agreeTerms => 'මම නියම සහ පෞද්ගලිකත්ව දැන්වීමට එකඟ වෙමි.';

  @override
  String get errName => 'ඔබේ සම්පූර්ණ නම ඇතුළත් කරන්න.';

  @override
  String get errEmail => 'වලංගු විද්‍යුත් තැපැල් ලිපිනයක් ඇතුළත් කරන්න.';

  @override
  String get errPassword => 'මුරපදයට අවම වශයෙන් අක්ෂර 8ක් අවශ්‍යයි.';

  @override
  String get errTerms => 'ඉදිරියට යාමට කරුණාකර එකඟ වන්න.';

  @override
  String get errAccountExists =>
      'මෙම විද්‍යුත් තැපෑලට ගිණුමක් දැනටමත් ඇත. ඇතුල් වීමට උත්සාහ කරන්න.';

  @override
  String get meLogIn => 'ඇතුල් වන්න හෝ පැතිකඩ සාදන්න';

  @override
  String get meLogInBody =>
      'ටිකට් සඳහා අවශ්‍යයි. දුම්රිය බැලීමට ගිණුමක් කිසිවිටෙක අවශ්‍ය නැත.';

  @override
  String get meTextSize => 'අකුරු ප්‍රමාණය';

  @override
  String get textSizeNormal => 'සාමාන්‍ය';

  @override
  String get textSizeLarge => 'විශාල';

  @override
  String get textSizeLarger => 'වඩා විශාල';

  @override
  String get meNotifications => 'දැනුම්දීම්';

  @override
  String get stateOn => 'සක්‍රීය';

  @override
  String get stateOff => 'අක්‍රීය';

  @override
  String get meDownloadData => 'මගේ දත්ත බාගන්න';

  @override
  String get meDeleteData => 'මගේ දත්ත මකන්න';

  @override
  String get meCallCentre => 'ඇමතුම් මධ්‍යස්ථානය අමතන්න';

  @override
  String get meSignOut => 'ඉවත් වන්න';

  @override
  String get downloadTitle => 'ඔබේ දත්ත';

  @override
  String get downloadCopy => 'පිටපත් කරන්න';

  @override
  String get downloadCopied => 'පසුරු පුවරුවට පිටපත් කළා';

  @override
  String get downloadFailed => 'ඔබේ දත්ත ලබා ගත නොහැකි විය. නැවත උත්සාහ කරන්න.';

  @override
  String get deleteTitle => 'ඔබේ දත්ත මකන්නද?';

  @override
  String get deleteBody =>
      'මෙය ආපසු හැරවිය නොහැක. ඔබේ නම, විද්‍යුත් තැපෑල, සුරැකි ස්ථාන සහ අනුගමනය කරන ගමන් අපි ඉවත් කරමු. ටිකට් සහ ගෙවීම් වාර්තා නීතියට අනුව තබා ගනිමු.';

  @override
  String get deleteConfirm => 'මගේ දත්ත මකන්න';

  @override
  String get cancel => 'අවලංගු කරන්න';

  @override
  String get deleteDone => 'ඔබේ දත්ත මකා දැමුවා.';

  @override
  String get callCentreTitle => 'ඇමතුම් මධ්‍යස්ථානය';

  @override
  String get callCentreBody =>
      'ටිකට් හෝ ගමන් පිළිබඳ ප්‍රශ්න සඳහා දුම්රිය සේවයේ ඇමතුම් මධ්‍යස්ථානය අමතන්න.';

  @override
  String callCentreNumber(String number) {
    return 'අංකය: $number';
  }

  @override
  String get callCentreNoNumber =>
      'දුම්රිය සේවය අංකය සකස් කළ පසු එය මෙහි පෙන්වයි.';

  @override
  String get close => 'වසන්න';

  @override
  String get ticketsTitle => 'ටිකට්';

  @override
  String get ticketsActive => 'සක්‍රීය';

  @override
  String get ticketsPast => 'පෙර';

  @override
  String get ticketsEmptyTitle => 'තවම කිසිවක් නැත';

  @override
  String get ticketsEmptyBody =>
      'ඔබට ටිකට් නැත. ටිකට් ගැනීමට ගමනක් සැලසුම් කරන්න.';

  @override
  String get ticketsBuy => 'ටිකට් ගන්න';

  @override
  String get ticketsAdd => 'ටිකට් එකතු කරන්න';

  @override
  String get ticketsSignInTitle => 'ඔබේ ටිකට් බැලීමට ඇතුල් වන්න';

  @override
  String get ticketsSignInBody =>
      'ටිකට් ඔබේ පැතිකඩට අයිතිය, එබැවින් දුරකථනය මාරු කළත් ආරක්ෂිතයි.';

  @override
  String ticketsRoute(String from, String to) {
    return '$from සිට $to දක්වා';
  }

  @override
  String ticketsWhen(String time) {
    return 'අද $time';
  }

  @override
  String ticketsWhenDate(String date, String time) {
    return '$date $time';
  }

  @override
  String ticketsCoachSeat(String coach, String seat) {
    return 'කෝච්චිය $coach · ආසනය $seat';
  }

  @override
  String ticketsCoachSeats(String coach, String seats) {
    return 'කෝච්චිය $coach · ආසන $seats';
  }

  @override
  String get ticketValid => 'වලංගුයි';

  @override
  String get ticketAffected => 'බලපෑමට ලක්ව ඇත';

  @override
  String get ticketPast => 'භාවිත කළා';

  @override
  String get ticketAffectedTitle => 'මෙම දුම්රිය අවලංගු කළා';

  @override
  String get ticketAffectedBody =>
      'දුම්රිය සේවය දුම්රිය අවලංගු කළ නිසා ඔබේ ටිකට් තවදුරටත් භාවිත කළ නොහැක. වෙනත් දුම්රියක් සොයන්න, නැතහොත් ඇමතුම් මධ්‍යස්ථානය අමතන්න.';

  @override
  String get ticketStatus => 'තත්ත්වය';

  @override
  String get ticketFindAnother => 'වෙනත් දුම්රියක් සොයන්න';

  @override
  String chooseTitle(String from, String to) {
    return '$from සිට $to දක්වා';
  }

  @override
  String chooseSummary(String who) {
    return 'අද · $who';
  }

  @override
  String travellersAdults(int n) {
    String _temp0 = intl.Intl.pluralLogic(
      n,
      locale: localeName,
      other: 'වැඩිහිටියන් $n',
      one: 'වැඩිහිටියෙක් 1',
    );
    return '$_temp0';
  }

  @override
  String travellersChildren(int n) {
    String _temp0 = intl.Intl.pluralLogic(
      n,
      locale: localeName,
      other: 'ළමයින් $n',
      one: 'ළමයෙක් 1',
    );
    return '$_temp0';
  }

  @override
  String travellersSeniors(int n) {
    String _temp0 = intl.Intl.pluralLogic(
      n,
      locale: localeName,
      other: 'ජ්‍යෙෂ්ඨයින් $n',
      one: 'ජ්‍යෙෂ්ඨයෙක් 1',
    );
    return '$_temp0';
  }

  @override
  String chooseDirect(int n) {
    return 'මිනිත්තු $n · සෘජු';
  }

  @override
  String get chooseSeatRequired => 'ආසනයක් අවශ්‍යයි';

  @override
  String get chooseNoSeat => 'ආසනයක් අවශ්‍ය නැත';

  @override
  String get chooseNoTrains => 'වෙන් කිරීමට දුම්රිය නැත';

  @override
  String get chooseNoTrainsBody =>
      'ඉදිරි පැය කිහිපයේ සෘජු දුම්රිය නැත. වෙනත් වේලාවක් හෝ ස්ථානයක් උත්සාහ කරන්න.';

  @override
  String get chooseNote =>
      'ප්‍රමාදයක් නිසා කිසිවිටෙක නව ටිකට් එකක් සෑදෙන්නේ නැත. ඔබේ ටිකට් එම දුම්රියේම පවතී.';

  @override
  String get chooseNoPayment =>
      'මෙම අනුවාදයේ ගෙවීමක් ගනු නොලැබේ. ගාස්තුව පෙන්වන්නේ තොරතුරු සඳහා පමණි.';

  @override
  String get continueLabel => 'ඉදිරියට';

  @override
  String get whoTitle => 'කවුද ගමන් කරන්නේ?';

  @override
  String get whoAdult => 'වැඩිහිටි';

  @override
  String get whoAdultHint => 'වයස 12 සහ ඉහළ';

  @override
  String get whoChild => 'ළමා';

  @override
  String get whoChildHint => 'වයස 5 සිට 11';

  @override
  String get whoSenior => 'ජ්‍යෙෂ්ඨ';

  @override
  String get whoSeniorHint => 'වයස 60 සහ ඉහළ';

  @override
  String get whoHelp => 'මට ස්ථානයේදී උදව් අවශ්‍යයි';

  @override
  String get whoHelpHint => 'කාර්ය මණ්ඩලයට දැනුම් දෙයි. මිලට බලපාන්නේ නැත.';

  @override
  String get whoConfirm => 'තහවුරු කරන්න';

  @override
  String get seatTitle => 'ආසනයක් තෝරන්න';

  @override
  String seatTitleMany(int n) {
    return 'ආසන $n ක් තෝරන්න';
  }

  @override
  String seatCoach(String c) {
    return 'කෝච්චිය $c';
  }

  @override
  String get seatFree => 'හිස්';

  @override
  String get seatTaken => 'ගෙන ඇත';

  @override
  String get seatYours => 'ඔබේ';

  @override
  String seatSelected(int n, int total) {
    return '$total න් $n ක් තෝරා ඇත';
  }

  @override
  String get seatConfirm => 'ආසනය තහවුරු කරන්න';

  @override
  String get seatConfirmMany => 'ආසන තහවුරු කරන්න';

  @override
  String seatLabelFree(int n) {
    return 'ආසනය $n, හිස්';
  }

  @override
  String seatLabelTaken(int n) {
    return 'ආසනය $n, ගෙන ඇත';
  }

  @override
  String seatLabelYours(int n) {
    return 'ආසනය $n, ඔබේ';
  }

  @override
  String get seatGone => 'කවුරුහරි ආසනයක් ගත්තා. නැවත තෝරන්න.';

  @override
  String get reviewTitle => 'සමාලෝචනය';

  @override
  String get reviewFare => 'ගාස්තුව';

  @override
  String get reviewTotal => 'මුළු';

  @override
  String get reviewConfirm => 'වෙන් කිරීම තහවුරු කරන්න';

  @override
  String reviewHold(String time) {
    return 'ආසන $time දක්වා රඳවා ඇත';
  }

  @override
  String get reviewExpired => 'ඔබේ ආසන නිදහස් විය. නැවත තෝරන්න.';

  @override
  String get ticketTitle => 'ටිකට්';

  @override
  String ticketOffline(String time) {
    return 'සබැඳියෙන් තොරව ක්‍රියා කරයි. SmartRail සමඟ අවසන් වරට $time ට පරීක්ෂා කළා.';
  }

  @override
  String ticketBookingNumber(String code) {
    return 'වෙන් කිරීමේ අංකය $code';
  }

  @override
  String get ticketBooked => 'ඔබේ වෙන් කිරීම තහවුරු කළා.';

  @override
  String get addTitle => 'ටිකට් එකතු කරන්න';

  @override
  String get addNumberLabel => 'වෙන් කිරීමේ හෝ ටිකට් අංකය';

  @override
  String get addNumberHint => 'ඔබේ වෙන් කිරීමේ අංකය ඇතුළත් කරන්න';

  @override
  String get addNumberHelp => 'අක්ෂර 8ක්. එය ඔබේ තහවුරු කිරීමේ ඇත.';

  @override
  String get addContactLabel => 'මිලදී ගැනීමේදී භාවිත කළ විද්‍යුත් තැපෑල';

  @override
  String get addContactHint => 'ඔබ භාවිත කළ විද්‍යුත් තැපෑල ඇතුළත් කරන්න';

  @override
  String get addNotFound => 'එම තොරතුරු සහිත ටිකට් එකක් සොයා ගත නොහැකි විය.';

  @override
  String get addDone => 'ටිකට් එකතු කළා';

  @override
  String get alertsTitle => 'දැනුම්දීම්';

  @override
  String get alertsToday => 'අද';

  @override
  String get alertsEarlier => 'කලින්';

  @override
  String get alertsNote =>
      'දැනුම්දීම් පෙන්වන්නේ ඔබ අනුගමනය කරන ගමන් සහ ස්ථාන සඳහා පමණි.';

  @override
  String get alertsOff => 'දැනුම්දීම් අක්‍රීයයි. Me හි ඒවා සක්‍රීය කරන්න.';

  @override
  String get alertsEmptyTitle => 'තවම දැනුම්දීම් නැත';

  @override
  String get alertsEmptyBody =>
      'ඔබ අනුගමනය කරන දුම්රියක් ප්‍රමාද වූ විට, අවලංගු වූ විට හෝ වේදිකාව මාරු වූ විට ඔබට එය මෙහි පෙනේ.';

  @override
  String get alertsClear => 'සියල්ල මකන්න';

  @override
  String get alertPlatformTitle => 'වේදිකාව වෙනස් විය';

  @override
  String alertPlatformBody(
    String origin,
    String destination,
    String now,
    String was,
  ) {
    return '$origin සිට $destination දක්වා දුම්රිය දැන් පිටත් වන්නේ $now වේදිකාවෙන් ($was නොවේ).';
  }

  @override
  String alertPlatformBodyNew(String origin, String destination, String now) {
    return '$origin සිට $destination දක්වා දුම්රිය දැන් පිටත් වන්නේ $now වේදිකාවෙන්.';
  }

  @override
  String get alertCancelledTitle => 'අවලංගු කළා';

  @override
  String alertCancelledBody(String time, String origin, String destination) {
    return '$time $origin සිට $destination දක්වා දුම්රිය අවලංගු කළා.';
  }

  @override
  String get alertDelayTitle => 'ප්‍රමාදය';

  @override
  String alertDelayBody(String origin, String destination, String time, int n) {
    return '$origin සිට $destination දක්වා දුම්රිය දැන් $time ට අපේක්ෂිතයි (+මිනිත්තු $n).';
  }

  @override
  String get alertCorrectionTitle => 'නිවැරදි කිරීම';

  @override
  String alertCorrectionBody(String time, String origin, String destination) {
    return '$time $origin සිට $destination දක්වා දුම්රිය ධාවනය වේ. කලින් පණිවිඩයක් එය අවලංගු කළා යැයි පැවසීය.';
  }

  @override
  String get disruptionTitle => 'බාධාව';

  @override
  String disruptionUpdated(String time) {
    return '$time ට යාවත්කාලීන කළා';
  }

  @override
  String get disruptionStations => 'බලපෑමට ලක් වූ ස්ථාන';

  @override
  String get disruptionJourneys => 'බලපෑමට ලක් වූ ගමන්';

  @override
  String get disruptionFollow => 'යාවත්කාලීන අනුගමනය කරන්න';

  @override
  String get disruptionFollowed => 'ඔබ බලපෑමට ලක් වූ ස්ථාන අනුගමනය කරයි.';

  @override
  String get disruptionNetworkWide => 'මෙය මුළු ජාලයටම බලපායි.';

  @override
  String disruptionExpectedEnd(String time) {
    return 'මෙය $time වන විට පිරිසිදු වේ යැයි අපේක්ෂා කරමු.';
  }

  @override
  String get disruptionGone => 'මෙම බාධාව අවසන්';

  @override
  String get disruptionGoneBody =>
      'එය විසඳා ඇත හෝ ඉවත් කර ඇත. පිටත්වීමේ පුවරු දුම්රිය දැන් ඇති ලෙස පෙන්වයි.';

  @override
  String get severityMinor => 'සුළු';

  @override
  String get severityMajor => 'විශාල';

  @override
  String get severitySevere => 'බරපතළ';

  @override
  String foodTitle(String station) {
    return '$station හි ආහාර';
  }

  @override
  String get foodAll => 'සියල්ල';

  @override
  String get foodOrderNote =>
      'ඇණවුම් කිරීම සහ ගෙවීම කෑම සපයන්නා විසින් සිදු කරයි.';

  @override
  String get foodCall => 'ඇණවුම් කිරීමට අමතන්න';

  @override
  String get foodUnavailable => 'මෙනුව දැන් නොමැත';

  @override
  String get foodUnavailableBody =>
      'දුම්රිය සහ ටිකට් වලට බලපෑමක් නැත. මද වේලාවකින් නැවත උත්සාහ කරන්න.';

  @override
  String get foodNone => 'මෙම ස්ථානයේ ආහාර නැත';

  @override
  String get foodLink => 'මෙම ස්ථානයේ ආහාර';

  @override
  String foodArrive(String time) {
    return '$time ට ළඟා වේ';
  }

  @override
  String get planWhenTitle => 'දිනය සහ වේලාව';

  @override
  String get planLaterToday => 'අද පසුව';

  @override
  String get planTomorrow => 'හෙට';

  @override
  String planTomorrowAt(String time) {
    return 'හෙට $time';
  }

  @override
  String planTimeNote(String zone) {
    return 'කාලසටහන් දේශීය වේලාව ($zone) භාවිත කරයි.';
  }

  @override
  String get createdLogIn => 'ඔබේ පැතිකඩ සාදා ඇත. කරුණාකර ඇතුල් වන්න.';

  @override
  String get planDepart => 'පිටත් වීම';

  @override
  String get planArrive => 'ළඟා වීම';

  @override
  String get planRecent => 'මෑත';

  @override
  String get foodMenuHint => 'මෙනුව සහ මිල ගණන් · ඇණවුම් කිරීමට අමතන්න';

  @override
  String get resultsFilter => 'පෙරහන';

  @override
  String get resultsDirectOnly => 'සෘජු දුම්රිය පමණි';

  @override
  String get ticketsMoreActions => 'තවත් ක්‍රියා';

  @override
  String get ticketsAddWithCode => 'වෙන් කිරීමේ කේතයෙන් ටිකට් එකතු කරන්න';

  @override
  String get ticketsNonePast => 'ඔබ භාවිත කළ ටිකට් මෙහි පෙනේ.';

  @override
  String get ticketReference => 'ටිකට්';

  @override
  String get seatHoldNote => 'ඔබ තහවුරු කළ පසු ආසන මිනිත්තු 10ක් රඳවා ගනී.';
}
