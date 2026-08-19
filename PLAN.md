# MIC Risk — React Frontend Plan

> **Superseded for frontend work** by the design spec at
> `mic-risk-frontend/docs/specs/2026-08-18-mic-risk-frontend-design.md`, which reflects the
> agreed decisions (openapi-fetch, Arabic/RTL, dense visual register) and the inherent/residual
> risk model. This file remains accurate as a record of the backend.

Target API: `v1.json` (47 endpoints), regenerated from the running service after the auth and error-contract work described in [Backend Changelog](#backend-changelog-already-done) below.

---

## 1. What the API now gives you

Read this section before writing any client code. Everything here was verified against a running instance, not inferred from the document.

### Authentication

| Endpoint                            | Auth               | Returns                                        |
| ----------------------------------- | ------------------ | ---------------------------------------------- |
| `POST /api/account/login`           | anonymous          | `AuthResponseDto` + sets refresh cookie        |
| `POST /api/account/refresh`         | anonymous (cookie) | `AuthResponseDto` + rotates refresh cookie     |
| `POST /api/account/logout`          | anonymous (cookie) | `204`, revokes the token family, clears cookie |
| `GET /api/account/me`               | bearer             | `CurrentUserDto`                               |
| `POST /api/account/change-password` | bearer             | `AuthResponseDto` + a fresh cookie             |
| `POST /api/employee/{id}/reset-password` | bearer, Admin | `204`; sets a new password and ends that employee's sessions |

```jsonc
// AuthResponseDto — returned identically by login, refresh and change-password
{
  "accessToken": "eyJhbGciOi…",
  "accessTokenExpiresAt": "2026-08-18T08:08:19+00:00",
  "roles": ["Admin"],
  "employee": {
    // the caller's own profile
    "id": 1, // <- this is the empId every request body wants
    "identityUserId": "…",
    "email": "admin@mic.test",
    "name": "Test Admin",
    "department": { "id": 1, "name": "Risk", "branchLocation": "HQ" },
    "active": true,
    "createdAt": "2026-08-18T10:51:48+03:00",
  },
}
```

Key consequences for the client:

- **The refresh token is never in a response body.** It lives only in `mic_refresh_token`, an `HttpOnly; Secure; SameSite=Strict; Path=/api/account` cookie. JavaScript cannot read it and should not try. Every request must go out with `credentials: 'include'`.
- **The access token lives in memory only.** Never `localStorage`, never `sessionStorage`. On a page reload the session is restored by calling `/refresh`, not by reading storage.
- **Access tokens last 15 minutes; the refresh cookie lasts 60 days and slides.** An employee who opens the app at least once every 60 days is never asked to log in again. Expiry of the access token is not a logout — it is a silent refresh.
- **`employee.id` removes the old workaround.** `CreateRiskReportRequestDto.empId`, `RecordResourceEngagementRequestDto.empId` and `CreateResourceRequestDto.uploadedByEmpId` all require this value, and the server returns `403` when it does not match the caller. There is no longer any reason to enumerate `GET /api/employee` to find out who you are.
- **Roles are exactly `Admin` and `User`.** Treat anything that is not `Admin` as a plain employee.
- **`change-password` returns `200` with a new `AuthResponseDto`**, not `204`. It signs out every other device and re-establishes the current one, so update the in-memory token from the response.

### Errors — one shape, everywhere

Every non-2xx response is RFC 9457 `application/problem+json`. This was verified for the MVC path, the `Forbid()` path, the JWT challenge path, and the exception middleware.

```jsonc
// 401 / 403 / 404 / 400-from-exception
{ "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Not Found", "status": 404,
  "detail": "Employee with ID 99999 was not found.",
  "instance": "/api/employee/99999", "traceId": "00-2527…" }

// 400 from model validation — same envelope plus `errors`
{ "errors": { "NewPassword": ["…minimum length of '8'."] },
  "type": "…#section-15.5.1", "title": "One or more validation errors occurred.",
  "status": 400, "traceId": "00-c83f…" }
```

So the error normalizer needs exactly two branches: with `errors` (field-level validation) and without (a message in `detail`). `instance` and `traceId` are optional — do not require them.

**Surface `detail` to users, never `traceId` or `instance`.** Log `traceId` for support.

### Numbers arrive as `number | string`

OpenAPI 3.1 emission from .NET renders every `int32`, `int64` and `double` as `"type": ["integer","string"]`. Orval will generate `number | string` for **every** id, count, page number, risk score and percentage in the API. Coerce at the boundary with `z.coerce.number()`; no component should ever see the union.

### Domain values the document types only as `string`

These are enforced server-side but declared as bare `string`, so the client Zod schemas must carry them:

- **Risk report status:** `Submitted` | `InReview` | `Resolved`. Legal transitions — `Submitted → InReview | Resolved`; `InReview → Submitted | Resolved`; `Resolved → InReview`. Same-to-same is a no-op. The triage UI must offer only legal targets; anything else comes back `400`.
- **Risk action status:** `Pending` | `Completed`. Setting `Completed` stamps `completedAt` server-side.
- **`severity`, `frequency`, `controlEffectiveness`, `priority`:** integers 1–5, enforced by a validator and a database check constraint. `controlEffectiveness` runs **1 = very strong to 5 = very weak**.
- **`inherentRisk`** is a stored computed column (`severity × frequency`, 1–25) — read-only.
- **`residualRisk`** is a stored computed column, `inherentRisk × controlEffectiveness`, integer, 1–125 — read-only. Because the rating runs 1 = strong to 5 = weak it multiplies exposure rather than discounting it, so residual is always **≥** inherent. Bands (both scores): Low ≤ 5, Moderate ≤ 10, High ≤ 15, Critical > 15. A rating ≥ 4 counts as a weak control.
- **Risk category:** `Financial` | `Operational` | `Strategic` | `Insurance` (DB check constraint).
- **Resource `type`:** the DB constraint allows `Video` | `Image` | `File` | `Quiz` | `Link`, but the upload endpoint only ever produces `Image` or `File` (derived from the extension).

### Endpoint shapes that will bite you

- `GET /api/risk-report/mine` returns a **plain array**. `GET /api/risk-report` returns a **paged envelope**. Two different view models.
- `GET /api/risk-report/{id}/history` **is paged** (`PagedResultDtoOfRiskReportStatusHistoryResponseDto`).
- `POST /api/resource-engagement` is an **upsert returning 200**, not a create returning 201.
- `PATCH /api/employee/{id}/toggle-active` returns **204 with no body** — an optimistic toggle cannot reconcile from a response, so it must invalidate instead.
- `GET /api/risk-subcategory/by-category/{category}` returns **404 when the result is empty**, not `200 []`. Treat that 404 as "empty", not as an error. There is no list-all endpoint — use `GET /api/risk-subcategory/categories` (categories with nested subcategories) for pickers.
- `DELETE /api/risk-subcategory/{id}` is a **soft delete** (`Active = false`) with no undelete.
- There are **no delete endpoints** for employees or departments. Do not build the affordance.
- **There is no quiz feature.** `ResourceEngagement` has only `viewed` and `surveyCompleted` booleans. The analytics field named `employeesWithQuizCompletion` is computed from `surveyCompleted`. Build a mark-viewed / mark-survey-complete UI, not a quiz engine.
- The whole `RiskAction` controller is **Admin-only**, including `GET /api/risk-action/by-report/{reportId}`. An employee cannot see corrective actions on their own report. See [Open questions](#7-open-questions).
- `GET /api/employee` and `GET /api/department` are `[Authorize]` only — any employee can list them. Mutations on both are Admin.
- Paging: `page` defaults to 1, `pageSize` to 20, **max 100** (silently clamped server-side).
- `GET /api/analytics/dashboard` takes optional `from` / `to` date-time query params.
- `POST /api/resource/upload` is multipart with fields `file`, `name`, `description`. Limit is **10 MB**. Extensions: images `.png .jpg .jpeg .gif .webp`; files `.pdf .doc .docx .xls .xlsx .ppt .pptx .txt .csv .mp4 .mp3 .av1 .m4a`. The generated body type is an `allOf` of three anonymous objects, which makes every field optional — write this one call by hand against the mutator and pin it with a test.

---

## 2. Stack and project setup

- Vite + React + TypeScript, Tailwind CSS, shadcn/ui, React Router, TanStack Query, React Hook Form, Zod, `@hookform/resolvers`.
- `@` path alias to `src`.
- Copy `v1.json` into the frontend repo as the versioned generation source. Re-copy and regenerate whenever the backend contract changes; never hand-edit generated files.
- **Vite dev proxy** — `/api` → `http://localhost:5166`. This makes dev same-origin, which:
  - removes CORS from the dev loop entirely, and
  - lets `SameSite=Strict` on the refresh cookie work in development exactly as in production.

  The backend does also allow credentialed CORS from `http://localhost:5173` if you would rather not proxy, but the proxy is the simpler path and matches production behaviour.

- `.env.example` documents `VITE_API_BASE_URL` only. No keys, no hardcoded URLs in source.

## 3. The API layer

Four layers, each with one job:

**a. Orval generation** → `src/api/generated/{types,endpoints}.ts`. Owned by the generator. Configured with a custom mutator so no UI code ever calls `fetch` directly.

**b. The mutator** (`src/api/client.ts`) — the only place that knows about transport:

- reads `VITE_API_BASE_URL`
- attaches the in-memory access token
- sets `credentials: 'include'` on every request
- forwards TanStack Query's `AbortSignal`
- normalizes the two ProblemDetails branches into one typed `ApiError` (`status`, `title`, `detail`, `fieldErrors?`, `traceId?`)
- runs the 401 → refresh → replay-once flow described below
- parses responses against endpoint Zod schemas before returning

**c. Mappers** (`src/api/mappers/`) — generated types → domain models. Coerces the `number | string` unions, parses `date-time` strings to `Date`, narrows the status strings to unions, normalizes nullables. Pure functions, unit-tested.

**d. Domain hooks** (`src/features/*/hooks/`) — `useRiskReports`, `useReportWorkflow`, `useResources`, `useResourceEngagement`, `useEmployees`, `useRiskActions`, `useAnalytics`. Pages receive typed view models, command callbacks, and explicit `idle | loading | success | error` state. Nothing else.

`QueryClient` defaults: retry transient failures only; never retry 401, 403, 404 or validation errors; stable query-key factories per feature; narrow invalidation after mutations.

## 4. Session handling — the part that needs care

```
app start ──► POST /refresh ──► 200: token in memory, render app
                           └──► 401: render login
```

**On a 401 from any request:**

1. Queue behind a single shared refresh promise. Do **not** let concurrent 401s each fire their own `/refresh` — that is a token-reuse storm.
2. If the refresh succeeds, replay the original request **once** with the new token.
3. If it fails, clear session state and route to login.

**Serialize across tabs, not just within one.** Each tab has its own in-memory access token but they share one cookie. Two tabs refreshing simultaneously both present the same token. The backend tolerates this — it has a 30-second reuse-leeway window that treats a just-rotated token as an honest concurrent refresh rather than theft — but outside that window a genuine replay revokes the entire family and signs the user out everywhere. Use a `BroadcastChannel` (or a `localStorage` mutex) so one tab performs the refresh and the others wait for its result.

**Optional but nice:** schedule a proactive refresh ~60 seconds before `accessTokenExpiresAt` so a user mid-form never sees a request fail and retry.

**Never** implement a refresh retry loop. One attempt, then log out.

**On logout:** call `POST /api/account/logout` (revokes the family server-side), then clear the in-memory token and the query cache. Clearing local state alone leaves a live session on the server.

## 5. Routes

Shell with responsive navigation. Role checks gate navigation only — the backend is the authority.

**Employee** — login, dashboard, submit risk report, my reports, report detail, report status history, learning resources, my engagement, change password.

**Admin** — analytics dashboard, all reports (status filter + paging), report detail & triage, auditor evaluation, status history, corrective actions, resource management & upload, employee management, department management, risk taxonomy management, engagement analytics.

## 6. UI conventions

- Every list, table, detail, card grid and form gets a **layout-equivalent** shadcn `Skeleton` — matching the real layout's dimensions, not a generic spinner.
- Render empty, loading, forbidden, not-found, validation-error and retry states **distinctly**. A 403 is not an error state; it is a "you don't have access to this" state.
- Route-level error boundaries around authenticated sections; focused boundaries around analytics and report detail. Recovery actions must never leak transport or server internals.
- Forms use React Hook Form + Zod schemas derived from §1. Map `errors` from a 400 onto the matching fields; treat server validation as confirmation, never as a substitute for client-side validation.
- **Optimistic updates** only where the action is reversible and locally deterministic: report status changes (legal transitions only), engagement toggles, resource patches, employee active toggles, risk-action status. Snapshot → update → roll back on failure → reconcile with the validated response **where one exists** (remember `toggle-active` returns no body) → invalidate dependent dashboard queries.
- **Non-optimistic** for employee creation, report creation, uploads, auditor evaluations and deletes. Disable duplicate submission; show progress and errors.
- Debounce search and filter inputs; include filters and paging in query keys; keep previous page data visible while the next loads; clamp `pageSize` to 100.
- Uploads use `FormData` with a client-side preflight against the 10 MB / extension rules, show progress and cancellation, and never set a manual multipart `Content-Type`.

## 7. Test plan

- **Unit** — Zod parsers, `number | string` coercion, date mapping, error normalization across both ProblemDetails branches, query-key factories, the status-transition guard.
- **Integration (MSW)** — malformed payload rejection, abort propagation, 400/401/403/404/500, the 404-means-empty subcategory case, and — most importantly — **one serialized refresh under concurrent 401s**, replay-once, and give-up-and-log-out.
- **Component** — the three UI states, error-boundary recovery, role-based navigation, validation feedback, optimistic success and rollback, the no-response-body toggle path, pagination, duplicate-submit prevention.
- **E2E** — employee submit-a-report journey; admin triage journey; and a session-persistence test that reloads the page and confirms the user stays signed in.
- Type check, lint, and production build in CI.

---

## Backend changelog (already done)

Implemented, built, and verified end-to-end against a real SQL Server instance.

**Refresh-token sessions**

- `RefreshToken` entity + `AddRefreshTokens` migration (table, FK to `AspNetUsers`, unique index on the hash, indexes on family and user). Only SHA-256 hashes are stored.
- Rotation on every refresh, with token families and **reuse detection** — replaying a spent token revokes the whole chain. A 30-second leeway window absorbs honest multi-tab races.
- Access tokens cut from **7 days to 15 minutes**; `ClockSkew` reduced from the 5-minute default to 30 seconds so a short token stays short.
- Refresh families are revoked on logout, on password change, and on employee deactivation (both `toggle-active` and `PUT /api/employee/{id}` with `active: false`).
- `/refresh` re-checks that the employee is still active, so a deactivated account cannot renew in the background.
- The per-request active-employee check in the JWT pipeline was **kept** — it is what makes deactivation take effect on the next request rather than up to 15 minutes later.

**Contract fixes**

- Login/refresh/change-password now return `employeeId`, `roles` and the token expiry. `NewUserDto` is gone; `AuthResponseDto` and `CurrentUserDto` replace it.
- Added `GET /api/account/me`.
- Every error is now real `application/problem+json`. Previously the document advertised `ProblemDetails` while the runtime returned `{"Message": …}`, and `Forbid()` / JWT challenges returned **empty bodies**. All three paths were fixed.
- CORS gained `.AllowCredentials()` — without it the refresh cookie could never be set or sent.

**Verified** `dotnet build` clean; `migrations has-pending-model-changes` reports none; migration applies to a fresh database; login → rotate → multi-tab leeway → strict reuse detection → family revocation → logout → deactivation lockout → change-password device revocation all confirmed against a live instance, along with the 401/403/404/400 body shapes and the credentialed CORS preflight.

**Not done, deliberately**

- `appsettings.json` still contains a live connection string and `appsettings.Development.json` a hard-coded JWT signing key. Move both to user-secrets or environment variables before this is shared. Out of scope for this change, but it should be next.

## 8. Open questions

1. Should employees see corrective actions on their own reports? Today the whole `RiskAction` controller is Admin-only. Relaxing just `by-report/{reportId}` to the existing `EditOrViewRiskReport` policy would do it. answer: no
2. Should `priority` be bounded server-side, and to what range? answer:very low, low, moderate, high, very high or (1-5)
3. Is 60 days the right idle window? An absolute cap is implemented but disabled (`JWT:RefreshTokenAbsoluteDays = 0`); set it to e.g. 180 if compliance wants a hard re-login. answer: yes
