import 'package:flutter/material.dart';

import '../theme/smartrail_theme.dart';
import '../tokens/tokens.dart';

/// The four kinds of the Figma "Button" and "Driver/Action button": Primary, Secondary, Tint, Danger.
enum ButtonKind { primary, secondary, tint, danger }

/// A full-width pill button. One primary action per screen. 56 dp high in the passenger app and 72 dp in the driver app, with the
/// label style of each app, all from the theme. The label comes from the caller.
class SmartRailButton extends StatelessWidget {
  const SmartRailButton({
    super.key,
    required this.label,
    required this.onPressed,
    this.kind = ButtonKind.primary,
    this.icon,
  });

  final String label;
  final VoidCallback? onPressed;
  final ButtonKind kind;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    final spec = SmartRailSpec.of(context);

    final (background, foreground, border) = switch (kind) {
      ButtonKind.primary => (
        SmartRailColors.primary,
        SmartRailColors.onPrimary,
        null,
      ),
      ButtonKind.secondary => (
        SmartRailColors.surface,
        SmartRailColors.textPrimary,
        SmartRailColors.borderStrong,
      ),
      ButtonKind.tint => (
        SmartRailColors.primarySubtle,
        SmartRailColors.primaryPressed,
        null,
      ),
      ButtonKind.danger => (
        SmartRailColors.cancelledText,
        SmartRailColors.onPrimary,
        null,
      ),
    };

    final style = FilledButton.styleFrom(
      backgroundColor: background,
      foregroundColor: foreground,
      disabledBackgroundColor: SmartRailColors.border,
      disabledForegroundColor: SmartRailColors.textTertiary,
      minimumSize: Size.fromHeight(spec.buttonHeight),
      padding: const EdgeInsets.symmetric(horizontal: SmartRailSpacing.xl),
      textStyle: spec.buttonLabel,
      shape: StadiumBorder(
        side: border == null
            ? BorderSide.none
            : BorderSide(color: border, width: spec.buttonBorderWidth),
      ),
    );

    final text = Text(label, textAlign: TextAlign.center);
    return icon == null
        ? FilledButton(onPressed: onPressed, style: style, child: text)
        : FilledButton.icon(
            onPressed: onPressed,
            style: style,
            icon: Icon(icon),
            label: text,
          );
  }
}
