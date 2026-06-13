using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Infrastructure.Persistence;

internal sealed class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<LoginTransaction> LoginTransactions => Set<LoginTransaction>();
    public DbSet<TokenOperation> TokenOperations => Set<TokenOperation>();
    public DbSet<RefreshTokenGrant> RefreshTokenGrants => Set<RefreshTokenGrant>();
    public DbSet<UserTenantMembership> UserTenantMemberships => Set<UserTenantMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LoginTransaction>(builder =>
        {
            builder.ToTable("auth_login_transactions");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.State).HasColumnName("state").IsRequired();
            builder.Property(x => x.Nonce).HasColumnName("nonce").IsRequired();
            builder.Property(x => x.RedirectUri).HasColumnName("redirect_uri").IsRequired();
            builder.Property(x => x.CodeChallenge).HasColumnName("code_challenge").IsRequired();
            builder.Property(x => x.CodeChallengeMethod).HasColumnName("code_challenge_method").IsRequired();
            builder.Property(x => x.TenantHint).HasColumnName("tenant_hint");
            builder.Property(x => x.CreatedUtc).HasColumnName("created_utc").IsRequired();
            builder.Property(x => x.ExpiresUtc).HasColumnName("expires_utc").IsRequired();
            builder.Property(x => x.AuthorizationCode).HasColumnName("authorization_code");
            builder.Property(x => x.ExchangeCompletedUtc).HasColumnName("exchange_completed_utc");

            builder.HasIndex(x => x.State).IsUnique();
        });

        modelBuilder.Entity<TokenOperation>(builder =>
        {
            builder.ToTable("auth_token_operations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.OperationType).HasColumnName("operation_type").IsRequired();
            builder.Property(x => x.Subject).HasColumnName("subject");
            builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key");
            builder.Property(x => x.RefreshTokenHash).HasColumnName("refresh_token_hash");
            builder.Property(x => x.AccessTokenJti).HasColumnName("access_token_jti");
            builder.Property(x => x.SessionId).HasColumnName("session_id");
            builder.Property(x => x.CreatedUtc).HasColumnName("created_utc").IsRequired();

            builder.HasIndex(x => x.IdempotencyKey);
        });

        modelBuilder.Entity<RefreshTokenGrant>(builder =>
        {
            builder.ToTable("auth_refresh_token_grants");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.FamilyId).HasColumnName("family_id").IsRequired();
            builder.Property(x => x.TokenHash).HasColumnName("token_hash").IsRequired();
            builder.Property(x => x.ParentTokenHash).HasColumnName("parent_token_hash");
            builder.Property(x => x.Subject).HasColumnName("subject");
            builder.Property(x => x.IssuedUtc).HasColumnName("issued_utc").IsRequired();
            builder.Property(x => x.ExpiresUtc).HasColumnName("expires_utc").IsRequired();
            builder.Property(x => x.ConsumedUtc).HasColumnName("consumed_utc");
            builder.Property(x => x.RevokedUtc).HasColumnName("revoked_utc");
            builder.Property(x => x.RevocationReason).HasColumnName("revocation_reason");

            builder.HasIndex(x => x.TokenHash).IsUnique();
            builder.HasIndex(x => x.FamilyId);
        });

        modelBuilder.Entity<UserTenantMembership>(builder =>
        {
            builder.ToTable("auth_user_tenant_memberships");
            builder.HasKey(x => new { x.Subject, x.TenantId, x.Role });

            builder.Property(x => x.Subject).HasColumnName("subject").IsRequired();
            builder.Property(x => x.TenantId).HasColumnName("tenant_id").IsRequired();
            builder.Property(x => x.Role).HasColumnName("role").IsRequired();
            builder.Property(x => x.CreatedUtc).HasColumnName("created_utc").IsRequired();
        });
    }
}
