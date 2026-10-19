import 'package:flutter/material.dart';

import '../tokens/tokens.dart';

/// The Figma "Input": a label above the field, never a placeholder alone. The field is 56 dp high with a 1.5 dp strong border and a
/// 12 dp radius. The label and hint come from the caller.
class SmartRailTextField extends StatelessWidget {
  const SmartRailTextField({
    super.key,
    required this.label,
    this.hint,
    this.controller,
    this.onChanged,
    this.keyboardType,
    this.obscureText = false,
    this.errorText,
    this.autofillHints,
  });

  final String label;
  final String? hint;
  final TextEditingController? controller;
  final ValueChanged<String>? onChanged;
  final TextInputType? keyboardType;
  final bool obscureText;

  /// Words under the field when its value is not accepted. Colour is never the only signal.
  final String? errorText;
  final Iterable<String>? autofillHints;

  @override
  Widget build(BuildContext context) {
    final border = OutlineInputBorder(
      borderRadius: BorderRadius.circular(SmartRailRadius.md),
      borderSide: const BorderSide(color: SmartRailColors.borderStrong, width: 1.5),
    );

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(label, style: SmartRailType.caption.copyWith(color: SmartRailColors.textSecondary)),
        const SizedBox(height: 6),
        ConstrainedBox(
          constraints: const BoxConstraints(minHeight: 56),
          child: TextField(
            controller: controller,
            onChanged: onChanged,
            keyboardType: keyboardType,
            obscureText: obscureText,
            autofillHints: autofillHints,
            style: SmartRailType.body,
            // The label is the text above, which a screen reader reads with the field.
            decoration: InputDecoration(
              hintText: hint,
              errorText: errorText,
              errorStyle: SmartRailType.caption.copyWith(color: SmartRailColors.cancelledText),
              errorBorder: border.copyWith(borderSide: const BorderSide(color: SmartRailColors.cancelledText, width: 1.5)),
              focusedErrorBorder: border.copyWith(borderSide: const BorderSide(color: SmartRailColors.cancelledText, width: 1.5)),
              hintStyle: SmartRailType.body.copyWith(color: SmartRailColors.textTertiary),
              filled: true,
              fillColor: SmartRailColors.surface,
              contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 16),
              border: border,
              enabledBorder: border,
              focusedBorder: border.copyWith(borderSide: const BorderSide(color: SmartRailColors.primary, width: 1.5)),
            ),
          ),
        ),
      ],
    );
  }
}
