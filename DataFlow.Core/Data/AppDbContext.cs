using DataFlow.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DataFlow.Core.Data
{
    public class AppDbContext : DbContext
    {
        
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            
        }

        public DbSet<ConfigTemplate> ConfigTemplates { get; set; } = null!;
        public DbSet<ConfigColumn> ConfigColumns { get; set; } = null!;
        public DbSet<ColumnRange> Ranges { get; set; } = null!;
        public DbSet<Parametro> Parametros { get; set; } = null!;
        public DbSet<HistProcess> HistProcesses { get; set; } = null!;
        public DbSet<DataTypeLookup> DataTypeLookups { get; set; } = null!;
        public DbSet<ColumnTypeLookup> ColumnTypeLookups { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ConfigTemplate>().ToTable("config_templates");
            modelBuilder.Entity<ConfigColumn>().ToTable("config_columns");
            modelBuilder.Entity<ColumnRange>().ToTable("ranges");
            modelBuilder.Entity<HistProcess>().ToTable("hist_process");
            modelBuilder.Entity<Parametro>().ToTable("parametros");
            modelBuilder.Entity<ColumnTypeLookup>().ToTable("column_types_lookup");
            modelBuilder.Entity<DataTypeLookup>().ToTable("data_types_lookup");

            modelBuilder.Entity<DataTypeLookup>().HasKey(d => d.Id);
            modelBuilder.Entity<ColumnTypeLookup>().HasKey(c => c.Id);


            // Configuracion de relaciones entre entidades
            modelBuilder.Entity<ConfigTemplate>()
                .HasMany(ct => ct.ConfigColumns)
                .WithOne(cc => cc.ConfigTemplate)
                .HasForeignKey(cc => cc.ConfigTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ConfigTemplate>()
                .HasMany(ct => ct.HistProcess)
                .WithOne(hp => hp.ConfigTemplate)
                .HasForeignKey(hp => hp.ConfigTemplateId)
                .OnDelete(DeleteBehavior.Cascade);
            
            modelBuilder.Entity<ConfigColumn>()
                .HasMany(cc => cc.Ranges)
                .WithOne(cr => cr.ConfigColumn)
                .HasForeignKey(cr => cr.ConfigColumnId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConfigColumn>()
                .HasOne(c => c.ColumnType)
                .WithMany()
                .HasForeignKey(c => c.ColumnTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConfigColumn>()
                .HasOne(c => c.DataType)
                .WithMany()
                .HasForeignKey(c => c.DataTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            base.OnModelCreating(modelBuilder);
        }



    }
}
