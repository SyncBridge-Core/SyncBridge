# Seed Idempotency Evidence

Approved deterministic rows after repeated seed execution:

| Entity | Count |
|---|---:|
| AppUser | 2 |
| Material | 1 |
| Equipment | 1 |
| Recipe | 1 |
| RecipeStep | 2 |
| RecipeMaterialRequirement | 1 |
| RecipeEquipmentRequirement | 1 |
| ProductionOrder | 2 |

Total: 11 fictional reference/demo rows.

The second seed invocation wrote zero rows. Batch, BatchStepExecution, MaterialVerification, EquipmentVerification, Deviation, ElectronicSignature, and AuditEvent counts remained zero. The seed performs add-if-missing checks and does not overwrite controlled manufacturing data.
