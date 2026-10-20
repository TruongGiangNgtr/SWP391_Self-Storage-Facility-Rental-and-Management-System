# Frms.ApiTests

API-level tests are based on the implementation currently present in `Frms.Api` and its registered services. They cover host wiring, authentication primitives and request validation, OpenAPI/health behavior, route/role metadata, API response behavior for implemented modules, and explicit `501 ENDPOINT_NOT_IMPLEMENTED` responses for remaining scaffold actions.

Do not treat a route or DTO contract as proof that a business workflow is implemented. Tests for endpoints with real controller/service behavior should assert the behavior visible in that implementation; scaffold tests should only assert the current `501` response.
