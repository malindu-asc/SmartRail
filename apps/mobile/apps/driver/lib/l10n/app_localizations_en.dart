// ignore: unused_import
import 'package:intl/intl.dart' as intl;
import 'app_localizations.dart';

// ignore_for_file: type=lint

/// The translations for English (`en`).
class AppLocalizationsEn extends AppLocalizations {
  AppLocalizationsEn([String locale = 'en']) : super(locale);

  @override
  String get appTitle => 'SmartRail Driver';

  @override
  String get retry => 'Try again';

  @override
  String get refresh => 'Refresh';

  @override
  String get cancel => 'Cancel';

  @override
  String get close => 'Close';

  @override
  String get errorTitle => 'We could not load this';

  @override
  String get errorNetwork => 'Check your connection and try again.';

  @override
  String get errorGeneric => 'Something went wrong. Try again in a moment.';

  @override
  String get errorForbidden =>
      'Your account does not have a driver role. Ask the administrator.';

  @override
  String get tabDuty => 'Duty';

  @override
  String get tabMessages => 'Alerts';

  @override
  String get tabMe => 'Me';

  @override
  String syncedAt(Object time) {
    return 'Synced $time';
  }

  @override
  String offlineSynced(Object time) {
    return 'Offline · last sync $time';
  }

  @override
  String get offlineNever => 'Offline';

  @override
  String get signInHello => 'Welcome Back';

  @override
  String get signInRole => 'Driver';

  @override
  String get signInSubtitle =>
      'Sign in with your organisation account to see today\'s duty.';

  @override
  String get staffIdLabel => 'Staff ID or email';

  @override
  String get staffIdHint => 'name@railway.lk';

  @override
  String get passwordLabel => 'Password';

  @override
  String get passwordHint => 'Your password';

  @override
  String get forgotPassword => 'Forgot your password? Contact control.';

  @override
  String get signInButton => 'Sign in';

  @override
  String get safetyNote =>
      'Safety messages go by railway radio. This app is never the safety channel.';

  @override
  String get signInBadCredentials =>
      'That Staff ID or password is not right. Check them and try again.';

  @override
  String get signInNotDriver => 'This account is not a driver account.';

  @override
  String get dutyTitle => 'Your duty';

  @override
  String get dutyActive => 'Active';

  @override
  String get dutyPast => 'Past';

  @override
  String dutyRoster(Object from, Object to, Object unit) {
    return 'Unit $unit · $from to $to';
  }

  @override
  String dutyRosterNoUnit(Object from, Object to) {
    return '$from to $to';
  }

  @override
  String signedOnChip(Object time) {
    return 'Signed on $time';
  }

  @override
  String get noticeFromControl => 'Message from control';

  @override
  String get journeyNext => 'Next';

  @override
  String get journeyLater => 'Later';

  @override
  String get journeyRunning => 'Running';

  @override
  String get journeyDone => 'Done';

  @override
  String journeyRoute(Object destination, Object origin) {
    return '$origin to $destination';
  }

  @override
  String journeyCardLine(Object platform, Object stops, Object time) {
    return 'Platform $platform · $stops stops · arrives $time';
  }

  @override
  String journeyCardNoPlatform(Object stops, Object time) {
    return '$stops stops · arrives $time';
  }

  @override
  String journeyCardDone(Object time) {
    return 'Arrived $time';
  }

  @override
  String journeyCardDoneLate(Object minutes, Object time) {
    return 'Arrived $time · $minutes min late';
  }

  @override
  String startJourney(Object time) {
    return 'Start $time journey';
  }

  @override
  String get signOnButton => 'Sign on to duty';

  @override
  String get signOffButton => 'Sign off duty';

  @override
  String dutyUpcoming(Object time) {
    return 'Your duty starts at $time. You can sign on from 30 minutes before.';
  }

  @override
  String get dutyNoneTitle => 'No duty assigned';

  @override
  String get dutyNoneBody =>
      'Contact control to confirm your roster for today.';

  @override
  String get dutyNotCurrentTitle => 'Duty not current';

  @override
  String get dutyNotCurrentBody => 'Today\'s duty shows when signal returns.';

  @override
  String get dutyCantShowTitle => 'We can\'t show today\'s duty yet';

  @override
  String get dutyCantShowBody =>
      'Use your paper duty sheet or the railway radio until this updates.';

  @override
  String get dutyHistorySub => 'Your own duties · last 30 days';

  @override
  String get dutyHistoryEmpty => 'No past duties in the last 30 days.';

  @override
  String get historyNeedsReview => 'Needs review';

  @override
  String get historySignedOff => 'Signed off';

  @override
  String historyLine(Object count) {
    return '$count journeys';
  }

  @override
  String get signOffTitle => 'Sign off duty?';

  @override
  String get signOffBody =>
      'All journeys are done and nothing is waiting to send.';

  @override
  String get signOffBodyUnfinished =>
      'Some journeys are not finished. They stay with control.';

  @override
  String get signOffConfirm => 'Sign off';

  @override
  String get staySignedOn => 'Stay signed on';

  @override
  String journeyTitle(Object destination, Object time) {
    return '$time to $destination';
  }

  @override
  String get helpButton => 'Help';

  @override
  String get nextStopLabel => 'NEXT STOP';

  @override
  String get atStopLabel => 'AT STOP';

  @override
  String arrivingIn(Object minutes) {
    return 'Arriving in about $minutes min';
  }

  @override
  String get arrivingNow => 'Arriving now';

  @override
  String departsAt(Object time) {
    return 'Departs $time';
  }

  @override
  String scheduledAt(Object time) {
    return 'Scheduled $time';
  }

  @override
  String arrivedDue(Object due, Object time) {
    return 'Arrived $time · due $due';
  }

  @override
  String delayChip(Object minutes) {
    return '+$minutes min';
  }

  @override
  String platformLine(Object platform) {
    return 'Platform $platform';
  }

  @override
  String stopDeparted(Object time) {
    return 'Departed $time';
  }

  @override
  String stopPlatformHere(Object platform) {
    return 'Platform $platform · here now';
  }

  @override
  String get stopHere => 'Here now';

  @override
  String stopPlatformExpected(Object platform) {
    return 'Platform $platform · expected';
  }

  @override
  String get stopEstimate => 'Estimate · may change';

  @override
  String get stopSkipped => 'Skipped';

  @override
  String get stopCancelled => 'Cancelled';

  @override
  String arrivedAt(Object station) {
    return 'Arrived at $station';
  }

  @override
  String departedFrom(Object station) {
    return 'Departed $station';
  }

  @override
  String eventArrived(Object station) {
    return 'Arrived $station';
  }

  @override
  String eventDeparted(Object station) {
    return 'Departed $station';
  }

  @override
  String eventSkipped(Object station) {
    return 'Skipped $station';
  }

  @override
  String eventSaved(Object event) {
    return '$event saved';
  }

  @override
  String get delayReasonButton => 'Delay reason';

  @override
  String get actionSaved => 'Saved';

  @override
  String get notSignedOn => 'Sign on to your duty to record events.';

  @override
  String get journeyNotMine => 'That journey is not on your duty.';

  @override
  String get delayReasonTitle => 'Why are we late?';

  @override
  String get delayReasonBody => 'Pick one reason. Control can change it later.';

  @override
  String get reasonSignalFault => 'Signal fault';

  @override
  String get reasonDoorFault => 'Door fault';

  @override
  String get reasonCrew => 'Crew';

  @override
  String get reasonPassengerIncident => 'Passenger incident';

  @override
  String get reasonTrack => 'Track';

  @override
  String get reasonOther => 'Other';

  @override
  String get saveReason => 'Save reason';

  @override
  String skipTitle(Object station) {
    return 'Not stopping at $station?';
  }

  @override
  String get skipBody =>
      'Control is alerted straight away. Passengers are told only after control confirms or the train is seen passing.';

  @override
  String get skipPlatformClosed => 'Platform closed';

  @override
  String get skipInstructed => 'Instructed by control';

  @override
  String get skipSafety => 'Safety';

  @override
  String get skipConfirm => 'Report skipped stop';

  @override
  String get mismatchLabel => 'Not saved';

  @override
  String get mismatchTitle => 'That stop doesn\'t match';

  @override
  String mismatchBody(Object attempt, Object station) {
    return '$attempt was not saved. The train is at $station. Nothing on the journey has changed.';
  }

  @override
  String mismatchRecord(Object event) {
    return 'Record $event';
  }

  @override
  String get mismatchHold => 'Hold for control';

  @override
  String get journeyCompleteTitle => 'Journey complete';

  @override
  String journeyCompleteBody(Object arrived, Object route, Object time) {
    return '$time $route · arrived $arrived';
  }

  @override
  String journeyCompleteBodyLate(
    Object arrived,
    Object minutes,
    Object route,
    Object time,
  ) {
    return '$time $route · arrived $arrived, $minutes min late';
  }

  @override
  String get completeEvents => 'Events sent';

  @override
  String get completeReasons => 'Delay reasons';

  @override
  String get completeAcks => 'Messages acknowledged';

  @override
  String get backToDuty => 'Back to duty';

  @override
  String get messagesTitle => 'Alerts';

  @override
  String get messagesSub => 'From control';

  @override
  String get messageDelivered => 'Delivered';

  @override
  String messageAcked(Object time) {
    return 'Acknowledged $time';
  }

  @override
  String messageFrom(Object time) {
    return 'Control · $time';
  }

  @override
  String messageSheetFrom(Object time) {
    return 'Message from control · $time';
  }

  @override
  String messageWaiting(Object time) {
    return 'Delivered $time · waiting for your acknowledgement';
  }

  @override
  String get messageAckButton => 'Acknowledge';

  @override
  String get assistTitle => 'Request assistance';

  @override
  String get assistBody => 'Control sees your journey and last known position.';

  @override
  String get assistMedical => 'Medical';

  @override
  String get assistOvercrowding => 'Overcrowding';

  @override
  String get assistUnsafe => 'Unsafe passenger situation';

  @override
  String get assistOther => 'Other';

  @override
  String get assistContinue => 'Continue';

  @override
  String get assistConfirmTitle => 'Send request to control?';

  @override
  String assistConfirmBody(Object journey, Object type) {
    return '$type · $journey. This does not change the journey. In an emergency, use the railway radio.';
  }

  @override
  String get assistSend => 'Send request';

  @override
  String get goBack => 'Go back';

  @override
  String get assistSentTitle => 'Request sent';

  @override
  String get assistSentBody =>
      'Control has your request and will decide what happens next. Stay on the railway radio.';

  @override
  String get assistType => 'Type';

  @override
  String get assistJourney => 'Journey';

  @override
  String get assistSent => 'Sent';

  @override
  String get backToJourney => 'Back to journey';

  @override
  String get meTitle => 'Me';

  @override
  String get meRole => 'Driver';

  @override
  String meDuty(Object from, Object to) {
    return 'Driver · Duty $from to $to';
  }

  @override
  String get sharedPhoneTitle => 'Shared phone';

  @override
  String get sharedPhoneBody =>
      'Your saved duty and unsent events are cleared when you sign out.';

  @override
  String get signOutRow => 'Sign out';

  @override
  String get signOutTitle => 'Sign out of SmartRail?';

  @override
  String get signOutBody =>
      'Your saved duty and any unsent events on this phone are cleared.';

  @override
  String get signOutConfirm => 'Sign out';

  @override
  String get chooseLanguageTitle => 'Choose your language';

  @override
  String get chooseLanguageBody => 'You can change this later in Me.';

  @override
  String get continueButton => 'Continue';

  @override
  String get languageEnglish => 'English';

  @override
  String get languageSinhala => 'සිංහල';

  @override
  String get meLanguage => 'Language';

  @override
  String get meHelpTitle => 'Help';

  @override
  String get techIssueRow => 'Report a technical issue';

  @override
  String get emergencyRow => 'Emergency contacts';

  @override
  String get techIssueTitle => 'Report a technical issue';

  @override
  String get techIssueBody =>
      'Tell control about a problem with the app, your phone or the train\'s equipment. It is sent to control, not saved on your phone.';

  @override
  String get techApp => 'App problem';

  @override
  String get techPhone => 'Phone or signal problem';

  @override
  String get techTrain => 'Train equipment fault';

  @override
  String get techOther => 'Other';

  @override
  String get techNoteHint => 'What is wrong? (optional)';

  @override
  String get techSend => 'Send to control';

  @override
  String get techSentTitle => 'Sent to control';

  @override
  String get techSentBody => 'Control has your report.';

  @override
  String get emergencyTitle => 'Emergency contacts';

  @override
  String get emergencyBody => 'In an emergency, use the railway radio first.';

  @override
  String get contactPolice => 'Police';

  @override
  String get contactAmbulance => 'Ambulance (Suwa Seriya)';

  @override
  String get contactFire => 'Fire and rescue';

  @override
  String get noticeNetwork => 'Network alert from control';

  @override
  String get alertDirect => 'Direct to you';

  @override
  String get alertNetwork => 'Network alert';

  @override
  String get severityMinor => 'Minor';

  @override
  String get severityMajor => 'Major';

  @override
  String get severitySevere => 'Severe';

  @override
  String get allStations => 'All stations';

  @override
  String alertSheetNetwork(Object time) {
    return 'Network alert · $time';
  }

  @override
  String alertSheetPlaces(Object places) {
    return 'Applies to $places';
  }

  @override
  String get skipButton => 'Not stopping here';

  @override
  String get alertsTabDirect => 'Direct';

  @override
  String get alertsTabNetwork => 'Network';

  @override
  String get alertsEmptyDirect => 'No messages for you from control.';

  @override
  String get alertsEmptyNetwork => 'No network alerts right now.';
}
