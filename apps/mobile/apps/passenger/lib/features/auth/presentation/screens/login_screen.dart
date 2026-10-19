import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../l10n/app_localizations.dart';
import '../providers/auth_providers.dart';

/// Log in (design 16). Only needed for tickets; browsing never asks for it.
class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _busy = false;
  String? _error;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    final l10n = AppLocalizations.of(context);
    if (_email.text.trim().isEmpty || _password.text.isEmpty) {
      setState(() => _error = l10n.loginBadCredentials);
      return;
    }

    setState(() {
      _busy = true;
      _error = null;
    });

    final failure = await ref
        .read(authProvider.notifier)
        .signIn(_email.text, _password.text);
    if (!mounted) return;

    if (failure == null) {
      // Back to where the person came from, or Me when there is nowhere to go back to.
      context.canPop() ? context.pop(true) : context.go(AppRoutes.me);
      return;
    }

    setState(() {
      _busy = false;
      _error = failure is NetworkFailure
          ? l10n.errorNetwork
          : (failure is UnauthorizedFailure
                ? l10n.loginBadCredentials
                : l10n.errorGeneric);
    });
  }

  Future<void> _forgot() async {
    final l10n = AppLocalizations.of(context);
    final email = _email.text.trim();
    if (email.isEmpty) return;

    await ref.read(authRepositoryProvider).forgotPassword(email);
    if (!mounted) return;
    ScaffoldMessenger.of(context)
      ..hideCurrentSnackBar()
      ..showSnackBar(SnackBar(content: Text(l10n.forgotSent)));
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      appBar: SmartRailAppBar(title: Text(l10n.loginTitle)),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            Text(
              l10n.loginWelcome,
              style: SmartRailType.headline.copyWith(
                color: SmartRailColors.textPrimary,
              ),
            ),
            const SizedBox(height: 16),
            SmartRailTextField(
              label: l10n.emailLabel,
              hint: l10n.emailHint,
              controller: _email,
              keyboardType: TextInputType.emailAddress,
              autofillHints: const [AutofillHints.email],
            ),
            const SizedBox(height: 16),
            SmartRailTextField(
              label: l10n.passwordLabel,
              hint: l10n.passwordHint,
              controller: _password,
              obscureText: true,
              autofillHints: const [AutofillHints.password],
            ),
            const SizedBox(height: 16),
            Align(
              alignment: Alignment.centerRight,
              child: GestureDetector(
                onTap: _forgot,
                child: Text(
                  l10n.forgotPassword,
                  style: SmartRailType.label14.copyWith(
                    color: SmartRailColors.primaryPressed,
                  ),
                ),
              ),
            ),
            const SizedBox(height: 16),
            if (_error != null) ...[
              InfoBanner(kind: BannerKind.warning, title: _error!),
              const SizedBox(height: 16),
            ],
            SmartRailButton(
              label: l10n.loginTitle,
              onPressed: _busy ? null : _submit,
            ),
            const SizedBox(height: 16),
            DecoratedBox(
              decoration: BoxDecoration(
                color: SmartRailColors.staleBg,
                borderRadius: BorderRadius.circular(SmartRailRadius.md),
              ),
              child: Padding(
                padding: const EdgeInsets.all(12),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const SvgIcon(
                      AppIcons.info,
                      size: 18,
                      color: SmartRailColors.staleText,
                    ),
                    const SizedBox(width: 10),
                    Expanded(
                      child: Text(
                        l10n.loginNoAccountNeeded,
                        style: SmartRailType.caption.copyWith(
                          color: SmartRailColors.staleText,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 32),
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 10),
              child: Wrap(
                alignment: WrapAlignment.center,
                spacing: 6,
                children: [
                  Text(
                    l10n.loginNew,
                    style: SmartRailType.body14.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                  GestureDetector(
                    onTap: _busy
                        ? null
                        : () =>
                              context.pushReplacement(AppRoutes.createProfile),
                    child: Text(
                      l10n.loginCreate,
                      style: SmartRailType.label14.copyWith(
                        color: SmartRailColors.primaryPressed,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
