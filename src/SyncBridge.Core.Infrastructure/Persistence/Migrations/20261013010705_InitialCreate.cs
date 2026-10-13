using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SyncBridge.Core.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "AppUser",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUser", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Equipment",
                schema: "dbo",
                columns: table => new
                {
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    CalibrationDueDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Equipment", x => x.EquipmentId);
                });

            migrationBuilder.CreateTable(
                name: "Material",
                schema: "dbo",
                columns: table => new
                {
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Material", x => x.MaterialId);
                });

            migrationBuilder.CreateTable(
                name: "Recipe",
                schema: "dbo",
                columns: table => new
                {
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipeCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Version = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipe", x => x.RecipeId);
                });

            migrationBuilder.CreateTable(
                name: "ProductionOrder",
                schema: "dbo",
                columns: table => new
                {
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProductCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProductName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrder", x => x.OrderId);
                    table.ForeignKey(
                        name: "FK_ProductionOrder_Recipe",
                        column: x => x.RecipeId,
                        principalSchema: "dbo",
                        principalTable: "Recipe",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecipeEquipmentRequirement",
                schema: "dbo",
                columns: table => new
                {
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeEquipmentRequirement", x => new { x.RecipeId, x.EquipmentId });
                    table.ForeignKey(
                        name: "FK_RecipeEquipmentRequirement_Equipment",
                        column: x => x.EquipmentId,
                        principalSchema: "dbo",
                        principalTable: "Equipment",
                        principalColumn: "EquipmentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeEquipmentRequirement_Recipe",
                        column: x => x.RecipeId,
                        principalSchema: "dbo",
                        principalTable: "Recipe",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecipeMaterialRequirement",
                schema: "dbo",
                columns: table => new
                {
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequiredQuantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeMaterialRequirement", x => new { x.RecipeId, x.MaterialId });
                    table.ForeignKey(
                        name: "FK_RecipeMaterialRequirement_Material",
                        column: x => x.MaterialId,
                        principalSchema: "dbo",
                        principalTable: "Material",
                        principalColumn: "MaterialId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecipeMaterialRequirement_Recipe",
                        column: x => x.RecipeId,
                        principalSchema: "dbo",
                        principalTable: "Recipe",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RecipeStep",
                schema: "dbo",
                columns: table => new
                {
                    RecipeStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RequiresSignature = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeStep", x => x.RecipeStepId);
                    table.ForeignKey(
                        name: "FK_RecipeStep_Recipe",
                        column: x => x.RecipeId,
                        principalSchema: "dbo",
                        principalTable: "Recipe",
                        principalColumn: "RecipeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Batch",
                schema: "dbo",
                columns: table => new
                {
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchNumber = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    FinalDisposition = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Batch", x => x.BatchId);
                    table.ForeignKey(
                        name: "FK_Batch_ProductionOrder",
                        column: x => x.OrderId,
                        principalSchema: "dbo",
                        principalTable: "ProductionOrder",
                        principalColumn: "OrderId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvent",
                schema: "dbo",
                columns: table => new
                {
                    AuditEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AffectedEntity = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    AffectedEntityId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ResultingState = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvent", x => x.AuditEventId);
                    table.ForeignKey(
                        name: "FK_AuditEvent_AppUser",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AuditEvent_Batch",
                        column: x => x.BatchId,
                        principalSchema: "dbo",
                        principalTable: "Batch",
                        principalColumn: "BatchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BatchStepExecution",
                schema: "dbo",
                columns: table => new
                {
                    StepExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RecipeStepId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true),
                    CompletedByUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RecordedData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchStepExecution", x => x.StepExecutionId);
                    table.ForeignKey(
                        name: "FK_BatchStepExecution_AppUser",
                        column: x => x.CompletedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchStepExecution_Batch",
                        column: x => x.BatchId,
                        principalSchema: "dbo",
                        principalTable: "Batch",
                        principalColumn: "BatchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchStepExecution_RecipeStep",
                        column: x => x.RecipeStepId,
                        principalSchema: "dbo",
                        principalTable: "RecipeStep",
                        principalColumn: "RecipeStepId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ElectronicSignature",
                schema: "dbo",
                columns: table => new
                {
                    SignatureId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedRecordType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RelatedRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Meaning = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectronicSignature", x => x.SignatureId);
                    table.ForeignKey(
                        name: "FK_ElectronicSignature_AppUser",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ElectronicSignature_Batch",
                        column: x => x.BatchId,
                        principalSchema: "dbo",
                        principalTable: "Batch",
                        principalColumn: "BatchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EquipmentVerification",
                schema: "dbo",
                columns: table => new
                {
                    VerificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EquipmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsReady = table.Column<bool>(type: "bit", nullable: false),
                    CalibrationValid = table.Column<bool>(type: "bit", nullable: true),
                    VerifiedByUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EquipmentVerification", x => x.VerificationId);
                    table.ForeignKey(
                        name: "FK_EquipmentVerification_AppUser",
                        column: x => x.VerifiedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentVerification_Batch",
                        column: x => x.BatchId,
                        principalSchema: "dbo",
                        principalTable: "Batch",
                        principalColumn: "BatchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EquipmentVerification_Equipment",
                        column: x => x.EquipmentId,
                        principalSchema: "dbo",
                        principalTable: "Equipment",
                        principalColumn: "EquipmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MaterialVerification",
                schema: "dbo",
                columns: table => new
                {
                    VerificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LotNumber = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    IsAccepted = table.Column<bool>(type: "bit", nullable: false),
                    VerifiedByUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaterialVerification", x => x.VerificationId);
                    table.ForeignKey(
                        name: "FK_MaterialVerification_AppUser",
                        column: x => x.VerifiedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialVerification_Batch",
                        column: x => x.BatchId,
                        principalSchema: "dbo",
                        principalTable: "Batch",
                        principalColumn: "BatchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaterialVerification_Material",
                        column: x => x.MaterialId,
                        principalSchema: "dbo",
                        principalTable: "Material",
                        principalColumn: "MaterialId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Deviation",
                schema: "dbo",
                columns: table => new
                {
                    DeviationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviationCode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    BatchId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepExecutionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ReportedByUserId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OpenedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: false),
                    ReviewComments = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FinalDisposition = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deviation", x => x.DeviationId);
                    table.ForeignKey(
                        name: "FK_Deviation_AppUser",
                        column: x => x.ReportedByUserId,
                        principalSchema: "dbo",
                        principalTable: "AppUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deviation_Batch",
                        column: x => x.BatchId,
                        principalSchema: "dbo",
                        principalTable: "Batch",
                        principalColumn: "BatchId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Deviation_BatchStepExecution",
                        column: x => x.StepExecutionId,
                        principalSchema: "dbo",
                        principalTable: "BatchStepExecution",
                        principalColumn: "StepExecutionId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UQ_AppUser_UserName",
                schema: "dbo",
                table: "AppUser",
                column: "UserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvent_Batch_Time",
                schema: "dbo",
                table: "AuditEvent",
                columns: new[] { "BatchId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvent_User",
                schema: "dbo",
                table: "AuditEvent",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Batch_ProductionOrder",
                schema: "dbo",
                table: "Batch",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Batch_Status",
                schema: "dbo",
                table: "Batch",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UQ_Batch_BatchNumber",
                schema: "dbo",
                table: "Batch",
                column: "BatchNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BatchStepExecution_CompletedByUser",
                schema: "dbo",
                table: "BatchStepExecution",
                column: "CompletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchStepExecution_RecipeStep",
                schema: "dbo",
                table: "BatchStepExecution",
                column: "RecipeStepId");

            migrationBuilder.CreateIndex(
                name: "UQ_BatchStepExecution_BatchId_StepNumber",
                schema: "dbo",
                table: "BatchStepExecution",
                columns: new[] { "BatchId", "StepNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Deviation_Batch_Status",
                schema: "dbo",
                table: "Deviation",
                columns: new[] { "BatchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Deviation_ReportedByUser",
                schema: "dbo",
                table: "Deviation",
                column: "ReportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Deviation_StepExecution",
                schema: "dbo",
                table: "Deviation",
                column: "StepExecutionId");

            migrationBuilder.CreateIndex(
                name: "UQ_Deviation_DeviationCode",
                schema: "dbo",
                table: "Deviation",
                column: "DeviationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicSignature_Batch",
                schema: "dbo",
                table: "ElectronicSignature",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicSignature_User",
                schema: "dbo",
                table: "ElectronicSignature",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UQ_Equipment_EquipmentCode",
                schema: "dbo",
                table: "Equipment",
                column: "EquipmentCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentVerification_Equipment",
                schema: "dbo",
                table: "EquipmentVerification",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_EquipmentVerification_VerifiedByUser",
                schema: "dbo",
                table: "EquipmentVerification",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_EquipmentVerification_BatchId_EquipmentId",
                schema: "dbo",
                table: "EquipmentVerification",
                columns: new[] { "BatchId", "EquipmentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Material_MaterialCode",
                schema: "dbo",
                table: "Material",
                column: "MaterialCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVerification_Material",
                schema: "dbo",
                table: "MaterialVerification",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialVerification_VerifiedByUser",
                schema: "dbo",
                table: "MaterialVerification",
                column: "VerifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "UQ_MaterialVerification_BatchId_MaterialId",
                schema: "dbo",
                table: "MaterialVerification",
                columns: new[] { "BatchId", "MaterialId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrder_Recipe",
                schema: "dbo",
                table: "ProductionOrder",
                column: "RecipeId");

            migrationBuilder.CreateIndex(
                name: "UQ_ProductionOrder_OrderNumber",
                schema: "dbo",
                table: "ProductionOrder",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Recipe_RecipeCode",
                schema: "dbo",
                table: "Recipe",
                column: "RecipeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecipeEquipmentRequirement_Equipment",
                schema: "dbo",
                table: "RecipeEquipmentRequirement",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_RecipeMaterialRequirement_Material",
                schema: "dbo",
                table: "RecipeMaterialRequirement",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "UQ_RecipeStep_RecipeId_StepNumber",
                schema: "dbo",
                table: "RecipeStep",
                columns: new[] { "RecipeId", "StepNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEvent",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Deviation",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ElectronicSignature",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "EquipmentVerification",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MaterialVerification",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RecipeEquipmentRequirement",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RecipeMaterialRequirement",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "BatchStepExecution",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Equipment",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Material",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AppUser",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Batch",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "RecipeStep",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "ProductionOrder",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Recipe",
                schema: "dbo");
        }
    }
}
