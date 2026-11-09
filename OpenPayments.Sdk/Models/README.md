# Models

Hand-owned partial-class augmentations of the generated DTOs (plus the shared
`Anonymous`/`Helpers` types and the contract resolvers). These files keep the
`OpenPayments.Sdk.Generated.*` namespaces because a partial class must share its
namespace with the generated half it extends — but they live outside `Generated/`
so that folder stays 100 % regenerable and CI can fail on any drift.
