import 'package:flutter/material.dart';

import '../tokens/tokens.dart';

/// Opens a Figma "Bottom sheet": white, 24 radius on top, a 40 by 4 handle, 20 at the sides, 12 above and 32 below, 14 between the
/// parts. It covers the tab bar (root navigator) and grows with the keyboard.
Future<T?> showSmartRailSheet<T>(
  BuildContext context, {
  required WidgetBuilder builder,
}) {
  return showModalBottomSheet<T>(
    context: context,
    useRootNavigator: true,
    isScrollControlled: true,
    showDragHandle: false,
    backgroundColor: SmartRailColors.surface,
    barrierColor: SmartRailColors.textPrimary.withValues(alpha: 0.45),
    shape: const RoundedRectangleBorder(
      borderRadius: BorderRadius.vertical(
        top: Radius.circular(SmartRailRadius.sheet),
      ),
    ),
    builder: (context) => Padding(
      padding: EdgeInsets.only(bottom: MediaQuery.viewInsetsOf(context).bottom),
      child: SafeArea(top: false, child: builder(context)),
    ),
  );
}

/// The body of a sheet: the handle, then [children] with 14 between them.
class SheetBody extends StatelessWidget {
  const SheetBody({
    super.key,
    required this.children,
    this.crossAxisAlignment = CrossAxisAlignment.stretch,
  });

  final List<Widget> children;
  final CrossAxisAlignment crossAxisAlignment;

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.fromLTRB(20, 12, 20, 32),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        crossAxisAlignment: crossAxisAlignment,
        children: [
          Center(
            child: Container(
              width: 40,
              height: 4,
              margin: const EdgeInsets.symmetric(vertical: 4),
              decoration: BoxDecoration(
                color: SmartRailColors.borderStrong,
                borderRadius: BorderRadius.circular(2),
              ),
            ),
          ),
          for (final child in children) ...[const SizedBox(height: 14), child],
        ],
      ),
    );
  }
}
