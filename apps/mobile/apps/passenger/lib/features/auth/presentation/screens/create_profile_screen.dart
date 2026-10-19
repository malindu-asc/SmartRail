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
      final messenger = ScaffoldMessenger.of(context);
      context.pushReplacement(AppRoutes.login);
      messenger
        ..hideCurrentSnackBar()
        ..showSnackBar(SnackBar(content: Text(l10n.createdLogIn)));
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
    return Scaffold(
      backgroundColor: SmartRailColors.page,
      appBar: SmartRailAppBar(title: Text(l10n.createTitle)),
      body: SafeArea(
        child: Column(
          children: [
            Expanded(
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  SmartRailTextField(
                    label: l10n.fullNameLabel,
                    hint: l10n.fullNameHint,
                    controller: _name,
                    onChanged: (_) => setState(() {}),
                    errorText: _tried && !_nameOk ? l10n.errName : null,
                    autofillHints: const [AutofillHints.name],
                  ),
                  const SizedBox(height: 14),
                  SmartRailTextField(
                    label: l10n.emailLabel,
                    hint: 'name@example.com',
                    controller: _email,
                    onChanged: (_) => setState(() {}),
                    keyboardType: TextInputType.emailAddress,
                    errorText: _tried && !_emailOk ? l10n.errEmail : null,
                    autofillHints: const [AutofillHints.email],
                  ),
                  const SizedBox(height: 14),
                  SmartRailTextField(
                    label: l10n.passwordLabel,
                    hint: l10n.passwordNewHint,
                    controller: _password,
                    obscureText: true,
                    onChanged: (_) => setState(() {}),
                    errorText: _tried && !_passwordOk ? l10n.errPassword : null,
                    autofillHints: const [AutofillHints.newPassword],
                  ),
                  const SizedBox(height: 14),
                  Semantics(
                    container: true,
                    checked: _agreed,
                    label: l10n.agreeTerms,
                    child: InkWell(
                      onTap: () => setState(() => _agreed = !_agreed),
                      child: ExcludeSemantics(
                        child: Row(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            AnimatedContainer(
                              duration: SmartRailMotion.of(
                                context,
                                SmartRailMotion.fast,
                              ),
                              width: 24,
                              height: 24,
                              decoration: BoxDecoration(
                                color: _agreed
                                    ? SmartRailColors.primary
                                    : SmartRailColors.surface,
                                border: Border.all(
                                  color: _agreed
                                      ? SmartRailColors.primary
                                      : SmartRailColors.borderStrong,
                                  width: 2,
                                ),
                                borderRadius: BorderRadius.circular(
                                  SmartRailRadius.sm,
                                ),
                              ),
                              child: _agreed
                                  ? const SvgIcon(
                                      AppIcons.check,
                                      size: 16,
                                      color: SmartRailColors.onPrimary,
                                    )
                                  : null,
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Text(
                                l10n.agreeTerms,
                                style: SmartRailType.body14.copyWith(
                                  color: SmartRailColors.textSecondary,
                                ),
                              ),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                  if (_tried && !_agreed)
                    Padding(
                      padding: const EdgeInsets.only(top: 6, left: 36),
                      child: Text(
                        l10n.errTerms,
                        style: SmartRailType.caption.copyWith(
                          color: SmartRailColors.cancelledText,
                        ),
                      ),
                    ),
                  if (_serverError != null) ...[
                    const SizedBox(height: 14),
                    InfoBanner(kind: BannerKind.warning, title: _serverError!),
                  ],
                ],
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
              child: SmartRailButton(
                label: l10n.createTitle,
                onPressed: _busy ? null : _submit,
              ),
            ),
          ],
        ),
      ),
    );
  }
}
