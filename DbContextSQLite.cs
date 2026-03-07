using Microsoft.EntityFrameworkCore;
using Progetto_Web_2_IoT_Auth.Data.Model;

namespace Progetto_Web_2_IoT_Auth.Data
{
    public class DbContextSQLite : DbContext
    {
        public DbContextSQLite(DbContextOptions options) : base(options)
        {
        }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<Zone> Zones { get; set; }
        public DbSet<Access> Accesses { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<Automation> Automations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>(b =>
            {
                b.ToTable("users");
                b.HasKey(x => x.Id);

                b.Property(x => x.Username).IsRequired();
                b.Property(x => x.Mail).IsRequired();
                b.Property(x => x.PasswordHash).IsRequired();
                b.Property(x => x.Role).IsRequired();

                b.HasIndex(x => x.Username).IsUnique();
                b.HasIndex(x => x.Mail).IsUnique();
            });

            modelBuilder.Entity<Zone>(b =>
            {
                b.ToTable("zone");
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.Type).IsRequired();
            });

            modelBuilder.Entity<Access>(b =>
            {
                b.ToTable("access");
                b.HasKey(x => x.Id);
                b.Property(x => x.AccessLevel).IsRequired();

                b.HasOne(x => x.User)
                    .WithMany(u => u.Accesses)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Zone)
                    .WithMany(z => z.Accesses)
                    .HasForeignKey(x => x.ZoneId)
                    .OnDelete(DeleteBehavior.Cascade);

                // a user shouldn't have multiple access rows for the same zone
                b.HasIndex(x => new { x.UserId, x.ZoneId }).IsUnique();
            });

            modelBuilder.Entity<Device>(b =>
            {
                b.ToTable("device");
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.Property(x => x.Type).IsRequired();

                b.HasOne(x => x.Zone)
                    .WithMany(z => z.Devices)
                    .HasForeignKey(x => x.ZoneId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Automation>(b =>
            {
                b.ToTable("automation");
                b.HasKey(x => x.Id);
                b.Property(x => x.TimeCondition).IsRequired();
                b.Property(x => x.WeatherCondition).IsRequired();

                b.HasOne(x => x.Device)
                    .WithMany(d => d.Automations)
                    .HasForeignKey(x => x.DeviceId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Seed: a default zone + admin user + full access
            // Must be deterministic for EF migrations: BCrypt.HashPassword uses random salts.
            // Default admin password: "admin"
            const string seededAdminHash = "$2a$11$5bXqGaqh3uehFVuTEdfWLOfFUxE7KFIRYv/XOqmEgdon7oNxpVQxS";

            modelBuilder.Entity<Zone>().HasData(new Zone
            {
                Id = 1,
                Name = "default",
                Type = "default"
            });

            modelBuilder.Entity<AppUser>().HasData(new AppUser
            {
                Id = 1,
                Username = "admin",
                Mail = "admin@example.local",
                PasswordHash = seededAdminHash,
                Role = "admin"
            });

            modelBuilder.Entity<Access>().HasData(new
            {
                Id = 1,
                UserId = 1,
                ZoneId = 1,
                AccessLevel = "admin"
            });
        }
    }
}
