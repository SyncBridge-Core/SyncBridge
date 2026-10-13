# Repository Round-Trip Evidence

- Recipe repository reconstructed stable RecipeId, RecipeCode, version, approval, two ordered steps, one material requirement, and one equipment requirement.
- ProductionOrder repository preserved stable OrderId, OrderNumber, RecipeId, quantity, timestamp, and every approved status: Planned, Released, InProcess, Completed, and Cancelled.
- Batch repository reconstructed Created, Ready, InProgress, Exception, and Completed states.
- Rich Batch fixture reconstructed step execution, material verification, equipment verification, signature, deviation/evidence associations, and audit events.
- Audit/signature row counts did not increase during load.
- Repository loads did not replay manufacturing commands.
- Infrastructure surrogate keys and Domain business identifiers remained unchanged.
