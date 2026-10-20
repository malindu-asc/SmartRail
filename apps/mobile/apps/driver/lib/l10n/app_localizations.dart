import 'dart:async';

import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';
import 'package:flutter_localizations/flutter_localizations.dart';
import 'package:intl/intl.dart' as intl;

import 'app_localizations_en.dart';

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
  static const List<Locale> supportedLocales = <Locale>[Locale('en')];

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
  /// **'Messages'**
  String get tabMessages;

  /// No description provided for @tabMe.
  ///
  /// In en, this message translates to:
  /// **'Me'**
  String get tabMe;

  /// No description provided for @offlineTitle.
  ///
  /// In en, this message translates to:
  /// **'You are offline'**
  String get offlineTitle;

  /// No description provided for @offlineDetail.
  ///
  /// In en, this message translates to:
  /// **'Actions cannot be sent until the connection is back.'**
  String get offlineDetail;

  /// No description provided for @signInTitle.
  ///
  /// In en, this message translates to:
  /// **'Driver sign in'**
  String get signInTitle;

  /// No description provided for @signInWelcome.
  ///
  /// In en, this message translates to:
  /// **'Sign in with the account your administrator gave you.'**
  String get signInWelcome;

  /// No description provided for @emailLabel.
  ///
  /// In en, this message translates to:
  /// **'Email'**
  String get emailLabel;

  /// No description provided for @emailHint.
  ///
  /// In en, this message translates to:
  /// **'name@railway.lk'**
  String get emailHint;

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

  /// No description provided for @signInButton.
  ///
  /// In en, this message translates to:
  /// **'Sign in'**
  String get signInButton;

  /// No description provided for @signInBadCredentials.
  ///
  /// In en, this message translates to:
  /// **'That email or password is not right. Check them and try again.'**
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

  /// No description provided for @dutyNoneTitle.
  ///
  /// In en, this message translates to:
  /// **'No duty today'**
  String get dutyNoneTitle;

  /// No description provided for @dutyNoneBody.
  ///
  /// In en, this message translates to:
  /// **'Nothing is assigned to you today. The scheduler assigns duties. Pull down to check again.'**
  String get dutyNoneBody;

  /// No description provided for @dutyUpcoming.
  ///
  /// In en, this message translates to:
  /// **'Your duty starts at {time}. You can sign on from 30 minutes before.'**
  String dutyUpcoming(Object time);

  /// No description provided for @dutyEnded.
  ///
  /// In en, this message translates to:
  /// **'This duty has ended.'**
  String get dutyEnded;

  /// No description provided for @dutyHistoryEmpty.
  ///
  /// In en, this message translates to:
  /// **'No past duties in the last two weeks.'**
  String get dutyHistoryEmpty;

  /// No description provided for @dutyNeedsReview.
  ///
  /// In en, this message translates to:
  /// **'This duty needs review by control.'**
  String get dutyNeedsReview;

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

  /// No description provided for @signedOnAt.
  ///
  /// In en, this message translates to:
  /// **'Signed on {time}'**
  String signedOnAt(Object time);

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

  /// No description provided for @noticeFromControl.
  ///
  /// In en, this message translates to:
  /// **'Notice from control'**
  String get noticeFromControl;

  /// No description provided for @signOffTitle.
  ///
  /// In en, this message translates to:
  /// **'Sign off duty?'**
  String get signOffTitle;

  /// No description provided for @signOffBody.
  ///
  /// In en, this message translates to:
  /// **'Only sign off when your shift is over. Journeys you have not finished stay with control.'**
  String get signOffBody;

  /// No description provided for @signOffConfirm.
  ///
  /// In en, this message translates to:
  /// **'Sign off'**
  String get signOffConfirm;

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

  /// No description provided for @movingOnly.
  ///
  /// In en, this message translates to:
  /// **'Moving · one-tap actions only'**
  String get movingOnly;

  /// No description provided for @nextStop.
  ///
  /// In en, this message translates to:
  /// **'NEXT STOP'**
  String get nextStop;

  /// No description provided for @atStop.
  ///
  /// In en, this message translates to:
  /// **'AT STOP'**
  String get atStop;

  /// No description provided for @scheduledAt.
  ///
  /// In en, this message translates to:
  /// **'Scheduled {time}'**
  String scheduledAt(Object time);

  /// No description provided for @expectedAt.
  ///
  /// In en, this message translates to:
  /// **'Expected {time}'**
  String expectedAt(Object time);

  /// No description provided for @delayChip.
  ///
  /// In en, this message translates to:
  /// **'+{minutes} min'**
  String delayChip(Object minutes);

  /// No description provided for @earlyChip.
  ///
  /// In en, this message translates to:
  /// **'{minutes} min early'**
  String earlyChip(Object minutes);

  /// No description provided for @platformCoach.
  ///
  /// In en, this message translates to:
  /// **'Platform {platform}'**
  String platformCoach(Object platform);

  /// No description provided for @stopHere.
  ///
  /// In en, this message translates to:
  /// **'here now'**
  String get stopHere;

  /// No description provided for @stopEstimate.
  ///
  /// In en, this message translates to:
  /// **'Estimate · may change'**
  String get stopEstimate;

  /// No description provided for @stopPlatformExpected.
  ///
  /// In en, this message translates to:
  /// **'Platform {platform} · expected'**
  String stopPlatformExpected(Object platform);

  /// No description provided for @stopDeparted.
  ///
  /// In en, this message translates to:
  /// **'Departed {time}'**
  String stopDeparted(Object time);

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

  /// No description provided for @delayReasonButton.
  ///
  /// In en, this message translates to:
  /// **'Delay reason'**
  String get delayReasonButton;

  /// No description provided for @moreSkipStop.
  ///
  /// In en, this message translates to:
  /// **'Skip this stop'**
  String get moreSkipStop;

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
  /// **'Skip {station}?'**
  String skipTitle(Object station);

  /// No description provided for @skipBody.
  ///
  /// In en, this message translates to:
  /// **'Use this only when the train does not stop here. Passengers will see the stop as skipped.'**
  String get skipBody;

  /// No description provided for @skipConfirm.
  ///
  /// In en, this message translates to:
  /// **'Skip this stop'**
  String get skipConfirm;

  /// No description provided for @mismatchTitle.
  ///
  /// In en, this message translates to:
  /// **'That is not the stop the train is at'**
  String get mismatchTitle;

  /// No description provided for @mismatchBody.
  ///
  /// In en, this message translates to:
  /// **'{expected}'**
  String mismatchBody(Object expected);

  /// No description provided for @mismatchRefresh.
  ///
  /// In en, this message translates to:
  /// **'Refresh the journey'**
  String get mismatchRefresh;

  /// No description provided for @journeyCompleteTitle.
  ///
  /// In en, this message translates to:
  /// **'Journey complete'**
  String get journeyCompleteTitle;

  /// No description provided for @journeyCompleteBody.
  ///
  /// In en, this message translates to:
  /// **'The train has arrived at {station}. Your events are saved.'**
  String journeyCompleteBody(Object station);

  /// No description provided for @backToDuty.
  ///
  /// In en, this message translates to:
  /// **'Back to duty'**
  String get backToDuty;

  /// No description provided for @messagesTitle.
  ///
  /// In en, this message translates to:
  /// **'Messages'**
  String get messagesTitle;

  /// No description provided for @messagesEmpty.
  ///
  /// In en, this message translates to:
  /// **'No messages from control.'**
  String get messagesEmpty;

  /// No description provided for @messageAckButton.
  ///
  /// In en, this message translates to:
  /// **'Acknowledge'**
  String get messageAckButton;

  /// No description provided for @messageAcked.
  ///
  /// In en, this message translates to:
  /// **'Acknowledged {time}'**
  String messageAcked(Object time);

  /// No description provided for @messageNeedsAck.
  ///
  /// In en, this message translates to:
  /// **'Needs your acknowledgement'**
  String get messageNeedsAck;

  /// No description provided for @messageFromControl.
  ///
  /// In en, this message translates to:
  /// **'Message from control'**
  String get messageFromControl;

  /// No description provided for @messageSent.
  ///
  /// In en, this message translates to:
  /// **'Sent {time}'**
  String messageSent(Object time);

  /// No description provided for @assistTitle.
  ///
  /// In en, this message translates to:
  /// **'Request assistance'**
  String get assistTitle;

  /// No description provided for @assistBody.
  ///
  /// In en, this message translates to:
  /// **'Control sees where you are and who you are. Say what is happening if you can.'**
  String get assistBody;

  /// No description provided for @assistNoteHint.
  ///
  /// In en, this message translates to:
  /// **'What is happening? (optional)'**
  String get assistNoteHint;

  /// No description provided for @assistSend.
  ///
  /// In en, this message translates to:
  /// **'Send request'**
  String get assistSend;

  /// No description provided for @assistSentTitle.
  ///
  /// In en, this message translates to:
  /// **'Request sent'**
  String get assistSentTitle;

  /// No description provided for @assistSentBody.
  ///
  /// In en, this message translates to:
  /// **'Control has your request and will contact you. Stay safe.'**
  String get assistSentBody;

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

  /// No description provided for @signOutRow.
  ///
  /// In en, this message translates to:
  /// **'Sign out'**
  String get signOutRow;

  /// No description provided for @signOutTitle.
  ///
  /// In en, this message translates to:
  /// **'Sign out?'**
  String get signOutTitle;

  /// No description provided for @signOutBody.
  ///
  /// In en, this message translates to:
  /// **'You will need to sign in again to see your duty.'**
  String get signOutBody;

  /// No description provided for @signOutConfirm.
  ///
  /// In en, this message translates to:
  /// **'Sign out'**
  String get signOutConfirm;

  /// No description provided for @signOutBlocked.
  ///
  /// In en, this message translates to:
  /// **'Sign off your duty before signing out.'**
  String get signOutBlocked;

  /// No description provided for @notFoundTitle.
  ///
  /// In en, this message translates to:
  /// **'We could not find that page'**
  String get notFoundTitle;
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
      <String>['en'].contains(locale.languageCode);

  @override
  bool shouldReload(_AppLocalizationsDelegate old) => false;
}

AppLocalizations lookupAppLocalizations(Locale locale) {
  // Lookup logic when only language code is specified.
  switch (locale.languageCode) {
    case 'en':
      return AppLocalizationsEn();
  }

  throw FlutterError(
    'AppLocalizations.delegate failed to load unsupported locale "$locale". This is likely '
    'an issue with the localizations generation tool. Please file an issue '
    'on GitHub with a reproducible sample app and the gen-l10n configuration '
    'that was used.',
  );
}
