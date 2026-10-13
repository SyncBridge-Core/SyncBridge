# Transaction and Failure Evidence

Successful case: one rollback-scoped fixture saved a Batch, step execution, material verification, equipment verification, electronic signature, and canonical audit events as one DbContext command boundary.

Induced FK failure: a valid AppUser marker and an invalid ProductionOrder referencing a missing Recipe were submitted in one unit-of-work commit. SQL Server rejected the command. The transaction was rolled back, and a new DbContext verified that the AppUser marker was absent.

Additional induced failures: duplicate unique business identifiers, duplicate Batch evidence cardinalities, duplicate Batch step sequence, decimal precision overflow, and overlength values. Each fixture ran in its own transaction and was rolled back.

Outcome: no partial business/evidence rows remained, and the final accepted database contained zero manufacturing-history rows.
