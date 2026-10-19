import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:smartrail_core/smartrail_core.dart';
import 'package:smartrail_design_system/smartrail_design_system.dart';

import '../../../../core/routing/app_routes.dart';
import '../../../../l10n/app_localizations.dart';
import '../providers/auth_providers.dart';

/// Create profile (design 17): name, email, password and agreement. It signs the person in when it is done.
class CreateProfileScreen extends ConsumerStatefulWidget {
  const CreateProfileScreen({super.key});

  @override
  ConsumerState<CreateProfileScreen> createState() =>
      _CreateProfileScreenState();
}

class _CreateProfileScreenState extends ConsumerState<CreateProfileScreen> {
  static final _emailPattern = RegExp(r'^[^@\s]+@[^@\s]+\.[^@\s]+$');

  final _name = TextEditingController();
  final _email = TextEditingController();
  final _password = TextEditingController();
  bool _agreed = false;
  bool _busy = false;
  bool _tried = false;
  String? _serverError;

  @override
  void dispose() {
    _name.dispose();
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  bool get _nameOk => _name.text.trim().isNotEmpty;
  bool get _emailOk => _emailPattern.hasMatch(_email.text.trim());
  bool get _passwordOk => _password.text.length >= 8;

  Future<void> _submit() async {
    final l10n = AppLocalizations.of(context);
    setState(() => _tried = true);
    if (!(_nameOk && _emailOk && _passwordOk && _agreed)) return;

    setState(() {
      _busy = true;
      _serverError = null;
    });

    final outcome = await ref
        .read(authProvider.notifier)
        .register(
          fullName: _name.text,
          email: _email.text,
          password: _password.text,
          acceptedTerms: _agreed,
        );
    if (!mounted) return;

    final failure = outcome.failure;
    if (failure == null) {
      context.canPop() ? context.pop(true) : context.go(AppRoutes.me);
      return;
    }

    // The profile exists but signing in failed: say so and go to Log in, instead of asking for the same email again.
    if (outcome.created) {
      ScaffoldMessenger.of(context)
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(l10n.createdLogIn)));
      context.pushReplacement(AppRoutes.login);
      return;
    }

    setState(() {
      _busy = false;
      _serverError = switch (failure) {
        ConflictFailure() => l10n.errAccountExists,
        NetworkFailure() => l10n.errorNetwork,
        ValidationFailure(:final message) when message != null => message,
        _ => l10n.errorGeneric,
      };
    });
  }

  @override
  Widget build(BuildContext context) {
    final l10n = AppLocalizations.of(context);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: Text(l10n.createTitle)),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(SmartRailSpacing.xl),
          children: [
            SmartRailTextField(
              label: l10n.fullNameLabel,
              hint: l10n.fullNameHint,
              controller: _name,
              onChanged: (_) => setState(() {}),
              errorText: _tried && !_nameOk ? l10n.errName : null,
              autofillHints: const [AutofillHints.name],
            ),
            const SizedBox(height: SmartRailSpacing.lg),
            SmartRailTextField(
              label: l10n.emailLabel,
              hint: 'name@example.com',
              controller: _email,
              onChanged: (_) => setState(() {}),
              keyboardType: TextInputType.emailAddress,
              errorText: _tried && !_emailOk ? l10n.errEmail : null,
              autofillHints: const [AutofillHints.email],
            ),
            const SizedBox(height: SmartRailSpacing.lg),
            SmartRailTextField(
              label: l10n.passwordLabel,
              hint: l10n.passwordNewHint,
              controller: _password,
              obscureText: true,
              onChanged: (_) => setState(() {}),
              errorText: _tried && !_passwordOk ? l10n.errPassword : null,
              autofillHints: const [AutofillHints.newPassword],
            ),
            const SizedBox(height: SmartRailSpacing.md),
            CheckboxListTile(
              contentPadding: EdgeInsets.zero,
              controlAffinity: ListTileControlAffinity.leading,
              value: _agreed,
              onChanged: (v) => setState(() => _agreed = v ?? false),
              title: Text(l10n.agreeTerms, style: theme.textTheme.bodyMedium),
              subtitle: _tried && !_agreed
                  ? Text(
                      l10n.errTerms,
                      style: theme.textTheme.labelSmall?.copyWith(
                        color: SmartRailColors.cancelledText,
                      ),
                    )
                  : null,
            ),
            const SizedBox(height: SmartRailSpacing.lg),
            if (_serverError != null) ...[
              InfoBanner(kind: BannerKind.warning, title: _serverError!),
              const SizedBox(height: SmartRailSpacing.md),
            ],
            SmartRailButton(
              label: l10n.createTitle,
              onPressed: _busy ? null : _submit,
            ),
          ],
        ),
      ),
    );
  }
}
