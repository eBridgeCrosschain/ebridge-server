using AElf.CrossChainServer.BridgeContract;
using AElf.CrossChainServer.Chains;
using AElf.CrossChainServer.CrossChain;
using AElf.CrossChainServer.TokenAccess;
using AElf.CrossChainServer.TokenPool;
using AElf.CrossChainServer.Tokens;
using Microsoft.EntityFrameworkCore;
using Volo.Abp.AuditLogging.EntityFrameworkCore;
using Volo.Abp.BackgroundJobs.EntityFrameworkCore;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EntityFrameworkCore;
using Volo.Abp.EntityFrameworkCore.Modeling;
using Volo.Abp.FeatureManagement.EntityFrameworkCore;
using Volo.Abp.Identity;
using Volo.Abp.Identity.EntityFrameworkCore;
using Volo.Abp.OpenIddict.EntityFrameworkCore;
using Volo.Abp.PermissionManagement.EntityFrameworkCore;
using Volo.Abp.SettingManagement.EntityFrameworkCore;
using Volo.Abp.TenantManagement;
using Volo.Abp.TenantManagement.EntityFrameworkCore;

namespace AElf.CrossChainServer.EntityFrameworkCore;

[ReplaceDbContext(typeof(IIdentityDbContext))]
[ReplaceDbContext(typeof(ITenantManagementDbContext))]
[ConnectionStringName("Default")]
public class CrossChainServerDbContext :
    AbpDbContext<CrossChainServerDbContext>,
    IIdentityDbContext,
    ITenantManagementDbContext
{
    /* Add DbSet properties for your Aggregate Roots / Entities here. */

    #region Entities from the modules

    /* Notice: We only implemented IIdentityDbContext and ITenantManagementDbContext
     * and replaced them for this DbContext. This allows you to perform JOIN
     * queries for the entities of these modules over the repositories easily. You
     * typically don't need that for other modules. But, if you need, you can
     * implement the DbContext interface of the needed module and use ReplaceDbContext
     * attribute just like IIdentityDbContext and ITenantManagementDbContext.
     *
     * More info: Replacing a DbContext of a module ensures that the related module
     * uses this DbContext on runtime. Otherwise, it will use its own DbContext class.
     */

    // Identity
    public DbSet<IdentityUser> Users { get; set; }
    public DbSet<IdentityRole> Roles { get; set; }
    public DbSet<IdentityClaimType> ClaimTypes { get; set; }
    public DbSet<OrganizationUnit> OrganizationUnits { get; set; }
    public DbSet<IdentitySecurityLog> SecurityLogs { get; set; }
    public DbSet<IdentityLinkUser> LinkUsers { get; set; }
    public DbSet<IdentityUserDelegation> UserDelegations { get; set; }
    public DbSet<IdentitySession> Sessions { get; set; }

    // Tenant Management
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantConnectionString> TenantConnectionStrings { get; set; }

    #endregion

    public DbSet<Chain> Chains { get; set; }
    public DbSet<Token> Tokens { get; set; }
    public DbSet<CrossChainIndexingInfo> CrossChainIndexingInfos { get; set; }
    public DbSet<CrossChainTransfer> CrossChainTransfers { get; set; }
    public DbSet<WalletUserDto> WalletUsers { get; set; }
    public DbSet<BridgeContractSyncInfo> BridgeContractSyncInfos { get; set; }
    public DbSet<OracleQueryInfo> OracleQueryInfos { get; set; }
    public DbSet<ReportInfo> ReportInfos { get; set; }
    public DbSet<Settings.Settings> Settings { get; set; }
    public DbSet<CrossChainDailyLimit> CrossChainDailyLimits { get; set; }
    public DbSet<CrossChainRateLimit> CrossChainRateLimits { get; set; }
    public DbSet<PoolLiquidityInfo> PoolLiquidityInfos { get; set; }
    public DbSet<UserLiquidityInfo> UserLiquidityInfos { get; set; }
    public DbSet<UserTokenAccessInfo> TokenAccessInfos { get; set; }
    public DbSet<UserTokenOwnerDto> UserTokenOwners { get; set; }
    public DbSet<TokenApplyOrder> TokenApplyOrders { get; set; }
    public DbSet<ChainTokenInfo> ChainTokenInfos { get; set; }
    public DbSet<StatusChangedRecord> StatusChangedRecords { get; set; }
    public DbSet<UserTokenAccessInfo> UserTokenAccessInfos { get; set; }
    public DbSet<UserTokenIssueDto> UserTokenIssues { get; set; }

    public CrossChainServerDbContext(DbContextOptions<CrossChainServerDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        /* Include modules to your migration db context */

        builder.ConfigurePermissionManagement();
        builder.ConfigureSettingManagement();
        builder.ConfigureBackgroundJobs();
        builder.ConfigureAuditLogging();
        builder.ConfigureFeatureManagement();
        builder.ConfigureIdentity();
        builder.ConfigureOpenIddict();
        builder.ConfigureTenantManagement();

        /* Configure your own tables/entities inside here */

        builder.Entity<Chain>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "Chains", CrossChainServerConsts.DbSchema);
            b.ConfigureByConvention();
        });

        builder.Entity<Token>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "Tokens", CrossChainServerConsts.DbSchema);
            b.ConfigureByConvention();
        });

        builder.Entity<CrossChainIndexingInfo>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "CrossChainIndexingInfos",
                CrossChainServerConsts.DbSchema);
            b.HasIndex(o => o.BlockTime);
            b.ConfigureByConvention();
        });

        builder.Entity<CrossChainTransfer>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "CrossChainTransfers", CrossChainServerConsts.DbSchema);
            b.HasIndex(o => new { o.FromChainId, o.ToChainId, o.TransferTransactionId }).IsUnique();
            b.HasIndex(o => new { o.FromChainId, o.ToChainId, o.InlineTransferTransactionId });
            b.HasIndex(o => new { o.FromChainId, o.ToChainId, o.ReceiptId });
            b.HasIndex(o => new { o.Status, o.ProgressUpdateTime });
            b.ConfigureByConvention();
        });

        builder.Entity<WalletUserDto>(entity =>
        {
            entity.ToTable(CrossChainServerConsts.DbTablePrefix + "WalletUsers", CrossChainServerConsts.DbSchema);
            entity.HasKey(e => e.UserId);
            entity.OwnsMany(e => e.AddressInfos);
        });

        builder.Entity<BridgeContractSyncInfo>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "BridgeContractSyncInfos",
                CrossChainServerConsts.DbSchema);
            b.ConfigureByConvention();
        });

        builder.Entity<OracleQueryInfo>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "OracleQueryInfos", CrossChainServerConsts.DbSchema);
            b.HasIndex(o => new { o.ChainId, o.QueryId });
            b.ConfigureByConvention();
        });

        builder.Entity<ReportInfo>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "ReportInfos", CrossChainServerConsts.DbSchema);
            b.HasIndex(o => new { o.ChainId, o.RoundId, o.Token, o.TargetChainId });
            b.ConfigureByConvention();
        });

        builder.Entity<Settings.Settings>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "Settings", CrossChainServerConsts.DbSchema);
            b.HasIndex(o => new { o.ChainId, o.Name });
            b.ConfigureByConvention();
        });

        // builder.Entity<UserTokenAccessInfo>(b =>
        // {
        //     b.ToTable(CrossChainServerConsts.DbTablePrefix + "UserTokenAccessInfo", CrossChainServerConsts.DbSchema);
        //     b.HasIndex(o => new { o.Symbol }).IsUnique();
        //     b.HasIndex(o => new { o.Address });
        //     b.ConfigureByConvention();
        // });
        builder.Entity<UserTokenAccessInfo>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "UserTokenAccessInfo", CrossChainServerConsts.DbSchema);
            b.HasIndex(o => new { o.Symbol }).IsUnique();
            b.HasIndex(o => new { o.Address });
            b.ConfigureByConvention();
        });

        builder.Entity<TokenApplyOrder>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "TokenApplyOrder", CrossChainServerConsts.DbSchema);
            b.ConfigureByConvention();
            b.HasIndex(o => new { o.UserAddress, o.Symbol });
            b.Ignore(o => o.ExtensionInfo);
            b.Ignore(o => o.StatusChangedRecord);
            b.Ignore(o => o.OtherChainTokenInfo);
            // b.OwnsMany(o => o.ChainTokenInfo);
            //Define the relation
            b.HasMany(x => x.ChainTokenInfo)
                .WithOne(x => x.Order)
                .HasForeignKey(x => x.Id)
                .IsRequired();
            // b.HasMany(x => x.StatusChangedRecords)
            //     .WithOne(x => x.Order)
            //     .HasForeignKey(x => x.Id)
            //     .IsRequired();
        });
        
        builder.Entity<ChainTokenInfo>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "ApplyOrderChainTokenInfo", CrossChainServerConsts.DbSchema);
            b.ConfigureByConvention();
        });
        
        builder.Entity<StatusChangedRecord>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "ApplyOrderStatusChangedRecord", CrossChainServerConsts.DbSchema);
            b.ConfigureByConvention();
        });

        builder.Entity<PoolLiquidityInfo>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "PoolLiquidityInfos", CrossChainServerConsts.DbSchema);
            b.HasIndex(o => new { o.ChainId, o.TokenId }).IsUnique();
            b.ConfigureByConvention();
        });

        builder.Entity<UserLiquidityInfo>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "UserLiquidityInfos", CrossChainServerConsts.DbSchema);
            b.HasIndex(o => new { o.ChainId, o.TokenId, o.Provider }).IsUnique();
            b.ConfigureByConvention();
        });

        builder.Entity<UserTokenOwnerDto>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "UserTokenOwners", CrossChainServerConsts.DbSchema);
            b.HasIndex(o => new { o.Address });
            b.OwnsMany(e => e.TokenOwnerList);
            b.ConfigureByConvention();
        });
        
        builder.Entity<CrossChainDailyLimit>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "CrossChainDailyLimits", CrossChainServerConsts.DbSchema);
            b.ConfigureByConvention(); 
        });
        
        builder.Entity<CrossChainRateLimit>(b =>
        {
            b.ToTable(CrossChainServerConsts.DbTablePrefix + "CrossChainRateLimits", CrossChainServerConsts.DbSchema);
            b.ConfigureByConvention(); 
        });
    }
}
