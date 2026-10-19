/// A station a passenger can look up. The name comes from the API already in the chosen language.
class Station {
  const Station({
    required this.code,
    required this.name,
    this.facilities = const [],
  });

  final String code;
  final String name;
  final List<String> facilities;

  Map<String, Object?> toJson() => {'code': code, 'name': name};

  /// The saved form only keeps the code and name; facilities are looked up again.
  factory Station.fromSaved(Map<String, Object?> json) =>
      Station(code: json['code']! as String, name: json['name']! as String);

  @override
  bool operator ==(Object other) =>
      other is Station && other.code == code && other.name == name;

  @override
  int get hashCode => Object.hash(code, name);
}
