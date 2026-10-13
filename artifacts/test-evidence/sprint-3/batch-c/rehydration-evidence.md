# Rehydration and Evidence Integrity

- Batch authoritative CreatedAt, StartedAt, and CompletedAt were reconciled with canonical audit evidence.
- MaterialVerification and EquipmentVerification required one matching canonical audit event.
- ElectronicSignature required the approved BatchStepExecution discriminator and an execution owned by the same Batch.
- Unsupported or cross-Batch signature targets were rejected.
- AuditEvent identities were reconstructed once; deviation-owned and Batch-direct events were not duplicated.
- Distinct events were preserved by stable AuditEventId.
- Completed Batch reconstruction retained terminal status and ordinary persistence mutations were rejected.
- Existing AuditEvent and ElectronicSignature update/delete states were rejected by the approved unit of work.
