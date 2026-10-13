# DBDS Amendment and Unicode Evidence

DBDS-AMD-001:

- DeviationCode is required and unique.
- A Unicode DeviationCode and description round-tripped unchanged.
- Stable DeviationId remained independent of DeviationCode.
- No additional table was introduced.

DBDS-AMD-002:

- AuditEvent.AffectedEntityId is deployed as `nvarchar(256) NOT NULL`.
- A 180-UTF-16-unit identifier containing `Å`, `漢`, and 89 supplementary-plane emoji round-tripped unchanged.
- A 257-unit value was rejected by SQL Server.
- 129 supplementary characters (258 UTF-16 units) were rejected by the persistence-boundary validator.
- No truncation, normalization, or Domain serialization change occurred.
