using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ChopChop.Api.Migrations
{
    /// <inheritdoc />
    public partial class Mechanics_RoomsTradingFarmingInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    EntityId = table.Column<string>(type: "TEXT", nullable: false),
                    DataJson = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rooms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    HostUserId = table.Column<string>(type: "TEXT", nullable: false),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rooms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Trades",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    RoomId = table.Column<string>(type: "TEXT", nullable: false),
                    InitiatorUserId = table.Column<string>(type: "TEXT", nullable: false),
                    ResponderUserId = table.Column<string>(type: "TEXT", nullable: false),
                    InitiatorCoins = table.Column<int>(type: "INTEGER", nullable: false),
                    ResponderCoins = table.Column<int>(type: "INTEGER", nullable: false),
                    InitiatorConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ResponderConfirmed = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FarmPlots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    RoomId = table.Column<string>(type: "TEXT", nullable: false),
                    OwnerUserId = table.Column<string>(type: "TEXT", nullable: false),
                    PosX = table.Column<float>(type: "REAL", nullable: false),
                    PosY = table.Column<float>(type: "REAL", nullable: false),
                    PosZ = table.Column<float>(type: "REAL", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    SeedItemId = table.Column<string>(type: "TEXT", nullable: true),
                    YieldItemId = table.Column<string>(type: "TEXT", nullable: true),
                    GrowSeconds = table.Column<int>(type: "INTEGER", nullable: false),
                    PlantedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    GrowthStartUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LastWateredUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WaterCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmPlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmPlots_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoomMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    RoomId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    IsConnected = table.Column<bool>(type: "INTEGER", nullable: false),
                    JoinedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LeftUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoomMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoomMembers_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TradeStakeItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TradeId = table.Column<string>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    ItemId = table.Column<string>(type: "TEXT", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeStakeItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeStakeItems_Trades_TradeId",
                        column: x => x.TradeId,
                        principalTable: "Trades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_Type_EntityId",
                table: "EventLogs",
                columns: new[] { "Type", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_UserId",
                table: "EventLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmPlots_OwnerUserId",
                table: "FarmPlots",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FarmPlots_RoomId",
                table: "FarmPlots",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_UserId_ItemId",
                table: "InventoryItems",
                columns: new[] { "UserId", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoomMembers_RoomId_UserId",
                table: "RoomMembers",
                columns: new[] { "RoomId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_InitiatorUserId_Status",
                table: "Trades",
                columns: new[] { "InitiatorUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_ResponderUserId_Status",
                table: "Trades",
                columns: new[] { "ResponderUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Trades_RoomId",
                table: "Trades",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeStakeItems_TradeId",
                table: "TradeStakeItems",
                column: "TradeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventLogs");

            migrationBuilder.DropTable(
                name: "FarmPlots");

            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropTable(
                name: "RoomMembers");

            migrationBuilder.DropTable(
                name: "TradeStakeItems");

            migrationBuilder.DropTable(
                name: "Rooms");

            migrationBuilder.DropTable(
                name: "Trades");
        }
    }
}
