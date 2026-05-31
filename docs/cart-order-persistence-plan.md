# Plan: Persist Cart & Orders via the API (with email moved server-side)

## Goal

Move the WebApp shopping cart off the browser cookie and into the database **through the API**, record completed orders in new `Orders` / `OrderDetails` tables, move the order-confirmation email into the API's place-order method, require authentication on the Listing page, add an EF migration for the new (empty) tables, and add integration tests that verify the database rows and that an email was sent (verified via the MailPit REST API).

## Current state (for reference)

- **Cart** lives entirely in a browser cookie `carvedrock-cart` (JSON of `{id, quantity}`). Added client-side by `addItemToCart()` JS in `CarvedRock.WebApp/Pages/Listing.cshtml`. Read/rebuilt in `Cart.cshtml.cs` and `Checkout.cshtml.cs`.
- **Order placement** happens entirely in the WebApp: `Checkout.cshtml.cs` → `OnPostSubmitOrder()` rebuilds the cart from the API's product list, builds an HTML email from `emailTemplate.html`, calls `IEmailSender.SendEmailAsync(...)` (`CarvedRock.WebApp/EmailService.cs` → `MailKit.Client`), deletes the cookie, redirects to `/ThankYou`. **No DB record is created.**
- **Email** uses the Aspire MailKit client (`builder.AddMailKitClient("smtp")` in WebApp `Program.cs`); the `smtp` (MailPit) resource is referenced by `webapp` in `AppHost.cs`. MailPit also exposes an `http` endpoint ("Email Inbox") whose REST API we'll use in tests.
- **Listing page** is `[AllowAnonymous]`; the WebApp otherwise globally `RequireAuthorization()`s all Razor Pages.
- **API** is layered: `Api` (controllers) → `Domain` (`ProductLogic`, FluentValidation validators, Riok.Mapperly mappers) → `Data` (`LocalContext`, `CarvedRockRepository`, `Entities/`, `Migrations/`) → `Core` (record DTOs). Endpoints authenticate JWTs; `[Authorize(Roles="admin")]` guards mutations; `admin` is granted at runtime by `AdminClaimsTransformation` (email starts with `bobsmith`, or client `m2m.short`).
- **Tests** (`tests/CarvedRock.Tests`) boot the whole AppHost once via `AppFixture` (`[Collection("Integration test collection")]`). API tests use `fixture.App.CreateHttpClient("api")`; **`WebAppTests` already uses Playwright** (`PageTest` base, `Page.GotoAsync(...)`, role-based locators) and includes an `AddToCartWorks` test (anonymous today) and a `CanLoginAsAdminAndGoToAdminPage` test that drives the Duende demo login UI (Sign in → Username/Password → Login). They currently verify **only via HTTP/UI**, never touching the DB directly.

## Key design decisions

1. **User identity for the cart is derived server-side from the JWT, never from the client.** The API resolves the caller's user id from the `sub` claim (present on the real user OIDC token a logged-in `alice` carries), falling back to `client_id` for any machine-to-machine callers. Cart and Order rows are keyed/indexed on this `UserId` string.
2. **Cart endpoints require authentication but NOT admin** (any logged-in user manages their own cart). Only product mutations remain admin-only. This means the Listing page's "add to cart" works for ordinary users like `alice`, unlike product create/edit.
3. **The place-order endpoint owns the email.** `OrderLogic` (Domain) orchestrates: read the user's cart rows, create `Order` + `OrderDetails`, clear the cart, then send the confirmation email via an `IOrderEmailSender` abstraction. The MailKit implementation + `emailTemplate.html` move into the **API** project.
4. **Customer email comes from the token's `email` claim.** Because tests now log in as a real user (`alice`) through the browser, the API always has an `email` claim, so `POST /order` needs **no cart or email in the request body** — the cart is read server-side by `UserId` and the email from the claim. (The WebApp still knows alice's email from OIDC; it just doesn't need to send it.)
5. **New tables start empty.** `MigrateAndCreateData()` continues to reseed **Products only**; carts/orders are never seeded or wiped by it.
6. **Tests are end-to-end via Playwright**, driving the real WebApp UI as a logged-in non-admin user (`alice` / `alice`, the public Duende demo credentials). They then assert side effects out-of-band: cart/order rows by constructing a `LocalContext` against the running Postgres resource's connection string (`fixture.App.GetConnectionString("CarvedRockPostgres")`), and the confirmation email via MailPit's REST API at the `smtp` resource's `http` endpoint.

---

## Phase 1 — Data layer (`CarvedRock.Data`)

New entities in `CarvedRock.Data/Entities/` (POCOs, matching the existing `Product.cs` style):

- **`CartItem.cs`** — `Id` (int, PK), `UserId` (string, indexed), `ProductId` (int), `Quantity` (int). *(Note: distinct from the WebApp's `CartItem` record; this is the persistence entity.)*
- **`Order.cs`** — `Id` (int, PK), `UserId` (string, indexed), `Email` (string), `OrderDate` (DateTime, UTC), `Total` (double), and `ICollection<OrderDetail> Details`.
- **`OrderDetail.cs`** — `Id` (int, PK), `OrderId` (int, FK → `Order`), `ProductId` (int), `ProductName` (string), `Quantity` (int), `UnitPrice` (double), `LineTotal` (double).

`LocalContext.cs`:
- Add `DbSet<CartItem> CartItems`, `DbSet<Order> Orders`, `DbSet<OrderDetail> OrderDetails`.
- In `OnModelCreating`: index `CartItem.UserId`, a **unique** index on `(UserId, ProductId)` (one row per product per user → enables upsert), index `Order.UserId`, and the `Order`→`OrderDetail` one-to-many with cascade delete.
- Leave `MigrateAndCreateData()` seeding **Products only** (no change to seed behavior).

`ICarvedRockRepository` / `CarvedRockRepository`:
- Cart: `GetCartItemsAsync(userId)`, `AddOrIncrementCartItemAsync(userId, productId, qty)` (upsert on the unique index), `UpdateCartItemQuantityAsync(...)`, `RemoveCartItemAsync(userId, productId)`, `ClearCartAsync(userId)`.
- Orders: `CreateOrderAsync(Order order)`, optionally `GetOrdersForUserAsync(userId)`.

## Phase 2 — Migration

From `CarvedRock.Data` with the API as startup project (per CLAUDE.md):

```bash
dotnet ef migrations add AddCartAndOrders -s ../CarvedRock.Api
```

Verify the generated `Up()` creates `CartItems`, `Orders`, `OrderDetails` with the indexes/FK above and that `Down()` drops them. Tables start empty.

## Phase 3 — Core DTOs (`CarvedRock.Core`)

Add record models mirroring the existing `ProductModel` style:
- `CartItemModel` — `ProductId`, `Quantity`, `Name`, `Category`, `Price`, `Total` (the API enriches name/price from the product so the client can't set price).
- `AddToCartModel` — `ProductId`, `Quantity`.
- `OrderModel` / `OrderDetailModel` — returned after placing an order. (No `NewOrderModel`/request body is needed: cart contents come from the server-side cart and the email from the `email` claim.)

## Phase 4 — Domain (`CarvedRock.Domain`)

- **`ICartLogic` / `CartLogic`**: get/add/update/remove/clear, enriching items with current product name/price/total via the repository (same pattern `Checkout.cshtml.cs` uses today, but server-side). Validate product existence and `Quantity > 0` (FluentValidation `AddToCartValidator`, auto-discovered like `NewProductValidator`).
- **`IOrderLogic` / `OrderLogic`**: `PlaceOrderAsync(userId, email)` (both resolved by the controller from the JWT claims) →
  1. read the user's cart rows (throw/return problem if empty),
  2. build `Order` + `OrderDetails` (snapshotting name/price/line total),
  3. `CreateOrderAsync`,
  4. `ClearCartAsync(userId)`,
  5. `await emailSender.SendOrderConfirmationAsync(order)`,
  6. return `OrderModel`.
- **`IOrderEmailSender`** interface defined here (keeps Domain free of MailKit): `Task SendOrderConfirmationAsync(Order order)`. Implemented in the API.
- Mappers in `CarvedRock.Domain/Mapping/` (Mapperly) for `CartItem`/`Order`/`OrderDetail` ↔ models.

## Phase 5 — API (`CarvedRock.Api`)

- **`CartController`** (`[Authorize]`, no role): `GET /cart`, `POST /cart` (`AddToCartModel`), `PUT /cart/{productId}`, `DELETE /cart/{productId}`, `DELETE /cart`. Resolve `userId` from `sub` ?? `client_id` via a small helper (e.g. extension on `ClaimsPrincipal`).
- **`OrderController`** (`[Authorize]`): `POST /order` (no body) → resolve `userId` (`sub` ?? `client_id`) and `email` (`email` claim) from the JWT, call `OrderLogic.PlaceOrderAsync`, return `201 Created` with `OrderModel`. Optionally `GET /order` for the user's order history.
- **Move email into the API**:
  - Add `MailKit.Client` project reference to `CarvedRock.Api`.
  - Add `builder.AddMailKitClient("smtp")` in API `Program.cs`.
  - Create `CarvedRock.Api/EmailService.cs` implementing `IOrderEmailSender` using `MailKitClientFactory` (port the logic from the WebApp's `EmailService`/`Checkout`: load `emailTemplate.html`, fill `{{NarrativeContent}}`, `{{ProductRows}}`, `{{AdditionalNotes}}`, send via SMTP).
  - Move `emailTemplate.html` into `CarvedRock.Api` with `<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>`.
  - Register `ICartLogic`, `IOrderLogic`, `IOrderEmailSender` in API DI (validators auto-discovered).

## Phase 6 — AppHost (`CarvedRock.AppHost/AppHost.cs`)

- Add `.WithReference(smtp).WaitFor(smtp)` to the **`api`** resource (so the API gets the MailPit connection string).
- Remove `.WithReference(smtp)` from `webapp` once the WebApp no longer sends email (optional cleanup; harmless to leave).

## Phase 7 — WebApp (`CarvedRock.WebApp`)

- **New `CartService`** (typed `HttpClient`, like `ProductService`, forwarding the bearer token via the existing `SetAuthorizationHeader` pattern): wraps `GET/POST/PUT/DELETE /cart` and `POST /order`.
- **Listing page**: remove `[AllowAnonymous]` from `Listing.cshtml`/`.cshtml.cs` so it inherits the global `RequireAuthorization()`. Change `addItemToCart()` from writing the cookie to **POSTing to a page handler** (e.g. `OnPostAddToCart(productId)`) that calls `CartService` → API (keeps anti-forgery + bearer-token forwarding server-side). Update the cart badge/count to come from the API.
- **`Cart.cshtml.cs`**: `OnGetAsync` reads the cart from `CartService` (API) instead of the cookie; `OnPostCancelOrder` calls `DELETE /cart`.
- **`Checkout.cshtml.cs`**: `OnGetAsync` reads cart from the API; `OnPostSubmitOrder` becomes a thin call to `CartService.PlaceOrderAsync()` (POST `/order`, no body) — **delete the template-building, email-sending, cookie, and claim-extraction code** (the API now derives email from the token). Redirect to `/ThankYou` on success.
- Remove the WebApp's `EmailService.cs` / `IEmailSender` registration and the `AddMailKitClient("smtp")` line (now unused). Remove `emailTemplate.html` from the WebApp project.
- Remove cookie read/write code paths for the cart.

## Phase 8 — Tests (`tests/CarvedRock.Tests`) — Playwright, logged in as `alice`

All new tests are **Playwright UI tests** following the existing `WebAppTests` pattern (`PageTest` base, `[Collection("Integration test collection")]`, `fixture.App.GetEndpoint("webapp", "https")`, role-based locators). They drive the real browser as a logged-in **non-admin** user, then assert side effects out-of-band against the DB and MailPit.

**Credentials:** use the public Duende demo non-admin user — username `alice`, password `alice`. Add a small login helper (mirroring `CanLoginAsAdminAndGoToAdminPage`, but with `alice`/`alice` literals instead of the `bob` admin parameters):

```
Sign in → fill Username "alice" → Tab → fill Password "alice" → click Login
```

Consider extracting a `LoginAsync(Page, username, password)` helper so both the existing admin test and the new alice tests share it.

**Update the existing `AddToCartWorks` test:** the Listing page now requires authentication, so the current anonymous flow will redirect to login and fail. Add a `LoginAsync(Page, "alice", "alice")` call before navigating to the category/adding to cart.

**Test utilities to add:**
- **Direct DB access helper** — build a `LocalContext` from `fixture.App.GetConnectionString("CarvedRockPostgres")` (`new DbContextOptionsBuilder<LocalContext>().UseNpgsql(cstr).Options`) to query `CartItems` / `Orders` / `OrderDetails`. Add a `CarvedRock.Data` project reference to the test project.
- **MailPit REST helper** — `fixture.App.GetEndpoint("smtp", "http")` as base address; `DELETE /api/v1/messages` to clear the inbox before a test and `GET /api/v1/messages` (or `GET /api/v1/search?query=...`) to read sent mail. (Confirm exact MailPit API paths against the running container.)

**Tests:**
1. **Add to cart persists DB rows** — `LoginAsync("alice","alice")` → navigate to a category (e.g. *Footwear*) → click the product's add-to-cart button (e.g. *Desert Walker*) → assert the cart badge shows `Cart (1)` (UI), **and** query `CartItems` via `LocalContext` and assert a row exists for the added `ProductId` with the expected `Quantity`.
2. **Completing an order writes Order + OrderDetails, clears cart, and sends email** — `LoginAsync("alice","alice")` → add item(s) → go to Cart → Checkout → Submit Order → assert landing on `/ThankYou`. Then assert out-of-band: an `Orders` row exists with alice's email plus matching `OrderDetails` rows; alice's `CartItems` are now empty; and MailPit shows one message to alice's email with subject `"Your CarvedRock Order"` (clear the inbox at the start of the test to avoid cross-test bleed).
3. (Optional) **Empty cart** → checkout is blocked / no order or email is created.

**Resolving "alice's rows" in the DB:** alice's `UserId` is her token `sub`, which the test doesn't know directly. Read it back from the side effect instead — query the most recent `Orders` row by `Email` (alice's demo email) and use its `UserId`/`Id` to fetch `OrderDetails` and confirm the cart is empty. For test 1 (cart only, no order yet), assert on the `CartItems` row matching the added `ProductId`/`Quantity`. Because the AppHost is shared across the collection, start each test by calling the `/internal/reset-data` command (Products) and clearing alice's cart/orders (or the MailPit inbox) so prior tests don't bleed in.

Note: these are real integration tests — they need Docker and Playwright browsers installed (`playwright.ps1 install` from the test build output, per CLAUDE.md).

## Phase 9 — Build, migrate, verify

```bash
dotnet build cloud-ready-with-aspire.slnx
# Playwright browsers (once), from the test project's build output:
#   playwright.ps1 install
dotnet test --filter "FullyQualifiedName~WebAppTests"
```

Then manually run via `aspire run` / F5: log in as `alice` (non-admin) to confirm Listing now requires login and add-to-cart works for ordinary users; complete an order and confirm the order rows in Postgres and the email in the MailPit "Email Inbox".

## Documentation

Update `readme.md` to extend the narrative (cart/order persistence + server-side email) and `CLAUDE.md`'s architecture/data-flow notes (new tables, cart/order endpoints, email moved to API, Listing now authenticated).

## Open questions / confirm before/while implementing

- **MailPit REST paths**: confirm `GET /api/v1/messages` / `DELETE /api/v1/messages` against the actual MailPit image used by `AddMailPit`.
- **User id claim**: confirm `sub` is present on alice's OIDC token in the WebApp → API path (it should be); `client_id` fallback covers any m2m callers.
- **alice's demo email**: confirm the exact email address the Duende demo issues for `alice` so the DB/MailPit queries can match on it (e.g. `AliceSmith@email.com`).
- **Cart isolation in tests**: since one AppHost is shared, decide how to reset cart/order rows and the MailPit inbox between tests — `/internal/reset-data` only touches Products, so cart/order cleanup needs the direct `LocalContext` (or a dedicated test reset) plus a `DELETE /api/v1/messages` for email.
- **Playwright add-to-cart UX**: if Phase 7 keeps add-to-cart as a server `OnPost` round-trip vs. a client `fetch`, the Playwright locator/assertion for the cart badge may need adjusting (the existing `#carvedrockcart` "Cart (n)" assertion should still hold).
