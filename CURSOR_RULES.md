# CURSOR Rules — LearningEdge

The TMS Web UI master specification now lives as an auto-applied Cursor rule:

**`LearningEdgeUI/.cursor/rules/tms-ui.mdc`**

It is the single source of truth. Edit it there — do not copy its contents back into this
file, because duplicated specs drift and the stale copy silently wins during code generation.

Section 3 of that rule (Backend API Contract) is verified against `LearningEdge/src` and is
authoritative: route shapes, the `ApiResult<T>` envelope, numeric `UserRole` values, and the
list of endpoints that actually exist. Re-verify it whenever the API changes.
