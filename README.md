# Subscription Tracker

Desktop WPF application for tracking recurring subscriptions, domains, hosting, VPN services, loans, and installment payments.

## What the app does

- Adds and edits subscriptions with billing cycle, amount, currency, next payment date, and reminder settings
- Shows a dashboard with monthly/yearly totals, upcoming charges, top spending categories, and 6-month forecast
- Displays subscription list, payment calendar, analytics, payment history, and settings
- Stores data locally in SQLite
- Exports subscriptions and payment history to Excel
- Supports dark and light themes
- Supports UI localization:
  - `Русский`
  - `English`
- Supports base currency switching:
  - `RUB`
  - `USD`
  - `EUR`
  - `GBP`

## Tech stack

- `.NET 8`
- `WPF`
- `SQLite`
- `Entity Framework Core`
- `Microsoft.Extensions.Hosting`
- `Microsoft.Extensions.DependencyInjection`
- `EPPlus`

## Solution structure

```text
SubscriptionTracker
├── SubscriptionTracker.Wpf
├── SubscriptionTracker.Application
├── SubscriptionTracker.Domain
├── SubscriptionTracker.Infrastructure
└── SubscriptionTracker.Tests
```

## Architecture

- `Domain`: entities, enums, domain services
- `Application`: DTOs, interfaces, localization catalog, formatting helpers
- `Infrastructure`: EF Core, SQLite, services, reminders, Excel export
- `Wpf`: views, view models, theme/localization services, window shell
- `Tests`: unit tests for recurring payment calculation

## Main screens

- `Dashboard`
- `Subscriptions`
- `Payment Calendar`
- `Analytics`
- `Payment History`
- `Settings`

## Settings

The settings screen currently allows:

- switching the app language
- switching the base currency for analytics and totals
- switching the theme
- changing reminder check interval
- enabling or disabling reminders

Settings are stored locally in:

```text
%LocalAppData%\SubscriptionTracker\settings.json
```

The SQLite database is stored locally in:

```text
%LocalAppData%\SubscriptionTracker\subscription_tracker.db
```

## Running the project

```bash
dotnet build SubscriptionTracker.sln
dotnet run --project SubscriptionTracker.Wpf
```

## Tests

```bash
dotnet test SubscriptionTracker.Tests\SubscriptionTracker.Tests.csproj
```

## Current MVP scope

- Local-only desktop app
- Offline currency conversion with fixed rates
- Reminder checks on startup and by timer while the app is open
- Excel export for subscriptions and payment history

## Future improvements

- Native toast notifications
- Auto-start with Windows
- Richer charts
- Import/export backup flow
- Editable system categories
- More languages and live exchange rates
