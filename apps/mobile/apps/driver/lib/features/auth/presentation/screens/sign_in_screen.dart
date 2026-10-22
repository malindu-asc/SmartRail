import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../l10n/app_localizations.dart';
import '../providers/auth_providers.dart';

/// Driver sign in (designs 00 and 41). Accounts are made by the administrator; there is no sign-up.
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
      body: Column(
        children: [
          Expanded(
            child: SafeArea(
              bottom: false,
              child: ListView(
                padding: const EdgeInsets.fromLTRB(24, 56, 24, 24),
                children: [
                  Text(
                    l10n.signInHello,
                    style: SmartRailType.display.copyWith(
                      color: SmartRailColors.textPrimary,
                    ),
                  ),
                  Text(
                    l10n.signInRole,
                    style: SmartRailType.display.copyWith(
                      color: SmartRailColors.primary,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Text(
                    l10n.signInSubtitle,
                    style: SmartRailType.body.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                  const SizedBox(height: 48),
                  SmartRailTextField(
                    label: l10n.staffIdLabel,
                    hint: l10n.staffIdHint,
                    controller: _email,
                    keyboardType: TextInputType.emailAddress,
                    autofillHints: const [AutofillHints.username],
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
                  Text(
                    l10n.forgotPassword,
                    style: SmartRailType.body14.copyWith(
                      color: SmartRailColors.textSecondary,
                    ),
                  ),
                  if (message != null) ...[
                    const SizedBox(height: 16),
                    InfoBanner(kind: BannerKind.warning, title: message),
                  ],
                ],
              ),
            ),
          ),
          StickyActionBar(
            children: [
              SmartRailButton(
                label: l10n.signInButton,
                onPressed: _busy ? null : _submit,
              ),
              const SizedBox(height: 12),
              Text(
                l10n.safetyNote,
                textAlign: TextAlign.center,
                style: SmartRailType.caption.copyWith(
                  color: SmartRailColors.textSecondary,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
