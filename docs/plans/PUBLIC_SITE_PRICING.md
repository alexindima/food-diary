# Public pricing: payment integration TODO

The public `/pricing` and `/ru/pricing` pages currently compare free manual
tracking with Premium AI capabilities. Online payment is not connected yet.
Keep prices hidden and show the localized availability message until it is ready.
The public page must not initiate checkout or imply that payment is available.

## TODO before enabling paid plans

- Connect and verify the chosen payment provider in the owning Billing module.
- Provide current public plan prices through a narrow read-only capability.
  Do not expose account, subscription, payment history or provider secrets.
  Use the generated API SDK for any new endpoint.
- Show localized monthly and yearly totals, billing periods and applicable tax
  information. Never maintain separate hardcoded prices in the marketing copy.
- Cover loading, unavailable configuration, provider failure, retry and final
  checkout price. If prices cannot be loaded, retain free signup and a clear
  unavailable state.
- Verify anonymous EN/RU pricing, authenticated checkout, subscription management
  and the provider's real sandbox flow before removing the placeholder.
- Update API contract snapshots and required tests if an endpoint is introduced.

Changing public payment availability is separate from editing the marketing
page. The current placeholder does not authorize a deployment or a payment.
