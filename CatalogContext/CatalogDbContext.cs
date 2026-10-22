using CatalogContext.Models;
using Microsoft.EntityFrameworkCore;
using System;

namespace CatalogContext
{
    public class CatalogDbContext : DbContext
    {
        public DbSet<Restaurant> Restaurants { get; set; }
        public DbSet<RestaurantDetail> RestaurantDetails { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<MenuItemCategory> MenuItemCategories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=localhost;Database=CatalogDb;User Id=sa;Password=YourStrong@Password123;TrustServerCertificate=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RestaurantDetail>()
                .HasKey(rd => rd.RestaurantId);

            modelBuilder.Entity<Restaurant>()
                .HasOne(r => r.Detail)
                .WithOne(rd => rd.Restaurant)
                .HasForeignKey<RestaurantDetail>(rd => rd.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MenuItem>()
                .HasOne(m => m.Restaurant)
                .WithMany(r => r.MenuItems)
                .HasForeignKey(m => m.RestaurantId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<MenuItem>()
                .Property(m => m.Price)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            modelBuilder.Entity<MenuItemCategory>()
                .HasKey(mc => new { mc.MenuItemId, mc.CategoryId });

            modelBuilder.Entity<MenuItemCategory>()
                .HasOne(mc => mc.MenuItem)
                .WithMany(m => m.MenuItemCategories)
                .HasForeignKey(mc => mc.MenuItemId);

            modelBuilder.Entity<MenuItemCategory>()
                .HasOne(mc => mc.Category)
                .WithMany(c => c.MenuItemCategories)
                .HasForeignKey(mc => mc.CategoryId);

            modelBuilder.Entity<Restaurant>().HasData(
                new Restaurant { Id = 1, Name = "Заліщицька Піцерія", IsDeleted = false }
            );

            modelBuilder.Entity<RestaurantDetail>().HasData(
                new RestaurantDetail { RestaurantId = 1, TaxId = "UA12345678", ContactEmail = "info@pizza-zal.com" }
            );

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Піца" },
                new Category { Id = 2, Name = "Напої" }
            );

            modelBuilder.Entity<MenuItem>().HasData(
                new MenuItem { Id = 101, RestaurantId = 1, Name = "Піца Папероні 30см", Price = 280.00m },
                new MenuItem { Id = 104, RestaurantId = 1, Name = "Лимонад цитрусовий 0.5л", Price = 60.00m }
            );

            modelBuilder.Entity<MenuItemCategory>().HasData(
                new MenuItemCategory { MenuItemId = 101, CategoryId = 1, AssignedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new MenuItemCategory { MenuItemId = 104, CategoryId = 2, AssignedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
            );
        }
    }
}