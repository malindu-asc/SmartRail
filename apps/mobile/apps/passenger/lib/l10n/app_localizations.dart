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
  /// **'SmartRail'**
  String get appTitle;

  /// No description provided for @tabHome.
  ///
  /// In en, this message translates to:
  /// **'Home'**
  String get tabHome;

  /// No description provided for @tabPlan.
  ///
  /// In en, this message translates to:
  /// **'Plan'**
  String get tabPlan;

  /// No description provided for @tabTickets.
  ///
  /// In en, this message translates to:
  /// **'Tickets'**
  String get tabTickets;

  /// No description provided for @tabAlerts.
  ///
  /// In en, this message translates to:
  /// **'Alerts'**
  String get tabAlerts;

  /// No description provided for @tabMe.
  ///
  /// In en, this message translates to:
  /// **'Me'**
  String get tabMe;

  /// No description provided for @onbSkip.
  ///
  /// In en, this message translates to:
  /// **'Skip'**
  String get onbSkip;

  /// No description provided for @onbNext.
  ///
  /// In en, this message translates to:
  /// **'Next'**
  String get onbNext;

  /// No description provided for @onbDone.
  ///
  /// In en, this message translates to:
  /// **'Go to app'**
  String get onbDone;

  /// No description provided for @onb1Title.
  ///
  /// In en, this message translates to:
  /// **'Know before you go'**
  String get onb1Title;

  /// No description provided for @onb1Body.
  ///
  /// In en, this message translates to:
  /// **'Live departures, delays and platform changes for every station.'**
  String get onb1Body;

  /// No description provided for @onb2Title.
  ///
  /// In en, this message translates to:
  /// **'Follow your journey'**
  String get onb2Title;

  /// No description provided for @onb2Body.
  ///
  /// In en, this message translates to:
  /// **'Get an alert when your platform changes or your train is delayed.'**
  String get onb2Body;

  /// No description provided for @onb3Title.
  ///
  /// In en, this message translates to:
  /// **'Honest about old data'**
  String get onb3Title;

  /// No description provided for @onb3Body.
  ///
  /// In en, this message translates to:
  /// **'When we can\'t confirm a time, we say so instead of guessing.'**
  String get onb3Body;

  /// No description provided for @greetingMorning.
  ///
  /// In en, this message translates to:
  /// **'Good morning'**
  String get greetingMorning;

  /// No description provided for @greetingAfternoon.
  ///
  /// In en, this message translates to:
  /// **'Good afternoon'**
  String get greetingAfternoon;

  /// No description provided for @greetingEvening.
  ///
  /// In en, this message translates to:
  /// **'Good evening'**
  String get greetingEvening;

  /// No description provided for @homeWhereTo.
  ///
  /// In en, this message translates to:
  /// **'Where to today?'**
  String get homeWhereTo;

  /// No description provided for @fromLabel.
  ///
  /// In en, this message translates to:
  /// **'From'**
  String get fromLabel;

  /// No description provided for @toLabel.
  ///
  /// In en, this message translates to:
  /// **'To'**
  String get toLabel;

  /// No description provided for @chooseStation.
  ///
  /// In en, this message translates to:
  /// **'Choose a station'**
  String get chooseStation;

  /// No description provided for @findTrains.
  ///
  /// In en, this message translates to:
  /// **'Find trains'**
  String get findTrains;

  /// No description provided for @homeFollowedStations.
  ///
  /// In en, this message translates to:
  /// **'Followed stations'**
  String get homeFollowedStations;

  /// No description provided for @homeFollowEmptyTitle.
  ///
  /// In en, this message translates to:
  /// **'No followed stations yet'**
  String get homeFollowEmptyTitle;

  /// No description provided for @homeFollowEmptyBody.
  ///
  /// In en, this message translates to:
  /// **'Follow a station to see its next train here.'**
  String get homeFollowEmptyBody;

  /// No description provided for @homeFindStation.
  ///
  /// In en, this message translates to:
  /// **'Find a station'**
  String get homeFindStation;

  /// No description provided for @homeFollowedJourneys.
  ///
  /// In en, this message translates to:
  /// **'Followed journeys'**
  String get homeFollowedJourneys;

  /// No description provided for @stationsTitle.
  ///
  /// In en, this message translates to:
  /// **'Stations'**
  String get stationsTitle;

  /// No description provided for @stationsSearchLabel.
  ///
  /// In en, this message translates to:
  /// **'Station name or code'**
  String get stationsSearchLabel;

  /// No description provided for @stationsSearchHint.
  ///
  /// In en, this message translates to:
  /// **'For example Central or CEN'**
  String get stationsSearchHint;

  /// No description provided for @stationsResults.
  ///
  /// In en, this message translates to:
  /// **'Results'**
  String get stationsResults;

  /// No description provided for @stationsRecent.
  ///
  /// In en, this message translates to:
  /// **'Recent'**
  String get stationsRecent;

  /// No description provided for @stationsEmptyTitle.
  ///
  /// In en, this message translates to:
  /// **'No stations match'**
  String get stationsEmptyTitle;

  /// No description provided for @stationsEmptyBody.
  ///
  /// In en, this message translates to:
  /// **'Try the name or the short code, for example CEN.'**
  String get stationsEmptyBody;

  /// No description provided for @stationsIntro.
  ///
  /// In en, this message translates to:
  /// **'Search for a station to see its live departures.'**
  String get stationsIntro;

  /// No description provided for @freshLive.
  ///
  /// In en, this message translates to:
  /// **'Live · updated {time}'**
  String freshLive(String time);

  /// No description provided for @freshStale.
  ///
  /// In en, this message translates to:
  /// **'As of {time}'**
  String freshStale(String time);

  /// No description provided for @freshSaved.
  ///
  /// In en, this message translates to:
  /// **'Saved {time}'**
  String freshSaved(String time);

  /// No description provided for @departuresToday.
  ///
  /// In en, this message translates to:
  /// **'Today'**
  String get departuresToday;

  /// No description provided for @departuresEmptyTitle.
  ///
  /// In en, this message translates to:
  /// **'No more trains today'**
  String get departuresEmptyTitle;

  /// No description provided for @departuresEmptyBody.
  ///
  /// In en, this message translates to:
  /// **'Check again tomorrow or look at another station.'**
  String get departuresEmptyBody;

  /// No description provided for @inMinutes.
  ///
  /// In en, this message translates to:
  /// **'in {n} min'**
  String inMinutes(int n);

  /// No description provided for @platformN.
  ///
  /// In en, this message translates to:
  /// **'Platform {p}'**
  String platformN(String p);

  /// No description provided for @platformTba.
  ///
  /// In en, this message translates to:
  /// **'Platform to be announced'**
  String get platformTba;

  /// No description provided for @chipOnTime.
  ///
  /// In en, this message translates to:
  /// **'On time'**
  String get chipOnTime;

  /// No description provided for @chipDelayed.
  ///
  /// In en, this message translates to:
  /// **'+{n} min'**
  String chipDelayed(int n);

  /// No description provided for @chipCancelled.
  ///
  /// In en, this message translates to:
  /// **'Cancelled'**
  String get chipCancelled;

  /// No description provided for @chipPlatformChanged.
  ///
  /// In en, this message translates to:
  /// **'Platform changed'**
  String get chipPlatformChanged;

  /// No description provided for @chipNoLiveData.
  ///
  /// In en, this message translates to:
  /// **'No live data'**
  String get chipNoLiveData;

  /// No description provided for @scheduledOnly.
  ///
  /// In en, this message translates to:
  /// **'Scheduled'**
  String get scheduledOnly;

  /// No description provided for @scheduledNoUpdate.
  ///
  /// In en, this message translates to:
  /// **'Scheduled · no update since {time}'**
  String scheduledNoUpdate(String time);

  /// No description provided for @expectedAt.
  ///
  /// In en, this message translates to:
  /// **'Expected {time}'**
  String expectedAt(String time);

  /// No description provided for @followStation.
  ///
  /// In en, this message translates to:
  /// **'Follow station'**
  String get followStation;

  /// No description provided for @followingStation.
  ///
  /// In en, this message translates to:
  /// **'Following'**
  String get followingStation;

  /// No description provided for @indicativeNote.
  ///
  /// In en, this message translates to:
  /// **'Times are provided for information. Listen for station announcements.'**
  String get indicativeNote;

  /// No description provided for @trainNumber.
  ///
  /// In en, this message translates to:
  /// **'Train {n}'**
  String trainNumber(String n);

  /// No description provided for @journeyHeader.
  ///
  /// In en, this message translates to:
  /// **'{time} to {destination}'**
  String journeyHeader(String time, String destination);

  /// No description provided for @journeyTitle.
  ///
  /// In en, this message translates to:
  /// **'{origin} to {destination}'**
  String journeyTitle(String origin, String destination);

  /// No description provided for @headlineRunningOnTime.
  ///
  /// In en, this message translates to:
  /// **'Running · on time'**
  String get headlineRunningOnTime;

  /// No description provided for @headlineDelayed.
  ///
  /// In en, this message translates to:
  /// **'Delayed · +{n} min'**
  String headlineDelayed(int n);

  /// No description provided for @headlineNoLive.
  ///
  /// In en, this message translates to:
  /// **'No live data since {time}'**
  String headlineNoLive(String time);

  /// No description provided for @headlineNoLiveEver.
  ///
  /// In en, this message translates to:
  /// **'No live data yet'**
  String get headlineNoLiveEver;

  /// No description provided for @headlineCancelled.
  ///
  /// In en, this message translates to:
  /// **'Cancelled'**
  String get headlineCancelled;

  /// No description provided for @headlineScheduled.
  ///
  /// In en, this message translates to:
  /// **'Scheduled · not departed yet'**
  String get headlineScheduled;

  /// No description provided for @headlineCompleted.
  ///
  /// In en, this message translates to:
  /// **'Journey complete'**
  String get headlineCompleted;

  /// No description provided for @detailLive.
  ///
  /// In en, this message translates to:
  /// **'Live · updated {time}'**
  String detailLive(String time);

  /// No description provided for @detailLiveDeparted.
  ///
  /// In en, this message translates to:
  /// **'Live · updated {updated} · Departed {station} at {time}'**
  String detailLiveDeparted(String updated, String station, String time);

  /// No description provided for @detailLastKnown.
  ///
  /// In en, this message translates to:
  /// **'Last known: departed {station} {time}'**
  String detailLastKnown(String station, String time);

  /// No description provided for @detailNoLiveBody.
  ///
  /// In en, this message translates to:
  /// **'Showing the timetable. Times are not live. Listen for station announcements.'**
  String get detailNoLiveBody;

  /// No description provided for @detailDelayedBody.
  ///
  /// In en, this message translates to:
  /// **'Expected {expected} at {station} instead of {scheduled}.'**
  String detailDelayedBody(String expected, String station, String scheduled);

  /// No description provided for @detailCancelledBody.
  ///
  /// In en, this message translates to:
  /// **'This train will not run. Look for another train or contact the call centre.'**
  String get detailCancelledBody;

  /// No description provided for @detailScheduledBody.
  ///
  /// In en, this message translates to:
  /// **'This train has not left yet. Times are the timetable.'**
  String get detailScheduledBody;

  /// No description provided for @platformBannerTitle.
  ///
  /// In en, this message translates to:
  /// **'Platform {p}'**
  String platformBannerTitle(String p);

  /// No description provided for @platformBannerBody.
  ///
  /// In en, this message translates to:
  /// **'Platform changed at {station}'**
  String platformBannerBody(String station);

  /// No description provided for @wordDepartedOnTime.
  ///
  /// In en, this message translates to:
  /// **'Departed {time} · on time'**
  String wordDepartedOnTime(String time);

  /// No description provided for @wordDepartedLate.
  ///
  /// In en, this message translates to:
  /// **'Departed {time} · {n} min late'**
  String wordDepartedLate(String time, int n);

  /// No description provided for @wordArrivedOnTime.
  ///
  /// In en, this message translates to:
  /// **'Arrived {time} · on time'**
  String wordArrivedOnTime(String time);

  /// No description provided for @wordArrivedLate.
  ///
  /// In en, this message translates to:
  /// **'Arrived {time} · {n} min late'**
  String wordArrivedLate(String time, int n);

  /// No description provided for @wordArrivedEarly.
  ///
  /// In en, this message translates to:
  /// **'Arrived {time} · {n} min early'**
  String wordArrivedEarly(String time, int n);

  /// No description provided for @wordAtPlatform.
  ///
  /// In en, this message translates to:
  /// **'At the platform since {time}'**
  String wordAtPlatform(String time);

  /// No description provided for @wordExpectedOnTime.
  ///
  /// In en, this message translates to:
  /// **'Expected {time} · on time'**
  String wordExpectedOnTime(String time);

  /// No description provided for @wordExpected.
  ///
  /// In en, this message translates to:
  /// **'Expected {time}'**
  String wordExpected(String time);

  /// No description provided for @wordEstimate.
  ///
  /// In en, this message translates to:
  /// **'Estimate · may change'**
  String get wordEstimate;

  /// No description provided for @wordScheduled.
  ///
  /// In en, this message translates to:
  /// **'Scheduled'**
  String get wordScheduled;

  /// No description provided for @wordSkipped.
  ///
  /// In en, this message translates to:
  /// **'Not stopping'**
  String get wordSkipped;

  /// No description provided for @estimatesNote.
  ///
  /// In en, this message translates to:
  /// **'Times after {station} are estimates and may change.'**
  String estimatesNote(String station);

  /// No description provided for @followJourney.
  ///
  /// In en, this message translates to:
  /// **'Follow this journey'**
  String get followJourney;

  /// No description provided for @followingJourney.
  ///
  /// In en, this message translates to:
  /// **'Following'**
  String get followingJourney;

  /// No description provided for @snackFollowingJourney.
  ///
  /// In en, this message translates to:
  /// **'You are following this journey'**
  String get snackFollowingJourney;

  /// No description provided for @snackUnfollowedJourney.
  ///
  /// In en, this message translates to:
  /// **'You stopped following this journey'**
  String get snackUnfollowedJourney;

  /// No description provided for @snackFollowingStation.
  ///
  /// In en, this message translates to:
  /// **'You are following this station'**
  String get snackFollowingStation;

  /// No description provided for @snackUnfollowedStation.
  ///
  /// In en, this message translates to:
  /// **'You stopped following this station'**
  String get snackUnfollowedStation;

  /// No description provided for @undo.
  ///
  /// In en, this message translates to:
  /// **'Undo'**
  String get undo;

  /// No description provided for @errorTitle.
  ///
  /// In en, this message translates to:
  /// **'Something went wrong'**
  String get errorTitle;

  /// No description provided for @errorNetwork.
  ///
  /// In en, this message translates to:
  /// **'We could not reach SmartRail. Check your connection and try again.'**
  String get errorNetwork;

  /// No description provided for @errorNotFound.
  ///
  /// In en, this message translates to:
  /// **'We could not find that. It may no longer exist.'**
  String get errorNotFound;

  /// No description provided for @errorGeneric.
  ///
  /// In en, this message translates to:
  /// **'Please try again in a moment.'**
  String get errorGeneric;

  /// No description provided for @retry.
  ///
  /// In en, this message translates to:
  /// **'Try again'**
  String get retry;

  /// No description provided for @comingTitlePlan.
  ///
  /// In en, this message translates to:
  /// **'Plan a journey'**
  String get comingTitlePlan;

  /// No description provided for @comingBodyPlan.
  ///
  /// In en, this message translates to:
  /// **'Choose two stations and see the trains between them, with a change if needed. This arrives in the next update. Until then you can look at any station\'s live departures.'**
  String get comingBodyPlan;

  /// No description provided for @comingTitleTickets.
  ///
  /// In en, this message translates to:
  /// **'Tickets'**
  String get comingTitleTickets;

  /// No description provided for @comingBodyTickets.
  ///
  /// In en, this message translates to:
  /// **'Your tickets will be here, and they will keep working without a connection. This arrives in an upcoming update.'**
  String get comingBodyTickets;

  /// No description provided for @comingTitleAlerts.
  ///
  /// In en, this message translates to:
  /// **'Alerts'**
  String get comingTitleAlerts;

  /// No description provided for @comingBodyAlerts.
  ///
  /// In en, this message translates to:
  /// **'Platform changes, delays and cancellations for the journeys and stations you follow will appear here. This arrives in an upcoming update.'**
  String get comingBodyAlerts;

  /// No description provided for @comingSoon.
  ///
  /// In en, this message translates to:
  /// **'Coming soon'**
  String get comingSoon;

  /// No description provided for @comingSeeDepartures.
  ///
  /// In en, this message translates to:
  /// **'See live departures'**
  String get comingSeeDepartures;

  /// No description provided for @meTitle.
  ///
  /// In en, this message translates to:
  /// **'Me'**
  String get meTitle;

  /// No description provided for @language.
  ///
  /// In en, this message translates to:
  /// **'Language'**
  String get language;

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

  /// No description provided for @meAbout.
  ///
  /// In en, this message translates to:
  /// **'SmartRail shows live railway information. Times are for information.'**
  String get meAbout;

  /// No description provided for @notFoundTitle.
  ///
  /// In en, this message translates to:
  /// **'Page not found'**
  String get notFoundTitle;

  /// No description provided for @notFoundBody.
  ///
  /// In en, this message translates to:
  /// **'That page does not exist.'**
  String get notFoundBody;

  /// No description provided for @notFoundAction.
  ///
  /// In en, this message translates to:
  /// **'Go to home'**
  String get notFoundAction;

  /// No description provided for @offlineTitle.
  ///
  /// In en, this message translates to:
  /// **'No connection'**
  String get offlineTitle;

  /// No description provided for @offlineDetail.
  ///
  /// In en, this message translates to:
  /// **'Showing the last information from {time}'**
  String offlineDetail(String time);

  /// No description provided for @offlineNever.
  ///
  /// In en, this message translates to:
  /// **'No information has been received yet.'**
  String get offlineNever;

  /// No description provided for @planTitle.
  ///
  /// In en, this message translates to:
  /// **'Plan a journey'**
  String get planTitle;

  /// No description provided for @planLeaveNow.
  ///
  /// In en, this message translates to:
  /// **'Leave now'**
  String get planLeaveNow;

  /// No description provided for @planLeaveAt.
  ///
  /// In en, this message translates to:
  /// **'Leave at {time}'**
  String planLeaveAt(String time);

  /// No description provided for @planPickTime.
  ///
  /// In en, this message translates to:
  /// **'Choose a time'**
  String get planPickTime;

  /// No description provided for @planSearch.
  ///
  /// In en, this message translates to:
  /// **'Show trains'**
  String get planSearch;

  /// No description provided for @resultsTitle.
  ///
  /// In en, this message translates to:
  /// **'{from} to {to}'**
  String resultsTitle(String from, String to);

  /// No description provided for @resultsDirect.
  ///
  /// In en, this message translates to:
  /// **'Direct'**
  String get resultsDirect;

  /// No description provided for @resultsChange.
  ///
  /// In en, this message translates to:
  /// **'1 change'**
  String get resultsChange;

  /// No description provided for @resultsDuration.
  ///
  /// In en, this message translates to:
  /// **'{n} min'**
  String resultsDuration(int n);

  /// No description provided for @resultsChangeAt.
  ///
  /// In en, this message translates to:
  /// **'Change at {station}'**
  String resultsChangeAt(String station);

  /// No description provided for @resultsWait.
  ///
  /// In en, this message translates to:
  /// **'{n} min to change'**
  String resultsWait(int n);

  /// No description provided for @resultsAtRiskTitle.
  ///
  /// In en, this message translates to:
  /// **'Connection at risk'**
  String get resultsAtRiskTitle;

  /// No description provided for @resultsAtRiskBody.
  ///
  /// In en, this message translates to:
  /// **'Only {n} min to change at {station}. You may miss the second train.'**
  String resultsAtRiskBody(int n, String station);

  /// No description provided for @resultsTrainLeg.
  ///
  /// In en, this message translates to:
  /// **'Train {number} · {from} to {to}'**
  String resultsTrainLeg(String number, String from, String to);

  /// No description provided for @resultsArrives.
  ///
  /// In en, this message translates to:
  /// **'Arrives {time}'**
  String resultsArrives(String time);

  /// No description provided for @resultsEmptyTitle.
  ///
  /// In en, this message translates to:
  /// **'No trains found'**
  String get resultsEmptyTitle;

  /// No description provided for @resultsEmptyBody.
  ///
  /// In en, this message translates to:
  /// **'There are no direct trains or trains with one change in the next hours. Try a later time.'**
  String get resultsEmptyBody;

  /// No description provided for @resultsLater.
  ///
  /// In en, this message translates to:
  /// **'Try later'**
  String get resultsLater;

  /// No description provided for @loginTitle.
  ///
  /// In en, this message translates to:
  /// **'Log in'**
  String get loginTitle;

  /// No description provided for @loginWelcome.
  ///
  /// In en, this message translates to:
  /// **'Welcome back'**
  String get loginWelcome;

  /// No description provided for @emailLabel.
  ///
  /// In en, this message translates to:
  /// **'Email'**
  String get emailLabel;

  /// No description provided for @emailHint.
  ///
  /// In en, this message translates to:
  /// **'Enter your email'**
  String get emailHint;

  /// No description provided for @passwordLabel.
  ///
  /// In en, this message translates to:
  /// **'Password'**
  String get passwordLabel;

  /// No description provided for @passwordHint.
  ///
  /// In en, this message translates to:
  /// **'Enter your password'**
  String get passwordHint;

  /// No description provided for @forgotPassword.
  ///
  /// In en, this message translates to:
  /// **'Forgot password?'**
  String get forgotPassword;

  /// No description provided for @forgotSent.
  ///
  /// In en, this message translates to:
  /// **'If that email has an account, we sent a link to reset the password.'**
  String get forgotSent;

  /// No description provided for @loginNoAccountNeeded.
  ///
  /// In en, this message translates to:
  /// **'You do not need an account to see departures, delays and platform changes.'**
  String get loginNoAccountNeeded;

  /// No description provided for @loginNew.
  ///
  /// In en, this message translates to:
  /// **'New here?'**
  String get loginNew;

  /// No description provided for @loginCreate.
  ///
  /// In en, this message translates to:
  /// **'Create profile'**
  String get loginCreate;

  /// No description provided for @loginBadCredentials.
  ///
  /// In en, this message translates to:
  /// **'That email and password do not match, or the account is locked for a few minutes.'**
  String get loginBadCredentials;

  /// No description provided for @createTitle.
  ///
  /// In en, this message translates to:
  /// **'Create profile'**
  String get createTitle;

  /// No description provided for @fullNameLabel.
  ///
  /// In en, this message translates to:
  /// **'Full name'**
  String get fullNameLabel;

  /// No description provided for @fullNameHint.
  ///
  /// In en, this message translates to:
  /// **'Jane Perera'**
  String get fullNameHint;

  /// No description provided for @passwordNewHint.
  ///
  /// In en, this message translates to:
  /// **'At least 8 characters'**
  String get passwordNewHint;

  /// No description provided for @agreeTerms.
  ///
  /// In en, this message translates to:
  /// **'I agree to the terms and the privacy notice.'**
  String get agreeTerms;

  /// No description provided for @errName.
  ///
  /// In en, this message translates to:
  /// **'Enter your full name.'**
  String get errName;

  /// No description provided for @errEmail.
  ///
  /// In en, this message translates to:
  /// **'Enter a valid email address.'**
  String get errEmail;

  /// No description provided for @errPassword.
  ///
  /// In en, this message translates to:
  /// **'The password needs at least 8 characters.'**
  String get errPassword;

  /// No description provided for @errTerms.
  ///
  /// In en, this message translates to:
  /// **'Please agree to continue.'**
  String get errTerms;

  /// No description provided for @errAccountExists.
  ///
  /// In en, this message translates to:
  /// **'An account with this email already exists. Try logging in.'**
  String get errAccountExists;

  /// No description provided for @meLogIn.
  ///
  /// In en, this message translates to:
  /// **'Log in or create a profile'**
  String get meLogIn;

  /// No description provided for @meLogInBody.
  ///
  /// In en, this message translates to:
  /// **'Needed for tickets. Browsing trains never needs an account.'**
  String get meLogInBody;

  /// No description provided for @meTextSize.
  ///
  /// In en, this message translates to:
  /// **'Text size'**
  String get meTextSize;

  /// No description provided for @textSizeNormal.
  ///
  /// In en, this message translates to:
  /// **'Normal'**
  String get textSizeNormal;

  /// No description provided for @textSizeLarge.
  ///
  /// In en, this message translates to:
  /// **'Large'**
  String get textSizeLarge;

  /// No description provided for @textSizeLarger.
  ///
  /// In en, this message translates to:
  /// **'Larger'**
  String get textSizeLarger;

  /// No description provided for @meNotifications.
  ///
  /// In en, this message translates to:
  /// **'Notifications'**
  String get meNotifications;

  /// No description provided for @stateOn.
  ///
  /// In en, this message translates to:
  /// **'On'**
  String get stateOn;

  /// No description provided for @stateOff.
  ///
  /// In en, this message translates to:
  /// **'Off'**
  String get stateOff;

  /// No description provided for @meDownloadData.
  ///
  /// In en, this message translates to:
  /// **'Download my data'**
  String get meDownloadData;

  /// No description provided for @meDeleteData.
  ///
  /// In en, this message translates to:
  /// **'Delete my data'**
  String get meDeleteData;

  /// No description provided for @meCallCentre.
  ///
  /// In en, this message translates to:
  /// **'Contact the call centre'**
  String get meCallCentre;

  /// No description provided for @meSignOut.
  ///
  /// In en, this message translates to:
  /// **'Sign out'**
  String get meSignOut;

  /// No description provided for @downloadTitle.
  ///
  /// In en, this message translates to:
  /// **'Your data'**
  String get downloadTitle;

  /// No description provided for @downloadCopy.
  ///
  /// In en, this message translates to:
  /// **'Copy'**
  String get downloadCopy;

  /// No description provided for @downloadCopied.
  ///
  /// In en, this message translates to:
  /// **'Copied to the clipboard'**
  String get downloadCopied;

  /// No description provided for @downloadFailed.
  ///
  /// In en, this message translates to:
  /// **'We could not get your data. Try again.'**
  String get downloadFailed;

  /// No description provided for @deleteTitle.
  ///
  /// In en, this message translates to:
  /// **'Delete your data?'**
  String get deleteTitle;

  /// No description provided for @deleteBody.
  ///
  /// In en, this message translates to:
  /// **'This can\'t be undone. We remove your name, email, saved stations and followed journeys. Ticket and payment records are kept as the law requires.'**
  String get deleteBody;

  /// No description provided for @deleteConfirm.
  ///
  /// In en, this message translates to:
  /// **'Delete my data'**
  String get deleteConfirm;

  /// No description provided for @cancel.
  ///
  /// In en, this message translates to:
  /// **'Cancel'**
  String get cancel;

  /// No description provided for @deleteDone.
  ///
  /// In en, this message translates to:
  /// **'Your data was deleted.'**
  String get deleteDone;

  /// No description provided for @callCentreTitle.
  ///
  /// In en, this message translates to:
  /// **'Call centre'**
  String get callCentreTitle;

  /// No description provided for @callCentreBody.
  ///
  /// In en, this message translates to:
  /// **'For questions about tickets or journeys, call the railway\'s call centre.'**
  String get callCentreBody;

  /// No description provided for @callCentreNumber.
  ///
  /// In en, this message translates to:
  /// **'Number: {number}'**
  String callCentreNumber(String number);

  /// No description provided for @callCentreNoNumber.
  ///
  /// In en, this message translates to:
  /// **'The number is shown here once the railway sets it.'**
  String get callCentreNoNumber;

  /// No description provided for @close.
  ///
  /// In en, this message translates to:
  /// **'Close'**
  String get close;

  /// No description provided for @ticketsTitle.
  ///
  /// In en, this message translates to:
  /// **'Tickets'**
  String get ticketsTitle;

  /// No description provided for @ticketsActive.
  ///
  /// In en, this message translates to:
  /// **'Active'**
  String get ticketsActive;

  /// No description provided for @ticketsPast.
  ///
  /// In en, this message translates to:
  /// **'Past'**
  String get ticketsPast;

  /// No description provided for @ticketsEmptyTitle.
  ///
  /// In en, this message translates to:
  /// **'Nothing here yet'**
  String get ticketsEmptyTitle;

  /// No description provided for @ticketsEmptyBody.
  ///
  /// In en, this message translates to:
  /// **'You don\'t have any tickets. Plan a journey to buy one.'**
  String get ticketsEmptyBody;

  /// No description provided for @ticketsBuy.
  ///
  /// In en, this message translates to:
  /// **'Buy a ticket'**
  String get ticketsBuy;

  /// No description provided for @ticketsAdd.
  ///
  /// In en, this message translates to:
  /// **'Add ticket'**
  String get ticketsAdd;

  /// No description provided for @ticketsSignInTitle.
  ///
  /// In en, this message translates to:
  /// **'Log in to see your tickets'**
  String get ticketsSignInTitle;

  /// No description provided for @ticketsSignInBody.
  ///
  /// In en, this message translates to:
  /// **'Tickets belong to your profile, so they are safe if you change phone.'**
  String get ticketsSignInBody;

  /// No description provided for @ticketsRoute.
  ///
  /// In en, this message translates to:
  /// **'{from} to {to}'**
  String ticketsRoute(String from, String to);

  /// No description provided for @ticketsWhen.
  ///
  /// In en, this message translates to:
  /// **'Today {time}'**
  String ticketsWhen(String time);

  /// No description provided for @ticketsWhenDate.
  ///
  /// In en, this message translates to:
  /// **'{date} {time}'**
  String ticketsWhenDate(String date, String time);

  /// No description provided for @ticketsCoachSeat.
  ///
  /// In en, this message translates to:
  /// **'Coach {coach} · Seat {seat}'**
  String ticketsCoachSeat(String coach, String seat);

  /// No description provided for @ticketsCoachSeats.
  ///
  /// In en, this message translates to:
  /// **'Coach {coach} · Seats {seats}'**
  String ticketsCoachSeats(String coach, String seats);

  /// No description provided for @ticketValid.
  ///
  /// In en, this message translates to:
  /// **'Valid'**
  String get ticketValid;

  /// No description provided for @ticketAffected.
  ///
  /// In en, this message translates to:
  /// **'Affected'**
  String get ticketAffected;

  /// No description provided for @ticketPast.
  ///
  /// In en, this message translates to:
  /// **'Used'**
  String get ticketPast;

  /// No description provided for @ticketAffectedTitle.
  ///
  /// In en, this message translates to:
  /// **'This train was cancelled'**
  String get ticketAffectedTitle;

  /// No description provided for @ticketAffectedBody.
  ///
  /// In en, this message translates to:
  /// **'Your ticket can no longer be used because the railway cancelled the train. Find another train, or contact the call centre.'**
  String get ticketAffectedBody;

  /// No description provided for @ticketStatus.
  ///
  /// In en, this message translates to:
  /// **'Status'**
  String get ticketStatus;

  /// No description provided for @ticketFindAnother.
  ///
  /// In en, this message translates to:
  /// **'Find another train'**
  String get ticketFindAnother;

  /// No description provided for @chooseTitle.
  ///
  /// In en, this message translates to:
  /// **'{from} to {to}'**
  String chooseTitle(String from, String to);

  /// No description provided for @chooseSummary.
  ///
  /// In en, this message translates to:
  /// **'Today · {who}'**
  String chooseSummary(String who);

  /// No description provided for @travellersAdults.
  ///
  /// In en, this message translates to:
  /// **'{n, plural, =1{1 adult} other{{n} adults}}'**
  String travellersAdults(int n);

  /// No description provided for @travellersChildren.
  ///
  /// In en, this message translates to:
  /// **'{n, plural, =1{1 child} other{{n} children}}'**
  String travellersChildren(int n);

  /// No description provided for @travellersSeniors.
  ///
  /// In en, this message translates to:
  /// **'{n, plural, =1{1 senior} other{{n} seniors}}'**
  String travellersSeniors(int n);

  /// No description provided for @chooseDirect.
  ///
  /// In en, this message translates to:
  /// **'{n} min · direct'**
  String chooseDirect(int n);

  /// No description provided for @chooseSeatRequired.
  ///
  /// In en, this message translates to:
  /// **'Seat required'**
  String get chooseSeatRequired;

  /// No description provided for @chooseNoSeat.
  ///
  /// In en, this message translates to:
  /// **'No seat needed'**
  String get chooseNoSeat;

  /// No description provided for @chooseNoTrains.
  ///
  /// In en, this message translates to:
  /// **'No trains to book'**
  String get chooseNoTrains;

  /// No description provided for @chooseNoTrainsBody.
  ///
  /// In en, this message translates to:
  /// **'There are no direct trains in the next hours. Try another time or station.'**
  String get chooseNoTrainsBody;

  /// No description provided for @chooseNote.
  ///
  /// In en, this message translates to:
  /// **'A delay never creates a new ticket. Your ticket stays on the same train.'**
  String get chooseNote;

  /// No description provided for @chooseNoPayment.
  ///
  /// In en, this message translates to:
  /// **'No payment is taken in this version. The fare is shown for information.'**
  String get chooseNoPayment;

  /// No description provided for @continueLabel.
  ///
  /// In en, this message translates to:
  /// **'Continue'**
  String get continueLabel;

  /// No description provided for @whoTitle.
  ///
  /// In en, this message translates to:
  /// **'Who is travelling?'**
  String get whoTitle;

  /// No description provided for @whoAdult.
  ///
  /// In en, this message translates to:
  /// **'Adult'**
  String get whoAdult;

  /// No description provided for @whoAdultHint.
  ///
  /// In en, this message translates to:
  /// **'12 and over'**
  String get whoAdultHint;

  /// No description provided for @whoChild.
  ///
  /// In en, this message translates to:
  /// **'Child'**
  String get whoChild;

  /// No description provided for @whoChildHint.
  ///
  /// In en, this message translates to:
  /// **'5 to 11'**
  String get whoChildHint;

  /// No description provided for @whoSenior.
  ///
  /// In en, this message translates to:
  /// **'Senior'**
  String get whoSenior;

  /// No description provided for @whoSeniorHint.
  ///
  /// In en, this message translates to:
  /// **'60 and over'**
  String get whoSeniorHint;

  /// No description provided for @whoHelp.
  ///
  /// In en, this message translates to:
  /// **'I need help at the station'**
  String get whoHelp;

  /// No description provided for @whoHelpHint.
  ///
  /// In en, this message translates to:
  /// **'Staff will be told. It does not change the price.'**
  String get whoHelpHint;

  /// No description provided for @whoConfirm.
  ///
  /// In en, this message translates to:
  /// **'Confirm'**
  String get whoConfirm;

  /// No description provided for @seatTitle.
  ///
  /// In en, this message translates to:
  /// **'Choose a seat'**
  String get seatTitle;

  /// No description provided for @seatTitleMany.
  ///
  /// In en, this message translates to:
  /// **'Choose {n} seats'**
  String seatTitleMany(int n);

  /// No description provided for @seatCoach.
  ///
  /// In en, this message translates to:
  /// **'Coach {c}'**
  String seatCoach(String c);

  /// No description provided for @seatFree.
  ///
  /// In en, this message translates to:
  /// **'Free'**
  String get seatFree;

  /// No description provided for @seatTaken.
  ///
  /// In en, this message translates to:
  /// **'Taken'**
  String get seatTaken;

  /// No description provided for @seatYours.
  ///
  /// In en, this message translates to:
  /// **'Yours'**
  String get seatYours;

  /// No description provided for @seatSelected.
  ///
  /// In en, this message translates to:
  /// **'{n} of {total} chosen'**
  String seatSelected(int n, int total);

  /// No description provided for @seatConfirm.
  ///
  /// In en, this message translates to:
  /// **'Confirm seat'**
  String get seatConfirm;

  /// No description provided for @seatConfirmMany.
  ///
  /// In en, this message translates to:
  /// **'Confirm seats'**
  String get seatConfirmMany;

  /// No description provided for @seatLabelFree.
  ///
  /// In en, this message translates to:
  /// **'Seat {n}, free'**
  String seatLabelFree(int n);

  /// No description provided for @seatLabelTaken.
  ///
  /// In en, this message translates to:
  /// **'Seat {n}, taken'**
  String seatLabelTaken(int n);

  /// No description provided for @seatLabelYours.
  ///
  /// In en, this message translates to:
  /// **'Seat {n}, yours'**
  String seatLabelYours(int n);

  /// No description provided for @seatGone.
  ///
  /// In en, this message translates to:
  /// **'Someone just took a seat. Please choose again.'**
  String get seatGone;

  /// No description provided for @reviewTitle.
  ///
  /// In en, this message translates to:
  /// **'Review'**
  String get reviewTitle;

  /// No description provided for @reviewFare.
  ///
  /// In en, this message translates to:
  /// **'Fare'**
  String get reviewFare;

  /// No description provided for @reviewTotal.
  ///
  /// In en, this message translates to:
  /// **'Total'**
  String get reviewTotal;

  /// No description provided for @reviewConfirm.
  ///
  /// In en, this message translates to:
  /// **'Confirm booking'**
  String get reviewConfirm;

  /// No description provided for @reviewHold.
  ///
  /// In en, this message translates to:
  /// **'Seats held until {time}'**
  String reviewHold(String time);

  /// No description provided for @reviewExpired.
  ///
  /// In en, this message translates to:
  /// **'Your seats were released. Please choose them again.'**
  String get reviewExpired;

  /// No description provided for @ticketTitle.
  ///
  /// In en, this message translates to:
  /// **'Ticket'**
  String get ticketTitle;

  /// No description provided for @ticketOffline.
  ///
  /// In en, this message translates to:
  /// **'Works offline. Last checked with SmartRail at {time}.'**
  String ticketOffline(String time);

  /// No description provided for @ticketBookingNumber.
  ///
  /// In en, this message translates to:
  /// **'Booking number {code}'**
  String ticketBookingNumber(String code);

  /// No description provided for @ticketBooked.
  ///
  /// In en, this message translates to:
  /// **'Your booking is confirmed.'**
  String get ticketBooked;

  /// No description provided for @addTitle.
  ///
  /// In en, this message translates to:
  /// **'Add ticket'**
  String get addTitle;

  /// No description provided for @addNumberLabel.
  ///
  /// In en, this message translates to:
  /// **'Booking or ticket number'**
  String get addNumberLabel;

  /// No description provided for @addNumberHint.
  ///
  /// In en, this message translates to:
  /// **'Enter your booking number'**
  String get addNumberHint;

  /// No description provided for @addNumberHelp.
  ///
  /// In en, this message translates to:
  /// **'8 characters. It is on your confirmation.'**
  String get addNumberHelp;

  /// No description provided for @addContactLabel.
  ///
  /// In en, this message translates to:
  /// **'Email used at purchase'**
  String get addContactLabel;

  /// No description provided for @addContactHint.
  ///
  /// In en, this message translates to:
  /// **'Enter the email you used'**
  String get addContactHint;

  /// No description provided for @addNotFound.
  ///
  /// In en, this message translates to:
  /// **'We could not find a ticket with those details.'**
  String get addNotFound;

  /// No description provided for @addDone.
  ///
  /// In en, this message translates to:
  /// **'Ticket added'**
  String get addDone;

  /// No description provided for @alertsTitle.
  ///
  /// In en, this message translates to:
  /// **'Alerts'**
  String get alertsTitle;

  /// No description provided for @alertsToday.
  ///
  /// In en, this message translates to:
  /// **'Today'**
  String get alertsToday;

  /// No description provided for @alertsEarlier.
  ///
  /// In en, this message translates to:
  /// **'Earlier'**
  String get alertsEarlier;

  /// No description provided for @alertsNote.
  ///
  /// In en, this message translates to:
  /// **'Alerts are only shown for journeys and stations you follow.'**
  String get alertsNote;

  /// No description provided for @alertsOff.
  ///
  /// In en, this message translates to:
  /// **'Alerts are off. Turn them on in Me.'**
  String get alertsOff;

  /// No description provided for @alertsEmptyTitle.
  ///
  /// In en, this message translates to:
  /// **'No alerts yet'**
  String get alertsEmptyTitle;

  /// No description provided for @alertsEmptyBody.
  ///
  /// In en, this message translates to:
  /// **'When a train you follow is delayed, cancelled or changes platform, you will see it here.'**
  String get alertsEmptyBody;

  /// No description provided for @alertsClear.
  ///
  /// In en, this message translates to:
  /// **'Clear all'**
  String get alertsClear;

  /// No description provided for @alertPlatformTitle.
  ///
  /// In en, this message translates to:
  /// **'Platform changed'**
  String get alertPlatformTitle;

  /// No description provided for @alertPlatformBody.
  ///
  /// In en, this message translates to:
  /// **'{origin} to {destination} now leaves from platform {now} (was {was}).'**
  String alertPlatformBody(
    String origin,
    String destination,
    String now,
    String was,
  );

  /// No description provided for @alertPlatformBodyNew.
  ///
  /// In en, this message translates to:
  /// **'{origin} to {destination} now leaves from platform {now}.'**
  String alertPlatformBodyNew(String origin, String destination, String now);

  /// No description provided for @alertCancelledTitle.
  ///
  /// In en, this message translates to:
  /// **'Cancelled'**
  String get alertCancelledTitle;

  /// No description provided for @alertCancelledBody.
  ///
  /// In en, this message translates to:
  /// **'The {time} {origin} to {destination} is cancelled.'**
  String alertCancelledBody(String time, String origin, String destination);

  /// No description provided for @alertDelayTitle.
  ///
  /// In en, this message translates to:
  /// **'Delay'**
  String get alertDelayTitle;

  /// No description provided for @alertDelayBody.
  ///
  /// In en, this message translates to:
  /// **'{origin} to {destination} now expected {time} (+{n} min).'**
  String alertDelayBody(String origin, String destination, String time, int n);

  /// No description provided for @alertCorrectionTitle.
  ///
  /// In en, this message translates to:
  /// **'Correction'**
  String get alertCorrectionTitle;

  /// No description provided for @alertCorrectionBody.
  ///
  /// In en, this message translates to:
  /// **'The {time} {origin} to {destination} is running. An earlier message said it was cancelled.'**
  String alertCorrectionBody(String time, String origin, String destination);

  /// No description provided for @disruptionTitle.
  ///
  /// In en, this message translates to:
  /// **'Disruption'**
  String get disruptionTitle;

  /// No description provided for @disruptionUpdated.
  ///
  /// In en, this message translates to:
  /// **'Updated {time}'**
  String disruptionUpdated(String time);

  /// No description provided for @disruptionStations.
  ///
  /// In en, this message translates to:
  /// **'Affected stations'**
  String get disruptionStations;

  /// No description provided for @disruptionJourneys.
  ///
  /// In en, this message translates to:
  /// **'Affected journeys'**
  String get disruptionJourneys;

  /// No description provided for @disruptionFollow.
  ///
  /// In en, this message translates to:
  /// **'Follow updates'**
  String get disruptionFollow;

  /// No description provided for @disruptionFollowed.
  ///
  /// In en, this message translates to:
  /// **'You are following the affected stations.'**
  String get disruptionFollowed;

  /// No description provided for @disruptionNetworkWide.
  ///
  /// In en, this message translates to:
  /// **'This affects the whole network.'**
  String get disruptionNetworkWide;

  /// No description provided for @disruptionExpectedEnd.
  ///
  /// In en, this message translates to:
  /// **'We expect this to clear by {time}.'**
  String disruptionExpectedEnd(String time);

  /// No description provided for @disruptionGone.
  ///
  /// In en, this message translates to:
  /// **'This disruption is over'**
  String get disruptionGone;

  /// No description provided for @disruptionGoneBody.
  ///
  /// In en, this message translates to:
  /// **'It was resolved or withdrawn. Departure boards show the trains as they are now.'**
  String get disruptionGoneBody;

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

  /// No description provided for @foodTitle.
  ///
  /// In en, this message translates to:
  /// **'Food at {station}'**
  String foodTitle(String station);

  /// No description provided for @foodAll.
  ///
  /// In en, this message translates to:
  /// **'All'**
  String get foodAll;

  /// No description provided for @foodOrderNote.
  ///
  /// In en, this message translates to:
  /// **'Ordering and payment are handled by the catering provider.'**
  String get foodOrderNote;

  /// No description provided for @foodCall.
  ///
  /// In en, this message translates to:
  /// **'Call to order'**
  String get foodCall;

  /// No description provided for @foodUnavailable.
  ///
  /// In en, this message translates to:
  /// **'Menu unavailable right now'**
  String get foodUnavailable;

  /// No description provided for @foodUnavailableBody.
  ///
  /// In en, this message translates to:
  /// **'Trains and tickets are not affected. Try again in a moment.'**
  String get foodUnavailableBody;

  /// No description provided for @foodNone.
  ///
  /// In en, this message translates to:
  /// **'No food at this station'**
  String get foodNone;

  /// No description provided for @foodLink.
  ///
  /// In en, this message translates to:
  /// **'Food at this station'**
  String get foodLink;

  /// No description provided for @foodArrive.
  ///
  /// In en, this message translates to:
  /// **'Arrive {time}'**
  String foodArrive(String time);

  /// No description provided for @planWhenTitle.
  ///
  /// In en, this message translates to:
  /// **'Date and time'**
  String get planWhenTitle;

  /// No description provided for @planLaterToday.
  ///
  /// In en, this message translates to:
  /// **'Later today'**
  String get planLaterToday;

  /// No description provided for @planTomorrow.
  ///
  /// In en, this message translates to:
  /// **'Tomorrow'**
  String get planTomorrow;

  /// No description provided for @planTomorrowAt.
  ///
  /// In en, this message translates to:
  /// **'Tomorrow {time}'**
  String planTomorrowAt(String time);

  /// No description provided for @planTimeNote.
  ///
  /// In en, this message translates to:
  /// **'Timetables use local time ({zone}).'**
  String planTimeNote(String zone);

  /// No description provided for @createdLogIn.
  ///
  /// In en, this message translates to:
  /// **'Your profile was created. Please log in.'**
  String get createdLogIn;
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
