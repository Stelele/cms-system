# pesepay

A .NET library for taking payments through the PesePay gateway in Zimbabwe — EcoCash, InnBucks, Visa, MasterCard, Zimswitch, Omari and PayGo across USD and ZiG — published to NuGet as `Stelele.PesePay`.

## What it does

You construct a `PesePayClient` with an integration key, an encryption key and an environment, then either initiate a redirect payment (customer is sent to a PesePay-hosted page) or a seamless server-to-server payment (mobile money by phone number, or a card), poll for status by reference number or poll URL, and list active currencies and per-currency payment methods. Requests are AES-encrypted before they leave; responses come back in a `{"payload": ...}` envelope that the client decrypts for you.

## How it works

`PesePayClient.cs` (285 lines) is the whole runtime. The constructor builds an `HttpClient` pointed at `https://api.test.sandbox.pesepay.com/payments-engine/` or `https://api.pesepay.com/api/payments-engine/` and sets an `authorization` header from the integration key. `InitiateRedirectPaymentAsync` assembles a `Transaction` (an `Amount` plus reason and merchant reference), serialises it with camelCase `JsonSerializerOptions`, encrypts it via `IPayloadCrypto` (`Crypto/AesCbcPayloadCrypto.cs`, 42 lines), posts to `v1/payments/initiate`, and hands the decrypted body to `DecryptResponsePayload` before deserialising. `InitiateSeamlessPaymentAsync` does the same but builds a `Payment` and requires either a `Card` or a `PhoneNumber`; a static dictionary `_methodCodes` maps `(PaymentMethodCode, CurrencyCode)` pairs such as `(EcoCash, ZiG) → "PZW201"` and throws `PesePayException` when a combination doesn't exist. `CheckPaymentStatusAsync` and `PollPaymentAsync` hit `v1/payments/check-payment`, `GetActiveCurrenciesAsync` hits `v1/currencies/active`, `GetPaymentMethodsAsync` hits `v1/payment-methods/for-currency`.

Domain records live in `Domain/` (19 small files: `Transaction`, `Payment`, `Amount`, `Customer`, `PesepayResult<T>`, request/response types). `ServiceCollectionExtensions.AddPesePay` takes either an `Action<PesePayConfiguration>` or an `IConfiguration`, builds one client and registers it as an `IPesePayClient` singleton.

### Stack

C# with `TargetFrameworks: net8.0;net9.0;net10.0`, `Nullable` and `ImplicitUsings` on. Only three runtime dependencies: `Microsoft.Extensions.DependencyInjection.Abstractions`, `Configuration.Abstractions` and `Configuration.Binder`. `MinVer` derives the package version from git tags; `docfx` generates the API site; GitHub Actions handles CI and NuGet publish.

## What works well

- Test coverage is real: 66 `[Fact]`/`[Theory]` attributes across `PesePay.Tests` (372-line `PesePayClientApiTests` drives the client through a `FakeHttpMessageHandler` and asserts on the encrypted request body) plus a separate `PesePay.Tests.Integration` project whose `SandboxFactAttribute` skips unless sandbox keys are in env vars.
- `ci.yml` (83 lines) runs unit tests always, integration tests when secrets exist (hard-fails on release tags without them), then packs, validates the package name against the tag, and pushes to NuGet via OIDC.
- The `IPesePayClient` interface carries XML docs on every method, and the README is 13 KB of worked examples including webhook handling and flow choice.
- The crypto and HTTP layers are injectable through an `internal` constructor exposed to tests via `InternalsVisibleTo`.

## What I'd change

- `PesepayResult<T>.Fail` exists but is never called by the client — grep finds it only in `PesepayResultTests`. Every failure path in `PesePayClient` throws `PesePayException`, so callers get a result type that is always successful or an exception.
- `AesCbcPayloadCrypto` derives the IV from the first 16 bytes of the encryption key (line 18) — a fixed, key-derived IV for CBC.
- `DecryptResponsePayload` (line 105) catches `CryptographicException` and silently returns the raw response body, which then blows up in the deserialiser with an unrelated message.
- `ServiceCollectionExtensions` eagerly news up a client with its own `HttpClient` (line 35 of `PesePayClient.cs`) instead of `IHttpClientFactory`; the client is never disposable and both overloads duplicate the same six lines.
- `GetActiveCurrenciesAsync` skips decryption that every other endpoint applies, and `PollPaymentAsync` is a single GET despite the name — there is no retry loop or backoff.

## Still outstanding

- No webhook helper in the library — signature/structure verification is left to the README's copy-paste controller.
- Card number and CVV are written into `PaymentRequestFields` (`PesePayClient.cs:176-179`) with no guidance on PCI scope.
- `docs/superpowers/` holds four plans and specs (2025-07 through 2026-08) whose follow-through isn't visible in code.

## Why I built it

<!-- TODO: this is the part only the author can write. What made them start it?
     What problem did they want to solve? Leave this heading and its TODO in
     place. -->
