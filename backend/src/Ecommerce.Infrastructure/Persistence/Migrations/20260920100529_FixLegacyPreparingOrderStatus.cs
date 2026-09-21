using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ecommerce.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Data-only fix: the OrderStatus enum's Preparing member was removed when it was merged into
    /// ReadyToShip (see CLAUDE.md section 12 history / PROJECT_CONTEXT.md part 18), but OrderStatus is
    /// persisted as a plain string (no DB enum type / no FK), so no schema migration ran at the time and
    /// pre-existing 'Preparing' rows were left behind. Any row still recording that status throws
    /// InvalidOperationException the moment EF Core tries to materialize it into the enum. Rewrite those
    /// rows to ReadyToShip, the state Preparing was folded into.
    /// </summary>
    public partial class FixLegacyPreparingOrderStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE \"OrderStatusHistories\" SET \"NewStatus\" = 'ReadyToShip' WHERE \"NewStatus\" = 'Preparing';");
            migrationBuilder.Sql(
                "UPDATE \"OrderStatusHistories\" SET \"OldStatus\" = 'ReadyToShip' WHERE \"OldStatus\" = 'Preparing';");
            migrationBuilder.Sql(
                "UPDATE \"Orders\" SET \"Status\" = 'ReadyToShip' WHERE \"Status\" = 'Preparing';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible by design — the original rows had no reliable way to distinguish which
            // ReadyToShip rows used to be Preparing, and reintroducing a removed enum member is not a
            // real rollback path.
        }
    }
}
