import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../l10n/app_localizations.dart';
import '../providers/onboarding_providers.dart';

/// The three first-run slides (design 01 to 03). The illustration container is white, as decided.
class OnboardingScreen extends ConsumerStatefulWidget {
  const OnboardingScreen({super.key});

  @override
  ConsumerState<OnboardingScreen> createState() => _OnboardingScreenState();
}

class _OnboardingScreenState extends ConsumerState<OnboardingScreen> {
  static const _images = [
    'assets/images/onboarding_1.png',
    'assets/images/onboarding_2.png',
    'assets/images/onboarding_3.png',
  ];

  final _controller = PageController();
  int _page = 0;

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  Future<void> _finish() async {
    await ref.read(onboardingSeenProvider.notifier).complete();
    if (mounted) context.go(AppRoutes.home);
  }

  void _next() {
    if (_page == _images.length - 1) {
      _finish();
    } else {
      _controller.nextPage(
        duration: SmartRailMotion.of(context, SmartRailMotion.slow),
        curve: SmartRailMotion.curve,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final titles = [l10n.onb1Title, l10n.onb2Title, l10n.onb3Title];
    final bodies = [l10n.onb1Body, l10n.onb2Body, l10n.onb3Body];
    final last = _page == _images.length - 1;

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      body: SafeArea(
        child: Column(
          children: [
            Align(
              alignment: Alignment.centerRight,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(
                  SmartRailSpacing.lg,
                  SmartRailSpacing.sm,
                  SmartRailSpacing.lg,
                  0,
                ),
                child: TextButton(
                  onPressed: _finish,
                  style: TextButton.styleFrom(
                    backgroundColor: SmartRailColors.bgSoft,
                    foregroundColor: SmartRailColors.textPrimary,
                    shape: const StadiumBorder(),
                    padding: const EdgeInsets.symmetric(
                      horizontal: 18,
                      vertical: 10,
                    ),
                  ),
                  child: Text(l10n.onbSkip),
                ),
              ),
            ),
            Expanded(
              child: PageView.builder(
                controller: _controller,
                itemCount: _images.length,
                onPageChanged: (index) => setState(() => _page = index),
                itemBuilder: (context, index) => Padding(
                  padding: const EdgeInsets.fromLTRB(
                    SmartRailSpacing.lg,
                    SmartRailSpacing.sm,
                    SmartRailSpacing.lg,
                    0,
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // A white container: the artwork sits straight on the page.
                      Container(
                        key: ValueKey('onboarding-art-$index'),
                        height: 300,
                        width: double.infinity,
                        decoration: BoxDecoration(
                          color: SmartRailColors.surface,
                          borderRadius: BorderRadius.circular(
                            SmartRailRadius.sheet,
                          ),
                        ),
                        child: Image.asset(
                          _images[index],
                          fit: BoxFit.contain,
                          excludeFromSemantics: true,
                        ),
                      ),
                      const SizedBox(height: 20),
                      Text(
                        titles[index],
                        style: SmartRailType.display.copyWith(
                          color: SmartRailColors.textPrimary,
                        ),
                      ),
                      const SizedBox(height: 20),
                      Text(
                        bodies[index],
                        style: SmartRailType.body.copyWith(
                          color: SmartRailColors.textSecondary,
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                for (var i = 0; i < _images.length; i++)
                  AnimatedContainer(
                    key: ValueKey('dot-$i'),
                    duration: SmartRailMotion.of(context, SmartRailMotion.base),
                    curve: SmartRailMotion.curve,
                    margin: const EdgeInsets.symmetric(horizontal: 4),
                    height: 8,
                    width: i == _page ? 20 : 8,
                    decoration: BoxDecoration(
                      color: i == _page
                          ? SmartRailColors.textPrimary
                          : SmartRailColors.borderStrong,
                      borderRadius: BorderRadius.circular(4),
                    ),
                  ),
              ],
            ),
            Padding(
              padding: const EdgeInsets.all(SmartRailSpacing.lg),
              child: SmartRailButton(
                label: last ? l10n.onbDone : l10n.onbNext,
                onPressed: _next,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
