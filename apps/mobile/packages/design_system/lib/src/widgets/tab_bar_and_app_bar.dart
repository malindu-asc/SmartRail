import 'package:flutter/material.dart';

import '../tokens/tokens.dart';
import 'svg_icon.dart';

class TabBarItem {
  const TabBarItem({required this.label, required this.icon});

  final String label;
  final String icon;
}

/// The Figma "Tab bar": white, a hairline on top, five equal items of icon (24) over a 12 Medium label, the current one in the brand
/// colour and the rest grey. There is no pill behind the current tab.
class SmartRailTabBar extends StatelessWidget {
  const SmartRailTabBar({
    super.key,
    required this.items,
    required this.currentIndex,
    required this.onTap,
  });

  final List<TabBarItem> items;
  final int currentIndex;
  final ValueChanged<int> onTap;

  @override
  Widget build(BuildContext context) {
    final bottom = MediaQuery.paddingOf(context).bottom;

    return DecoratedBox(
      decoration: const BoxDecoration(
        color: SmartRailColors.surface,
        border: Border(top: BorderSide(color: SmartRailColors.border)),
      ),
      child: Padding(
        // 8 above and 24 below in the design; the phone's own bottom inset counts towards the 24.
        padding: EdgeInsets.fromLTRB(8, 8, 8, bottom < 24 ? 24 : bottom),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            for (var i = 0; i < items.length; i++)
              Expanded(
                child: Semantics(
                  button: true,
                  selected: i == currentIndex,
                  label: items[i].label,
                  child: ExcludeSemantics(
                    child: InkWell(
                      borderRadius: BorderRadius.circular(SmartRailRadius.md),
                      onTap: () => onTap(i),
                      child: ConstrainedBox(
                        constraints: const BoxConstraints(minHeight: 52),
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            SvgIcon(
                              items[i].icon,
                              size: 24,
                              color: i == currentIndex
                                  ? SmartRailColors.primary
                                  : SmartRailColors.textTertiary,
                            ),
                            const SizedBox(height: 2),
                            Text(
                              items[i].label,
                              style: SmartRailType.caption.copyWith(
                                color: i == currentIndex
                                    ? SmartRailColors.primary
                                    : SmartRailColors.textTertiary,
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}

/// The Figma "App bar": 56 high, a 40 round back button with a hairline, then the title in 18 Semi Bold. White, no shadow.
class SmartRailAppBar extends StatelessWidget implements PreferredSizeWidget {
  const SmartRailAppBar({
    super.key,
    this.title,
    this.actions = const [],
    this.bottom,
    this.backgroundColor,
    this.foregroundColor,
    this.showBack,
  });

  final Widget? title;
  final List<Widget> actions;
  final PreferredSizeWidget? bottom;
  final Color? backgroundColor;
  final Color? foregroundColor;

  /// Null shows the back button whenever there is somewhere to go back to.
  final bool? showBack;

  @override
  Size get preferredSize =>
      Size.fromHeight(56 + (bottom?.preferredSize.height ?? 0));

  @override
  Widget build(BuildContext context) {
    final onColor = foregroundColor ?? SmartRailColors.textPrimary;
    final canBack = showBack ?? Navigator.of(context).canPop();

    return Material(
      color: backgroundColor ?? SmartRailColors.surface,
      child: SafeArea(
        bottom: false,
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            SizedBox(
              height: 56,
              child: Padding(
                padding: const EdgeInsets.symmetric(
                  horizontal: SmartRailSpacing.lg,
                  vertical: SmartRailSpacing.sm,
                ),
                child: Row(
                  children: [
                    if (canBack) ...[
                      Semantics(
                        button: true,
                        label: MaterialLocalizations.of(
                          context,
                        ).backButtonTooltip,
                        child: ExcludeSemantics(
                          child: InkWell(
                            customBorder: const CircleBorder(),
                            onTap: () => Navigator.of(context).maybePop(),
                            child: Container(
                              width: 40,
                              height: 40,
                              alignment: Alignment.center,
                              decoration: BoxDecoration(
                                shape: BoxShape.circle,
                                border: Border.all(
                                  color: backgroundColor == null
                                      ? SmartRailColors.border
                                      : onColor.withValues(alpha: 0.4),
                                ),
                              ),
                              child: SvgIcon(
                                AppIcons.back,
                                size: 20,
                                color: onColor,
                              ),
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: SmartRailSpacing.md),
                    ],
                    Expanded(
                      child: DefaultTextStyle(
                        style: SmartRailType.title.copyWith(
                          color: onColor,
                          fontSize: 18,
                          height: 24 / 18,
                          fontWeight: FontWeight.w600,
                        ),
                        overflow: TextOverflow.ellipsis,
                        maxLines: 1,
                        child: title ?? const SizedBox.shrink(),
                      ),
                    ),
                    ...actions,
                  ],
                ),
              ),
            ),
            ?bottom,
          ],
        ),
      ),
    );
  }
}
