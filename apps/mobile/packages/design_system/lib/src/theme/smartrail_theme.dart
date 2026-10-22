import 'package:animations/animations.dart';
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
  SmartRailSpec lerp(ThemeExtension<SmartRailSpec>? other, double t) =>
      other is! SmartRailSpec || t < 0.5 ? this : other;

  static SmartRailSpec of(BuildContext context) =>
      Theme.of(context).extension<SmartRailSpec>() ??
      SmartRailTheme.passengerSpec;
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
    TextStyle floor(TextStyle style) => style.copyWith(
      fontSize: style.fontSize! < spec.minTextSize
          ? spec.minTextSize
          : style.fontSize,
    );

    final textTheme =
        TextTheme(
          headlineLarge: floor(SmartRailType.headline),
          titleLarge: floor(SmartRailType.title20),
          titleMedium: floor(SmartRailType.title),
          bodyLarge: floor(SmartRailType.body),
          bodyMedium: floor(SmartRailType.body14),
          labelLarge: floor(SmartRailType.label14),
          labelSmall: floor(SmartRailType.caption),
        ).apply(
          bodyColor: SmartRailColors.textPrimary,
          displayColor: SmartRailColors.textPrimary,
        );

    OutlineInputBorder field(Color color) => OutlineInputBorder(
      borderRadius: BorderRadius.circular(SmartRailRadius.md),
      borderSide: BorderSide(color: color, width: 1.5),
    );

    final pill = StadiumBorder(
      side: BorderSide(
        color: SmartRailColors.borderStrong,
        width: spec.buttonBorderWidth,
      ),
    );

    const sharedAxis = SharedAxisPageTransitionsBuilder(
      transitionType: SharedAxisTransitionType.horizontal,
    );

    return ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      scaffoldBackgroundColor: SmartRailColors.page,
      fontFamily: SmartRailType.fontFamily,
      textTheme: textTheme,
      extensions: [spec],
      materialTapTargetSize: MaterialTapTargetSize.padded,
      splashFactory: InkRipple.splashFactory,

      // Pushed screens slide on a shared axis; tabs fade (see the app shell). Both are off when motion is reduced.
      pageTransitionsTheme: const PageTransitionsTheme(
        builders: {
          TargetPlatform.android: sharedAxis,
          TargetPlatform.iOS: sharedAxis,
          TargetPlatform.windows: sharedAxis,
          TargetPlatform.linux: sharedAxis,
          TargetPlatform.macOS: sharedAxis,
        },
      ),

      appBarTheme: const AppBarTheme(
        backgroundColor: SmartRailColors.surface,
        foregroundColor: SmartRailColors.textPrimary,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        scrolledUnderElevation: 0,
        centerTitle: false,
      ),

      filledButtonTheme: FilledButtonThemeData(
        style: FilledButton.styleFrom(
          backgroundColor: SmartRailColors.primary,
          foregroundColor: SmartRailColors.onPrimary,
          minimumSize: Size.fromHeight(spec.buttonHeight),
          textStyle: spec.buttonLabel,
          shape: const StadiumBorder(),
          elevation: 0,
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          backgroundColor: SmartRailColors.surface,
          foregroundColor: SmartRailColors.textPrimary,
          minimumSize: Size.fromHeight(spec.buttonHeight),
          textStyle: spec.buttonLabel,
          shape: pill,
          side: BorderSide(
            color: SmartRailColors.borderStrong,
            width: spec.buttonBorderWidth,
          ),
        ),
      ),
      textButtonTheme: TextButtonThemeData(
        style: TextButton.styleFrom(
          foregroundColor: SmartRailColors.primary,
          minimumSize: Size(spec.touchTarget, spec.touchTarget),
          textStyle: floor(SmartRailType.label14),
        ),
      ),

      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: SmartRailColors.surface,
        contentPadding: const EdgeInsets.symmetric(
          horizontal: 14,
          vertical: 16,
        ),
        labelStyle: floor(
          SmartRailType.caption,
        ).copyWith(color: SmartRailColors.textSecondary),
        hintStyle: SmartRailType.body.copyWith(
          color: SmartRailColors.textTertiary,
        ),
        errorStyle: floor(
          SmartRailType.caption,
        ).copyWith(color: SmartRailColors.cancelledText),
        border: field(SmartRailColors.borderStrong),
        enabledBorder: field(SmartRailColors.borderStrong),
        focusedBorder: field(SmartRailColors.primary),
        errorBorder: field(SmartRailColors.cancelledText),
        focusedErrorBorder: field(SmartRailColors.cancelledText),
      ),

      cardTheme: CardThemeData(
        color: SmartRailColors.surface,
        surfaceTintColor: Colors.transparent,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(SmartRailRadius.lg),
          side: const BorderSide(color: SmartRailColors.border),
        ),
      ),

      // The Figma chip: 14 Semi Bold on white with a strong hairline; the chosen one is brand-subtle with a brand hairline and dark-green
      // text. No check mark.
      chipTheme: ChipThemeData(
        backgroundColor: SmartRailColors.surface,
        selectedColor: SmartRailColors.primarySubtle,
        showCheckmark: false,
        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 0),
        side: const BorderSide(color: SmartRailColors.borderStrong),
        shape: const StadiumBorder(),
        labelStyle: floor(
          SmartRailType.label14,
        ).copyWith(color: SmartRailColors.textPrimary),
        secondaryLabelStyle: floor(
          SmartRailType.label14,
        ).copyWith(color: SmartRailColors.primaryPressed),
        secondarySelectedColor: SmartRailColors.primarySubtle,
      ),

      bottomSheetTheme: const BottomSheetThemeData(
        backgroundColor: SmartRailColors.surface,
        surfaceTintColor: Colors.transparent,
        showDragHandle: true,
        dragHandleColor: SmartRailColors.borderStrong,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.vertical(
            top: Radius.circular(SmartRailRadius.sheet),
          ),
        ),
      ),

      snackBarTheme: SnackBarThemeData(
        behavior: SnackBarBehavior.floating,
        backgroundColor: SmartRailColors.textPrimary,
        contentTextStyle: floor(
          SmartRailType.body14,
        ).copyWith(color: SmartRailColors.onPrimary),
        actionTextColor: SmartRailColors.primarySubtle,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(SmartRailRadius.md),
        ),
      ),

      navigationBarTheme: NavigationBarThemeData(
        backgroundColor: SmartRailColors.surface,
        surfaceTintColor: Colors.transparent,
        indicatorColor: SmartRailColors.primarySubtle,
        elevation: 0,
        height: spec.touchTarget + 24,
        labelTextStyle: WidgetStateProperty.resolveWith(
          (states) => floor(SmartRailType.caption).copyWith(
            color: states.contains(WidgetState.selected)
                ? SmartRailColors.primary
                : SmartRailColors.textTertiary,
          ),
        ),
        iconTheme: WidgetStateProperty.resolveWith(
          (states) => IconThemeData(
            color: states.contains(WidgetState.selected)
                ? SmartRailColors.primary
                : SmartRailColors.textTertiary,
          ),
        ),
      ),

      dividerTheme: const DividerThemeData(
        color: SmartRailColors.border,
        space: 1,
        thickness: 1,
      ),
    );
  }
}
