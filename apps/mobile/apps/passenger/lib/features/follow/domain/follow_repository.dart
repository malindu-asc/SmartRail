/// A station the passenger follows. Kept on the phone until accounts can keep it (SPEC-021).
class FollowedStation {
  const FollowedStation({required this.code, required this.name});

  final String code;
  final String name;

  Map<String, Object?> toJson() => {'code': code, 'name': name};

  factory FollowedStation.fromJson(Map<String, Object?> json) =>
      FollowedStation(
        code: json['code']! as String,
        name: json['name']! as String,
      );
}

/// A journey the passenger follows, with just enough to show it on Home and offline.
class FollowedJourney {
  const FollowedJourney({
    required this.journeyId,
    required this.trainNumber,
    required this.originName,
    required this.destinationName,
    required this.scheduledDeparture,
  });

  final String journeyId;
  final String trainNumber;
  final String originName;
  final String destinationName;
  final DateTime scheduledDeparture;

  Map<String, Object?> toJson() => {
    'journeyId': journeyId,
    'trainNumber': trainNumber,
    'originName': originName,
    'destinationName': destinationName,
    'scheduledDeparture': scheduledDeparture.toUtc().toIso8601String(),
  };

  factory FollowedJourney.fromJson(Map<String, Object?> json) =>
      FollowedJourney(
        journeyId: json['journeyId']! as String,
        trainNumber: json['trainNumber']! as String,
        originName: json['originName']! as String,
        destinationName: json['destinationName']! as String,
        scheduledDeparture: DateTime.parse(
          json['scheduledDeparture']! as String,
        ).toUtc(),
      );
}

abstract interface class FollowRepository {
  List<FollowedStation> stations();

  List<FollowedJourney> journeys();

  Future<void> saveStations(List<FollowedStation> stations);

  Future<void> saveJourneys(List<FollowedJourney> journeys);
}
