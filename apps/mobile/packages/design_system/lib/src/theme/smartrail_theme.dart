import 'package:flutter/material.dart';

import '../tokens/tokens.dart';

/// Values that differ between the passenger and the driver app and that widgets read from the theme.
@immutable
class SmartRailSpec extends ThemeExtension<SmartRailSpec> {
  const SmartRailSpec({
    required this.touchTarget,
    required this.buttonHeight,
    required this.buttonBorderWidth,
    required this.buttonLabel,
    required this.minTextSize,
  });

  /// The smallest size of anything the user taps: passenger 48 dp, driver 72 dp (REQ-PAX-014, REQ-DRV).
  final double touchTarget;

  /// The height of a full-width pill button in the Figma: 56 in the passenger app, 72 in the driver app.
  final double buttonHeight;

  final double buttonBorderWidth;

  /// Button/16 Semi Bold for passengers, Title/20 Bold for the driver's action buttons.
  final TextStyle buttonLabel;

  /// The smallest text the app shows: the driver app never goes below 14.
  final double minTextSize;

  @override
  SmartRailSpec copyWith({
    double? touchTarget,
    double? buttonHeight,
    double? buttonBorderWidth,
    TextStyle? buttonLabel,
    double? minTextSize,
  }) => SmartRailSpec(
    touchTarget: touchTarget ?? this.touchTarget,
    buttonHeight: buttonHeight ?? this.buttonHeight,
    buttonBorderWidth: buttonBorderWidth ?? this.buttonBorderWidth,
    buttonLabel: buttonLabel ?? this.buttonLabel,
    minTextSize: minTextSize ?? this.minTextSize,
  );

  @override
  SmartRailSpec lerp(ThemeExtension<SmartRailSpec>? other, double t) => other is! SmartRailSpec || t < 0.5 ? this : other;

  static SmartRailSpec of(BuildContext context) =>
      Theme.of(context).extension<SmartRailSpec>() ?? SmartRailTheme.passengerSpec;
}

abstract final class SmartRailTheme {
  static const passengerSpec = SmartRailSpec(
    touchTarget: 48,
    buttonHeight: 56,
    buttonBorderWidth: 1.5,
    buttonLabel: SmartRailType.button,
    minTextSize: 12,
  );

  static const driverSpec = SmartRailSpec(
    touchTarget: 72,
    buttonHeight: 72,
    buttonBorderWidth: 2,
    buttonLabel: SmartRailType.title20,
    minTextSize: 14,
  );

  static ThemeData passenger() => _build(passengerSpec);

  static ThemeData driver() => _build(driverSpec);

  static ThemeData _build(SmartRailSpec spec) {
    const scheme = ColorScheme.light(
      primary: SmartRailColors.primary,
      onPrimary: SmartRailColors.onPrimary,
      primaryContainer: SmartRailColors.primarySubtle,
      onPrimaryContainer: SmartRailColors.primaryPressed,
      secondary: SmartRailColors.textSecondary,
      onSecondary: SmartRailColors.onPrimary,
      surface: SmartRailColors.surface,
      onSurface: SmartRailColors.textPrimary,
      onSurfaceVariant: SmartRailColors.textSecondary,
      outline: SmartRailColors.borderStrong,
      outlineVariant: SmartRailColors.border,
      error: SmartRailColors.cancelledText,
      onError: SmartRailColors.onPrimary,
    );

    // The driver app never shows text under its minimum size.
    TextStyle floor(TextStyle style) =>
        style.copyWith(fontSize: style.fontSize! < spec.minTextSize ? spec.minTextSize : style.fontSize);

    final textTheme = TextTheme(
      headlineLarge: floor(SmartRailType.headline),
      titleLarge: floor(SmartRailType.title20),
      titleMedium: floor(SmartRailType.title),
      bodyLarge: floor(SmartRailType.body),
      bodyMedium: floor(SmartRailType.body14),
      labelLarge: floor(SmartRailType.label14),
      labelSmall: floor(SmartRailType.caption),
    ).apply(bodyColor: SmartRailColors.textPrimary, displayColor: SmartRailColors.textPrimary);

    return ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      scaffoldBackgroundColor: SmartRailColors.page,
      fontFamily: SmartRailType.fontFamily,
      textTheme: textTheme,
      extensions: [spec],
      materialTapTargetSize: MaterialTapTargetSize.padded,
      appBarTheme: const AppBarTheme(
        backgroundColor: SmartRailColors.surface,
        foregroundColor: SmartRailColors.textPrimary,
        elevation: 0,
        scrolledUnderElevation: 0,
      ),
      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: SmartRailColors.surface,
        indicatorColor: SmartRailColors.primarySubtle,
        height: spec.touchTarget + 24,
        labelTextStyle: WidgetStatePropertyAll(floor(SmartRailType.caption)),
      ),
      dividerTheme: const DividerThemeData(color: SmartRailColors.border, space: 1, thickness: 1),
    );
  }
}
