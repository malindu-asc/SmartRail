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
  String get tabMessages => 'Messages';

  @override
  String get tabMe => 'Me';

  @override
  String get offlineTitle => 'You are offline';

  @override
  String get offlineDetail =>
      'Actions cannot be sent until the connection is back.';

  @override
  String get signInTitle => 'Driver sign in';

  @override
  String get signInWelcome =>
      'Sign in with the account your administrator gave you.';

  @override
  String get emailLabel => 'Email';

  @override
  String get emailHint => 'name@railway.lk';

  @override
  String get passwordLabel => 'Password';

  @override
  String get passwordHint => 'Your password';

  @override
  String get signInButton => 'Sign in';

  @override
  String get signInBadCredentials =>
      'That email or password is not right. Check them and try again.';

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
  String get dutyNoneTitle => 'No duty today';

  @override
  String get dutyNoneBody =>
      'Nothing is assigned to you today. The scheduler assigns duties. Pull down to check again.';

  @override
  String dutyUpcoming(Object time) {
    return 'Your duty starts at $time. You can sign on from 30 minutes before.';
  }

  @override
  String get dutyEnded => 'This duty has ended.';

  @override
  String get dutyHistoryEmpty => 'No past duties in the last two weeks.';

  @override
  String get dutyNeedsReview => 'This duty needs review by control.';

  @override
  String get signOnButton => 'Sign on to duty';

  @override
  String get signOffButton => 'Sign off duty';

  @override
  String signedOnAt(Object time) {
    return 'Signed on $time';
  }

  @override
  String get journeyNext => 'Next';

  @override
  String get journeyLater => 'Later';

  @override
  String get journeyRunning => 'Running';

  @override
  String get journeyDone => 'Done';

  @override
  String journeyCardLine(Object platform, Object stops, Object time) {
    return 'Platform $platform · $stops stops · arrives $time';
  }

  @override
  String journeyCardNoPlatform(Object stops, Object time) {
    return '$stops stops · arrives $time';
  }

  @override
  String get noticeFromControl => 'Notice from control';

  @override
  String get signOffTitle => 'Sign off duty?';

  @override
  String get signOffBody =>
      'Only sign off when your shift is over. Journeys you have not finished stay with control.';

  @override
  String get signOffConfirm => 'Sign off';

  @override
  String journeyTitle(Object destination, Object time) {
    return '$time to $destination';
  }

  @override
  String get helpButton => 'Help';

  @override
  String get movingOnly => 'Moving · one-tap actions only';

  @override
  String get nextStop => 'NEXT STOP';

  @override
  String get atStop => 'AT STOP';

  @override
  String scheduledAt(Object time) {
    return 'Scheduled $time';
  }

  @override
  String expectedAt(Object time) {
    return 'Expected $time';
  }

  @override
  String delayChip(Object minutes) {
    return '+$minutes min';
  }

  @override
  String earlyChip(Object minutes) {
    return '$minutes min early';
  }

  @override
  String platformCoach(Object platform) {
    return 'Platform $platform';
  }

  @override
  String get stopHere => 'here now';

  @override
  String get stopEstimate => 'Estimate · may change';

  @override
  String stopPlatformExpected(Object platform) {
    return 'Platform $platform · expected';
  }

  @override
  String stopDeparted(Object time) {
    return 'Departed $time';
  }

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
  String get delayReasonButton => 'Delay reason';

  @override
  String get moreSkipStop => 'Skip this stop';

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
    return 'Skip $station?';
  }

  @override
  String get skipBody =>
      'Use this only when the train does not stop here. Passengers will see the stop as skipped.';

  @override
  String get skipConfirm => 'Skip this stop';

  @override
  String get mismatchTitle => 'That is not the stop the train is at';

  @override
  String mismatchBody(Object expected) {
    return '$expected';
  }

  @override
  String get mismatchRefresh => 'Refresh the journey';

  @override
  String get journeyCompleteTitle => 'Journey complete';

  @override
  String journeyCompleteBody(Object station) {
    return 'The train has arrived at $station. Your events are saved.';
  }

  @override
  String get backToDuty => 'Back to duty';

  @override
  String get messagesTitle => 'Messages';

  @override
  String get messagesEmpty => 'No messages from control.';

  @override
  String get messageAckButton => 'Acknowledge';

  @override
  String messageAcked(Object time) {
    return 'Acknowledged $time';
  }

  @override
  String get messageNeedsAck => 'Needs your acknowledgement';

  @override
  String get messageFromControl => 'Message from control';

  @override
  String messageSent(Object time) {
    return 'Sent $time';
  }

  @override
  String get assistTitle => 'Request assistance';

  @override
  String get assistBody =>
      'Control sees where you are and who you are. Say what is happening if you can.';

  @override
  String get assistNoteHint => 'What is happening? (optional)';

  @override
  String get assistSend => 'Send request';

  @override
  String get assistSentTitle => 'Request sent';

  @override
  String get assistSentBody =>
      'Control has your request and will contact you. Stay safe.';

  @override
  String get meTitle => 'Me';

  @override
  String get meRole => 'Driver';

  @override
  String get signOutRow => 'Sign out';

  @override
  String get signOutTitle => 'Sign out?';

  @override
  String get signOutBody => 'You will need to sign in again to see your duty.';

  @override
  String get signOutConfirm => 'Sign out';

  @override
  String get signOutBlocked => 'Sign off your duty before signing out.';

  @override
  String get notFoundTitle => 'We could not find that page';
}
