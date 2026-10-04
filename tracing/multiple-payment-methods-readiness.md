# Multiple Payment Methods — Readiness Trace

Source requirement: [`docs/business/multiple-payment-methods.feature`](../docs/business/multiple-payment-methods.feature)

| Requirement ID | Business requirement | Source file/section | Current code evidence | Status | Gap |
|---|---|---|---|---|---|
| PAY-MULTI-001 | A staff member can settle a VND 500,000 order with VND 200,000 cash and VND 300,000 card, record both payment details, show a successful receipt, and leave no balance. | `multiple-payment-methods.feature`, scenario | `PaymentUseCaseService.ProcessPaymentAsync` accepts one `ProcessPaymentRequest`, creates one `Payment`, and immediately sets the order to `Paid`. `Payment` stores `OrderId`, `Amount`, and `Method`. | BLOCKED — documentation first | The contract for submitting multiple payments is unspecified; partial-payment status, overpayment/underpayment rules, atomicity, allowed methods, and receipt output need confirmation. |

## Code-readiness gate

| Readiness criterion | Met/Not met | Evidence | Missing information or action |
|---|---|---|---|
| Specific business result | Met | Scenario specifies total and two payment amounts. | — |
| Traceable source and section | Met | Feature file and scenario above. | — |
| Actor/workflow identified | Met | Staff member creates, selects, and confirms payment. | — |
| Current gap identifiable | Met | Current service processes one payment and marks the order paid. | — |
| Acceptance test is precise | Not met | Happy path is precise, but request/response and sequencing are not. | Define endpoint payload and receipt response. |
| Inputs, outputs, errors, boundaries | Not met | No rules for zero, underpayment, overpayment, duplicate confirmation, or invalid methods. | Confirm validation and failure behavior. |
| No conflicting documentation | Met | No other multiple-payment requirement found. | — |
| Affected code area identifiable | Met | Payment service, controller, repository, and `Payment` entity. | — |
| Test infrastructure exists or can be created | Met | `HelloWorldMvc.sln` includes `WebApplication.Tests/WebApplication.Tests.csproj`; xUnit smoke test is discovered and passes. | Add payment use-case tests after the business contract is confirmed. |
| Enough time for safe implementation | Met | Existing xUnit project and baseline test pass; this run has an explicit 10–11 minute timebox. | Implementation remains blocked by the business contract, not by time. |

## Requirement prepared for next run

- **ID:** PAY-MULTI-001
- **Objective:** Reconcile one order across multiple captured payment methods.
- **Actor/workflow:** Staff member confirms a set of payments for an open unpaid order.
- **Preconditions:** Order exists, is open, and has total VND 500,000; selected payments total exactly VND 500,000.
- **Expected behavior:** Persist both payment rows atomically, mark the order fully paid, return/show a successful receipt, and report zero outstanding balance.
- **Error behavior:** Still requires confirmation for underpayment, overpayment, duplicate confirmation, unsupported method, and persistence failure.
- **First acceptance test:** Submit cash VND 200,000 plus card VND 300,000 and verify two payment records, paid status, receipt, and zero balance.
- **Expected code area:** `PaymentUseCaseService`, payment controller/request model, order/payment persistence.
- **Status:** BLOCKED — pending the contract and boundary decisions above; then `READY_FOR_TDD`.

## Decision record — DR-PAY-MULTI-001

The following decisions are required before coding. They are questions, not assumed requirements.

| Decision | Options | Impact | Recommended choice |
|---|---|---|---|
| Submission contract | One request containing `payments[]`; multiple existing single-payment requests; other | Determines controller DTO, confirmation semantics, and receipt response. | One atomic request containing `payments[]`, because the feature says the staff member confirms the set together. |
| Exact reconciliation | Require sum = order total; allow underpayment as partially paid; allow overpayment/change | Determines order status, outstanding balance, and financial correctness. | Require exact equality for this feature; reject under/overpayment without creating records. |
| Atomicity | All payment rows and status update commit together; partial writes allowed | Prevents payment/order mismatches identified by risk R-05. | Atomic transaction/all-or-nothing commit. |
| Accepted methods | Cash/Card only; configurable list; any non-empty string | Determines validation and future extensibility. | Confirm whether this feature is limited to Cash and Card. |
| Receipt result | Receipt payload in API response; print integration; display-only marker | Determines acceptance-test assertion and integration scope. | Return a receipt payload/identifier; printing is outside this API change unless explicitly required. |

Until these choices are confirmed, the first acceptance test cannot be written against a stable API contract. Once confirmed, the requirement becomes `READY_FOR_TDD`; the first test is the exact VND 200,000 cash + VND 300,000 card happy path, asserting two captured rows, paid status, zero balance, and receipt evidence.

## Run evidence — 2026-09-03

This reassessment checked the source feature, repository review, project charter business case, success criteria, stakeholder list, risk register, payment service/controller/models, repository ports, and the existing xUnit project. No confirmed business decision resolves the five contract questions above. Therefore this run remains documentation-only; production code, tests, configuration, dependencies, and the source feature were intentionally left unchanged.

Verification recorded for this run: solution test suite passes (`1/1`), Release build succeeds with zero warnings and errors, Docker Compose configuration validates, and `git diff --check` reports no whitespace errors. The current working tree contained a pre-existing modification to this trace file; no source feature, production code, tests, configuration, dependencies, or generated artifacts were changed by this run.
