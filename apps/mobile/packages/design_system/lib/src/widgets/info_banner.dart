import 'package:flutter/material.dart';

import '../tokens/tokens.dart';

/// The three kinds of the Figma "Driver/Banner": offline, warning and message.
enum BannerKind {
  offline(Icons.wifi_off, SmartRailColors.staleText, SmartRailColors.staleBg),
  warning(Icons.warning_amber_rounded, SmartRailColors.delayedText, SmartRailColors.delayedBg),
  message(Icons.chat_bubble_outline, SmartRailColors.primaryPressed, SmartRailColors.primarySubtle);

  const BannerKind(this.icon, this.color, this.background);

  final IconData icon;
  final Color color;
  final Color background;
}

/// A short notice with an icon, a bold title and a line of detail. The words come from the caller. It is a live region, so a
/// screen reader announces it when it appears.
class InfoBanner extends StatelessWidget {
  const InfoBanner({super.key, required this.kind, required this.title, this.detail});

  final BannerKind kind;
  final String title;
  final String? detail;

  @override
  Widget build(BuildContext context) {
    return Semantics(
      container: true,
      liveRegion: true,
      label: detail == null ? title : '$title. $detail',
      child: ExcludeSemantics(
        child: Container(
          width: double.infinity,
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(color: kind.background, borderRadius: BorderRadius.circular(SmartRailRadius.md)),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(kind.icon, size: 22, color: kind.color),
              const SizedBox(width: SmartRailSpacing.md),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, style: SmartRailType.label14.copyWith(color: kind.color)),
                    if (detail != null) ...[
                      const SizedBox(height: 2),
                      Text(detail!, style: SmartRailType.body14.copyWith(color: kind.color)),
                    ],
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// The phone cannot reach the platform. It says so, and when it last could (REQ-NFR-006).
class OfflineBanner extends StatelessWidget {
  const OfflineBanner({super.key, required this.title, this.detail});

  final String title;
  final String? detail;

  @override
  Widget build(BuildContext context) => InfoBanner(kind: BannerKind.offline, title: title, detail: detail);
}
