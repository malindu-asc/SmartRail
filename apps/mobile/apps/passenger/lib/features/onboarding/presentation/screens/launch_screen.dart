import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../providers/onboarding_providers.dart';

/// The first thing on screen: the SmartRail mark, shown for a moment, then on to the introduction (first time) or Home.
class LaunchScreen extends ConsumerStatefulWidget {
  const LaunchScreen({
    super.key,
    this.duration = const Duration(milliseconds: 1400),
  });

  final Duration duration;

  @override
  ConsumerState<LaunchScreen> createState() => _LaunchScreenState();
}

class _LaunchScreenState extends ConsumerState<LaunchScreen> {
  Timer? _timer;
  bool _visible = false;

  @override
  void initState() {
    super.initState();
    // Fade the mark in on the first frame, then move on.
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (mounted) setState(() => _visible = true);
    });
    _timer = Timer(widget.duration, _continue);
  }

  void _continue() {
    if (!mounted) return;
    final seen = ref.read(onboardingSeenProvider);
    context.go(seen ? AppRoutes.home : AppRoutes.onboarding);
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: SmartRailColors.page,
      body: Center(
        child: AnimatedOpacity(
          opacity: _visible ? 1 : 0,
          duration: SmartRailMotion.of(
            context,
            const Duration(milliseconds: 500),
          ),
          curve: SmartRailMotion.curve,
          child: Semantics(
            label: 'SmartRail',
            child: Image.asset(
              'assets/images/logo.png',
              width: 230,
              height: 120,
              fit: BoxFit.contain,
              excludeFromSemantics: true,
            ),
          ),
        ),
      ),
    );
  }
}
