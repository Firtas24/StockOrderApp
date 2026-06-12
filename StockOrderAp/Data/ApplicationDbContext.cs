using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StockOrderApp.Models;

namespace StockOrderAp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        { }

        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<Product> Products { get; set; } = default!;

        // Sipariş tabloları
        public DbSet<Order> Orders { get; set; } = default!;
        public DbSet<OrderItem> OrderItems { get; set; } = default!;

        // 👇 Profil tablosu
        public DbSet<UserProfile> UserProfiles { get; set; } = default!;
        public DbSet<UserFavorite> UserFavorites { get; set; } = default!;
        public DbSet<ProductReview> ProductReviews { get; set; } = default!;

        public DbSet<Coupon> Coupons { get; set; } = default!;



        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Category>()
                   .HasIndex(x => x.Name)
                   .IsUnique();

            builder.Entity<OrderItem>()
                   .HasOne(oi => oi.Order)
                   .WithMany(o => o.Items)
                   .HasForeignKey(oi => oi.OrderId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                   .HasOne(oi => oi.Product)
                   .WithMany()
                   .HasForeignKey(oi => oi.ProductId)
                   .OnDelete(DeleteBehavior.Restrict);
            builder.Entity<UserFavorite>()
        .           HasIndex(x => new { x.UserId, x.ProductId })
                    .IsUnique();
            builder.Entity<ProductReview>()
                    .HasIndex(x => new { x.ProductId, x.UserId })
                    .IsUnique(); // aynı kullanıcı aynı ürüne 2 yorum yazamasın


        }
    }
}
