# Schema Reconciliation

Database identity: `SyncBridgeCore`

## Core table/column counts

| Table | Columns |
|---|---:|
| AppUser | 4 |
| AuditEvent | 9 |
| Batch | 8 |
| BatchStepExecution | 9 |
| Deviation | 11 |
| ElectronicSignature | 7 |
| Equipment | 4 |
| EquipmentVerification | 8 |
| Material | 3 |
| MaterialVerification | 8 |
| ProductionOrder | 8 |
| Recipe | 5 |
| RecipeEquipmentRequirement | 2 |
| RecipeMaterialRequirement | 3 |
| RecipeStep | 6 |

Core tables: 15. Foreign keys: 23. Restricted/no-action foreign keys: 23. User triggers: 0. User stored procedures: 0. User views: 0.

All 15 tables have approved primary keys. Composite primary keys are deployed for RecipeMaterialRequirement and RecipeEquipmentRequirement. Unique indexes are deployed for AppUser.UserName; RecipeCode; Recipe/StepNumber; MaterialCode; EquipmentCode; OrderNumber; BatchNumber; Batch/StepNumber; Batch/material verification; Batch/equipment verification; and DeviationCode.

DBDS-AMD-001: DeviationId `uniqueidentifier NOT NULL`; DeviationCode `nvarchar(40) NOT NULL`, unique.

DBDS-AMD-002: AuditEvent.AffectedEntityId `nvarchar(256) NOT NULL` (SQL metadata max_length 512 bytes for NVARCHAR).

Migration history: `20261013010705_InitialCreate`.
