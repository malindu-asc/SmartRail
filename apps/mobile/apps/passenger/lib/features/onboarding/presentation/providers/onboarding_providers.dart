import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/storage/key_value_store.dart';
import '../../data/local_onboarding_repository.dart';
import '../../domain/onboarding_repository.dart';

final onboardingRepositoryProvider = Provider<OnboardingRepository>(
  (ref) => LocalOnboardingRepository(ref.watch(keyValueStoreProvider)),
);

/// Whether the introduction has been finished or skipped. After that the app opens straight on Home.
final onboardingSeenProvider = NotifierProvider<OnboardingSeenNotifier, bool>(
  OnboardingSeenNotifier.new,
);

class OnboardingSeenNotifier extends Notifier<bool> {
  @override
  bool build() => ref.read(onboardingRepositoryProvider).isSeen();

  Future<void> complete() async {
    state = true;
    await ref.read(onboardingRepositoryProvider).markSeen();
  }
}
