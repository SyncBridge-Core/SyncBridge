# WP-6 Sprint 3 Task 1 — OQ-1 Impact Analysis

## 1. Decision Under Review

The Team has confirmed the following manufacturing rule:

- `ProductionOrder.Quantity` is the authoritative planned quantity.
- Every `Batch` associated with a Production Order executes that full quantity.
- Multiple Batches may reference the same Production Order, but quantity is not divided or allocated among them.
- The approved DBDS remains the exact 15-table design; no Batch quantity or unit-of-measure column is to be added.

This report evaluates the resulting Domain and persistence impact only. It does not implement or approve OQ-1.

## 2. Current-State Evidence

### 2.1 Domain model

`ProductionOrder` already requires a positive `Quantity`, stores it as immutable state, and is therefore a valid authoritative source.

`Batch` currently:

- accepts an independently supplied `plannedQuantity` and `UnitOfMeasure` in its public constructor;
- rejects a non-positive Batch quantity;
- stores both values with private setters; and
- does not use either value in lifecycle transitions, verification prerequisites, deviations, signatures, step execution, or audit evidence.

The current constructor permits the Batch quantity to disagree with its associated Production Order because `Batch` has only `ProductionOrderNumber` and receives quantity independently.

### 2.2 Requirements and database design

The reviewed SRS and SDD require Production Order/Batch association, manufacturing workflow, verification, deviation, signature, audit, and EBR behavior. They do not establish an independent Batch quantity or unit-of-measure business rule.

The approved DBDS stores `ProductionOrder.Quantity decimal(18,3)` and associates each Batch through `Batch.OrderId`. The Batch table contains no quantity or unit column. The EBR can obtain the authoritative quantity through the existing Batch-to-ProductionOrder relationship. No approved table provides a unit-of-measure value.

### 2.3 Test suite

Eight test files pass `new UnitOfMeasure("kg")` only to satisfy Batch construction. No test asserts `Batch.PlannedQuantity` or `Batch.UnitOfMeasure`, and no lifecycle behavior consumes either property. Production Order tests already verify quantity preservation and rejection of zero or negative quantities.

## 3. Options Evaluated

| Option | Requirements alignment | DBDS impact | Domain/API impact | Assessment |
|---|---|---|---|---|
| **A. Retain `Batch.PlannedQuantity` as derived state; remove `Batch.UnitOfMeasure`** | Matches the confirmed rule and preserves useful Batch execution context | None; load quantity by joining the associated Production Order | Removes independently authoritative Batch quantity input and the unsupported unit property/value object | **Recommended** |
| **B. Remove both Batch properties** | Still permits quantity to be read from Production Order, but removes previously accepted Batch Domain context | None | Larger behavioral/API contraction; callers must always traverse or separately load Production Order | Compliant but not preferred |
| **C. Persist Batch quantity and/or unit** | Duplicates the authoritative quantity and invents a unit source | Adds unauthorized columns or a table and creates synchronization risk | Introduces competing state | Rejected |

## 4. Recommendation

Adopt Option A:

1. Retain `Batch.PlannedQuantity` as immutable, derived Domain state.
2. Source it exclusively from the associated `ProductionOrder.Quantity` during creation and persistence reconstruction.
3. Remove `Batch.UnitOfMeasure` and the `UnitOfMeasure` value object because there is no approved requirement, invariant, or persistence source for it.
4. Preserve only `ProductionOrderNumber` on Batch; do not add a navigation property or retained Production Order object reference.
5. Preserve the exact 15-table DBDS without additional columns or tables.

For persistence reconstruction, `IBatchRepository.GetExecutionAggregateAsync` can load the Batch row with its Production Order and provide `ProductionOrder.Quantity` to the approved non-public Domain rehydration seam. This is Infrastructure reconstruction, not a Domain dependency on EF Core or SQL.

For new Batch creation, the later implementation must prevent an arbitrary caller-supplied quantity from becoming independent Batch state. The quantity should be copied from an already loaded Production Order through an approved creation/assembly path while the Batch continues to retain only its `ProductionOrderNumber`. The exact creation API should be settled with the broader rehydration design; this investigation does not authorize a new navigation property, Domain service, or aggregate dependency.

## 5. Business-Rule Impact

- Each Batch for a Production Order exposes the same `PlannedQuantity` as `ProductionOrder.Quantity`; no allocation or residual-quantity calculation is introduced.
- The positive-quantity invariant remains owned by `ProductionOrder`. A Batch reconstructed from a valid Production Order inherits a positive quantity, so a duplicate Batch-level positivity rule is unnecessary.
- Batch lifecycle, ordered execution, material/equipment verification, deviation, electronic-signature, completed-record protection, and audit rules are unchanged.
- No audit event currently records quantity or unit, so audit semantics are unchanged.
- EBR production information can obtain quantity from the Production Order associated with the Batch.
- Removing unit of measure does not weaken an approved requirement because no reviewed SRS, SDD, or DBDS field defines or supplies it.

## 6. Persistence and Architecture Impact

- No DBDS change is required or permitted.
- The Batch table remains unchanged.
- The ProductionOrder table remains the single persisted source of quantity.
- Infrastructure may use the existing `Batch.OrderId -> ProductionOrder.OrderId` relationship when assembling a Batch.
- Domain remains independent of EF Core, SQL Server, repository implementations, ASP.NET Core, and Infrastructure.
- Application repository contracts require no signature change for this decision.
- No navigation property, new repository, Domain service, Specification, or new value object is required.

## 7. Potential Implementation File Impact

### Production files

| File | Potential later change |
|---|---|
| `src/SyncBridge.Core.Domain/Entities/Batch.cs` | Remove the `UnitOfMeasure` constructor argument/property/guard; make `PlannedQuantity` derive from the authoritative Production Order quantity through the approved creation and rehydration paths rather than independent caller data. |
| `src/SyncBridge.Core.Domain/ValueObjects/UnitOfMeasure.cs` | Delete after all constructor fixtures are updated; repository-wide search found no other production use. |

No change is currently indicated for `ProductionOrder.cs`, `IBatchRepository.cs`, or `IProductionOrderRepository.cs`. `ProductionOrder.Quantity` and the existing Batch lookup contract already support the rule.

### Existing test files requiring constructor-fixture updates

- `tests/SyncBridge.Core.Tests/BatchAuditEvidenceTests.cs`
- `tests/SyncBridge.Core.Tests/BatchDeviationLifecycleTests.cs`
- `tests/SyncBridge.Core.Tests/BatchLifecycleTests.cs`
- `tests/SyncBridge.Core.Tests/BatchPrerequisiteVerificationTests.cs`
- `tests/SyncBridge.Core.Tests/BatchStepExecutionTests.cs`
- `tests/SyncBridge.Core.Tests/CompletedRecordProtectionTests.cs`
- `tests/SyncBridge.Core.Tests/ElectronicSignatureGateTests.cs`
- `tests/SyncBridge.Core.Tests/OrderedWorkflowExecutionTests.cs`

Focused tests should verify that Batch quantity equals its associated Production Order quantity and cannot diverge through the public creation path. A later repository integration test should verify the same equality after persistence round trip. Existing lifecycle assertions should remain unchanged.

### Documentation

Accepted Sprint 2 reports are historical evidence and should not be rewritten. A later implementation/completion report should record this Team-approved clarification as the disposition of OQ-1. The existing Task 1 design-gate and completion reports are intentionally unchanged by this investigation.

## 8. Risks and Controls

| Risk | Required control |
|---|---|
| A caller continues supplying an arbitrary Batch quantity | Remove or constrain the independent public input; derive from a loaded Production Order in the approved creation path |
| Infrastructure loads Batch without its Production Order | Treat the missing required association as persistence-integrity failure; do not invent a quantity |
| A persisted Production Order contains an invalid quantity | Fail reconstruction as corrupted data; do not bypass the Domain invariant |
| Historical consumers expect `Batch.UnitOfMeasure` | Treat removal as an intentional API correction and update compile-time fixtures; no approved persistence source exists |
| Future unit semantics become required | Handle through a separately approved requirements and DBDS change; do not infer a default such as `kg` |

## 9. Conclusion

Option A is the smallest architecture-compliant resolution. It removes the unreconstructable unit, preserves useful Batch quantity context, makes `ProductionOrder.Quantity` the sole authority, and requires no change to the approved 15-table DBDS. Implementation should proceed only after the Team records approval of this OQ-1 recommendation and coordinates it with the aggregate rehydration decision.

**Investigation result: recommendation complete; OQ-1 remains unapproved and no implementation is authorized by this report.**
