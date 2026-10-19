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
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.loginTitle)),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(SmartRailSpacing.xl),
          children: [
            Text(l10n.loginWelcome, style: theme.textTheme.headlineLarge),
            const SizedBox(height: SmartRailSpacing.xl),
            SmartRailTextField(
              label: l10n.emailLabel,
              hint: l10n.emailHint,
              controller: _email,
              keyboardType: TextInputType.emailAddress,
              autofillHints: const [AutofillHints.email],
            ),
            const SizedBox(height: SmartRailSpacing.lg),
            SmartRailTextField(
              label: l10n.passwordLabel,
              hint: l10n.passwordHint,
              controller: _password,
              obscureText: true,
              autofillHints: const [AutofillHints.password],
            ),
            Align(
              alignment: Alignment.centerRight,
              child: TextButton(
                onPressed: _forgot,
                child: Text(l10n.forgotPassword),
              ),
            ),
            if (_error != null) ...[
              InfoBanner(kind: BannerKind.warning, title: _error!),
              const SizedBox(height: SmartRailSpacing.md),
            ],
            SmartRailButton(
              label: l10n.loginTitle,
              onPressed: _busy ? null : _submit,
            ),
            const SizedBox(height: SmartRailSpacing.lg),
            InfoBanner(
              kind: BannerKind.message,
              title: l10n.loginNoAccountNeeded,
            ),
            const SizedBox(height: SmartRailSpacing.xl),
            Center(
              child: Wrap(
                crossAxisAlignment: WrapCrossAlignment.center,
                children: [
                  Text(l10n.loginNew, style: theme.textTheme.bodyMedium),
                  TextButton(
                    onPressed: _busy
                        ? null
                        : () =>
                              context.pushReplacement(AppRoutes.createProfile),
                    child: Text(l10n.loginCreate),
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
