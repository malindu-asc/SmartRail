---
name: smartrail-flutter
description: Use when adding or changing code in the SmartRail Flutter apps (apps/mobile - passenger and driver) - creating a feature, screen, provider, route, repository, use case, API call, localization string, or when asked about the mobile architecture, state management or navigation. Feature-based clean architecture with Riverpod (code generation), go_router and fpdart, in a Dart pub workspace with shared packages.
---

# SmartRail mobile - Flutter

> **Examples are for reference only.** Every code block shows a *pattern*. Entity names, fields, routes and values are illustrative. Take the real names, fields and rules from the approved spec, `docs/00-requirements` and the Figma file. Never paste an example in as the implementation.

Reference project: `C:\works\MobileWorkshop\Libra` (a library app built with the same stack). It is a **read-only reference for patterns**. Do not copy its code, names or domain, and do not repeat its shortcuts (see section 12).

**Process:** no feature code without an approved spec (`specs/`, skill `spec-driven-development`). Branch `feature/<feature-name>`. Every acceptance criterion gets a test. Backend work follows `smartrail-modular-monolith`.

---

## 1. Stack and decisions

| Concern | Choice |
|---|---|
| State and DI | `flutter_riverpod` 3 with `riverpod_annotation` and `riverpod_generator` (`@riverpod`), `build_runner` |
| Navigation | `go_router` with one `appRouterProvider` (declarative, redirect guard, `StatefulShellRoute` for tabs) |
| Errors | `fpdart` `Either<Failure, T>` in the domain and data layers |
| Architecture | Feature-based clean architecture: `data` / `domain` / `presentation` inside each feature |
| Models | Plain immutable Dart classes; `Model extends Entity` adds `fromJson` |
| Strings | `flutter_localizations` + `intl` ARB files (`en`, `si`; `ta` later). No hard-coded user text |
| Secure storage | `flutter_secure_storage` for tokens |
| Design | Tokens and shared widgets come from the `smartrail_design_system` package, which mirrors the Figma variables |
| Testing | `flutter_test`, `ProviderContainer` overrides, widget tests; no mocking libraries unless a spec needs one |

Dependencies are declared once per app `pubspec.yaml` with `resolution: workspace`. Check current versions with `flutter pub outdated` before adding anything.

---

## 2. Workspace layout

```
apps/mobile/
  pubspec.yaml                 workspace: apps/*, packages/*
  melos.yaml                   analyze, test scripts
  apps/
    passenger/                 passenger app
    driver/                    driver app
  packages/
    core/                      Failure types, UseCase base, extensions shared by both apps
    design_system/             colours, spacing, radius, text styles, theme, shared widgets
    api_client/                HTTP client, auth token handling, ProblemDetails mapping, base URL
```

Rules:
- A package never imports an app. Apps import packages, never each other.
- Anything used by both apps goes in a package. Anything used by one app stays in that app's `lib/`.
- A feature that both apps need (for example station search) is built once in a package only when a second app really needs it; until then it lives in the first app.

Inside one app:

```
lib/
  main.dart                    ProviderScope + MaterialApp.router
  core/
    routing/app_router.dart    every route, redirect guard
    widgets/                   app-only shared widgets (shell with tab bar)
  features/
    <feature>/
      domain/
        entities/              plain data classes
        repositories/          abstract interface
        usecases/              one class per operation
      data/
        models/                fromJson, extends the entity
        datasources/           remote (API) and local (cache) interfaces + implementations
        repositories/          implements the domain interface, maps errors to Failure
      presentation/
        providers/             wiring + controllers (@riverpod)
        screens/               one file per screen
        widgets/               widgets used only by this feature
  l10n/                        app_en.arb, app_si.arb
```

---

## 3. Dependency rules

```
presentation -> domain  <- data
```

| From | To | Allowed |
|---|---|---|
| `domain` | Flutter, `data`, `presentation`, any package except `fpdart` and `core` | **Never** |
| `data` | `domain` of its own feature, `api_client`, `core` | Yes |
| `data` | another feature's `data` | **Never** |
| `presentation` | own `domain` (entities, use cases), own providers | Yes |
| `presentation` | own `data` directly (only `providers/` wires it) | Wiring only |
| feature A | feature B | Only through B's `domain` interface or a shared package. Never B's `data` or `presentation` |

Widgets never call repositories, datasources or the HTTP client. They `ref.watch` a provider.

---

## 4. Domain layer

```dart
// Reference only - illustrative names
class Station {
  const Station({required this.code, required this.name, this.facilities = const []});
  final String code;
  final String name;
  final List<String> facilities;
}

abstract class StationRepository {
  Future<Either<Failure, List<Station>>> search(String query, {required String language});
}

class SearchStations implements UseCase<List<Station>, SearchStationsParams> {
  const SearchStations(this.repository);
  final StationRepository repository;

  @override
  Future<Either<Failure, List<Station>>> call(SearchStationsParams params) =>
      repository.search(params.query, language: params.language);
}
```

- Entities are `const`, immutable, with no JSON code.
- One use case per business operation. It does one thing; if it only forwards a call that is still correct, because the screen must not know the repository.
- Business rules (for example "a search needs 2 or more characters") live in the use case or entity, not in the widget.
- `Failure` is a sealed class in `packages/core` (section 7).

---

## 5. Data layer

```dart
// Reference only
class StationModel extends Station {
  const StationModel({required super.code, required super.name, super.facilities});

  factory StationModel.fromJson(Map<String, dynamic> json) => StationModel(
        code: json['code'] as String,
        name: json['name'] as String,
        facilities: (json['facilities'] as List<dynamic>? ?? []).cast<String>(),
      );
}

abstract class StationRemoteDataSource {
  Future<List<StationModel>> search(String query, {required String language});
}

class StationRepositoryImpl implements StationRepository {
  const StationRepositoryImpl(this.remote);
  final StationRemoteDataSource remote;

  @override
  Future<Either<Failure, List<Station>>> search(String query, {required String language}) async {
    try {
      return Right(await remote.search(query, language: language));
    } on ApiException catch (e) {
      return Left(e.toFailure());          // typed mapping, section 7
    }
  }
}
```

Rules:
- Datasources are interfaces with a remote implementation (HTTP) and, where the spec needs offline, a local one (cache). The repository decides between them.
- Datasources throw `ApiException` (from `api_client`). The repository **catches that specific type**, never a bare `catch (e)`, and returns a typed `Failure`.
- Public reads send `Accept-Language` and use the `ETag` the API returns.
- Base URL and flags come from `--dart-define` (for example `API_BASE_URL`), never hard-coded.
- A fake datasource that reads JSON assets is fine for the very first run of a feature before the API exists, but it must sit behind the same interface and be replaced, not extended.

---

## 6. Presentation layer (Riverpod)

Wiring and controllers live together in `presentation/providers/<feature>_providers.dart`:

```dart
part 'station_providers.g.dart';

@Riverpod(keepAlive: true)
StationRemoteDataSource stationRemoteDataSource(Ref ref) =>
    StationRemoteDataSourceImpl(ref.watch(apiClientProvider));

@Riverpod(keepAlive: true)
StationRepository stationRepository(Ref ref) =>
    StationRepositoryImpl(ref.watch(stationRemoteDataSourceProvider));

@riverpod
SearchStations searchStationsUseCase(Ref ref) =>
    SearchStations(ref.watch(stationRepositoryProvider));

// A query that depends on a parameter: a family (generated from the function arguments).
@riverpod
Future<List<Station>> stationSearch(Ref ref, String query) async {
  final language = ref.watch(appLanguageProvider);
  final result = await ref.watch(searchStationsUseCaseProvider)(
    SearchStationsParams(query: query, language: language));
  return result.match((failure) => throw failure, (stations) => stations);
}
```

Rules:
- Datasource and repository providers are `keepAlive: true`. Use cases and screen state are auto-dispose.
- A failure is **thrown as the `Failure` object**, so the UI can pattern-match on its type. Do not turn it into a string.
- Reads use a function provider or `AsyncNotifier` (`build()` returns the initial load). Writes and actions use an `AsyncNotifier` controller:

```dart
@riverpod
class SubmitReportController extends _$SubmitReportController {
  @override
  FutureOr<void> build() {}

  Future<void> submit(ReportParams params) async {
    state = const AsyncLoading();
    final result = await ref.read(submitReportUseCaseProvider)(params);
    if (!ref.mounted) return;                 // the screen may be gone
    state = result.match(
      (failure) => AsyncError(failure, StackTrace.current),
      (_) => const AsyncData(null),
    );
  }
}
```

- Screens are `ConsumerWidget` (or `ConsumerStatefulWidget` only when they own a controller or timer). Render every async state:

```dart
ref.watch(stationSearchProvider(query)).when(
  data: (items) => items.isEmpty ? const EmptyState(...) : StationList(items),
  loading: () => const LoadingList(),
  error: (e, _) => ErrorState(failure: e as Failure, onRetry: () => ref.invalidate(stationSearchProvider(query))),
);
```

- Every surface designs the five states (REQ-NFR-006): **empty, loading, stale, error, offline**. A spec that omits one is incomplete.
- Debounce free-text search (300 ms) and do not call the API for fewer than 2 characters.
- Run `dart run build_runner build -d` after changing any `@riverpod` file. Commit the generated `.g.dart` files only if the project already commits them; check `.gitignore` first and stay consistent.

---

## 7. Failures and errors

`packages/core` defines one sealed hierarchy; `api_client` maps HTTP results into it.

| Failure | When |
|---|---|
| `ValidationFailure` | 400 or client-side validation |
| `UnauthorizedFailure` | 401 (token missing or expired) |
| `ForbiddenFailure` | 403 |
| `NotFoundFailure` | 404 |
| `ConflictFailure` | 409 |
| `OfflineFailure` | no connection or timeout |
| `ServerFailure` | 5xx or an unreadable response |

- The API returns RFC 9457 `ProblemDetails` with a stable `code` such as `Network.StationNotFound`. Keep the `code` on the failure so the UI can pick a specific localized message; never show raw server text to a passenger.
- Mapping lives in one place (`api_client`). Features do not parse status codes.
- A 401 triggers one silent token refresh, then the sign-in flow. Do not retry in a loop.

---

## 8. Navigation (go_router)

```dart
// Reference only
final _rootNavigatorKey = GlobalKey<NavigatorState>();

@Riverpod(keepAlive: true)
GoRouter appRouter(Ref ref) => GoRouter(
  navigatorKey: _rootNavigatorKey,
  initialLocation: '/',
  redirect: (context, state) { /* decide only here what needs a session */ },
  routes: [
    GoRoute(path: '/', builder: (_, __) => const LaunchScreen()),
    StatefulShellRoute.indexedStack(
      builder: (_, __, shell) => MainShell(navigationShell: shell),
      branches: [
        StatefulShellBranch(routes: [GoRoute(path: '/home', builder: ...)]),
        StatefulShellBranch(routes: [
          GoRoute(path: '/stations', builder: ..., routes: [
            GoRoute(path: ':code', parentNavigatorKey: _rootNavigatorKey, builder: ...),
          ]),
        ]),
      ],
    ),
  ],
);
```

Rules:
- Every route is declared in `core/routing/app_router.dart`. No `Navigator.push` in feature code; use `context.go`, `context.push`, `context.pop`.
- One router provider, `keepAlive: true`, so the stack survives rebuilds.
- Public routes are a named set; the redirect is the only place that checks the session.
- Tabs use `StatefulShellRoute` so each tab keeps its stack. Detail screens use `parentNavigatorKey: _rootNavigatorKey` so they cover the tab bar.
- **Confirmations are bottom sheets** (`showModalBottomSheet`), not dialogs. This is a design decision for both apps.
- The driver app has no tab bar during an active journey; it uses the root navigator for that flow.
- Pass ids and codes in the path (`/stations/:code`), never whole objects.

---

## 9. Design system and UI rules

- Colours, spacing, radius and text styles come from `smartrail_design_system`; they mirror the Figma variables and styles (`SmartRail / Color`, `Spacing`, `Radius`, Inter text styles). **No hex values or magic numbers in feature code.**
- Primary colour is `brand/primary` (`#118167`). White page background, grey (`bg/soft`) for switches and skeletons. Status is always icon + text + colour (REQ-PAX-004).
- Touch targets: passenger 48 dp, driver actions 72 dp.
- Respect text scaling up to 200 % (passenger) and a 14 px minimum in the driver app.
- Shared widgets (`EmptyState`, `ErrorState`, `OfflineBanner`, `StatusChip`, `PrimaryButton`) live in the design system and take localized text from the caller.
- Match the Figma frame for the screen (file `ZCUQkZpq24jKc7zLvs5KFR`: pages Passenger app and Driver app). Name the screen file after the frame.

---

## 10. Localization

- Every user-visible string is an ARB key in `lib/l10n/app_en.arb`, with `app_si.arb` kept in step. Use `L10n.of(context).key`.
- Station and route names come from the API already localized (`?lang=` or `Accept-Language`); do not translate them in the app.
- Dates and times: server sends UTC, the app shows the network time zone (REQ-NFR-005). Use `intl` formatters.
- Plain wording from the content guide; delay is shown as a delta and the scheduled time stays visible (REQ-PAX-002).

---

## 11. Auth, offline and the driver app

- Sign-in is Keycloak (authorization code with PKCE, client `smartrail-mobile`). Tokens live in `flutter_secure_storage`, never in preferences or logs.
- The passenger app browses anonymously; an account is needed only for tickets (REQ-ACC-001).
- Offline: cache the last good response with its fetch time, show the last-sync time, and mark data **stale** instead of hiding it. Public reads use the `ETag`.
- Driver app (REQ-DRV-003 to 009): every action is an event with device time, a sequence number, an idempotency key and the duty id; the queue is persisted, survives an app kill, shows a pending count, and replays in order. Free-text input is disabled while between stops. Never apply events from an ended duty silently.
- Never log tokens, personal data or driver location.

---

## 12. Differences from the Libra reference (what we do better)

| Libra | SmartRail |
|---|---|
| Repository catches every exception and returns `CacheFailure(e.toString())` | Catch `ApiException` only and map to typed failures; no raw error text to the UI |
| Controllers turn failures into `AsyncError(failure.message)` (a string) | Throw or store the `Failure` object so the UI can match on type |
| Strings written directly in widgets | All text in ARB files (`en`, `si`) |
| Theme and shared widgets inside the one app | Tokens and shared widgets in the `design_system` package, used by both apps |
| Feature-specific widget (`book_cover`) in `core/widgets` | `core/widgets` holds only truly app-wide widgets; feature widgets stay in the feature |
| Mock JSON datasource only | Remote datasource against the real API, optional local cache, JSON fake only before the API exists |
| Login-only redirect | Anonymous browsing allowed; redirect rules per spec |
| Single app | Two apps sharing packages in a pub workspace |
| Few tests | A test for every acceptance criterion |

---

## 13. Commands

Run from `apps/mobile` (the workspace root) unless noted.

```bash
flutter pub get                                   # resolves the whole workspace
cd apps/passenger && dart run build_runner build -d    # generate providers
cd apps/passenger && flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080
dart run melos run analyze                        # flutter analyze in every package
dart run melos run test                           # flutter test in every package
cd apps/passenger && flutter gen-l10n             # after editing ARB files
```

`10.0.2.2` is the Android emulator's address for the host machine; use the machine's LAN address on a real device.

---

## 14. Done checklist

- [ ] Spec approved; branch `feature/<feature-name>`; ACs listed in the spec
- [ ] Feature has `domain`, `data`, `presentation`; no inward dependency broken (section 3)
- [ ] Entities are plain; models extend them; datasource is an interface
- [ ] Repository catches `ApiException` only and returns typed `Failure`
- [ ] Providers wired; datasource and repository are `keepAlive`; generated files up to date
- [ ] Screen handles loading, empty, error, offline and stale; retry works
- [ ] All text is localized (`en`, `si`); no hex or magic numbers; tokens from the design system
- [ ] Route added in `app_router.dart`; confirmations are bottom sheets
- [ ] Matches the Figma frame; touch targets and text scale checked
- [ ] A test for every AC: use-case and repository unit tests, provider tests with overrides, a widget test per screen state
- [ ] `flutter analyze` and `flutter test` pass; spec verification table filled in

## 15. Keeping this skill current

When a pattern changes, a mistake is corrected or a new constraint is agreed, update this file in the same change, including every section that mentions the old pattern.
