# ADR 0001: Migrate serialization from Newtonsoft.Json to System.Text.Json

- Status: Accepted (not yet scheduled)
- Date: 2026-07-23

## Context

The SDK serializes with Newtonsoft.Json 13, configured by a single lenient
`OpenPaymentsContractResolver` (see Phase 4). Modern .NET consumers increasingly expect
System.Text.Json (STJ): it removes a third-party dependency, and source-generated
contexts enable trimming/NativeAOT support, which Newtonsoft cannot offer.

What ties us to Newtonsoft today:

1. **Generated DTOs** carry `[Newtonsoft.Json.JsonProperty]` attributes (NSwag `/JsonLibrary` defaults).
2. **Hand-written type aliases** in `Generated/*/Types.cs` use `[JsonProperty]`, `[JsonExtensionData]`,
   `[JsonConverter(typeof(StringEnumConverter))]`, and `Required`/`NullValueHandling` settings.
3. **The lenient drift policy** is implemented as a `DefaultContractResolver` subclass.
4. **`GeneratedClientBase`** serializes request bodies and deserializes responses via `JsonConvert`.
5. **Consumers' `Metadata` fields** are `object?` and materialize as `JObject` (tests rely on this).

## Decision

Migrate to STJ with source-generated contexts in one coordinated breaking release
(pre-1.0 or a major bump), rather than dual-supporting both serializers.

## Phased plan

1. **Regenerate for STJ.** Add `/JsonLibrary:SystemTextJson` to the NSwag flags in the `Makefile`
   and regenerate. Generated DTOs switch to `[System.Text.Json.Serialization.JsonPropertyName]`,
   `[JsonExtensionData]` (STJ flavor, requires `IDictionary<string, JsonElement>` or `JsonObject`), and
   `JsonStringEnumConverter`-compatible enums. The codegen-check workflow keeps this honest.
2. **Port the hand-written type aliases.** Mechanical attribute swap in `Generated/*/Types.cs`
   (`JsonProperty` → `JsonPropertyName` + `JsonIgnore(Condition = WhenWritingNull)`;
   `StringEnumConverter` → `JsonStringEnumConverter` with `EnumMember`-value mapping via
   `JsonStringEnumMemberNameAttribute` (.NET 9) or a custom converter on net8.0).
3. **Replace the drift policy.** STJ has no contract resolver subclassing, but
   `DefaultJsonTypeInfoResolver` + type-info modifiers reproduce it: clear
   `IsRequired` on all properties and set null-ignoring defaults in one modifier —
   the direct equivalent of `OpenPaymentsContractResolver`.
4. **Swap the plumbing.** `GeneratedClientBase` moves from `JsonConvert`/`JsonSerializerSettings` to
   `System.Text.Json.JsonSerializer`/`JsonSerializerOptions`; `OpenPaymentsSerialization.DefaultSettings`
   becomes `JsonSerializerOptions DefaultOptions`.
5. **Source-generate.** Add a `JsonSerializerContext` partial listing every DTO
   (`[JsonSerializable(typeof(...))]` per root type), wire it into `DefaultOptions.TypeInfoResolver`,
   and enable `<IsAotCompatible>` + `<EnableTrimAnalyzer>` in the SDK csproj to prove it.
6. **Drop the dependency.** Remove `Newtonsoft.Json` from `Directory.Packages.props`; document the
   `Metadata` type change (`JObject` → `JsonElement`/`JsonObject`) in the CHANGELOG as breaking.

## Consequences

- Consumers reading `Metadata` as `JObject` must move to `JsonElement`/`JsonObject` (breaking).
- The lenient-drift tests from Phase 4 (`SerializationDrift_Tests`) carry over verbatim and gate step 3.
- Until scheduled, new serialization-touching code must go through `OpenPaymentsSerialization`
  so the future swap stays single-point.
