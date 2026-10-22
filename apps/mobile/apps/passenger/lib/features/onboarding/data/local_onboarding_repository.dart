import '../../../core/storage/key_value_store.dart';
import '../domain/onboarding_repository.dart';

class LocalOnboardingRepository implements OnboardingRepository {
  LocalOnboardingRepository(this._store);

  static const _key = 'onboarding_seen';

  final KeyValueStore _store;

  @override
  bool isSeen() => _store.getBool(_key) ?? false;

  @override
  Future<void> markSeen() => _store.setBool(_key, true);
}
