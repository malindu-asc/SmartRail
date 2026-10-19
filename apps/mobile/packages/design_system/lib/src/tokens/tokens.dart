import 'package:flutter/material.dart';

/// Colours from the Figma file "SmartRail-HighFed" (Components page). Feature code uses these names, never hex values.
abstract final class SmartRailColors {
  // brand/*
  static const primary = Color(0xFF118167);
  static const primaryPressed = Color(0xFF0B5F4B);
  static const primarySubtle = Color(0xFFE3F3EE);
  static const onPrimary = Color(0xFFFFFFFF);

  // text/*
  static const textPrimary = Color(0xFF0F1412);
  static const textSecondary = Color(0xFF4B5563);
  static const textTertiary = Color(0xFF6B7280);

  // bg/* and border/*
  static const surface = Color(0xFFFFFFFF);
  static const page = Color(0xFFFFFFFF);
  static const borderStrong = Color(0xFFD1D5DB);

  // A light divider. Taken from the Figma board's Foundations frame; confirm against the file's border token (SPEC-008 Q1).
  static const border = Color(0xFFE5E7EB);

  // status/*: each has a text colour and a soft background. Never the only signal: always with an icon and words (REQ-PAX-004).
  static const delayedText = Color(0xFFB45309);
  static const delayedBg = Color(0xFFFEF3C7);
  static const cancelledText = Color(0xFFB91C1C);
  static const cancelledBg = Color(0xFFFEE2E2);
  static const changedText = Color(0xFF1D4ED8);
  static const changedBg = Color(0xFFDBEAFE);
  static const staleText = Color(0xFF4B5563);
  static const staleBg = Color(0xFFF1F5F9);

  /// The dashed outline of the "no live data" chip.
  static const staleBorder = Color(0xFF94A3B8);
}

/// Spacing scale in logical pixels (`space/*`).
abstract final class SmartRailSpacing {
  static const double xs = 4;
  static const double sm = 8;
  static const double md = 12;
  static const double lg = 16;
  static const double xl = 24;
  static const double xxl = 32;
}

/// `radius/*`. Buttons and chips are pills; fields and banners use [md].
abstract final class SmartRailRadius {
  static const double sm = 8;
  static const double md = 12;
  static const double lg = 16;
  static const double pill = 999;
}

/// Inter text styles of the Figma file. Until the font files are bundled the platform font stands in (the family is already set).
abstract final class SmartRailType {
  static const fontFamily = 'Inter';

  static const headline = TextStyle(fontFamily: fontFamily, fontSize: 32, fontWeight: FontWeight.w700, height: 1.2);

  /// Title/20 Bold: the label of a driver action button.
  static const title20 = TextStyle(fontFamily: fontFamily, fontSize: 20, fontWeight: FontWeight.w700, height: 26 / 20);

  /// Title 18 Semi Bold (from the Figma board; the file's own title style is confirmed with SPEC-009).
  static const title = TextStyle(fontFamily: fontFamily, fontSize: 18, fontWeight: FontWeight.w600, height: 1.3);

  /// Button/16 Semi Bold.
  static const button = TextStyle(fontFamily: fontFamily, fontSize: 16, fontWeight: FontWeight.w600, height: 24 / 16);

  /// Body/16 Regular.
  static const body = TextStyle(fontFamily: fontFamily, fontSize: 16, fontWeight: FontWeight.w400, height: 24 / 16);

  /// Body/14 Regular.
  static const body14 = TextStyle(fontFamily: fontFamily, fontSize: 14, fontWeight: FontWeight.w400, height: 20 / 14);

  /// Label/14 Semi Bold.
  static const label14 = TextStyle(fontFamily: fontFamily, fontSize: 14, fontWeight: FontWeight.w600, height: 20 / 14);

  /// Caption/12 Medium.
  static const caption = TextStyle(fontFamily: fontFamily, fontSize: 12, fontWeight: FontWeight.w500, height: 16 / 12);

  /// Times line up in a column when digits have the same width ("14:25 → 15:42").
  static const timeFeatures = [FontFeature.tabularFigures()];
}
