using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace ProductRequests.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "varchar(320)", maxLength: 320, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false),
                    Role = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ProductRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ClientId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProductName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "char(3)", nullable: false),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    AcceptedOfferId = table.Column<Guid>(type: "char(36)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Version = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductRequests_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Offers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProductRequestId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProviderId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProposedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CounterAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AgreedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DeliveryDays = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Version = table.Column<Guid>(type: "char(36)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Offers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Offers_ProductRequests_ProductRequestId",
                        column: x => x.ProductRequestId,
                        principalTable: "ProductRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Offers_Users_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OfferHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false),
                    OfferId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ProductRequestId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ActorId = table.Column<Guid>(type: "char(36)", nullable: false),
                    ActorRole = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    Action = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: false),
                    PreviousStatus = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    NewStatus = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Comment = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OfferHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfferHistories_Offers_OfferId",
                        column: x => x.OfferId,
                        principalTable: "Offers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfferHistories_ProductRequests_ProductRequestId",
                        column: x => x.ProductRequestId,
                        principalTable: "ProductRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OfferHistories_Users_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_OfferHistories_ActorId",
                table: "OfferHistories",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_OfferHistories_OfferId_OccurredAt",
                table: "OfferHistories",
                columns: new[] { "OfferId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OfferHistories_ProductRequestId",
                table: "OfferHistories",
                column: "ProductRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ProductRequestId",
                table: "Offers",
                column: "ProductRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ProviderId",
                table: "Offers",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "UX_Offers_ProductRequestId_ProviderId",
                table: "Offers",
                columns: new[] { "ProductRequestId", "ProviderId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequests_ClientId",
                table: "ProductRequests",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductRequests_Status",
                table: "ProductRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "UX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.Sql("ALTER TABLE Users ENGINE=InnoDB;");
            migrationBuilder.Sql("ALTER TABLE ProductRequests ENGINE=InnoDB;");
            migrationBuilder.Sql("ALTER TABLE Offers ENGINE=InnoDB;");
            migrationBuilder.Sql("ALTER TABLE OfferHistories ENGINE=InnoDB;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OfferHistories");

            migrationBuilder.DropTable(
                name: "Offers");

            migrationBuilder.DropTable(
                name: "ProductRequests");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
