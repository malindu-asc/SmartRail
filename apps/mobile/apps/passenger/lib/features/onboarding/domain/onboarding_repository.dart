abstract interface class OnboardingRepository {
  /// Whether the first-run introduction has been finished or skipped.
  bool isSeen();

  Future<void> markSeen();
}
