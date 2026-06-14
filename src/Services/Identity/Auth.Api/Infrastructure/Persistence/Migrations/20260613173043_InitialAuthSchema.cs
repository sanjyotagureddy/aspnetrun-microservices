using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuthSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auth_login_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    nonce = table.Column<string>(type: "text", nullable: false),
                    redirect_uri = table.Column<string>(type: "text", nullable: false),
                    code_challenge = table.Column<string>(type: "text", nullable: false),
                    code_challenge_method = table.Column<string>(type: "text", nullable: false),
                    tenant_hint = table.Column<string>(type: "text", nullable: true),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    authorization_code = table.Column<string>(type: "text", nullable: true),
                    exchange_completed_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_login_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "auth_refresh_token_grants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    parent_token_hash = table.Column<string>(type: "text", nullable: true),
                    subject = table.Column<string>(type: "text", nullable: true),
                    issued_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consumed_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revoked_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_refresh_token_grants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "auth_token_operations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_type = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: true),
                    idempotency_key = table.Column<string>(type: "text", nullable: true),
                    refresh_token_hash = table.Column<string>(type: "text", nullable: true),
                    access_token_jti = table.Column<string>(type: "text", nullable: true),
                    session_id = table.Column<string>(type: "text", nullable: true),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_token_operations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "auth_user_tenant_memberships",
                columns: table => new
                {
                    subject = table.Column<string>(type: "text", nullable: false),
                    tenant_id = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    created_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_auth_user_tenant_memberships", x => new { x.subject, x.tenant_id, x.role });
                });

            migrationBuilder.CreateIndex(
                name: "IX_auth_login_transactions_state",
                table: "auth_login_transactions",
                column: "state",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_refresh_token_grants_family_id",
                table: "auth_refresh_token_grants",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "IX_auth_refresh_token_grants_token_hash",
                table: "auth_refresh_token_grants",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_auth_token_operations_operation_type_idempotency_key",
                table: "auth_token_operations",
                columns: new[] { "operation_type", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auth_login_transactions");

            migrationBuilder.DropTable(
                name: "auth_refresh_token_grants");

            migrationBuilder.DropTable(
                name: "auth_token_operations");

            migrationBuilder.DropTable(
                name: "auth_user_tenant_memberships");
        }
    }
}
