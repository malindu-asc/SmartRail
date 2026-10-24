import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../core/storage/key_value_store.dart';

/// How big the text is on top of the phone's own setting (Me, "Text size", design 32).
enum TextSize {
  normal(1.0),
  large(1.15),
  larger(1.3);

  const TextSize(this.factor);

  final double factor;
}

final textSizeProvider = NotifierProvider<TextSizeNotifier, TextSize>(
  TextSizeNotifier.new,
);

class TextSizeNotifier extends Notifier<TextSize> {
  static const _key = 'text_size';

  @override
  TextSize build() {
    final saved = ref.read(keyValueStoreProvider).getString(_key);
    return TextSize.values.where((s) => s.name == saved).firstOrNull ??
        TextSize.normal;
  }

  Future<void> set(TextSize size) async {
    state = size;
    await ref.read(keyValueStoreProvider).setString(_key, size.name);
  }
}

/// Whether alerts for followed stations and journeys are shown in the Alerts tab (Me, "Notifications"). Phone push messages are
/// not part of this version.
final alertsOnProvider = NotifierProvider<AlertsOnNotifier, bool>(
  AlertsOnNotifier.new,
);

class AlertsOnNotifier extends Notifier<bool> {
  static const _key = 'alerts_on';

  @override
  bool build() => ref.read(keyValueStoreProvider).getBool(_key) ?? true;

  Future<void> set(bool on) async {
    state = on;
    await ref.read(keyValueStoreProvider).setBool(_key, on);
  }
}
