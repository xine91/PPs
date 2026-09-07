using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Models;

namespace topfact.Pulse.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        // ---- Audit & Logs
        public DbSet<PulseLogin> PulseLogins { get; set; }

        // ---- Navigation (neue Struktur)
        public DbSet<PulseBereich> PulseBereiche { get; set; }
        public DbSet<PulseGruppe> PulseGruppen { get; set; }
        public DbSet<PulseKategorie> PulseKategorien { get; set; }

        // ---- KPI
        public DbSet<PulseKpiDefinition> PulseKpiDefinitions { get; set; }

        // ---- Access Control
        public DbSet<PulseAccess> PulseAccess { get; set; }

        public DbSet<PulseConnector> PulseConnectors { get; set; }
        public DbSet<PulseQueryResult2> PulseQueryResults2 { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ========================================================================
            // PULSE_QUERYRESULTS2 - Query-Ergebnisse
            // ========================================================================
            modelBuilder.Entity<PulseQueryResult2>(entity =>
            {
                entity.ToTable("Pulse_QueryResults2", "dbo");
                entity.HasKey(x => x.QueryID);
                entity.Property(x => x.QueryID).HasColumnName("QueryID").ValueGeneratedOnAdd();
                entity.Property(x => x.KategorieID).HasColumnName("KategorieID").IsRequired(false);
                entity.Property(x => x.Sql_query).HasColumnName("Sql_query").IsRequired();
                entity.Property(x => x.Daten).HasColumnName("Daten").IsRequired(false);
                entity.Property(x => x.created_at).HasColumnName("created_at").HasDefaultValueSql("GETUTCDATE()");
                entity.HasOne(x => x.Kategorie)
                    .WithMany()
                    .HasForeignKey(x => x.KategorieID)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                entity.HasIndex(x => x.KategorieID).IsUnique(false);
            });


            // ========================================================================
            // PULSE_QUERYRESULTS2 - Query-Ergebnisse
            // ========================================================================
            modelBuilder.Entity<PulseQueryResult2>(entity =>
            {
                entity.ToTable("Pulse_QueryResults2", "dbo");
                entity.HasKey(x => x.QueryID);
                entity.Property(x => x.QueryID).HasColumnName("QueryID").ValueGeneratedOnAdd();
                entity.Property(x => x.KategorieID).HasColumnName("KategorieID").IsRequired(false);
                entity.Property(x => x.Sql_query).HasColumnName("Sql_query").IsRequired();
                entity.Property(x => x.Daten).HasColumnName("Daten").IsRequired(false);
                entity.Property(x => x.created_at).HasColumnName("created_at").HasDefaultValueSql("GETUTCDATE()");
                entity.HasOne(x => x.Kategorie)
                    .WithMany()
                    .HasForeignKey(x => x.KategorieID)
                    .OnDelete(DeleteBehavior.SetNull)
                    .IsRequired(false);
                entity.HasIndex(x => x.KategorieID).IsUnique(false);
            });
            // ========================================================================
            // PULSE_LOGINS -- Login-Audit
            // ========================================================================
            modelBuilder.Entity<PulseLogin>(entity =>
            {
                entity.ToTable("Pulse_Logins", "dbo");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Username).HasColumnName("user_name").HasMaxLength(200).IsRequired();
                entity.Property(x => x.LoginTime).HasColumnName("login_time");
                entity.Property(x => x.Success).HasColumnName("success");
                entity.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(50);

                // Indexes
                entity.HasIndex(x => new { x.Username, x.LoginTime }).IsUnique(false);
                entity.HasIndex(x => x.Success).IsUnique(false);
            });

            // ========================================================================
            // PULSE_BEREICH -- Hauptbereiche
            // ========================================================================
            modelBuilder.Entity<PulseBereich>(entity =>
            {
                entity.ToTable("Pulse_Bereich", "dbo");
                entity.HasKey(x => x.BereichID);
                entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
                entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
                entity.Property(x => x.RequiredRole).HasMaxLength(100);

                // Indexes
                entity.HasIndex(x => x.Code).IsUnique();
                entity.HasIndex(x => x.IsActive).IsUnique(false);
            });

            // ========================================================================
            // PULSE_GRUPPE -- Kategoriegruppen
            // ========================================================================
            modelBuilder.Entity<PulseGruppe>(entity =>
            {
                entity.ToTable("Pulse_Gruppe", "dbo");
                entity.HasKey(x => x.GruppeID);
                entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
                entity.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();

                // Foreign Keys
                entity.HasOne(x => x.Bereich)
                    .WithMany(x => x.Gruppen)
                    .HasForeignKey(x => x.BereichID)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                // Indexes
                entity.HasIndex(x => new { x.BereichID, x.Code }).IsUnique();
                entity.HasIndex(x => x.IsActive).IsUnique(false);
            });

            // ========================================================================
            // PULSE_KATEGORIE -- Navigationspunkte
            // ========================================================================
            modelBuilder.Entity<PulseKategorie>(entity =>
            {
                entity.ToTable("Pulse_Kategorie", "dbo");
                entity.HasKey(x => x.KategorieID);
                entity.Property(x => x.Title).HasMaxLength(120).IsRequired();
                entity.Property(x => x.IconCss).HasMaxLength(80);
                entity.Property(x => x.RouteBereich).HasMaxLength(50);
                entity.Property(x => x.BadgeText).HasMaxLength(40);
                entity.Property(x => x.Sql_query).HasMaxLength(2000);

                // Foreign Keys
                entity.HasOne(x => x.Gruppe)
                    .WithMany(x => x.Kategorien)
                    .HasForeignKey(x => x.GruppeID)
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                // Indexes
                entity.HasIndex(x => new { x.GruppeID, x.Title }).IsUnique();
                entity.HasIndex(x => x.IsActive).IsUnique(false);
            });

            // ========================================================================
            // ========================================================================
            // PULSE_KPIDEFINITION -- KPI-Definitionen
            // ========================================================================
            modelBuilder.Entity<PulseKpiDefinition>(entity =>
            {
                entity.ToTable("Pulse_KpiDefinition", "dbo");
                entity.HasKey(x => x.KpiDefinitionID);
                entity.Property(x => x.Title).HasMaxLength(150).IsRequired();
                entity.Property(x => x.Description).HasMaxLength(500);
                entity.Property(x => x.IconCss).HasMaxLength(80).HasDefaultValue("ri-bar-chart-line");
                entity.Property(x => x.Color).HasMaxLength(50).HasDefaultValue("#338562");
                entity.Property(x => x.Bereich).HasMaxLength(100);
                entity.Property(x => x.QuerySql).HasMaxLength(2000);
                entity.Property(x => x.Unit).HasMaxLength(30);
                entity.Property(x => x.UpdatedBy).HasMaxLength(150);
                entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                // FK zu Pulse_Kategorie (optional)
                entity.HasOne(x => x.Kategorie)
                    .WithMany()
                    .HasForeignKey(x => x.KategorieID)
                    .OnDelete(DeleteBehavior.SetNull);

                // FK zu Pulse_ConnectorManager (optional)
                entity.HasOne<topfact.Pulse.Models.PulseConnector>()
                    .WithMany()
                    .HasForeignKey(x => x.ConnectorID)
                    .OnDelete(DeleteBehavior.SetNull);

                // Indexes
                entity.HasIndex(x => x.IsActive).IsUnique(false);
                entity.HasIndex(x => x.Bereich).IsUnique(false);
                entity.HasIndex(x => x.KategorieID).IsUnique(false);
                entity.HasIndex(x => x.ConnectorID).IsUnique(false);
            });

            // ========================================================================
            // PULSE_ACCESS -- Benutzer-Rechte
            // ========================================================================
            modelBuilder.Entity<PulseAccess>(entity =>
            {
                entity.ToTable("Pulse_Access", "dbo");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Username)
                    .HasColumnName("user_name")
                    .HasMaxLength(200)
                    .IsRequired();
                entity.Property(x => x.Permission).HasMaxLength(50).HasDefaultValue("View");
                entity.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(x => x.UpdatedBy).HasMaxLength(150);

                // Foreign Keys
                entity.HasOne(x => x.Bereich)
                    .WithMany(x => x.Accesses)
                    .HasForeignKey(x => x.BereichID)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(x => x.Gruppe)
                    .WithMany(x => x.Accesses)
                    .HasForeignKey(x => x.GruppeID)
                    .OnDelete(DeleteBehavior.Restrict);

                // Indexes
                entity.HasIndex(x => new { x.Username, x.IsActive }).IsUnique(false);
                entity.HasIndex(x => new { x.BereichID, x.GruppeID }).IsUnique(false);
            });

            // ========================================================================
            // CONNECTORS -- Connector Konfigurationen
            // ========================================================================
            modelBuilder.Entity<PulseConnector>(entity =>
            {
                entity.ToTable("Pulse_ConnectorManager", "dbo");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).HasColumnName("id");
                entity.Property(x => x.UserName).HasColumnName("user_name").HasMaxLength(150).IsRequired();
                entity.Property(x => x.ConnectorType).HasColumnName("connector_type").HasMaxLength(200);
                entity.Property(x => x.ApiConfig).HasColumnName("api_config");
                entity.Property(x => x.SqlConfig).HasColumnName("sql_config");
                entity.Property(x => x.ManualConfig).HasColumnName("manual_config");
                entity.Property(x => x.CreatedAt).HasColumnName("created_at");
                entity.Property(x => x.UpdatedAt).HasColumnName("updated_at");
                entity.Property(x => x.Bezeichnung).HasColumnName("bezeichnung").HasMaxLength(200);
                entity.Property(x => x.Status).HasColumnName("status").HasMaxLength(50);
                entity.Property(x => x.KategorieID).HasColumnName("KategorieID");
            });
        }
    }
}
