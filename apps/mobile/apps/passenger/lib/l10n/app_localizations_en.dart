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
  String get tabPlan => 'Plan';

  @override
  String get tabTickets => 'Tickets';

  @override
  String get tabAlerts => 'Alerts';

  @override
  String get tabMe => 'Me';

  @override
  String get onbSkip => 'Skip';

  @override
  String get onbNext => 'Next';

  @override
  String get onbDone => 'Go to app';

  @override
  String get onb1Title => 'Know before you go';

  @override
  String get onb1Body =>
      'Live departures, delays and platform changes for every station.';

  @override
  String get onb2Title => 'Follow your journey';

  @override
  String get onb2Body =>
      'Get an alert when your platform changes or your train is delayed.';

  @override
  String get onb3Title => 'Honest about old data';

  @override
  String get onb3Body =>
      'When we can\'t confirm a time, we say so instead of guessing.';

  @override
  String get greetingMorning => 'Good morning';

  @override
  String get greetingAfternoon => 'Good afternoon';

  @override
  String get greetingEvening => 'Good evening';

  @override
  String get homeWhereTo => 'Where to today?';

  @override
  String get fromLabel => 'From';

  @override
  String get toLabel => 'To';

  @override
  String get chooseStation => 'Choose a station';

  @override
  String get findTrains => 'Find trains';

  @override
  String get homeFollowedStations => 'Followed stations';

  @override
  String get homeFollowEmptyTitle => 'No followed stations yet';

  @override
  String get homeFollowEmptyBody =>
      'Follow a station to see its next train here.';

  @override
  String get homeFindStation => 'Find a station';

  @override
  String get homeFollowedJourneys => 'Followed journeys';

  @override
  String get stationsTitle => 'Stations';

  @override
  String get stationsSearchLabel => 'Station name or code';

  @override
  String get stationsSearchHint => 'For example Central or CEN';

  @override
  String get stationsResults => 'Results';

  @override
  String get stationsRecent => 'Recent';

  @override
  String get stationsEmptyTitle => 'No stations match';

  @override
  String get stationsEmptyBody =>
      'Try the name or the short code, for example CEN.';

  @override
  String get stationsIntro =>
      'Search for a station to see its live departures.';

  @override
  String freshLive(String time) {
    return 'Live · updated $time';
  }

  @override
  String freshStale(String time) {
    return 'As of $time';
  }

  @override
  String freshSaved(String time) {
    return 'Saved $time';
  }

  @override
  String get departuresToday => 'Today';

  @override
  String get departuresEmptyTitle => 'No more trains today';

  @override
  String get departuresEmptyBody =>
      'Check again tomorrow or look at another station.';

  @override
  String inMinutes(int n) {
    return 'in $n min';
  }

  @override
  String platformN(String p) {
    return 'Platform $p';
  }

  @override
  String get platformTba => 'Platform to be announced';

  @override
  String get chipOnTime => 'On time';

  @override
  String chipDelayed(int n) {
    return '+$n min';
  }

  @override
  String get chipCancelled => 'Cancelled';

  @override
  String get chipPlatformChanged => 'Platform changed';

  @override
  String get chipNoLiveData => 'No live data';

  @override
  String get scheduledOnly => 'Scheduled';

  @override
  String scheduledNoUpdate(String time) {
    return 'Scheduled · no update since $time';
  }

  @override
  String expectedAt(String time) {
    return 'Expected $time';
  }

  @override
  String get followStation => 'Follow station';

  @override
  String get followingStation => 'Following';

  @override
  String get indicativeNote =>
      'Times are provided for information. Listen for station announcements.';

  @override
  String trainNumber(String n) {
    return 'Train $n';
  }

  @override
  String journeyHeader(String time, String destination) {
    return '$time to $destination';
  }

  @override
  String journeyTitle(String origin, String destination) {
    return '$origin to $destination';
  }

  @override
  String get headlineRunningOnTime => 'Running · on time';

  @override
  String headlineDelayed(int n) {
    return 'Delayed · +$n min';
  }

  @override
  String headlineNoLive(String time) {
    return 'No live data since $time';
  }

  @override
  String get headlineNoLiveEver => 'No live data yet';

  @override
  String get headlineCancelled => 'Cancelled';

  @override
  String get headlineScheduled => 'Scheduled · not departed yet';

  @override
  String get headlineCompleted => 'Journey complete';

  @override
  String detailLive(String time) {
    return 'Live · updated $time';
  }

  @override
  String detailLiveDeparted(String updated, String station, String time) {
    return 'Live · updated $updated · Departed $station at $time';
  }

  @override
  String detailLastKnown(String station, String time) {
    return 'Last known: departed $station $time';
  }

  @override
  String get detailNoLiveBody =>
      'Showing the timetable. Times are not live. Listen for station announcements.';

  @override
  String detailDelayedBody(String expected, String station, String scheduled) {
    return 'Expected $expected at $station instead of $scheduled.';
  }

  @override
  String get detailCancelledBody =>
      'This train will not run. Look for another train or contact the call centre.';

  @override
  String get detailScheduledBody =>
      'This train has not left yet. Times are the timetable.';

  @override
  String platformBannerTitle(String p) {
    return 'Platform $p';
  }

  @override
  String platformBannerBody(String station) {
    return 'Platform changed at $station';
  }

  @override
  String wordDepartedOnTime(String time) {
    return 'Departed $time · on time';
  }

  @override
  String wordDepartedLate(String time, int n) {
    return 'Departed $time · $n min late';
  }

  @override
  String wordArrivedOnTime(String time) {
    return 'Arrived $time · on time';
  }

  @override
  String wordArrivedLate(String time, int n) {
    return 'Arrived $time · $n min late';
  }

  @override
  String wordArrivedEarly(String time, int n) {
    return 'Arrived $time · $n min early';
  }

  @override
  String wordAtPlatform(String time) {
    return 'At the platform since $time';
  }

  @override
  String wordExpectedOnTime(String time) {
    return 'Expected $time · on time';
  }

  @override
  String wordExpected(String time) {
    return 'Expected $time';
  }

  @override
  String get wordEstimate => 'Estimate · may change';

  @override
  String get wordScheduled => 'Scheduled';

  @override
  String get wordSkipped => 'Not stopping';

  @override
  String estimatesNote(String station) {
    return 'Times after $station are estimates and may change.';
  }

  @override
  String get followJourney => 'Follow this journey';

  @override
  String get followingJourney => 'Following';

  @override
  String get snackFollowingJourney => 'You are following this journey';

  @override
  String get snackUnfollowedJourney => 'You stopped following this journey';

  @override
  String get snackFollowingStation => 'You are following this station';

  @override
  String get snackUnfollowedStation => 'You stopped following this station';

  @override
  String get undo => 'Undo';

  @override
  String get errorTitle => 'Something went wrong';

  @override
  String get errorNetwork =>
      'We could not reach SmartRail. Check your connection and try again.';

  @override
  String get errorNotFound => 'We could not find that. It may no longer exist.';

  @override
  String get errorGeneric => 'Please try again in a moment.';

  @override
  String get retry => 'Try again';

  @override
  String get comingTitlePlan => 'Plan a journey';

  @override
  String get comingBodyPlan =>
      'Choose two stations and see the trains between them, with a change if needed. This arrives in the next update. Until then you can look at any station\'s live departures.';

  @override
  String get comingTitleTickets => 'Tickets';

  @override
  String get comingBodyTickets =>
      'Your tickets will be here, and they will keep working without a connection. This arrives in an upcoming update.';

  @override
  String get comingTitleAlerts => 'Alerts';

  @override
  String get comingBodyAlerts =>
      'Platform changes, delays and cancellations for the journeys and stations you follow will appear here. This arrives in an upcoming update.';

  @override
  String get comingSoon => 'Coming soon';

  @override
  String get comingSeeDepartures => 'See live departures';

  @override
  String get meTitle => 'Me';

  @override
  String get language => 'Language';

  @override
  String get languageEnglish => 'English';

  @override
  String get languageSinhala => 'සිංහල';

  @override
  String get meAbout =>
      'SmartRail shows live railway information. Times are for information.';

  @override
  String get notFoundTitle => 'Page not found';

  @override
  String get notFoundBody => 'That page does not exist.';

  @override
  String get notFoundAction => 'Go to home';

  @override
  String get offlineTitle => 'No connection';

  @override
  String offlineDetail(String time) {
    return 'Showing the last information from $time';
  }

  @override
  String get offlineNever => 'No information has been received yet.';

  @override
  String get planTitle => 'Plan a journey';

  @override
  String get planLeaveNow => 'Leave now';

  @override
  String planLeaveAt(String time) {
    return 'Leave at $time';
  }

  @override
  String get planPickTime => 'Choose a time';

  @override
  String get planSearch => 'Search trains';

  @override
  String resultsTitle(String from, String to) {
    return '$from to $to';
  }

  @override
  String get resultsDirect => 'Direct';

  @override
  String get resultsChange => '1 change';

  @override
  String resultsDuration(int n) {
    return '$n min';
  }

  @override
  String resultsChangeAt(String station) {
    return 'Change at $station';
  }

  @override
  String resultsWait(int n) {
    return '$n min to change';
  }

  @override
  String get resultsAtRiskTitle => 'Connection at risk';

  @override
  String resultsAtRiskBody(int n, String station) {
    return 'Only $n min to change at $station. You may miss the second train.';
  }

  @override
  String resultsTrainLeg(String number, String from, String to) {
    return 'Train $number · $from to $to';
  }

  @override
  String resultsArrives(String time) {
    return 'Arrives $time';
  }

  @override
  String get resultsEmptyTitle => 'No trains found';

  @override
  String get resultsEmptyBody =>
      'There are no direct trains or trains with one change in the next hours. Try a later time.';

  @override
  String get resultsLater => 'Try later';

  @override
  String get loginTitle => 'Log in';

  @override
  String get loginWelcome => 'Welcome back';

  @override
  String get emailLabel => 'Email';

  @override
  String get emailHint => 'Enter your email';

  @override
  String get passwordLabel => 'Password';

  @override
  String get passwordHint => 'Enter your password';

  @override
  String get forgotPassword => 'Forgot password?';

  @override
  String get forgotSent =>
      'If that email has an account, we sent a link to reset the password.';

  @override
  String get loginNoAccountNeeded =>
      'You do not need an account to see departures, delays and platform changes.';

  @override
  String get loginNew => 'New here?';

  @override
  String get loginCreate => 'Create profile';

  @override
  String get loginBadCredentials =>
      'That email and password do not match, or the account is locked for a few minutes.';

  @override
  String get createTitle => 'Create profile';

  @override
  String get fullNameLabel => 'Full name';

  @override
  String get fullNameHint => 'Jane Perera';

  @override
  String get passwordNewHint => 'At least 8 characters';

  @override
  String get agreeTerms => 'I agree to the terms and the privacy notice.';

  @override
  String get errName => 'Enter your full name.';

  @override
  String get errEmail => 'Enter a valid email address.';

  @override
  String get errPassword => 'The password needs at least 8 characters.';

  @override
  String get errTerms => 'Please agree to continue.';

  @override
  String get errAccountExists =>
      'An account with this email already exists. Try logging in.';

  @override
  String get meLogIn => 'Log in or create a profile';

  @override
  String get meLogInBody =>
      'Needed for tickets. Browsing trains never needs an account.';

  @override
  String get meTextSize => 'Text size';

  @override
  String get textSizeNormal => 'Normal';

  @override
  String get textSizeLarge => 'Large';

  @override
  String get textSizeLarger => 'Larger';

  @override
  String get meNotifications => 'Notifications';

  @override
  String get stateOn => 'On';

  @override
  String get stateOff => 'Off';

  @override
  String get meDownloadData => 'Download my data';

  @override
  String get meDeleteData => 'Delete my data';

  @override
  String get meCallCentre => 'Contact the call centre';

  @override
  String get meSignOut => 'Sign out';

  @override
  String get downloadTitle => 'Your data';

  @override
  String get downloadCopy => 'Copy';

  @override
  String get downloadCopied => 'Copied to the clipboard';

  @override
  String get downloadFailed => 'We could not get your data. Try again.';

  @override
  String get deleteTitle => 'Delete your data?';

  @override
  String get deleteBody =>
      'This can\'t be undone. We remove your name, email, saved stations and followed journeys. Ticket and payment records are kept as the law requires.';

  @override
  String get deleteConfirm => 'Delete my data';

  @override
  String get cancel => 'Cancel';

  @override
  String get deleteDone => 'Your data was deleted.';

  @override
  String get callCentreTitle => 'Call centre';

  @override
  String get callCentreBody =>
      'For questions about tickets or journeys, call the railway\'s call centre.';

  @override
  String callCentreNumber(String number) {
    return 'Number: $number';
  }

  @override
  String get callCentreNoNumber =>
      'The number is shown here once the railway sets it.';

  @override
  String get close => 'Close';

  @override
  String get ticketsTitle => 'Tickets';

  @override
  String get ticketsActive => 'Active';

  @override
  String get ticketsPast => 'Past';

  @override
  String get ticketsEmptyTitle => 'Nothing here yet';

  @override
  String get ticketsEmptyBody =>
      'You don\'t have any tickets. Plan a journey to buy one.';

  @override
  String get ticketsBuy => 'Buy a ticket';

  @override
  String get ticketsAdd => 'Add ticket';

  @override
  String get ticketsSignInTitle => 'Log in to see your tickets';

  @override
  String get ticketsSignInBody =>
      'Tickets belong to your profile, so they are safe if you change phone.';

  @override
  String ticketsRoute(String from, String to) {
    return '$from to $to';
  }

  @override
  String ticketsWhen(String time) {
    return 'Today $time';
  }

  @override
  String ticketsWhenDate(String date, String time) {
    return '$date $time';
  }

  @override
  String ticketsCoachSeat(String coach, String seat) {
    return 'Coach $coach · Seat $seat';
  }

  @override
  String ticketsCoachSeats(String coach, String seats) {
    return 'Coach $coach · Seats $seats';
  }

  @override
  String get ticketValid => 'Valid';

  @override
  String get ticketAffected => 'Affected';

  @override
  String get ticketPast => 'Used';

  @override
  String get ticketAffectedTitle => 'This train was cancelled';

  @override
  String get ticketAffectedBody =>
      'Your ticket can no longer be used because the railway cancelled the train. Find another train, or contact the call centre.';

  @override
  String get ticketStatus => 'Status';

  @override
  String get ticketFindAnother => 'Find another train';

  @override
  String chooseTitle(String from, String to) {
    return '$from to $to';
  }

  @override
  String chooseSummary(String who) {
    return 'Today · $who';
  }

  @override
  String travellersAdults(int n) {
    String _temp0 = intl.Intl.pluralLogic(
      n,
      locale: localeName,
      other: '$n adults',
      one: '1 adult',
    );
    return '$_temp0';
  }

  @override
  String travellersChildren(int n) {
    String _temp0 = intl.Intl.pluralLogic(
      n,
      locale: localeName,
      other: '$n children',
      one: '1 child',
    );
    return '$_temp0';
  }

  @override
  String travellersSeniors(int n) {
    String _temp0 = intl.Intl.pluralLogic(
      n,
      locale: localeName,
      other: '$n seniors',
      one: '1 senior',
    );
    return '$_temp0';
  }

  @override
  String chooseDirect(int n) {
    return '$n min · direct';
  }

  @override
  String get chooseSeatRequired => 'Seat required';

  @override
  String get chooseNoSeat => 'No seat needed';

  @override
  String get chooseNoTrains => 'No trains to book';

  @override
  String get chooseNoTrainsBody =>
      'There are no direct trains in the next hours. Try another time or station.';

  @override
  String get chooseNote =>
      'A delay never creates a new ticket. Your ticket stays on the same train.';

  @override
  String get chooseNoPayment =>
      'No payment is taken in this version. The fare is shown for information.';

  @override
  String get continueLabel => 'Continue';

  @override
  String get whoTitle => 'Who is travelling?';

  @override
  String get whoAdult => 'Adult';

  @override
  String get whoAdultHint => '12 and over';

  @override
  String get whoChild => 'Child';

  @override
  String get whoChildHint => '5 to 11';

  @override
  String get whoSenior => 'Senior';

  @override
  String get whoSeniorHint => '60 and over';

  @override
  String get whoHelp => 'I need help at the station';

  @override
  String get whoHelpHint => 'Staff will be told. It does not change the price.';

  @override
  String get whoConfirm => 'Confirm';

  @override
  String get seatTitle => 'Choose a seat';

  @override
  String seatTitleMany(int n) {
    return 'Choose $n seats';
  }

  @override
  String seatCoach(String c) {
    return 'Coach $c';
  }

  @override
  String get seatFree => 'Free';

  @override
  String get seatTaken => 'Taken';

  @override
  String get seatYours => 'Yours';

  @override
  String seatSelected(int n, int total) {
    return '$n of $total chosen';
  }

  @override
  String get seatConfirm => 'Confirm seat';

  @override
  String get seatConfirmMany => 'Confirm seats';

  @override
  String seatLabelFree(int n) {
    return 'Seat $n, free';
  }

  @override
  String seatLabelTaken(int n) {
    return 'Seat $n, taken';
  }

  @override
  String seatLabelYours(int n) {
    return 'Seat $n, yours';
  }

  @override
  String get seatGone => 'Someone just took a seat. Please choose again.';

  @override
  String get reviewTitle => 'Review';

  @override
  String get reviewFare => 'Fare';

  @override
  String get reviewTotal => 'Total';

  @override
  String get reviewConfirm => 'Confirm booking';

  @override
  String reviewHold(String time) {
    return 'Seats held until $time';
  }

  @override
  String get reviewExpired =>
      'Your seats were released. Please choose them again.';

  @override
  String get ticketTitle => 'Ticket';

  @override
  String ticketOffline(String time) {
    return 'Works offline. Last checked with SmartRail at $time.';
  }

  @override
  String ticketBookingNumber(String code) {
    return 'Booking number $code';
  }

  @override
  String get ticketBooked => 'Your booking is confirmed.';

  @override
  String get addTitle => 'Add ticket';

  @override
  String get addNumberLabel => 'Booking or ticket number';

  @override
  String get addNumberHint => 'Enter your booking number';

  @override
  String get addNumberHelp => '8 characters. It is on your confirmation.';

  @override
  String get addContactLabel => 'Email used at purchase';

  @override
  String get addContactHint => 'Enter the email you used';

  @override
  String get addNotFound => 'We could not find a ticket with those details.';

  @override
  String get addDone => 'Ticket added';

  @override
  String get alertsTitle => 'Alerts';

  @override
  String get alertsToday => 'Today';

  @override
  String get alertsEarlier => 'Earlier';

  @override
  String get alertsNote =>
      'Alerts are only shown for journeys and stations you follow.';

  @override
  String get alertsOff => 'Alerts are off. Turn them on in Me.';

  @override
  String get alertsEmptyTitle => 'No alerts yet';

  @override
  String get alertsEmptyBody =>
      'When a train you follow is delayed, cancelled or changes platform, you will see it here.';

  @override
  String get alertsClear => 'Clear all';

  @override
  String get alertPlatformTitle => 'Platform changed';

  @override
  String alertPlatformBody(
    String origin,
    String destination,
    String now,
    String was,
  ) {
    return '$origin to $destination now leaves from platform $now (was $was).';
  }

  @override
  String alertPlatformBodyNew(String origin, String destination, String now) {
    return '$origin to $destination now leaves from platform $now.';
  }

  @override
  String get alertCancelledTitle => 'Cancelled';

  @override
  String alertCancelledBody(String time, String origin, String destination) {
    return 'The $time $origin to $destination is cancelled.';
  }

  @override
  String get alertDelayTitle => 'Delay';

  @override
  String alertDelayBody(String origin, String destination, String time, int n) {
    return '$origin to $destination now expected $time (+$n min).';
  }

  @override
  String get alertCorrectionTitle => 'Correction';

  @override
  String alertCorrectionBody(String time, String origin, String destination) {
    return 'The $time $origin to $destination is running. An earlier message said it was cancelled.';
  }

  @override
  String get disruptionTitle => 'Disruption';

  @override
  String disruptionUpdated(String time) {
    return 'Updated $time';
  }

  @override
  String get disruptionStations => 'Affected stations';

  @override
  String get disruptionJourneys => 'Affected journeys';

  @override
  String get disruptionFollow => 'Follow updates';

  @override
  String get disruptionFollowed => 'You are following the affected stations.';

  @override
  String get disruptionNetworkWide => 'This affects the whole network.';

  @override
  String disruptionExpectedEnd(String time) {
    return 'We expect this to clear by $time.';
  }

  @override
  String get disruptionGone => 'This disruption is over';

  @override
  String get disruptionGoneBody =>
      'It was resolved or withdrawn. Departure boards show the trains as they are now.';

  @override
  String get severityMinor => 'Minor';

  @override
  String get severityMajor => 'Major';

  @override
  String get severitySevere => 'Severe';

  @override
  String foodTitle(String station) {
    return 'Food at $station';
  }

  @override
  String get foodAll => 'All';

  @override
  String get foodOrderNote =>
      'Ordering and payment are handled by the catering provider.';

  @override
  String get foodCall => 'Call to order';

  @override
  String get foodUnavailable => 'Menu unavailable right now';

  @override
  String get foodUnavailableBody =>
      'Trains and tickets are not affected. Try again in a moment.';

  @override
  String get foodNone => 'No food at this station';

  @override
  String get foodLink => 'Food at this station';

  @override
  String foodArrive(String time) {
    return 'Arrive $time';
  }

  @override
  String get planWhenTitle => 'Date and time';

  @override
  String get planLaterToday => 'Later today';

  @override
  String get planTomorrow => 'Tomorrow';

  @override
  String planTomorrowAt(String time) {
    return 'Tomorrow $time';
  }

  @override
  String planTimeNote(String zone) {
    return 'Timetables use local time ($zone).';
  }

  @override
  String get createdLogIn => 'Your profile was created. Please log in.';

  @override
  String get planDepart => 'Depart';

  @override
  String get planArrive => 'Arrive';

  @override
  String get planRecent => 'Recent';

  @override
  String get foodMenuHint => 'Menu and prices · call to order';

  @override
  String get resultsFilter => 'Filter';

  @override
  String get resultsDirectOnly => 'Direct trains only';

  @override
  String get ticketsMoreActions => 'More actions';

  @override
  String get ticketsAddWithCode => 'Add ticket with booking code';

  @override
  String get ticketsNonePast => 'Tickets you have used will appear here.';

  @override
  String get ticketReference => 'Ticket';

  @override
  String get seatHoldNote =>
      'Your seats are held for 10 minutes once you confirm.';

  @override
  String get stationsAll => 'All stations';

  @override
  String get foodOrder => 'Order food';

  @override
  String get foodPick => 'Choose a station';

  @override
  String get ticketBackHome => 'Back to home';
}
