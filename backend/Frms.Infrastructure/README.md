# Frms.Infrastructure scaffold

This supporting assembly registers payment, email, optional AI, and notification adapters behind Business-owned interfaces. Phase 0 adapters fail with `EXTERNAL_PROVIDER_NOT_CONFIGURED` (503); they never fabricate successful delivery/payment. Actual provider integration is deferred to the corresponding later phase.
