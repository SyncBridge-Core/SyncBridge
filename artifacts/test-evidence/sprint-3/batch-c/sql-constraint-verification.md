# SQL Constraint Verification

Target: approved `SyncBridgeCore` database only.

Passing live negative/positive checks:

- Duplicate AppUser primary key rejected.
- Duplicate RecipeCode, MaterialCode, EquipmentCode, OrderNumber, BatchNumber, and DeviationCode rejected.
- Duplicate Recipe/material composite key rejected.
- Duplicate Recipe/StepNumber rejected.
- Duplicate Batch/StepNumber rejected.
- Duplicate Batch/material verification rejected.
- Duplicate Batch/equipment verification rejected.
- Missing required Material.Name rejected.
- Invalid ProductionOrder-to-Recipe FK rejected.
- Referenced Recipe delete rejected.
- `decimal(18,3)` overflow rejected; excess scale rounded by SQL Server and reconstructed as stored.
- Overlength MaterialCode and AuditEvent.AffectedEntityId rejected without truncation.
- Date and DateTimeOffset values round-tripped exactly.
- Closed enum converters and technical GUID boundary tests reject unsupported values.

All fixtures used unique identifiers and rollback-scoped transactions. Final manufacturing row counts were zero.
