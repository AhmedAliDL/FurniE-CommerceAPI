# Furni E-Commerce API

**A complete, production-style e-commerce backend for a furniture store** — catalog, cart, checkout, orders, Paymob payments, cancellations, returns, support chat, notifications, and an admin control plane — built with **.NET 8** and **Clean Architecture**.

| Resource | Link |
| --- | --- |
| Live API | <https://furniapi.runasp.net> |
| Swagger UI | <https://furniapi.runasp.net/swagger> |
| Test suite | 706 NUnit tests, all passing |
| Deployed endpoints | 97 routes, all verified against the live API |

> The API is JWT-secured, validates every request with FluentValidation, records payment webhooks idempotently, and runs scheduled maintenance/reporting jobs with Hangfire.

---

## Table of contents

1. [What this project is](#1-what-this-project-is)
2. [Feature tour](#2-feature-tour)
3. [Technology stack](#3-technology-stack)
4. [Solution architecture](#4-solution-architecture)
5. [Data model](#5-data-model)
6. [Core business flows](#6-core-business-flows)
7. [API reference](#7-api-reference)
8. [Configuration](#8-configuration)
9. [Running locally](#9-running-locally)
10. [Testing](#10-testing)
11. [Background jobs](#11-background-jobs)
12. [Deployment](#12-deployment)
13. [Operational notes and known limitations](#13-operational-notes-and-known-limitations)
14. [Contributing](#14-contributing)

---

## 1. What this project is

Furni is a REST API for an online furniture store. It serves three kinds of users:

| Actor | What they can do |
| --- | --- |
| **Customer** | Browse and search the catalog, manage a cart and wishlist, place orders, pay online, track orders, request cancellations or returns, review products, and chat with support. |
| **Support agent** | See every support ticket, reply in real time, close/reopen conversations, and view order details. |
| **Admin** | Manage the catalog, images, categories, coupons, users and roles; drive the order state machine; approve cancellations and returns; issue Paymob refunds and voids; inspect transactions and audit logs; monitor Hangfire jobs. |

The codebase follows **Clean Architecture** with **CQRS**: controllers are thin, every use case is a MediatR command or query, business rules live in the Application layer, and infrastructure details (SQL Server, Redis, Cloudinary, Paymob, SMTP, SignalR, Hangfire) are isolated behind interfaces.

---

## 2. Feature tour

### Storefront and catalog

- Paginated product listing, search, filtering and sorting (`PriceAscending`, `PriceDescending`, `Newest`, `HighestRated`, `Popular`).
- Product details, active products, browse-by-category, and stock-aware availability.
- Multiple images per product with a primary image, custom order, Cloudinary storage, and per-upload limits (5 MB, jpg/jpeg/png/webp, up to 10 files).
- Categories with admin CRUD.
- Redis-backed recently-viewed history (capped, expiring).
- Product reviews (one per customer per product) and rating aggregation.

### Cart, wishlist and checkout

- Per-user shopping cart with quantity updates, item removal, and clear-all.
- Wishlist with idempotent add, remove, and move-to-cart.
- Server-side checkout pricing: the client never decides totals. Coupon percentage, per-city shipping rate, and stock checks are resolved on the server.
- `Idempotency-Key` support on checkout so a retried request returns the original order instead of creating a duplicate.
- Stock is decremented at placement; concurrent stock changes are detected with `RowVersion` and reported as a friendly error.

### Orders and post-purchase workflows

- Customer order list and full order details (including `OrderItemId`, needed for returns).
- Admin-driven status state machine with validated transitions.
- Direct cancellation for unpaid orders (owner only).
- Cancellation requests for paid, pre-shipment orders; approval voids the Paymob transaction, restores stock, and notifies the customer.
- Return requests after delivery (14-day window) with per-item quantities and reason categories.
- Return workflow: accept (assign courier) → receive (inspection) → process refund. Shipping fees are refundable only for merchant defects.

### Payments (Paymob)

- Payment sessions created through Paymob's Intention API; checkout URL built from the public key and client secret.
- HMAC-SHA512-verified, idempotent callback endpoint that persists every transaction and updates local payment state.
- Admin transaction listing — global (`/transactions/all`) or filtered by order.
- Partial and cumulative refunds (delivered orders, 14-day window).
- Voids (within 24 hours of payment, subject to Paymob settlement rules).
- Idempotency records prevent duplicate payment sessions for the same order and key.

### Support

- Support tickets with statuses (`Open`, `InProgress`, `WaitingForCustomer`, `Resolved`, `Closed`), priorities, and staff-only queues.
- Real-time chat over SignalR (`SupportHub`) with ticket-level authorization, read receipts, close/reopen, and persisted messages.
- Notifications persisted to the database and pushed to the user's `NotificationHub` connection as `newMessage`.

### Admin and operations

- User administration (paginated search, activate/deactivate; cannot deactivate yourself).
- Role administration (create, assign, unassign, delete; built-in roles protected).
- Coupon management with server-side validation (expiry, percentage).
- Audit log viewer (currently records `Notification` changes through `IAuditLog`).
- Contact form that sends branded HTML email.
- Hangfire dashboard at `/hangfire` for recurring jobs and their history.

### Platform

- JWT access tokens + rotating refresh tokens stored in an `HttpOnly`, `Secure` cookie.
- Role-based authorization (`Admin`, `SupportAgent`, `Customer`) plus ownership checks in handlers.
- Global rate limiter: 100 requests per minute per user/anonymous partition.
- RFC 7807 `ProblemDetails` error responses with an `errors` array.
- Soft delete with global query filters; deleted rows disappear from every repository.
- Branded HTML email for every outgoing message.
- Demo data seeder for local/staging environments.

---

## 3. Technology stack

| Concern | Technology | Version |
| --- | --- | --- |
| Runtime | .NET | 8.0 |
| Web framework | ASP.NET Core | 8.0 |
| CQRS / mediator | MediatR | 14.2.0 |
| Validation | FluentValidation (+ DI extensions) | 12.1.1 |
| ORM | Entity Framework Core | 8.0.32 |
| Database | SQL Server | — |
| Identity | ASP.NET Core Identity | 8.0.32 |
| Cache / recently viewed | Redis (StackExchange.Redis) | 3.4.0 |
| Background jobs | Hangfire (SQL Server storage) | 1.8.25 |
| Real-time | ASP.NET Core SignalR | 8.0 |
| Email | MailKit | 4.18.1 |
| Image storage | CloudinaryDotNet | 1.29.3 |
| Payments | Paymob (Intention API + Acceptance API) | — |
| Phone OTP | AUTHEVO | — |
| API docs | Swashbuckle / Swagger | 6.6.2 |
| Configuration | dotenv.net (`.env`) + appsettings + env vars | 4.2.0 |
| Tests | NUnit + Moq | 5.0.0 / 4.21.0 |

---

## 4. Solution architecture

```text
Furni E-Commerce Service/
├── App.Domain/              Entities and enums. No dependencies on other layers.
├── App.Application/         Use cases (commands/queries), validators, DTOs,
│                            interfaces, pipeline behaviors, result contracts.
├── App.Infrastructure/      EF Core, repositories/UnitOfWork, Identity, JWT,
│                            email, Paymob, Cloudinary, Redis, Hangfire jobs,
│                            SignalR hubs, seeder, interceptors.
├── App.Api/                 Controllers, middleware, Program.cs, Swagger.
└── App.Test/                NUnit unit tests (controllers, handlers, validators,
                             services).
```

### Layer rules

- **Domain** — anemic POCO entities and enums only. No business methods, no framework code.
- **Application** — all business logic, inside MediatR handlers. It depends on Domain only and never references EF Core or Infrastructure.
- **Infrastructure** — implements Application interfaces: repositories, UnitOfWork, identity, JWT, email, payments, storage, caching, jobs, hubs.
- **Api** — thin controllers that dispatch through `ISender` and translate results into HTTP responses.
- Handlers throw application exceptions (`NotFoundException`, `ConflictException`, `ForbiddenException`, `InvalidRoleException`) for failure paths that should change the HTTP status.

### Request pipeline

```text
HTTP request
   ↓
ExceptionHandlingMiddleware      → ProblemDetails (400/403/404/409/500)
   ↓
Authentication                   → JWT bearer (query token allowed for /hubs)
   ↓
Rate limiter                     → 429 after 100 requests/minute per user
   ↓
Authorization                    → role requirements
   ↓
Controller                       → ISender.Send(command | query)
   ↓
ValidationBehavior<,>            → FluentValidation (throws 400 payload)
   ↓
Handler                          → business rules + repositories + UnitOfWork
   ↓
ResponseResult<T> | PagedResult<T> | DTO
```

### Response contracts

```jsonc
// Business outcome (cart, checkout, payments, workflows)
{ "isSuccess": true, "value": { }, "errors": [] }

// Paginated collection
{ "items": [], "page": 1, "pageSize": 20, "totalCount": 0, "totalPages": 0 }

// Error (RFC 7807)
{
  "title": "Bad Request",
  "status": 400,
  "errors": ["Page size must be between 1 and 100."]
}
```

### Cross-cutting behavior

| Concern | Implementation |
| --- | --- |
| Soft delete | `ISoftDeletable` + global query filters + `SoftDeleteInterceptor`. Exempt: `AppUser`, `AuditLog`, `RefreshToken`, `IdempotencyRecord`. |
| Audit fields | `CreatedAt` set by the database on insert; mutating handlers set `UpdatedAt` explicitly. |
| Audit trail | `AuditInterceptor` writes an `AuditLog` row for entities implementing `IAuditLog` (currently `Notification`). |
| Id generation | EF generates all `Guid` keys; handlers never call `Guid.NewGuid()` for new entities. |
| Money | `decimal` throughout the domain; cents are used only at the Paymob boundary. |
| Pagination | Every unbounded list uses `FindPagedAsync` and validates `Page >= 1`, `PageSize` 1–100. |
| Secrets | Loaded from appsettings/environment/`.env`; never logged or returned by the API. |

---

## 5. Data model

| Group | Entities |
| --- | --- |
| Identity | `AppUser`, `RefreshToken` |
| Catalog | `Category`, `Product`, `ProductImage`, `Coupon`, `Review` |
| Cart and wishlist | `ShoppingCart`, `CartItem`, `WishlistItem` |
| Orders | `Order`, `OrderItem`, `CancellationRequest`, `ReturnRequest`, `ReturnItem`, `Payment` |
| Payments | `PaymentTransaction`, `IdempotencyRecord` |
| Support | `SupportTicket`, `SupportConversation`, `SupportMessage` |
| Operations | `Notification`, `AuditLog` |

Core relationships:

```mermaid
erDiagram
    AppUser ||--o{ Order : places
    AppUser ||--o{ CartItem : owns
    AppUser ||--o{ WishlistItem : saves
    AppUser ||--o{ Review : writes
    AppUser ||--o{ SupportTicket : opens

    Category ||--o{ Product : groups
    Product ||--o{ ProductImage : has
    Product ||--o{ OrderItem : ordered-as
    Product ||--o{ Review : receives

    Order ||--|{ OrderItem : contains
    Order ||--o| Payment : has
    Order ||--o{ PaymentTransaction : records
    Order ||--o{ CancellationRequest : may-have
    Order ||--o{ ReturnRequest : may-have
    ReturnRequest ||--|{ ReturnItem : contains

    SupportTicket ||--o| SupportConversation : has
    SupportConversation ||--o{ SupportMessage : contains
```

Entities use `Guid` keys, audit timestamps, and soft-delete flags (except the four exemptions above). `Product.RowVersion` provides optimistic concurrency for stock updates.

### Database notes

- The API **does not run EF migrations at runtime** and does not create the schema. The schema is provisioned with raw SQL scripts maintained outside the repository; `App.Infrastructure/Migrations` is historical and is not the deployment mechanism.
- Hangfire stores its own tables in the same SQL Server database.
- Redis is used for recently-viewed history and OTP state, not as a general cache.

---

## 6. Core business flows

### 6.1 Order lifecycle

```mermaid
stateDiagram-v2
    [*] --> Processing : place order
    Processing --> Shipped : admin
    Shipped --> OutForDelivery : admin
    Shipped --> Delivered : admin
    OutForDelivery --> Delivered : admin
    Processing --> Cancelled : direct cancel (unpaid) / approved request
    Processing --> CancellationRequested : paid customer request
    CancellationRequested --> Cancelled : admin approves
    CancellationRequested --> Processing : admin rejects
    Delivered --> [*]
    Cancelled --> [*]
```

Allowed admin transitions are enforced by `OrderStatusTransitions`; anything else is rejected. Delivery time is recorded as `Order.UpdatedAt` when the order reaches `Delivered`, which starts the 14-day return/refund window.

### 6.2 Checkout and pricing

- The cart must be non-empty and every item in stock.
- Unit price = product price minus the coupon percentage (resolved server-side from the coupon code).
- Shipping fee = city-based zone from `ShippingRates:Zones`, falling back to `ShippingRates:DefaultFee`.
- `TotalPrice` is always recomputed on the server; the value sent by the client is ignored.
- Stock is decremented; a product that reaches zero becomes inactive.
- The order is created as `Processing`; `Payment` starts as `Pending` for every method (Visa completes via the Paymob callback, cash on delivery completes later).
- A cart/order is never created twice for the same `Idempotency-Key`.

### 6.3 Cancellations

1. **Unpaid order** — the customer cancels directly (`PUT /api/orders/{orderId}/cancel`). Only `Pending`, `Confirmed`, and `Processing` orders are cancellable, and only by their owner.
2. **Paid order** — the customer submits a cancellation request while the order is pre-shipment.
3. **Admin approval** — voids the successful Paymob transaction, marks the payment and transaction `Voided`, cancels the order, restores stock, and notifies the customer.
4. **Admin rejection** — returns the order to `Processing`-style fulfilment and notifies the customer.

### 6.4 Returns and refunds

```text
Customer request (delivered order, ≤ 14 days)
        ↓
Admin accepts  → courier name/phone stored
        ↓
Admin receives → inspection timestamp
        ↓
Admin processes refund → Paymob refund → status Completed
```

- Return quantity is validated against the original order line.
- Refund cap = returned items × unit price, **plus shipping only for `MerchantDefect`** returns. Partial refunds are allowed; the cap is enforced.
- Refunds reuse the admin refund endpoint, which also enforces the 14-day window and prevents over-refunding.

### 6.5 Paymob payment flow

```mermaid
sequenceDiagram
    participant C as Customer
    participant A as Furni API
    participant P as Paymob

    C->>A: POST /api/payments/session {orderId}
    A->>P: POST /v1/intention (amount cents, special_reference = orderId)
    P-->>A: client_secret
    A-->>C: { clientSecret, checkoutUrl, paymentMethods }
    C->>P: completes checkout (card / wallet)
    P->>A: POST /api/payments/callback?hmac=...
    A->>A: verify HMAC → dedupe by transaction id → persist
    A->>A: mark Payment Completed / Failed / Pending
    A-->>P: 200 OK
```

- The callback never throws on malformed payloads or bad signatures; it logs and acknowledges so Paymob does not retry forever.
- Transactions are deduplicated by `PaymobTransactionId`; retries are safe.
- Refunds and voids use Paymob's legacy Acceptance API (`/api/acceptance/void_refund/{refund|void}`). The URL has **no trailing slash**.
- Voids are limited to 24 hours after payment; refunds to 14 days after delivery.

### 6.6 Support chat

```mermaid
sequenceDiagram
    participant U as Customer
    participant H as SupportHub
    participant A as API / Handler
    participant S as Support agent

    U->>H: JoinTicket(ticketId)
    H->>A: CheckTicketAccessQuery
    U->>H: BroadcastMessageAsync(ticketId, content)
    H->>A: SendMessageCommand
    A-->>H: SupportMessageDto
    H-->>U: newMessage (group ticket-{id})
    H-->>S: newMessage (group ticket-{id})
```

- Sending messages is **SignalR-only**; there is no REST send endpoint.
- Customers implicitly reopen a closed conversation by sending a new message; agents must wait for that.
- Closed conversations reject agent replies with a `HubException`.
- `messagesRead` is broadcast when a participant marks the conversation read.

---

## 7. API reference

### Conventions

| Item | Value |
| --- | --- |
| Base URL | `https://furniapi.runasp.net` |
| Local base URL | `https://localhost:<port>` (see `App.Api/Properties/launchSettings.json`) |
| Auth header | `Authorization: Bearer <accessToken>` |
| Refresh token | `HttpOnly`, `Secure` cookie set by login/refresh; rotated on every refresh |
| Content type | `application/json` (multipart for image uploads) |
| Pagination | `?page=1&pageSize=20` (`pageSize` 1–100) |
| Idempotency | `Idempotency-Key` header on checkout and an `idempotencyKey` body field on payment sessions |
| Swagger | `/swagger` (enabled in every environment) |
| Hangfire | `/hangfire` (Admin JWT or loopback only) |
| Rate limit | 100 requests/minute per user (or anonymous partition) → `429` |

**Access legend:** `Public` = anonymous, `Auth` = any signed-in user, `Owner` = owner of the resource, `Staff` = `Admin` or `SupportAgent`, `Admin` = `Admin` only.

### 7.1 Authentication — `/api/auth`

| Method | Endpoint | Access | Purpose |
| --- | --- | --- | --- |
| POST | `/api/auth/register` | Public | Create a customer account |
| POST | `/api/auth/login` | Public | Authenticate; returns JWT and sets refresh cookie |
| POST | `/api/auth/refresh-token` | Public (cookie) | Rotate access/refresh tokens |
| DELETE | `/api/auth/logout` | Auth | Revoke the refresh token |
| POST | `/api/auth/send-email-confirmation` | Public | Send the email confirmation link |
| POST | `/api/auth/confirm-email` | Public | Confirm an email address |
| POST | `/api/auth/forget-password` | Public | Send a password-reset email |
| POST | `/api/auth/reset-password` | Public | Reset the password with a token |
| POST | `/api/auth/send-phone-otp` | Auth | Send a phone OTP via AUTHEVO |
| POST | `/api/auth/verify-phone` | Auth | Verify the phone OTP |
| GET | `/api/auth/profile` | Auth | Current user profile |
| PUT | `/api/auth/profile` | Auth | Update profile |
| POST | `/api/auth/change-password` | Auth | Change the password |

### 7.2 Catalog

**Products — `/api/products`**

| Method | Endpoint | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/products` | Admin | All products (paginated) |
| GET | `/api/products/search` | Public | Search/filter/sort + pagination |
| GET | `/api/products/active` | Public | Active products + categories |
| GET | `/api/products/category/{categoryName}` | Public | Products in a category |
| GET | `/api/products/{productId}` | Public | Product by id |
| GET | `/api/products/{productId}/details` | Public | Product details |
| GET | `/api/products/recently-viewed` | Auth | Redis-backed history |
| POST | `/api/products` | Admin | Create a product |
| PUT | `/api/products/{productId}` | Admin | Update; may enqueue price-drop alerts |
| DELETE | `/api/products/{productId}` | Admin | Soft delete (blocked if ordered) |

**Product images — `/api/products/{productId}/images`**

| Method | Endpoint | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/products/{productId}/images` | Public | List images |
| POST | `/api/products/{productId}/images` | Admin | Upload 1–10 images to Cloudinary |
| PUT | `/api/products/{productId}/images/{imageId}/primary` | Admin | Set primary |
| PUT | `/api/products/{productId}/images/order` | Admin | Reorder all images |
| DELETE | `/api/products/{productId}/images/{imageId}` | Admin | Delete image and Cloudinary asset |

**Categories — `/api/categories`**

| Method | Endpoint | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/categories` | Public | List categories |
| POST | `/api/categories` | Admin | Create |
| PUT | `/api/categories/{categoryId}` | Admin | Update |
| DELETE | `/api/categories/{categoryId}` | Admin | Delete |

### 7.3 Cart and wishlist

**Cart — `/api/cart`** (all `Auth`)

| Method | Endpoint | Purpose |
| --- | --- | --- |
| GET | `/api/cart` | Get the current cart |
| DELETE | `/api/cart` | Clear the cart |
| POST | `/api/cart/items/{productId}` | Add an item |
| PUT | `/api/cart/quantities` | Batch-update quantities |
| DELETE | `/api/cart/items/{productId}` | Remove an item |

**Wishlist — `/api/wishlist`** (all `Auth`)

| Method | Endpoint | Purpose |
| --- | --- | --- |
| GET | `/api/wishlist` | List saved products |
| POST | `/api/wishlist/{productId}` | Idempotent add |
| POST | `/api/wishlist/{productId}/move-to-cart` | Move to cart and remove |
| DELETE | `/api/wishlist/{productId}` | Remove |

### 7.4 Checkout and orders

**Checkout — `/api/checkout`** (all `Auth`)

| Method | Endpoint | Purpose |
| --- | --- | --- |
| POST | `/api/checkout` | Get checkout details and totals |
| POST | `/api/checkout/place-order` | Place the order (`Idempotency-Key` supported) |

**Orders — `/api/orders`** (class: `Auth`)

| Method | Endpoint | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/orders` | Auth | Current user's orders (paginated) |
| GET | `/api/orders/{orderId}/details` | Owner / Staff | Order details with `OrderItemId` |
| PUT | `/api/orders/{orderId}/status` | Admin | Validated status transition |
| PUT | `/api/orders/{orderId}/cancel` | Owner | Cancel an unpaid order |
| POST | `/api/orders/{orderId}/cancellation-request` | Owner | Request cancellation of a paid order |
| POST | `/api/orders/{orderId}/return-request` | Owner | Request a return after delivery |
| GET | `/api/orders/cancellation-requests` | Admin | Cancellation queue (paginated) |
| POST | `/api/orders/cancellation-requests/{requestId}/approve` | Admin | Void payment, cancel, restore stock |
| POST | `/api/orders/cancellation-requests/{requestId}/reject` | Admin | Reject the request |
| GET | `/api/orders/return-requests` | Admin | Return queue (paginated) |
| POST | `/api/orders/return-requests/{requestId}/accept` | Admin | Assign courier |
| POST | `/api/orders/return-requests/{requestId}/receive` | Admin | Mark received/inspection |
| POST | `/api/orders/return-requests/{requestId}/reject` | Admin | Reject the return |
| POST | `/api/orders/return-requests/{requestId}/process-refund` | Admin | Refund with cap enforcement |
| DELETE | `/api/orders/{orderId}` | Owner / Admin | Delete an unpaid, inactive order |
| DELETE | `/api/orders/{orderId}/items/{productId}` | Owner / Admin | Remove an item pre-shipment |

### 7.5 Payments — `/api/payments`

| Method | Endpoint | Access | Purpose |
| --- | --- | --- | --- |
| POST | `/api/payments/session` | Auth (owner) | Create a Paymob intention and checkout URL |
| POST | `/api/payments/callback` | Public | Paymob webhook (`?hmac=`), HMAC-verified, idempotent |
| GET | `/api/payments/transactions` | Admin | Paginated transactions, optional `orderId` filter |
| GET | `/api/payments/transactions/all` | Admin | Paginated transactions without an order filter |
| POST | `/api/payments/refund` | Admin | Partial/full refund on a delivered order |
| POST | `/api/payments/void` | Admin | Void within 24 hours of payment |

### 7.6 Coupons — `/api/coupons`

| Method | Endpoint | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/coupons` | Admin | List coupons (paginated) |
| GET | `/api/coupons/check/{couponCode}` | Auth | Validate a coupon and get its ratio |
| GET | `/api/coupons/{couponId}` | Admin | Get a coupon for editing |
| POST | `/api/coupons` | Admin | Create |
| PUT | `/api/coupons/{couponId}` | Admin | Update |
| DELETE | `/api/coupons/{couponId}` | Admin | Delete |

### 7.7 Reviews — `/api/reviews`

| Method | Endpoint | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/reviews/product/{productId}` | Public | Paginated product reviews |
| POST | `/api/reviews` | Auth | Add a review (one per product per customer) |
| PUT | `/api/reviews/{reviewId}` | Owner | Edit a review |
| DELETE | `/api/reviews/{reviewId}` | Owner | Delete a review |

### 7.8 Support tickets — `/api/support-tickets`

| Method | Endpoint | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/support-tickets` | Auth | Current user's tickets (paginated) |
| GET | `/api/support-tickets/all` | Staff | All tickets (paginated, filterable by status) |
| GET | `/api/support-tickets/{ticketId}` | Owner / Staff | Ticket details |
| POST | `/api/support-tickets` | Auth | Create a ticket |
| PUT | `/api/support-tickets/{ticketId}/status` | Staff | Update status; closed tickets can only reopen with `Open` |

### 7.9 Support chat — `/api/support-tickets/{ticketId}/chat`

| Method | Endpoint | Access | Purpose |
| --- | --- | --- | --- |
| GET | `/api/support-tickets/{ticketId}/chat` | Owner / Staff | Conversation metadata |
| GET | `/api/support-tickets/{ticketId}/chat/messages` | Owner / Staff | Paginated messages (default 50) |
| POST | `/api/support-tickets/{ticketId}/chat/read` | Owner / Staff | Mark messages read |
| POST | `/api/support-tickets/{ticketId}/chat/close` | Staff | Close the conversation |
| POST | `/api/support-tickets/{ticketId}/chat/reopen` | Staff | Reopen the conversation |

Sending a message is done with `SupportHub.BroadcastMessageAsync` (SignalR), not HTTP.

### 7.10 Notifications — `/api/notifications` (Admin)

| Method | Endpoint | Purpose |
| --- | --- | --- |
| GET | `/api/notifications/admin` | Paginated notifications |
| PATCH | `/api/notifications/{id}/read` | Mark one notification read (receiver only) |

### 7.11 Administration

**Users — `/api/users`** (Admin)

| Method | Endpoint | Purpose |
| --- | --- | --- |
| GET | `/api/users` | Paginated users with optional `search` |
| PUT | `/api/users/{userId}/status` | Activate/deactivate (cannot deactivate self) |

**Roles — `/api/roles`** (Admin)

| Method | Endpoint | Purpose |
| --- | --- | --- |
| GET | `/api/roles` | List roles |
| POST | `/api/roles` | Create a role |
| POST | `/api/roles/assign` | Assign a role to a user |
| POST | `/api/roles/unassign` | Remove a role from a user |
| DELETE | `/api/roles/{roleName}` | Delete a role (built-in or assigned roles are protected) |

**Audit logs — `/api/audit-logs`** (Admin)

| Method | Endpoint | Purpose |
| --- | --- | --- |
| GET | `/api/audit-logs` | Paginated audit entries, optional `entityName` filter |

**Contact — `/api/contact`** (Auth)

| Method | Endpoint | Purpose |
| --- | --- | --- |
| GET | `/api/contact` | Contact details |
| POST | `/api/contact/send-email` | Send a contact email to the store |

### 7.12 Real-time hubs

| Hub | Route | Direction | Events |
| --- | --- | --- | --- |
| `NotificationHub` | `/hubs/notification` | Server → client | `newMessage(message, senderId)` |
| `SupportHub` | `/hubs/support` | Client → server | `JoinTicket(ticketId)`, `LeaveTicket(ticketId)`, `BroadcastMessageAsync(ticketId, content)` |
| `SupportHub` | `/hubs/support` | Server → client | `newMessage(message)`, `messagesRead(readerId)` |

Authenticate SignalR with `?access_token=<JWT>` (the API reads it only for `/hubs` paths). Send messages only after `JoinTicket` succeeds; access is checked server-side.

### 7.13 Quick examples

```bash
# Login (refresh token arrives as an HttpOnly cookie)
curl -X POST https://furniapi.runasp.net/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@furni.test","password":"Furni@2026"}'
```

```bash
# Place an order idempotently
curl -X POST https://furniapi.runasp.net/api/checkout/place-order \
  -H "Authorization: Bearer $TOKEN" \
  -H "Idempotency-Key: 8f14e45f-eaad-4b7e-9d1a-2f2b3c4d5e6f" \
  -H "Content-Type: application/json" \
  -d '{"paymentMethod":"Visa","city":"Cairo","couponCode":"WELCOME10"}'
```

```bash
# Create a payment session, then open the returned checkoutUrl
curl -X POST https://furniapi.runasp.net/api/payments/session \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"orderId":"<order-guid>","idempotencyKey":"payment-<order-guid>"}'
```

```bash
# Admin: list every transaction, newest first
curl "https://furniapi.runasp.net/api/payments/transactions/all?page=1&pageSize=20" \
  -H "Authorization: Bearer $ADMIN_TOKEN"
```

---

## 8. Configuration

Configuration is read from `appsettings.json`, `appsettings.{Environment}.json`, environment variables, and a `.env` file (loaded by `dotenv.net` with `probeForEnv: true`). **Never commit real credentials.**

| Key | Purpose |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server (application + Hangfire storage) |
| `ConnectionStrings:RedisConnection` | Redis (recently viewed, OTP state) |
| `Jwt:SecurityKey`, `Jwt:Issuer`, `Jwt:Audience` | Token signing and validation |
| `Jwt:ExpirationInMinutes`, `Jwt:ExpirationOfRefreshTokenInDays` | Access/refresh lifetimes |
| `SmtpSettings:SmtpServer`, `Port`, `SmtpEmail`, `Password`, `AdminEmail` | Outgoing mail (MailKit) |
| `Paymob:BaseUrl`, `CheckoutBaseUrl`, `PublicKey`, `SecretKey`, `HmacSecret`, `IntegrationsId`, `ApiKey`, `NotificationUrl`, `RedirectionUrl` | Payments |
| `CLOUDINARY_URL` (env var) or `Cloudinary:Url` | Cloudinary credentials (`cloudinary://key:secret@cloud`); `Cloudinary:Folder` optional |
| `AUTHEVO_KEY` | Phone OTP provider |
| `ShippingRates:DefaultFee`, `ShippingRates:Zones` | City/zone shipping fees |
| `FileStorage:MaxFileSizeBytes`, `AllowedExtensions`, `MaxFilesPerUpload` | Upload limits |
| `RecentlyViewed:MaxItems`, `ExpiryDays` | Redis history caps |
| `Cors:AllowedOrigins` | Allowed browser origins (array; falls back to `http://localhost:4200`) |
| `AllowedHosts` | Host filtering |

Example `.env` (placeholders only):

```dotenv
ConnectionStrings__DefaultConnection=Server=<server>;Database=<db>;User Id=<user>;Password=<password>;TrustServerCertificate=True
ConnectionStrings__RedisConnection=<host>:6379
JWT__SecurityKey=<long-random-secret>
JWT__Issuer=<issuer>
JWT__Audience=<audience>
SmtpSettings__SmtpServer=<smtp-host>
SmtpSettings__Port=587
SmtpSettings__SmtpEmail=<address>
SmtpSettings__Password=<password>
SmtpSettings__AdminEmail=<address>
Paymob__BaseUrl=https://accept.paymob.com
Paymob__CheckoutBaseUrl=https://accept.paymob.com/unifiedcheckout
Paymob__PublicKey=<paymob-public-key>
Paymob__SecretKey=<paymob-secret-key>
Paymob__ApiKey=<paymob-api-key>
Paymob__HmacSecret=<paymob-hmac-secret>
Paymob__IntegrationsId=<integration-id>
Paymob__NotificationUrl=<public-callback-url>
Paymob__RedirectionUrl=<frontend-redirect-url>
CLOUDINARY_URL=cloudinary://<api-key>:<api-secret>@<cloud-name>
AUTHEVO_KEY=<authevo-key>
```

> `.env` and `appsettings.Development.json` are git-ignored. Use environment variables or user-secrets in environments where files are not appropriate.

---

## 9. Running locally

### Prerequisites

- **.NET 8 SDK**
- **SQL Server** with the Furni schema applied (the API never creates it)
- **Redis**
- **Cloudinary** account (startup fails if no `CLOUDINARY_URL` is configured)
- Optional for full functionality: Paymob sandbox credentials, SMTP credentials, AUTHEVO key

### Steps

```bash
git clone <repository-url>
cd "Furni E-Commerce Service"

dotnet restore "Furni E-Commerce Service.sln"
dotnet build "Furni E-Commerce Service.sln"
dotnet run --project App.Api
```

1. Provision the database schema with the raw SQL scripts supplied by the maintainer (there is no runtime `Database.Migrate()`).
2. Start Redis and configure the connection string.
3. Create `App.Api/.env` (or set environment variables) using the keys in [Configuration](#8-configuration).
4. Start the API and open:
   - Swagger: `https://localhost:<port>/swagger`
   - Hangfire: `https://localhost:<port>/hangfire`
   - Hubs: `/hubs/notification`, `/hubs/support`

At startup the API:

1. Registers all Hangfire recurring jobs.
2. Runs `DbSeeder` (idempotent, gated by the marker product `"Nordic 3-Seater Linen Sofa"`).
3. Starts serving requests.

### Demo accounts

All seeded accounts use password **`Furni@2026`**:

| Email | Role |
| --- | --- |
| `admin@furni.test` | Admin |
| `sara.ops@furni.test` | Admin |
| `nour.support@furni.test` | SupportAgent |
| `karim.support@furni.test` | SupportAgent |
| `mariam@furni.test` | Customer |
| `omar@furni.test` | Customer |
| `laila@furni.test` | Customer |
| `youssef@furni.test` | Customer |

The seeder also creates the furniture catalog, carts, wishlists, orders across statuses, payments, returns, support conversations, notifications, and idempotency records. **Remove or change these accounts before a production launch.**

### Local test card (Paymob sandbox)

| Field | Value |
| --- | --- |
| Card | `5123 4567 8901 2346` |
| Expiry | `12/30` |
| CVV | `123` |
| Name | `AHMED ALI` |

---

## 10. Testing

```bash
dotnet test "Furni E-Commerce Service.sln"
```

- **Framework:** NUnit 5 + Moq 4.21
- **Suite:** 706 tests, all passing
- **Layout:**
  - `App.Test/Controllers` — one fixture per controller, mocking `IMediator`.
  - `App.Test/Handlers` — one fixture per feature, mocking UnitOfWork/repositories/services.
  - `App.Test/Validators` — validation rules for every command/query.
  - `App.Test/Services` — infrastructure services with fake HTTP handlers (Paymob).
- Tests are pure unit tests: no SQL Server, Redis, HTTP, SMTP, or Cloudinary calls.
- Naming convention:
  - `EnsureThat<Action>Returns<Result>When<Condition>`
  - `Ensure<Handler>Throws<Exception>When<Condition>`

> OTP command handlers are intentionally not unit-tested because they call the live AUTHEVO provider; their validators and controllers are covered.

---

## 11. Background jobs

| Job | Schedule | Purpose |
| --- | --- | --- |
| `CartExpirationBackgroundJob` | Weekly | Deletes cart items older than 7 days |
| `OrderReviewReminderBackgroundJob` | Daily | Emails review reminders for delivered orders (idempotent via `ReviewReminderSentAt`) |
| `ScheduledOrderReminderBackgroundJob` | Daily | Emails shipping/delivery reminders for orders in the expected statuses |
| `PriceDropDetectionBackgroundJob` | Enqueued on product price update | Emails wishlist owners when a saved product drops in price |

Jobs are registered at startup and visible at `/hangfire`. The dashboard accepts an Admin JWT or loopback requests only.

---

## 12. Deployment

The current deployment is hosted at **<https://furniapi.runasp.net>** with Swagger available at `/swagger`.

Checklist for a deployment:

1. Provision SQL Server and apply the schema SQL.
2. Provide all configuration values through environment variables (or a `.env` file that is never committed).
3. Ensure Redis is reachable.
4. Configure Paymob's `NotificationUrl` with the public `POST /api/payments/callback` URL and set the same `HmacSecret` used by Paymob.
5. Configure Cloudinary (`CLOUDINARY_URL`) — the application fails fast at startup without it.
6. Configure `Cors:AllowedOrigins` for the frontend origin(s).
7. Publish `App.Api` and verify:
   - `GET /swagger/v1/swagger.json` returns the route list.
   - `/hangfire` is reachable by an admin.
   - A test login returns a JWT, and an authenticated endpoint returns 200.

**Do not run EF migrations in production.** Schema changes are applied as reviewed raw SQL scripts.

---

## 13. Operational notes and known limitations

- **Schema DDL is not in this repository.** It is maintained as raw SQL and applied manually; the historical `Migrations` folder is not used.
- **Cloudinary is mandatory at startup.** Missing `CLOUDINARY_URL`/`Cloudinary:Url` throws during service registration.
- **Audit coverage is narrow.** Only entities implementing `IAuditLog` are recorded; currently that is `Notification` only.
- **No restore endpoint** for soft-deleted rows. Repositories intentionally expose no `IgnoreQueryFilters` escape hatch.
- **OTP handlers depend on the live AUTHEVO provider** and are excluded from unit tests.
- **Paymob refunds/voids depend on settlement state.** The API enforces 24-hour void and 14-day refund windows; Paymob may still reject an operation that has already settled.
- **Shipping zones are configured for Egypt**; unknown cities fall back to `ShippingRates:DefaultFee` (120 EGP if unset).
- **Swagger is always enabled**, including production. Disable it in `Program.cs` if the API is public and you do not want the schema exposed.
- **Demo data is seeded on startup.** Disable or adjust `DbSeeder` before production use.

