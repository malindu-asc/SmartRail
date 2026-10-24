---
name: smartrail-frontend
description: How to build and change the SmartRail web consoles (React, TypeScript, Tailwind, TanStack Query). Use when adding or editing anything under apps/web, including pages, features, shared UI, API calls, auth and role checks.
---

# SmartRail frontend skill

Distilled from two reference projects: `intern-training-v3` (API layer, TanStack Query, query-key factories, mutations) and `deskline` (folder structure, container/presentational split, URL state, design tokens, tests). Read this before writing frontend code. Backend contract: `docs/guides/ops-admin-console-api.md`.

## 1. Stack

React 19, TypeScript (strict), Vite, Tailwind CSS v4 with CSS-variable theme tokens, `class-variance-authority` + `clsx` for variants, `lucide-react` icons, `react-router-dom` v7, `@tanstack/react-query` v5, `react-hot-toast`, `keycloak-js` (PKCE), Vitest + Testing Library, Oxlint.

## 2. Folder structure (one question per folder)

```
src/
  main.tsx          bootstrap only
  App.tsx           composes providers and routes, nothing else
  app/              providers, queryClient, route table, navigation map
  layouts/          ConsoleLayout, Sidebar, TopBar (the shared shell)
  pages/            one component per route: decides what data the screen needs, hands it to features
  features/<name>/  api/ (calls), hooks/ (queries and mutations), components/ (dumb UI), types.ts, index.ts
  shared/
    api/            http client, ApiError, config
    auth/           Keycloak, session, permissions, <Can>, route guards
    components/ui/  design-system primitives (Button, Badge, Card, Modal, Skeleton, Table ...)
    hooks/  utils/  types/
  styles/           tokens and globals
```

- A feature never imports another feature's internals; use its `index.ts` barrel. Shared code never imports a feature.
- Generic UI knows nothing about trains, notices or users. If a component says "journey", it belongs in a feature.
- Named exports; each folder has an `index.ts` barrel that is its public surface.

## 3. Data and API

- **Components never call `fetch`.** All traffic goes through `shared/api/httpClient.ts` (typed `get/post/put/patch/delete`, bearer token added, JSON in and out).
- Failures throw `ApiError { status, code, message }` built from the backend's problem details (`code` is stable, e.g. `Journeys.VersionConflict`). Map codes to friendly text in one place (`shared/api/errors.ts`).
- Server state lives in TanStack Query only. No `useEffect` + `useState` fetching.
  - Each feature has a **query-key factory** (`journeyKeys.list(filters)`) and `queryOptions` helpers.
  - Hooks are thin: `useBoard(filters)`, `useCancelJourney()`. Components call hooks, not the client.
  - A mutation invalidates the keys it changes and shows a toast. Live screens poll with `refetchInterval`.
- **URL is state** for anything a person would share or reload: filters, tabs, selected row (`useSearchParams`). Use component state only for transient UI (an open modal, a draft).
- Keep pure logic (filtering, formatting, mapping status to a pill) in plain functions with unit tests.

## 4. Auth and RBAC

- The backend is the authority: every endpoint checks one permission. The frontend mirrors it so people do not see buttons that will fail, and never as the only guard.
- Sign in with Keycloak (PKCE). The session comes from `GET /api/me`: `{ roles, permissions, consoles }`.
- Check **permissions, not roles**: `useCan(Permissions.JourneyCancel)`, `<Can permission=...>`, `<RequirePermission permission=...>` for routes. A role check is only for choosing a landing page.
- Navigation items declare the permission they need; the sidebar and the route table both read that one map, so a hidden item is also a blocked route (403 page).
- Hide actions the role can never do; disable with a reason when the role can, but the item's state does not allow it (use the journey's `actions[].allowed` and `whyNot`).
- The administrator holds no operational permission, so the admin console never shows operational actions. A station master sees only their stations.
- Test every guard: a permission present shows the control, absent hides it, a route redirects to 403.

## 5. UI rules

- **Theme tokens only.** Colours, radius, shadow, spacing and fonts come from CSS variables in `styles/index.css` (mapped into Tailwind with `@theme`). No hex values, no `bg-[#...]` in components. Light and dark themes both work.
- Status colours come from tokens (`on-time`, `delayed`, `cancelled`, `stale`, `changed`), through one `StatusPill` variant map.
- **Loading skeletons**, never a bare spinner for page data: a skeleton that has the shape of the content (`TableSkeleton`, `CardSkeleton`). Buttons show a pending state while a mutation runs.
- Every list has an **empty state** and every query an **error state** with Retry.
- **Toasts** (`react-hot-toast`) for outcomes: success after a mutation, a clear error on failure. Use the `notify` helper, one toast per action, text a controller can act on. Do not use toasts for form validation: show it next to the field.
- **Modals** for anything that needs a decision or a reason: confirm an irreversible action (cancel, retract, revoke), a short form (invite user, register device, change platform). Modals trap focus, close on Escape, return focus, and never nest. Large forms and detail are pages.
- Actions that need a reason have a required reason field and a clear primary button label (the verb, not "OK").
- Forms are controlled, validate on submit, show errors beside fields, disable submit while pending.
- Accessible: real buttons and links, labels for every field, `aria-*` on custom controls, visible focus, colour never the only signal (status pills have text).
- Times are shown in the network time zone (Asia/Colombo) with the helpers in `shared/utils/format.ts`; the API sends UTC.
- Plain text only for notice and message content. Never `dangerouslySetInnerHTML`.

## 6. Quality gates

Before a commit: `npm run lint`, `npm run typecheck`, `npm run test`, `npm run build` in `apps/web/console`.

- TypeScript strict, no `any`, no non-null `!` on data from the API; narrow it.
- Hooks follow the rules of hooks; effects are for synchronising with the outside world, not for deriving state.
- Keep components small. If a file passes about 200 lines or has two reasons to change, split it.
- Tests: unit tests for pure functions and guards; component tests for permission-dependent UI and for modals (open, validate, submit, close).
- Small conventional commits on a feature branch.

## 7. Adding a screen (checklist)

1. Types in `features/<name>/types.ts` copied from the guide's response shapes.
2. API function in `features/<name>/api/`, key factory and hooks in `hooks/`.
3. Dumb components in `components/` (props in, events out).
4. The page in `pages/` reads URL state, calls hooks, renders skeleton, error, empty and data states.
5. Add the route and the navigation entry with its permission in `app/`.
6. Mutations: confirm modal if destructive, toast on success and failure, invalidate keys.
7. Tests for guard, filters and the main interaction. Update the guide if the API changed.
