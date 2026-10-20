import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../l10n/app_localizations.dart';
import '../providers/auth_providers.dart';

/// Driver sign in (design 00 and 41). Accounts are made by the administrator; there is no sign-up.
class SignInScreen extends ConsumerStatefulWidget {
  const SignInScreen({super.key});

  @override
  ConsumerState<SignInScreen> createState() => _SignInScreenState();
}

class _SignInScreenState extends ConsumerState<SignInScreen> {
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _busy = false;
  SignInProblem? _problem;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_email.text.trim().isEmpty || _password.text.isEmpty) {
      setState(() => _problem = SignInProblem.badCredentials);
      return;
    }
    setState(() {
      _busy = true;
      _problem = null;
    });
    final problem = await ref
        .read(authProvider.notifier)
        .signIn(_email.text, _password.text);
    // On success the router moves on by itself.
    if (mounted) {
      setState(() {
        _busy = false;
        _problem = problem;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final message = switch (_problem) {
      null => null,
      SignInProblem.badCredentials => l10n.signInBadCredentials,
      SignInProblem.notADriver => l10n.signInNotDriver,
      SignInProblem.network => l10n.errorNetwork,
      SignInProblem.other => l10n.errorGeneric,
    };

    return Scaffold(
      backgroundColor: SmartRailColors.page,
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(SmartRailSpacing.lg),
          children: [
            const SizedBox(height: 24),
            Center(child: Image.asset('assets/images/logo.png', height: 72)),
            const SizedBox(height: 24),
            Text(
              l10n.signInTitle,
              style: SmartRailType.headline.copyWith(
                color: SmartRailColors.textPrimary,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              l10n.signInWelcome,
              style: SmartRailType.body14.copyWith(
                color: SmartRailColors.textSecondary,
              ),
            ),
            const SizedBox(height: 24),
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
            if (message != null) ...[
              InfoBanner(kind: BannerKind.warning, title: message),
              const SizedBox(height: 16),
            ],
            SmartRailButton(
              label: l10n.signInButton,
              onPressed: _busy ? null : _submit,
            ),
          ],
        ),
      ),
    );
  }
}
