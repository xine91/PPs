using Microsoft.EntityFrameworkCore;
using topfact.Pulse.Models;

namespace topfact.Pulse.Data
{
   
    public class AnalyticsDbContext : DbContext
    {
        public AnalyticsDbContext(DbContextOptions<AnalyticsDbContext> options)
            : base(options) { }

        public DbSet<ClientStartupInformation> ClientStartupInformations => Set<ClientStartupInformation>();
        public DbSet<FormRecStatus> FormRecStatuses => Set<FormRecStatus>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ClientStartupInformation>(entity =>
            {
                // Tabelle liegt im dbo-Schema der topfactAnalytics-DB
                entity.ToTable("ClientStartupInformation", "dbo");

                // Falls die Tabelle keinen klassischen PK hat: Id als HasNoKey() –
                // dann die Zeile unten einkommentieren und [Key] / Id-Property entfernen.
                // entity.HasNoKey();

                entity.Property(e => e.Customer).HasMaxLength(255);
                entity.Property(e => e.Clientname).HasMaxLength(255);
                entity.Property(e => e.Username).HasMaxLength(255);
                entity.Property(e => e.Domainname).HasMaxLength(255);
                entity.Property(e => e.AppName).HasMaxLength(255);
                entity.Property(e => e.AppVersion).HasMaxLength(100);
            });

            modelBuilder.Entity<FormRecStatus>(entity =>
            {
                entity.HasNoKey();
                entity.ToView("FormRec_STATUS", "dbo");
            });
        }
    }
}