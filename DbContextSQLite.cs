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
        public DbSet<DeviceType> DeviceTypes { get; set; }
        public DbSet<Automation> Automations { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }

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
                b.Property(x => x.DarkMode).IsRequired();

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
                b.Property(x => x.DeviceTypeId).IsRequired();
                b.Property(x => x.IpAddress);

                b.HasOne(x => x.Zone)
                    .WithMany(z => z.Devices)
                    .HasForeignKey(x => x.ZoneId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.DeviceType)
                    .WithMany(t => t.Devices)
                    .HasForeignKey(x => x.DeviceTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<DeviceType>(b =>
            {
                b.ToTable("device_type");
                b.HasKey(x => x.Id);
                b.Property(x => x.Name).IsRequired();
                b.HasIndex(x => x.Name).IsUnique();
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

            modelBuilder.Entity<AppSetting>(b =>
            {
                b.ToTable("app_setting");
                b.HasKey(x => x.Id);
                b.Property(x => x.Key).IsRequired();
                b.HasIndex(x => x.Key).IsUnique();
            });

            // Default admin password: "admin"
            const string seededAdminHash = "$2a$11$5bXqGaqh3uehFVuTEdfWLOfFUxE7KFIRYv/XOqmEgdon7oNxpVQxS";
            // Default user password: "user"
            const string seededUserHash = "$2a$12$vXc041cxsHJ7zMmuMHxjl.JpanScQdMOicmmzD74UErojYRCcHaoi";

            // --- Zones ---
            modelBuilder.Entity<Zone>().HasData(
                new Zone { Id = 1, Name = "default",     Type = "default"   },
                new Zone { Id = 2, Name = "Living Room", Type = "indoor"    },
                new Zone { Id = 3, Name = "Garden",      Type = "outdoor"   },
                new Zone { Id = 4, Name = "Bedroom",     Type = "indoor"    },
                new Zone { Id = 5, Name = "Garage",      Type = "outdoor"   }
            );

            // --- Users ---
            modelBuilder.Entity<AppUser>().HasData(
                new AppUser { Id = 1, Username = "admin",   Mail = "admin@example.local",   PasswordHash = seededAdminHash, Role = "admin", DarkMode = false },
                new AppUser { Id = 2, Username = "alice",   Mail = "alice@example.local",   PasswordHash = seededUserHash,  Role = "user",  DarkMode = false },
                new AppUser { Id = 3, Username = "bob",     Mail = "bob@example.local",     PasswordHash = seededUserHash,  Role = "user",  DarkMode = true  },
                new AppUser { Id = 4, Username = "charlie", Mail = "charlie@example.local", PasswordHash = seededUserHash,  Role = "user",  DarkMode = false }
            );

            // --- Device types ---
            modelBuilder.Entity<DeviceType>().HasData(
                new DeviceType { Id = 1, Name = "light"      },
                new DeviceType { Id = 2, Name = "sprinkler"  },
                new DeviceType { Id = 3, Name = "thermostat" }
            );

            // --- Devices ---
            modelBuilder.Entity<Device>().HasData(
                new Device { Id = 1, Name = "Main Light",       ZoneId = 2, DeviceTypeId = 1, IpAddress = "192.168.1.10", Power = true,  Level = 80 },
                new Device { Id = 2, Name = "TV Backlight",     ZoneId = 2, DeviceTypeId = 1, IpAddress = "192.168.1.11", Power = false, Level = 40 },
                new Device { Id = 3, Name = "Garden Sprinkler", ZoneId = 3, DeviceTypeId = 2, IpAddress = "192.168.1.20", Power = false, Level = 0  },
                new Device { Id = 4, Name = "Lawn Sprinkler",   ZoneId = 3, DeviceTypeId = 2, IpAddress = "192.168.1.21", Power = false, Level = 0  },
                new Device { Id = 5, Name = "Bedroom Light",    ZoneId = 4, DeviceTypeId = 1, IpAddress = "192.168.1.30", Power = false, Level = 60 },
                new Device { Id = 6, Name = "Thermostat",       ZoneId = 4, DeviceTypeId = 3, IpAddress = "192.168.1.31", Power = true,  Level = 21 },
                new Device { Id = 7, Name = "Garage Light",     ZoneId = 5, DeviceTypeId = 1, IpAddress = "192.168.1.40", Power = false, Level = 100}
            );

            // --- Access ---
            // admin gets full access to every zone
            modelBuilder.Entity<Access>().HasData(
                // alice: living room + garden (operator)
                new Access { Id = 1, UserId = 2, ZoneId = 2, AccessLevel = "operator" },
                new Access { Id = 2, UserId = 2, ZoneId = 3, AccessLevel = "operator" },
                // bob: bedroom only (view)
                new Access { Id = 3, UserId = 3, ZoneId = 4, AccessLevel = "view"  },
                // charlie: all zones (view)
                new Access { Id = 4, UserId = 4, ZoneId = 2, AccessLevel = "view"  },
                new Access { Id = 5, UserId = 4, ZoneId = 3, AccessLevel = "view"  },
                new Access { Id = 6, UserId = 4, ZoneId = 4, AccessLevel = "view"  },
                new Access { Id = 7, UserId = 4, ZoneId = 5, AccessLevel = "view"  }
            );

            // --- Automations ---
            modelBuilder.Entity<Automation>().HasData(
                new Automation { Id = 1, DeviceId = 1, Power = true,  Level = 100, TimeCondition = "18:00", WeatherCondition = "any"    },
                new Automation { Id = 2, DeviceId = 1, Power = false, Level = 0,   TimeCondition = "23:00", WeatherCondition = "any"    },
                new Automation { Id = 3, DeviceId = 3, Power = true,  Level = 80,  TimeCondition = "07:00", WeatherCondition = "sunny"  },
                new Automation { Id = 4, DeviceId = 3, Power = false, Level = 0,   TimeCondition = "09:00", WeatherCondition = "any"    },
                new Automation { Id = 5, DeviceId = 6, Power = true,  Level = 20,  TimeCondition = "22:00", WeatherCondition = "cold"   }
            );

            modelBuilder.Entity<AppSetting>().HasData(new AppSetting
            {
                Id = 1,
                Key = "WeatherCity",
                Value = "Roma"
            });
        }
    }
}
