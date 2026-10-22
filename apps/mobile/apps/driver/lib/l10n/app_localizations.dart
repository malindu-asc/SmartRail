import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_en.dart';
import 'app_localizations_si.dart';

// ignore_for_file: type=lint

/// Callers can lookup localized strings with an instance of AppLocalizations
/// returned by `AppLocalizations.of(context)`.
///
/// Applications need to include `AppLocalizations.delegate()` in their app's
/// `localizationDelegates` list, and the locales they support in the app's
/// `supportedLocales` list. For example:
///
/// ```dart
/// import 'l10n/app_localizations.dart';
///
/// return MaterialApp(
///   localizationsDelegates: AppLocalizations.localizationsDelegates,
///   supportedLocales: AppLocalizations.supportedLocales,
///   home: MyApplicationHome(),
/// );
/// ```
///
/// ## Update pubspec.yaml
///
/// Please make sure to update your pubspec.yaml to include the following
/// packages:
///
/// ```yaml
/// dependencies:
///   # Internationalization support.
///   flutter_localizations:
///     sdk: flutter
///   intl: any # Use the pinned version from flutter_localizations
///
///   # Rest of dependencies
/// ```
///
/// ## iOS Applications
///
/// iOS applications define key application metadata, including supported
/// locales, in an Info.plist file that is built into the application bundle.
/// To configure the locales supported by your app, you’ll need to edit this
/// file.
///
/// First, open your project’s ios/Runner.xcworkspace Xcode workspace file.
/// Then, in the Project Navigator, open the Info.plist file under the Runner
/// project’s Runner folder.
///
/// Next, select the Information Property List item, select Add Item from the
/// Editor menu, then select Localizations from the pop-up menu.
///
/// Select and expand the newly-created Localizations item then, for each
/// locale your application supports, add a new item and select the locale
/// you wish to add from the pop-up menu in the Value field. This list should
/// be consistent with the languages listed in the AppLocalizations.supportedLocales
/// property.
abstract class AppLocalizations {
  AppLocalizations(String locale)
    : localeName = intl.Intl.canonicalizedLocale(locale.toString());

  final String localeName;

  static AppLocalizations of(BuildContext context) {
    return Localizations.of<AppLocalizations>(context, AppLocalizations)!;
  }

  static const LocalizationsDelegate<AppLocalizations> delegate =
      _AppLocalizationsDelegate();

  /// A list of this localizations delegate along with the default localizations
  /// delegates.
  ///
  /// Returns a list of localizations delegates containing this delegate along with
  /// GlobalMaterialLocalizations.delegate, GlobalCupertinoLocalizations.delegate,
  /// and GlobalWidgetsLocalizations.delegate.
  ///
  /// Additional delegates can be added by appending to this list in
  /// MaterialApp. This list does not have to be used at all if a custom list
  /// of delegates is preferred or required.
  static const List<LocalizationsDelegate<dynamic>> localizationsDelegates =
      <LocalizationsDelegate<dynamic>>[
        delegate,
        GlobalMaterialLocalizations.delegate,
        GlobalCupertinoLocalizations.delegate,
        GlobalWidgetsLocalizations.delegate,
      ];

  /// A list of this localizations delegate's supported locales.
  static const List<Locale> supportedLocales = <Locale>[
    Locale('en'),
    Locale('si'),
  ];

  /// No description provided for @appTitle.
  ///
  /// In en, this message translates to:
  /// **'SmartRail Driver'**
  String get appTitle;

  /// No description provided for @retry.
  ///
  /// In en, this message translates to:
  /// **'Try again'**
  String get retry;

  /// No description provided for @refresh.
  ///
  /// In en, this message translates to:
  /// **'Refresh'**
  String get refresh;

  /// No description provided for @cancel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get cancel;

  /// No description provided for @close.
  ///
  /// In en, this message translates to:
  /// **'Close'**
  String get close;

  /// No description provided for @errorTitle.
  ///
  /// In en, this message translates to:
  /// **'We could not load this'**
  String get errorTitle;

  /// No description provided for @errorNetwork.
  ///
  /// In en, this message translates to:
  /// **'Check your connection and try again.'**
  String get errorNetwork;

  /// No description provided for @errorGeneric.
  ///
  /// In en, this message translates to:
  /// **'Something went wrong. Try again in a moment.'**
  String get errorGeneric;

  /// No description provided for @errorForbidden.
  ///
  /// In en, this message translates to:
  /// **'Your account does not have a driver role. Ask the administrator.'**
  String get errorForbidden;

  /// No description provided for @tabDuty.
  ///
  /// In en, this message translates to:
  /// **'Duty'**
  String get tabDuty;

  /// No description provided for @tabMessages.
  ///
  /// In en, this message translates to:
  /// **'Alerts'**
  String get tabMessages;

  /// No description provided for @tabMe.
  ///
  /// In en, this message translates to:
  /// **'Me'**
  String get tabMe;

  /// No description provided for @syncedAt.
  ///
  /// In en, this message translates to:
  /// **'Synced {time}'**
  String syncedAt(Object time);

  /// No description provided for @offlineSynced.
  ///
  /// In en, this message translates to:
  /// **'Offline · last sync {time}'**
  String offlineSynced(Object time);

  /// No description provided for @offlineNever.
  ///
  /// In en, this message translates to:
  /// **'Offline'**
  String get offlineNever;

  /// No description provided for @signInHello.
  ///
  /// In en, this message translates to:
  /// **'Welcome Back'**
  String get signInHello;

  /// No description provided for @signInRole.
  ///
  /// In en, this message translates to:
  /// **'Driver'**
  String get signInRole;

  /// No description provided for @signInSubtitle.
  ///
  /// In en, this message translates to:
  /// **'Sign in with your organisation account to see today\'s duty.'**
  String get signInSubtitle;

  /// No description provided for @staffIdLabel.
  ///
  /// In en, this message translates to:
  /// **'Staff ID or email'**
  String get staffIdLabel;

  /// No description provided for @staffIdHint.
  ///
  /// In en, this message translates to:
  /// **'name@railway.lk'**
  String get staffIdHint;

  /// No description provided for @passwordLabel.
  ///
  /// In en, this message translates to:
  /// **'Password'**
  String get passwordLabel;

  /// No description provided for @passwordHint.
  ///
  /// In en, this message translates to:
  /// **'Your password'**
  String get passwordHint;

  /// No description provided for @forgotPassword.
  ///
  /// In en, this message translates to:
  /// **'Forgot your password? Contact control.'**
  String get forgotPassword;

  /// No description provided for @signInButton.
  ///
  /// In en, this message translates to:
  /// **'Sign in'**
  String get signInButton;

  /// No description provided for @safetyNote.
  ///
  /// In en, this message translates to:
  /// **'Safety messages go by railway radio. This app is never the safety channel.'**
  String get safetyNote;

  /// No description provided for @signInBadCredentials.
  ///
  /// In en, this message translates to:
  /// **'That Staff ID or password is not right. Check them and try again.'**
  String get signInBadCredentials;

  /// No description provided for @signInNotDriver.
  ///
  /// In en, this message translates to:
  /// **'This account is not a driver account.'**
  String get signInNotDriver;

  /// No description provided for @dutyTitle.
  ///
  /// In en, this message translates to:
  /// **'Your duty'**
  String get dutyTitle;

  /// No description provided for @dutyActive.
  ///
  /// In en, this message translates to:
  /// **'Active'**
  String get dutyActive;

  /// No description provided for @dutyPast.
  ///
  /// In en, this message translates to:
  /// **'Past'**
  String get dutyPast;

  /// No description provided for @dutyRoster.
  ///
  /// In en, this message translates to:
  /// **'Unit {unit} · {from} to {to}'**
  String dutyRoster(Object from, Object to, Object unit);

  /// No description provided for @dutyRosterNoUnit.
  ///
  /// In en, this message translates to:
  /// **'{from} to {to}'**
  String dutyRosterNoUnit(Object from, Object to);

  /// No description provided for @signedOnChip.
  ///
  /// In en, this message translates to:
  /// **'Signed on {time}'**
  String signedOnChip(Object time);

  /// No description provided for @noticeFromControl.
  ///
  /// In en, this message translates to:
  /// **'Message from control'**
  String get noticeFromControl;

  /// No description provided for @journeyNext.
  ///
  /// In en, this message translates to:
  /// **'Next'**
  String get journeyNext;

  /// No description provided for @journeyLater.
  ///
  /// In en, this message translates to:
  /// **'Later'**
  String get journeyLater;

  /// No description provided for @journeyRunning.
  ///
  /// In en, this message translates to:
  /// **'Running'**
  String get journeyRunning;

  /// No description provided for @journeyDone.
  ///
  /// In en, this message translates to:
  /// **'Done'**
  String get journeyDone;

  /// No description provided for @journeyRoute.
  ///
  /// In en, this message translates to:
  /// **'{origin} to {destination}'**
  String journeyRoute(Object destination, Object origin);

  /// No description provided for @journeyCardLine.
  ///
  /// In en, this message translates to:
  /// **'Platform {platform} · {stops} stops · arrives {time}'**
  String journeyCardLine(Object platform, Object stops, Object time);

  /// No description provided for @journeyCardNoPlatform.
  ///
  /// In en, this message translates to:
  /// **'{stops} stops · arrives {time}'**
  String journeyCardNoPlatform(Object stops, Object time);

  /// No description provided for @journeyCardDone.
  ///
  /// In en, this message translates to:
  /// **'Arrived {time}'**
  String journeyCardDone(Object time);

  /// No description provided for @journeyCardDoneLate.
  ///
  /// In en, this message translates to:
  /// **'Arrived {time} · {minutes} min late'**
  String journeyCardDoneLate(Object minutes, Object time);

  /// No description provided for @startJourney.
  ///
  /// In en, this message translates to:
  /// **'Start {time} journey'**
  String startJourney(Object time);

  /// No description provided for @signOnButton.
  ///
  /// In en, this message translates to:
  /// **'Sign on to duty'**
  String get signOnButton;

  /// No description provided for @signOffButton.
  ///
  /// In en, this message translates to:
  /// **'Sign off duty'**
  String get signOffButton;

  /// No description provided for @dutyUpcoming.
  ///
  /// In en, this message translates to:
  /// **'Your duty starts at {time}. You can sign on from 30 minutes before.'**
  String dutyUpcoming(Object time);

  /// No description provided for @dutyNoneTitle.
  ///
  /// In en, this message translates to:
  /// **'No duty assigned'**
  String get dutyNoneTitle;

  /// No description provided for @dutyNoneBody.
  ///
  /// In en, this message translates to:
  /// **'Contact control to confirm your roster for today.'**
  String get dutyNoneBody;

  /// No description provided for @dutyNotCurrentTitle.
  ///
  /// In en, this message translates to:
  /// **'Duty not current'**
  String get dutyNotCurrentTitle;

  /// No description provided for @dutyNotCurrentBody.
  ///
  /// In en, this message translates to:
  /// **'Today\'s duty shows when signal returns.'**
  String get dutyNotCurrentBody;

  /// No description provided for @dutyCantShowTitle.
  ///
  /// In en, this message translates to:
  /// **'We can\'t show today\'s duty yet'**
  String get dutyCantShowTitle;

  /// No description provided for @dutyCantShowBody.
  ///
  /// In en, this message translates to:
  /// **'Use your paper duty sheet or the railway radio until this updates.'**
  String get dutyCantShowBody;

  /// No description provided for @dutyHistorySub.
  ///
  /// In en, this message translates to:
  /// **'Your own duties · last 30 days'**
  String get dutyHistorySub;

  /// No description provided for @dutyHistoryEmpty.
  ///
  /// In en, this message translates to:
  /// **'No past duties in the last 30 days.'**
  String get dutyHistoryEmpty;

  /// No description provided for @historyNeedsReview.
  ///
  /// In en, this message translates to:
  /// **'Needs review'**
  String get historyNeedsReview;

  /// No description provided for @historySignedOff.
  ///
  /// In en, this message translates to:
  /// **'Signed off'**
  String get historySignedOff;

  /// No description provided for @historyLine.
  ///
  /// In en, this message translates to:
  /// **'{count} journeys'**
  String historyLine(Object count);

  /// No description provided for @signOffTitle.
  ///
  /// In en, this message translates to:
  /// **'Sign off duty?'**
  String get signOffTitle;

  /// No description provided for @signOffBody.
  ///
  /// In en, this message translates to:
  /// **'All journeys are done and nothing is waiting to send.'**
  String get signOffBody;

  /// No description provided for @signOffBodyUnfinished.
  ///
  /// In en, this message translates to:
  /// **'Some journeys are not finished. They stay with control.'**
  String get signOffBodyUnfinished;

  /// No description provided for @signOffConfirm.
  ///
  /// In en, this message translates to:
  /// **'Sign off'**
  String get signOffConfirm;

  /// No description provided for @staySignedOn.
  ///
  /// In en, this message translates to:
  /// **'Stay signed on'**
  String get staySignedOn;

  /// No description provided for @journeyTitle.
  ///
  /// In en, this message translates to:
  /// **'{time} to {destination}'**
  String journeyTitle(Object destination, Object time);

  /// No description provided for @helpButton.
  ///
  /// In en, this message translates to:
  /// **'Help'**
  String get helpButton;

  /// No description provided for @nextStopLabel.
  ///
  /// In en, this message translates to:
  /// **'NEXT STOP'**
  String get nextStopLabel;

  /// No description provided for @atStopLabel.
  ///
  /// In en, this message translates to:
  /// **'AT STOP'**
  String get atStopLabel;

  /// No description provided for @arrivingIn.
  ///
  /// In en, this message translates to:
  /// **'Arriving in about {minutes} min'**
  String arrivingIn(Object minutes);

  /// No description provided for @arrivingNow.
  ///
  /// In en, this message translates to:
  /// **'Arriving now'**
  String get arrivingNow;

  /// No description provided for @departsAt.
  ///
  /// In en, this message translates to:
  /// **'Departs {time}'**
  String departsAt(Object time);

  /// No description provided for @scheduledAt.
  ///
  /// In en, this message translates to:
  /// **'Scheduled {time}'**
  String scheduledAt(Object time);

  /// No description provided for @arrivedDue.
  ///
  /// In en, this message translates to:
  /// **'Arrived {time} · due {due}'**
  String arrivedDue(Object due, Object time);

  /// No description provided for @delayChip.
  ///
  /// In en, this message translates to:
  /// **'+{minutes} min'**
  String delayChip(Object minutes);

  /// No description provided for @platformLine.
  ///
  /// In en, this message translates to:
  /// **'Platform {platform}'**
  String platformLine(Object platform);

  /// No description provided for @stopDeparted.
  ///
  /// In en, this message translates to:
  /// **'Departed {time}'**
  String stopDeparted(Object time);

  /// No description provided for @stopPlatformHere.
  ///
  /// In en, this message translates to:
  /// **'Platform {platform} · here now'**
  String stopPlatformHere(Object platform);

  /// No description provided for @stopHere.
  ///
  /// In en, this message translates to:
  /// **'Here now'**
  String get stopHere;

  /// No description provided for @stopPlatformExpected.
  ///
  /// In en, this message translates to:
  /// **'Platform {platform} · expected'**
  String stopPlatformExpected(Object platform);

  /// No description provided for @stopEstimate.
  ///
  /// In en, this message translates to:
  /// **'Estimate · may change'**
  String get stopEstimate;

  /// No description provided for @stopSkipped.
  ///
  /// In en, this message translates to:
  /// **'Skipped'**
  String get stopSkipped;

  /// No description provided for @stopCancelled.
  ///
  /// In en, this message translates to:
  /// **'Cancelled'**
  String get stopCancelled;

  /// No description provided for @arrivedAt.
  ///
  /// In en, this message translates to:
  /// **'Arrived at {station}'**
  String arrivedAt(Object station);

  /// No description provided for @departedFrom.
  ///
  /// In en, this message translates to:
  /// **'Departed {station}'**
  String departedFrom(Object station);

  /// No description provided for @eventArrived.
  ///
  /// In en, this message translates to:
  /// **'Arrived {station}'**
  String eventArrived(Object station);

  /// No description provided for @eventDeparted.
  ///
  /// In en, this message translates to:
  /// **'Departed {station}'**
  String eventDeparted(Object station);

  /// No description provided for @eventSkipped.
  ///
  /// In en, this message translates to:
  /// **'Skipped {station}'**
  String eventSkipped(Object station);

  /// No description provided for @eventSaved.
  ///
  /// In en, this message translates to:
  /// **'{event} saved'**
  String eventSaved(Object event);

  /// No description provided for @delayReasonButton.
  ///
  /// In en, this message translates to:
  /// **'Delay reason'**
  String get delayReasonButton;

  /// No description provided for @actionSaved.
  ///
  /// In en, this message translates to:
  /// **'Saved'**
  String get actionSaved;

  /// No description provided for @notSignedOn.
  ///
  /// In en, this message translates to:
  /// **'Sign on to your duty to record events.'**
  String get notSignedOn;

  /// No description provided for @journeyNotMine.
  ///
  /// In en, this message translates to:
  /// **'That journey is not on your duty.'**
  String get journeyNotMine;

  /// No description provided for @delayReasonTitle.
  ///
  /// In en, this message translates to:
  /// **'Why are we late?'**
  String get delayReasonTitle;

  /// No description provided for @delayReasonBody.
  ///
  /// In en, this message translates to:
  /// **'Pick one reason. Control can change it later.'**
  String get delayReasonBody;

  /// No description provided for @reasonSignalFault.
  ///
  /// In en, this message translates to:
  /// **'Signal fault'**
  String get reasonSignalFault;

  /// No description provided for @reasonDoorFault.
  ///
  /// In en, this message translates to:
  /// **'Door fault'**
  String get reasonDoorFault;

  /// No description provided for @reasonCrew.
  ///
  /// In en, this message translates to:
  /// **'Crew'**
  String get reasonCrew;

  /// No description provided for @reasonPassengerIncident.
  ///
  /// In en, this message translates to:
  /// **'Passenger incident'**
  String get reasonPassengerIncident;

  /// No description provided for @reasonTrack.
  ///
  /// In en, this message translates to:
  /// **'Track'**
  String get reasonTrack;

  /// No description provided for @reasonOther.
  ///
  /// In en, this message translates to:
  /// **'Other'**
  String get reasonOther;

  /// No description provided for @saveReason.
  ///
  /// In en, this message translates to:
  /// **'Save reason'**
  String get saveReason;

  /// No description provided for @skipTitle.
  ///
  /// In en, this message translates to:
  /// **'Not stopping at {station}?'**
  String skipTitle(Object station);

  /// No description provided for @skipBody.
  ///
  /// In en, this message translates to:
  /// **'Control is alerted straight away. Passengers are told only after control confirms or the train is seen passing.'**
  String get skipBody;

  /// No description provided for @skipPlatformClosed.
  ///
  /// In en, this message translates to:
  /// **'Platform closed'**
  String get skipPlatformClosed;

  /// No description provided for @skipInstructed.
  ///
  /// In en, this message translates to:
  /// **'Instructed by control'**
  String get skipInstructed;

  /// No description provided for @skipSafety.
  ///
  /// In en, this message translates to:
  /// **'Safety'**
  String get skipSafety;

  /// No description provided for @skipConfirm.
  ///
  /// In en, this message translates to:
  /// **'Report skipped stop'**
  String get skipConfirm;

  /// No description provided for @mismatchLabel.
  ///
  /// In en, this message translates to:
  /// **'Not saved'**
  String get mismatchLabel;

  /// No description provided for @mismatchTitle.
  ///
  /// In en, this message translates to:
  /// **'That stop doesn\'t match'**
  String get mismatchTitle;

  /// No description provided for @mismatchBody.
  ///
  /// In en, this message translates to:
  /// **'{attempt} was not saved. The train is at {station}. Nothing on the journey has changed.'**
  String mismatchBody(Object attempt, Object station);

  /// No description provided for @mismatchRecord.
  ///
  /// In en, this message translates to:
  /// **'Record {event}'**
  String mismatchRecord(Object event);

  /// No description provided for @mismatchHold.
  ///
  /// In en, this message translates to:
  /// **'Hold for control'**
  String get mismatchHold;

  /// No description provided for @journeyCompleteTitle.
  ///
  /// In en, this message translates to:
  /// **'Journey complete'**
  String get journeyCompleteTitle;

  /// No description provided for @journeyCompleteBody.
  ///
  /// In en, this message translates to:
  /// **'{time} {route} · arrived {arrived}'**
  String journeyCompleteBody(Object arrived, Object route, Object time);

  /// No description provided for @journeyCompleteBodyLate.
  ///
  /// In en, this message translates to:
  /// **'{time} {route} · arrived {arrived}, {minutes} min late'**
  String journeyCompleteBodyLate(
    Object arrived,
    Object minutes,
    Object route,
    Object time,
  );

  /// No description provided for @completeEvents.
  ///
  /// In en, this message translates to:
  /// **'Events sent'**
  String get completeEvents;

  /// No description provided for @completeReasons.
  ///
  /// In en, this message translates to:
  /// **'Delay reasons'**
  String get completeReasons;

  /// No description provided for @completeAcks.
  ///
  /// In en, this message translates to:
  /// **'Messages acknowledged'**
  String get completeAcks;

  /// No description provided for @backToDuty.
  ///
  /// In en, this message translates to:
  /// **'Back to duty'**
  String get backToDuty;

  /// No description provided for @messagesTitle.
  ///
  /// In en, this message translates to:
  /// **'Alerts'**
  String get messagesTitle;

  /// No description provided for @messagesSub.
  ///
  /// In en, this message translates to:
  /// **'From control'**
  String get messagesSub;

  /// No description provided for @messageDelivered.
  ///
  /// In en, this message translates to:
  /// **'Delivered'**
  String get messageDelivered;

  /// No description provided for @messageAcked.
  ///
  /// In en, this message translates to:
  /// **'Acknowledged {time}'**
  String messageAcked(Object time);

  /// No description provided for @messageFrom.
  ///
  /// In en, this message translates to:
  /// **'Control · {time}'**
  String messageFrom(Object time);

  /// No description provided for @messageSheetFrom.
  ///
  /// In en, this message translates to:
  /// **'Message from control · {time}'**
  String messageSheetFrom(Object time);

  /// No description provided for @messageWaiting.
  ///
  /// In en, this message translates to:
  /// **'Delivered {time} · waiting for your acknowledgement'**
  String messageWaiting(Object time);

  /// No description provided for @messageAckButton.
  ///
  /// In en, this message translates to:
  /// **'Acknowledge'**
  String get messageAckButton;

  /// No description provided for @assistTitle.
  ///
  /// In en, this message translates to:
  /// **'Request assistance'**
  String get assistTitle;

  /// No description provided for @assistBody.
  ///
  /// In en, this message translates to:
  /// **'Control sees your journey and last known position.'**
  String get assistBody;

  /// No description provided for @assistMedical.
  ///
  /// In en, this message translates to:
  /// **'Medical'**
  String get assistMedical;

  /// No description provided for @assistOvercrowding.
  ///
  /// In en, this message translates to:
  /// **'Overcrowding'**
  String get assistOvercrowding;

  /// No description provided for @assistUnsafe.
  ///
  /// In en, this message translates to:
  /// **'Unsafe passenger situation'**
  String get assistUnsafe;

  /// No description provided for @assistOther.
  ///
  /// In en, this message translates to:
  /// **'Other'**
  String get assistOther;

  /// No description provided for @assistContinue.
  ///
  /// In en, this message translates to:
  /// **'Continue'**
  String get assistContinue;

  /// No description provided for @assistConfirmTitle.
  ///
  /// In en, this message translates to:
  /// **'Send request to control?'**
  String get assistConfirmTitle;

  /// No description provided for @assistConfirmBody.
  ///
  /// In en, this message translates to:
  /// **'{type} · {journey}. This does not change the journey. In an emergency, use the railway radio.'**
  String assistConfirmBody(Object journey, Object type);

  /// No description provided for @assistSend.
  ///
  /// In en, this message translates to:
  /// **'Send request'**
  String get assistSend;

  /// No description provided for @goBack.
  ///
  /// In en, this message translates to:
  /// **'Go back'**
  String get goBack;

  /// No description provided for @assistSentTitle.
  ///
  /// In en, this message translates to:
  /// **'Request sent'**
  String get assistSentTitle;

  /// No description provided for @assistSentBody.
  ///
  /// In en, this message translates to:
  /// **'Control has your request and will decide what happens next. Stay on the railway radio.'**
  String get assistSentBody;

  /// No description provided for @assistType.
  ///
  /// In en, this message translates to:
  /// **'Type'**
  String get assistType;

  /// No description provided for @assistJourney.
  ///
  /// In en, this message translates to:
  /// **'Journey'**
  String get assistJourney;

  /// No description provided for @assistSent.
  ///
  /// In en, this message translates to:
  /// **'Sent'**
  String get assistSent;

  /// No description provided for @backToJourney.
  ///
  /// In en, this message translates to:
  /// **'Back to journey'**
  String get backToJourney;

  /// No description provided for @meTitle.
  ///
  /// In en, this message translates to:
  /// **'Me'**
  String get meTitle;

  /// No description provided for @meRole.
  ///
  /// In en, this message translates to:
  /// **'Driver'**
  String get meRole;

  /// No description provided for @meDuty.
  ///
  /// In en, this message translates to:
  /// **'Driver · Duty {from} to {to}'**
  String meDuty(Object from, Object to);

  /// No description provided for @sharedPhoneTitle.
  ///
  /// In en, this message translates to:
  /// **'Shared phone'**
  String get sharedPhoneTitle;

  /// No description provided for @sharedPhoneBody.
  ///
  /// In en, this message translates to:
  /// **'Your saved duty and unsent events are cleared when you sign out.'**
  String get sharedPhoneBody;

  /// No description provided for @signOutRow.
  ///
  /// In en, this message translates to:
  /// **'Sign out'**
  String get signOutRow;

  /// No description provided for @signOutTitle.
  ///
  /// In en, this message translates to:
  /// **'Sign out of SmartRail?'**
  String get signOutTitle;

  /// No description provided for @signOutBody.
  ///
  /// In en, this message translates to:
  /// **'Your saved duty and any unsent events on this phone are cleared.'**
  String get signOutBody;

  /// No description provided for @signOutConfirm.
  ///
  /// In en, this message translates to:
  /// **'Sign out'**
  String get signOutConfirm;

  /// No description provided for @chooseLanguageTitle.
  ///
  /// In en, this message translates to:
  /// **'Choose your language'**
  String get chooseLanguageTitle;

  /// No description provided for @chooseLanguageBody.
  ///
  /// In en, this message translates to:
  /// **'You can change this later in Me.'**
  String get chooseLanguageBody;

  /// No description provided for @continueButton.
  ///
  /// In en, this message translates to:
  /// **'Continue'**
  String get continueButton;

  /// No description provided for @languageEnglish.
  ///
  /// In en, this message translates to:
  /// **'English'**
  String get languageEnglish;

  /// No description provided for @languageSinhala.
  ///
  /// In en, this message translates to:
  /// **'සිංහල'**
  String get languageSinhala;

  /// No description provided for @meLanguage.
  ///
  /// In en, this message translates to:
  /// **'Language'**
  String get meLanguage;

  /// No description provided for @meHelpTitle.
  ///
  /// In en, this message translates to:
  /// **'Help'**
  String get meHelpTitle;

  /// No description provided for @techIssueRow.
  ///
  /// In en, this message translates to:
  /// **'Report a technical issue'**
  String get techIssueRow;

  /// No description provided for @emergencyRow.
  ///
  /// In en, this message translates to:
  /// **'Emergency contacts'**
  String get emergencyRow;

  /// No description provided for @techIssueTitle.
  ///
  /// In en, this message translates to:
  /// **'Report a technical issue'**
  String get techIssueTitle;

  /// No description provided for @techIssueBody.
  ///
  /// In en, this message translates to:
  /// **'Tell control about a problem with the app, your phone or the train\'s equipment. It is sent to control, not saved on your phone.'**
  String get techIssueBody;

  /// No description provided for @techApp.
  ///
  /// In en, this message translates to:
  /// **'App problem'**
  String get techApp;

  /// No description provided for @techPhone.
  ///
  /// In en, this message translates to:
  /// **'Phone or signal problem'**
  String get techPhone;

  /// No description provided for @techTrain.
  ///
  /// In en, this message translates to:
  /// **'Train equipment fault'**
  String get techTrain;

  /// No description provided for @techOther.
  ///
  /// In en, this message translates to:
  /// **'Other'**
  String get techOther;

  /// No description provided for @techNoteHint.
  ///
  /// In en, this message translates to:
  /// **'What is wrong? (optional)'**
  String get techNoteHint;

  /// No description provided for @techSend.
  ///
  /// In en, this message translates to:
  /// **'Send to control'**
  String get techSend;

  /// No description provided for @techSentTitle.
  ///
  /// In en, this message translates to:
  /// **'Sent to control'**
  String get techSentTitle;

  /// No description provided for @techSentBody.
  ///
  /// In en, this message translates to:
  /// **'Control has your report.'**
  String get techSentBody;

  /// No description provided for @emergencyTitle.
  ///
  /// In en, this message translates to:
  /// **'Emergency contacts'**
  String get emergencyTitle;

  /// No description provided for @emergencyBody.
  ///
  /// In en, this message translates to:
  /// **'In an emergency, use the railway radio first.'**
  String get emergencyBody;

  /// No description provided for @contactPolice.
  ///
  /// In en, this message translates to:
  /// **'Police'**
  String get contactPolice;

  /// No description provided for @contactAmbulance.
  ///
  /// In en, this message translates to:
  /// **'Ambulance (Suwa Seriya)'**
  String get contactAmbulance;

  /// No description provided for @contactFire.
  ///
  /// In en, this message translates to:
  /// **'Fire and rescue'**
  String get contactFire;

  /// No description provided for @noticeNetwork.
  ///
  /// In en, this message translates to:
  /// **'Network alert from control'**
  String get noticeNetwork;

  /// No description provided for @alertDirect.
  ///
  /// In en, this message translates to:
  /// **'Direct to you'**
  String get alertDirect;

  /// No description provided for @alertNetwork.
  ///
  /// In en, this message translates to:
  /// **'Network alert'**
  String get alertNetwork;

  /// No description provided for @severityMinor.
  ///
  /// In en, this message translates to:
  /// **'Minor'**
  String get severityMinor;

  /// No description provided for @severityMajor.
  ///
  /// In en, this message translates to:
  /// **'Major'**
  String get severityMajor;

  /// No description provided for @severitySevere.
  ///
  /// In en, this message translates to:
  /// **'Severe'**
  String get severitySevere;

  /// No description provided for @allStations.
  ///
  /// In en, this message translates to:
  /// **'All stations'**
  String get allStations;

  /// No description provided for @alertSheetNetwork.
  ///
  /// In en, this message translates to:
  /// **'Network alert · {time}'**
  String alertSheetNetwork(Object time);

  /// No description provided for @alertSheetPlaces.
  ///
  /// In en, this message translates to:
  /// **'Applies to {places}'**
  String alertSheetPlaces(Object places);

  /// No description provided for @skipButton.
  ///
  /// In en, this message translates to:
  /// **'Not stopping here'**
  String get skipButton;

  /// No description provided for @alertsTabDirect.
  ///
  /// In en, this message translates to:
  /// **'Direct'**
  String get alertsTabDirect;

  /// No description provided for @alertsTabNetwork.
  ///
  /// In en, this message translates to:
  /// **'Network'**
  String get alertsTabNetwork;

  /// No description provided for @alertsEmptyDirect.
  ///
  /// In en, this message translates to:
  /// **'No messages for you from control.'**
  String get alertsEmptyDirect;

  /// No description provided for @alertsEmptyNetwork.
  ///
  /// In en, this message translates to:
  /// **'No network alerts right now.'**
  String get alertsEmptyNetwork;
}

class _AppLocalizationsDelegate
    extends LocalizationsDelegate<AppLocalizations> {
  const _AppLocalizationsDelegate();

  @override
  Future<AppLocalizations> load(Locale locale) {
    return SynchronousFuture<AppLocalizations>(lookupAppLocalizations(locale));
  }

  @override
  bool isSupported(Locale locale) =>
      <String>['en', 'si'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'en':
      return AppLocalizationsEn();
    case 'si':
      return AppLocalizationsSi();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
