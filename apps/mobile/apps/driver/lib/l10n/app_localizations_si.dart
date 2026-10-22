// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for Sinhala Sinhalese (`si`).
class AppLocalizationsSi extends AppLocalizations {
  AppLocalizationsSi([String locale = 'si']) : super(locale);

  @override
  String get appTitle => 'SmartRail රියදුරු';

  @override
  String get retry => 'නැවත උත්සාහ කරන්න';

  @override
  String get refresh => 'නැවුම් කරන්න';

  @override
  String get cancel => 'අවලංගු කරන්න';

  @override
  String get close => 'වසන්න';

  @override
  String get errorTitle => 'මෙය පූරණය කළ නොහැකි විය';

  @override
  String get errorNetwork => 'ඔබේ සම්බන්ධතාවය පරීක්ෂා කර නැවත උත්සාහ කරන්න.';

  @override
  String get errorGeneric =>
      'යම් දෝෂයක් සිදු විය. මද වේලාවකින් නැවත උත්සාහ කරන්න.';

  @override
  String get errorForbidden =>
      'ඔබේ ගිණුමට රියදුරු භූමිකාවක් නැත. පරිපාලකගෙන් විමසන්න.';

  @override
  String get tabDuty => 'රාජකාරිය';

  @override
  String get tabMessages => 'දැනුම්දීම්';

  @override
  String get tabMe => 'මම';

  @override
  String syncedAt(Object time) {
    return 'සමමුහුර්ත $time';
  }

  @override
  String offlineSynced(Object time) {
    return 'සබැඳියෙන් පිටත · අවසන් සමමුහුර්තය $time';
  }

  @override
  String get offlineNever => 'සබැඳියෙන් පිටත';

  @override
  String get signInHello => 'නැවත සාදරයෙන් පිළිගනිමු';

  @override
  String get signInRole => 'රියදුරු';

  @override
  String get signInSubtitle =>
      'අද රාජකාරිය බැලීමට ඔබේ සංවිධාන ගිණුමෙන් ඇතුල් වන්න.';

  @override
  String get staffIdLabel => 'සේවක අංකය හෝ විද්‍යුත් තැපෑල';

  @override
  String get staffIdHint => 'name@railway.lk';

  @override
  String get passwordLabel => 'මුරපදය';

  @override
  String get passwordHint => 'ඔබේ මුරපදය';

  @override
  String get forgotPassword => 'මුරපදය අමතකද? පාලන මධ්‍යස්ථානය අමතන්න.';

  @override
  String get signInButton => 'ඇතුල් වන්න';

  @override
  String get safetyNote =>
      'ආරක්ෂාව පිළිබඳ පණිවිඩ දුම්රිය රේඩියෝ මගින් යයි. මෙම යෙදුම කිසිදා ආරක්ෂක නාලිකාව නොවේ.';

  @override
  String get signInBadCredentials =>
      'එම සේවක අංකය හෝ මුරපදය නිවැරදි නැත. පරීක්ෂා කර නැවත උත්සාහ කරන්න.';

  @override
  String get signInNotDriver => 'මෙය රියදුරු ගිණුමක් නොවේ.';

  @override
  String get dutyTitle => 'ඔබේ රාජකාරිය';

  @override
  String get dutyActive => 'සක්‍රිය';

  @override
  String get dutyPast => 'පසුගිය';

  @override
  String dutyRoster(Object from, Object to, Object unit) {
    return 'ඒකකය $unit · $from සිට $to දක්වා';
  }

  @override
  String dutyRosterNoUnit(Object from, Object to) {
    return '$from සිට $to දක්වා';
  }

  @override
  String signedOnChip(Object time) {
    return '$time ට රාජකාරියට ඇතුල් විය';
  }

  @override
  String get noticeFromControl => 'පාලන මධ්‍යස්ථානයෙන් පණිවිඩයක්';

  @override
  String get journeyNext => 'ඊළඟ';

  @override
  String get journeyLater => 'පසුව';

  @override
  String get journeyRunning => 'ධාවනය වේ';

  @override
  String get journeyDone => 'අවසන්';

  @override
  String journeyRoute(Object destination, Object origin) {
    return '$origin සිට $destination දක්වා';
  }

  @override
  String journeyCardLine(Object platform, Object stops, Object time) {
    return 'වේදිකාව $platform · නැවතුම් $stops · $time ට පැමිණේ';
  }

  @override
  String journeyCardNoPlatform(Object stops, Object time) {
    return 'නැවතුම් $stops · $time ට පැමිණේ';
  }

  @override
  String journeyCardDone(Object time) {
    return '$time ට පැමිණියේය';
  }

  @override
  String journeyCardDoneLate(Object minutes, Object time) {
    return '$time ට පැමිණියේය · මිනිත්තු $minutes ක් ප්‍රමාදයි';
  }

  @override
  String startJourney(Object time) {
    return '$time ගමන ආරම්භ කරන්න';
  }

  @override
  String get signOnButton => 'රාජකාරියට ඇතුල් වන්න';

  @override
  String get signOffButton => 'රාජකාරියෙන් ඉවත් වන්න';

  @override
  String dutyUpcoming(Object time) {
    return 'ඔබේ රාජකාරිය $time ට ආරම්භ වේ. මිනිත්තු 30කට පෙර සිට ඇතුල් විය හැක.';
  }

  @override
  String get dutyNoneTitle => 'රාජකාරියක් පවරා නැත';

  @override
  String get dutyNoneBody =>
      'අද ඔබේ රාජකාරි ලේඛනය තහවුරු කර ගැනීමට පාලන මධ්‍යස්ථානය අමතන්න.';

  @override
  String get dutyNotCurrentTitle => 'රාජකාරිය යාවත්කාලීන නැත';

  @override
  String get dutyNotCurrentBody => 'සංඥාව ලැබුණු විට අද රාජකාරිය පෙන්වයි.';

  @override
  String get dutyCantShowTitle => 'අද රාජකාරිය තවම පෙන්විය නොහැක';

  @override
  String get dutyCantShowBody =>
      'මෙය යාවත්කාලීන වන තුරු කඩදාසි රාජකාරි පත්‍රිකාව හෝ දුම්රිය රේඩියෝව භාවිතා කරන්න.';

  @override
  String get dutyHistorySub => 'ඔබේ රාජකාරි · පසුගිය දින 30';

  @override
  String get dutyHistoryEmpty => 'පසුගිය දින 30 තුළ රාජකාරි නැත.';

  @override
  String get historyNeedsReview => 'සමාලෝචනය අවශ්‍යයි';

  @override
  String get historySignedOff => 'ඉවත් විය';

  @override
  String historyLine(Object count) {
    return 'ගමන් $count';
  }

  @override
  String get signOffTitle => 'රාජකාරියෙන් ඉවත් වන්නද?';

  @override
  String get signOffBody => 'සියලු ගමන් අවසන්. යැවීමට කිසිවක් ඉතිරි නැත.';

  @override
  String get signOffBodyUnfinished =>
      'සමහර ගමන් අවසන් වී නැත. ඒවා පාලන මධ්‍යස්ථානය සතුව පවතී.';

  @override
  String get signOffConfirm => 'ඉවත් වන්න';

  @override
  String get staySignedOn => 'රාජකාරියේ සිටින්න';

  @override
  String journeyTitle(Object destination, Object time) {
    return '$destination වෙත $time';
  }

  @override
  String get helpButton => 'උදව්';

  @override
  String get nextStopLabel => 'ඊළඟ නැවතුම';

  @override
  String get atStopLabel => 'නැවතුමේ';

  @override
  String arrivingIn(Object minutes) {
    return 'මිනිත්තු $minutes කින් පමණ පැමිණේ';
  }

  @override
  String get arrivingNow => 'දැන් පැමිණේ';

  @override
  String departsAt(Object time) {
    return '$time ට පිටත් වේ';
  }

  @override
  String scheduledAt(Object time) {
    return 'නියමිත $time';
  }

  @override
  String arrivedDue(Object due, Object time) {
    return '$time ට පැමිණියේය · නියමිත $due';
  }

  @override
  String delayChip(Object minutes) {
    return '+මිනි. $minutes';
  }

  @override
  String platformLine(Object platform) {
    return 'වේදිකාව $platform';
  }

  @override
  String stopDeparted(Object time) {
    return '$time ට පිටත් විය';
  }

  @override
  String stopPlatformHere(Object platform) {
    return 'වේදිකාව $platform · දැන් මෙහි';
  }

  @override
  String get stopHere => 'දැන් මෙහි';

  @override
  String stopPlatformExpected(Object platform) {
    return 'වේදිකාව $platform · අපේක්ෂිතයි';
  }

  @override
  String get stopEstimate => 'ඇස්තමේන්තුව · වෙනස් විය හැක';

  @override
  String get stopSkipped => 'මග හැරියා';

  @override
  String get stopCancelled => 'අවලංගුයි';

  @override
  String arrivedAt(Object station) {
    return '$station වෙත පැමිණියා';
  }

  @override
  String departedFrom(Object station) {
    return '$station සිට පිටත් විය';
  }

  @override
  String eventArrived(Object station) {
    return '$station වෙත පැමිණියා';
  }

  @override
  String eventDeparted(Object station) {
    return '$station සිට පිටත් විය';
  }

  @override
  String eventSkipped(Object station) {
    return '$station මග හැරියා';
  }

  @override
  String eventSaved(Object event) {
    return '$event සුරකින ලදී';
  }

  @override
  String get delayReasonButton => 'ප්‍රමාදයේ හේතුව';

  @override
  String get actionSaved => 'සුරකින ලදී';

  @override
  String get notSignedOn => 'සිදුවීම් සටහන් කිරීමට ඔබේ රාජකාරියට ඇතුල් වන්න.';

  @override
  String get journeyNotMine => 'එම ගමන ඔබේ රාජකාරියේ නැත.';

  @override
  String get delayReasonTitle => 'අපි ප්‍රමාද වන්නේ ඇයි?';

  @override
  String get delayReasonBody =>
      'එක් හේතුවක් තෝරන්න. පාලන මධ්‍යස්ථානයට පසුව එය වෙනස් කළ හැක.';

  @override
  String get reasonSignalFault => 'සංඥා දෝෂය';

  @override
  String get reasonDoorFault => 'දොර දෝෂය';

  @override
  String get reasonCrew => 'කාර්ය මණ්ඩලය';

  @override
  String get reasonPassengerIncident => 'මගියෙකු සම්බන්ධ සිද්ධියක්';

  @override
  String get reasonTrack => 'ධාවන පථය';

  @override
  String get reasonOther => 'වෙනත්';

  @override
  String get saveReason => 'හේතුව සුරකින්න';

  @override
  String skipTitle(Object station) {
    return '$station හි නොනවතීද?';
  }

  @override
  String get skipBody =>
      'පාලන මධ්‍යස්ථානයට වහාම දැනුම් දෙයි. පාලන මධ්‍යස්ථානය තහවුරු කළ පසු හෝ දුම්රිය නොනවතා යන බව දුටු පසු පමණක් මගීන්ට දැනුම් දෙයි.';

  @override
  String get skipPlatformClosed => 'වේදිකාව වසා ඇත';

  @override
  String get skipInstructed => 'පාලන මධ්‍යස්ථානයේ උපදෙස්';

  @override
  String get skipSafety => 'ආරක්ෂාව';

  @override
  String get skipConfirm => 'මග හැරි නැවතුම වාර්තා කරන්න';

  @override
  String get mismatchLabel => 'සුරකින ලද්දේ නැත';

  @override
  String get mismatchTitle => 'එම නැවතුම නොගැලපේ';

  @override
  String mismatchBody(Object attempt, Object station) {
    return '$attempt සුරකින ලද්දේ නැත. දුම්රිය ඇත්තේ $station හිය. ගමනේ කිසිවක් වෙනස් වී නැත.';
  }

  @override
  String mismatchRecord(Object event) {
    return '$event සටහන් කරන්න';
  }

  @override
  String get mismatchHold => 'පාලනයට භාර දෙන්න';

  @override
  String get journeyCompleteTitle => 'ගමන අවසන්';

  @override
  String journeyCompleteBody(Object arrived, Object route, Object time) {
    return '$time $route · $arrived ට පැමිණියේය';
  }

  @override
  String journeyCompleteBodyLate(
    Object arrived,
    Object minutes,
    Object route,
    Object time,
  ) {
    return '$time $route · $arrived ට පැමිණියේය, මිනිත්තු $minutes ක් ප්‍රමාදයි';
  }

  @override
  String get completeEvents => 'යැවූ සිදුවීම්';

  @override
  String get completeReasons => 'ප්‍රමාද හේතු';

  @override
  String get completeAcks => 'පිළිගත් පණිවිඩ';

  @override
  String get backToDuty => 'රාජකාරියට ආපසු';

  @override
  String get messagesTitle => 'දැනුම්දීම්';

  @override
  String get messagesSub => 'පාලන මධ්‍යස්ථානයෙන්';

  @override
  String get messageDelivered => 'ලැබුණි';

  @override
  String messageAcked(Object time) {
    return '$time ට පිළිගත්තා';
  }

  @override
  String messageFrom(Object time) {
    return 'පාලනය · $time';
  }

  @override
  String messageSheetFrom(Object time) {
    return 'පාලන මධ්‍යස්ථානයෙන් පණිවිඩයක් · $time';
  }

  @override
  String messageWaiting(Object time) {
    return '$time ට ලැබුණි · ඔබේ පිළිගැනීම බලාපොරොත්තුවෙන්';
  }

  @override
  String get messageAckButton => 'පිළිගන්නවා';

  @override
  String get assistTitle => 'සහාය ඉල්ලන්න';

  @override
  String get assistBody =>
      'පාලන මධ්‍යස්ථානයට ඔබේ ගමන සහ අවසන් දන්නා ස්ථානය පෙනේ.';

  @override
  String get assistMedical => 'වෛද්‍ය';

  @override
  String get assistOvercrowding => 'අධික ජන ගහනය';

  @override
  String get assistUnsafe => 'අනාරක්ෂිත මගී තත්ත්වයක්';

  @override
  String get assistOther => 'වෙනත්';

  @override
  String get assistContinue => 'ඉදිරියට';

  @override
  String get assistConfirmTitle => 'පාලන මධ්‍යස්ථානයට ඉල්ලීම යවන්නද?';

  @override
  String assistConfirmBody(Object journey, Object type) {
    return '$type · $journey. මෙය ගමන වෙනස් නොකරයි. හදිසි අවස්ථාවකදී දුම්රිය රේඩියෝව භාවිතා කරන්න.';
  }

  @override
  String get assistSend => 'ඉල්ලීම යවන්න';

  @override
  String get goBack => 'ආපසු';

  @override
  String get assistSentTitle => 'ඉල්ලීම යැවුවා';

  @override
  String get assistSentBody =>
      'පාලන මධ්‍යස්ථානයට ඔබේ ඉල්ලීම ලැබුණි. ඊළඟට කුමක් කළ යුතුදැයි ඔවුහු තීරණය කරති. දුම්රිය රේඩියෝවෙන් සිටින්න.';

  @override
  String get assistType => 'වර්ගය';

  @override
  String get assistJourney => 'ගමන';

  @override
  String get assistSent => 'යැවූ වේලාව';

  @override
  String get backToJourney => 'ගමනට ආපසු';

  @override
  String get meTitle => 'මම';

  @override
  String get meRole => 'රියදුරු';

  @override
  String meDuty(Object from, Object to) {
    return 'රියදුරු · රාජකාරිය $from සිට $to දක්වා';
  }

  @override
  String get sharedPhoneTitle => 'හවුල් දුරකථනය';

  @override
  String get sharedPhoneBody =>
      'ඔබ ඉවත් වන විට ඔබේ සුරකින ලද රාජකාරිය සහ නොයැවූ සිදුවීම් මැකේ.';

  @override
  String get signOutRow => 'ඉවත් වන්න';

  @override
  String get signOutTitle => 'SmartRail වෙතින් ඉවත් වන්නද?';

  @override
  String get signOutBody =>
      'මෙම දුරකථනයේ ඔබේ සුරකින ලද රාජකාරිය සහ නොයැවූ සිදුවීම් මැකේ.';

  @override
  String get signOutConfirm => 'ඉවත් වන්න';

  @override
  String get chooseLanguageTitle => 'ඔබේ භාෂාව තෝරන්න';

  @override
  String get chooseLanguageBody => 'මෙය පසුව \'මම\' හි වෙනස් කළ හැක.';

  @override
  String get continueButton => 'ඉදිරියට';

  @override
  String get languageEnglish => 'English';

  @override
  String get languageSinhala => 'සිංහල';

  @override
  String get meLanguage => 'භාෂාව';

  @override
  String get meHelpTitle => 'උදව්';

  @override
  String get techIssueRow => 'තාක්ෂණික ගැටලුවක් වාර්තා කරන්න';

  @override
  String get emergencyRow => 'හදිසි සම්බන්ධතා';

  @override
  String get techIssueTitle => 'තාක්ෂණික ගැටලුවක් වාර්තා කරන්න';

  @override
  String get techIssueBody =>
      'යෙදුම, ඔබේ දුරකථනය හෝ දුම්රියේ උපකරණවල ගැටලුවක් පාලන මධ්‍යස්ථානයට දැනුම් දෙන්න. එය දුරකථනයේ සුරකින්නේ නැත, පාලන මධ්‍යස්ථානයට යවයි.';

  @override
  String get techApp => 'යෙදුමේ ගැටලුවක්';

  @override
  String get techPhone => 'දුරකථන හෝ සංඥා ගැටලුවක්';

  @override
  String get techTrain => 'දුම්රිය උපකරණ දෝෂයක්';

  @override
  String get techOther => 'වෙනත්';

  @override
  String get techNoteHint => 'කුමක් වැරදිද? (අත්‍යවශ්‍ය නොවේ)';

  @override
  String get techSend => 'පාලන මධ්‍යස්ථානයට යවන්න';

  @override
  String get techSentTitle => 'පාලන මධ්‍යස්ථානයට යැවුවා';

  @override
  String get techSentBody => 'පාලන මධ්‍යස්ථානයට ඔබේ වාර්තාව ලැබුණි.';

  @override
  String get emergencyTitle => 'හදිසි සම්බන්ධතා';

  @override
  String get emergencyBody =>
      'හදිසි අවස්ථාවකදී මුලින්ම දුම්රිය රේඩියෝව භාවිතා කරන්න.';

  @override
  String get contactPolice => 'පොලිසිය';

  @override
  String get contactAmbulance => 'ගිලන්රථ (සුව සැරිය)';

  @override
  String get contactFire => 'ගිනි නිවීම සහ ගලවා ගැනීම';

  @override
  String get noticeNetwork => 'පාලන මධ්‍යස්ථානයෙන් ජාල දැනුම්දීමක්';

  @override
  String get alertDirect => 'ඔබට සෘජුව';

  @override
  String get alertNetwork => 'ජාල දැනුම්දීම';

  @override
  String get severityMinor => 'සුළු';

  @override
  String get severityMajor => 'ප්‍රධාන';

  @override
  String get severitySevere => 'බරපතල';

  @override
  String get allStations => 'සියලු දුම්රිය ස්ථාන';

  @override
  String alertSheetNetwork(Object time) {
    return 'ජාල දැනුම්දීම · $time';
  }

  @override
  String alertSheetPlaces(Object places) {
    return 'අදාළ ස්ථාන: $places';
  }

  @override
  String get skipButton => 'මෙහි නොනවතී';

  @override
  String get alertsTabDirect => 'සෘජු';

  @override
  String get alertsTabNetwork => 'ජාලය';

  @override
  String get alertsEmptyDirect => 'පාලන මධ්‍යස්ථානයෙන් ඔබට පණිවිඩ නැත.';

  @override
  String get alertsEmptyNetwork => 'දැනට ජාල දැනුම්දීම් නැත.';
}
