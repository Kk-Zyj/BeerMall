using BeerMall.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BeerMall.Api.Data
{
    public class BeerMallContext : DbContext
    {
        public BeerMallContext(DbContextOptions<BeerMallContext> options) : base(options)
        {
        }

        // 注册表
        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<ProductSku> ProductSkus { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<UserAddress> UserAddresses { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<FissionTask> FissionTasks { get; set; }
        public DbSet<GroupBuyRule> GroupBuyRules { get; set; }
        public DbSet<GroupBuyInstance> GroupBuyInstances { get; set; }
        public DbSet<Coupon> Coupons { get; set; }
        public DbSet<UserCoupon> UserCoupons { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 软删除过滤：查询时自动过滤 IsDeleted=true 的数据
            modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<ProductSku>().HasQueryFilter(p => !p.IsDeleted);

            // 索引配置
            modelBuilder.Entity<User>()
                .HasIndex(u => u.OpenId)
                .IsUnique(); // OpenId 必须唯一

            // 配置 1:N 关系 (虽然 EF Core 能自动推断，但显式写出来更安全)
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict); // 防止删除分类时误删商品

            // 预置一些初始数据 (种子数据）
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "人气热销", Sort = 100 },
                new Category { Id = 2, Name = "精酿", Sort = 90 },
                new Category { Id = 3, Name = "鲜啤", Sort = 80 },
                new Category { Id = 4, Name = "啤酒", Sort = 70 }
            );
            modelBuilder.Entity<ShoppingCart>().HasIndex(c => c.UserId).IsUnique();
            // ==========================================
            // 2. 初始化商品 (Products)
            // 注意：MainImage 使用了占位图，后续你可以替换为真实 URL
            // ==========================================
            modelBuilder.Entity<Product>().HasData(
                // --- 分类1：精酿生啤 ---
                new Product
                {
                    Id = 1,
                    CategoryId = 1,
                    Name = "爱丁博格小麦白啤",
                    Description = "德国经典小麦啤，口感醇厚，伴有香蕉与丁香的香气。",
                    MainImage = "/static/product/wheat_beer.jpg",
                    Content = "<p>正宗德式小麦啤，未过滤保留酵母...</p>",
                    Sort = 100,
                    IsOnShelf = true
                },
                new Product
                {
                    Id = 2,
                    CategoryId = 4,
                    Name = "爱丁博格小麦啤酒",
                    Description = "典型的美式浑浊IPA，热带水果香气炸裂。",
                    MainImage = "/static/product/beer.jpg",
                    Content = "<p>酒花投放量巨大，苦度适中...</p>",
                    Sort = 90,
                    IsOnShelf = true
                }
            );

            // ==========================================
            // 3. 初始化规格 (ProductSkus)
            // 这里的类名取决于你之前的定义，可能是 ProductSku 或 Sku
            // ==========================================
            modelBuilder.Entity<ProductSku>().HasData(
                // 商品1：爱丁博格 (两个规格)
                new ProductSku { Id = 1, ProductId = 1, SpecName = "500ml/杯", Price = 25.00m, Stock = 999, Weight = 0 },
                new ProductSku { Id = 2, ProductId = 1, SpecName = "1L/酒炮", Price = 128.00m, Stock = 50, Weight = 0 },

                // 商品2：迷失海岸 (一个规格)
                new ProductSku { Id = 3, ProductId = 2, SpecName = "330ml/杯", Price = 38.00m, Stock = 200, Weight = 0 }
            );
        }
    }
}