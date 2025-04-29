using BuildingBlocks.Services;
using Marten;
using Marten.Schema;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Catalog.API.Data
{
    public class CatalogInitialData : IInitialData
    {
        private readonly IServiceProvider _serviceProvider;

        public CatalogInitialData(IServiceProvider serviceProvider = null)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Populate(IDocumentStore store, CancellationToken cancellation)
        {
            using var session = store.LightweightSession();

            if (await session.Query<Product>().AnyAsync(cancellation))
            {
                return;
            }

            // Kiểm tra xem có thể lấy LeaderElectionService không
            if (_serviceProvider != null)
            {
                try
                {
                    var leaderElectionService = _serviceProvider.GetService<ILeaderElectionService>();
                    
                    // Nếu dịch vụ Leader Election tồn tại và không phải là leader, bỏ qua
                    if (leaderElectionService != null && !await leaderElectionService.IsLeaderAsync())
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    // Bỏ qua lỗi, tiếp tục seed data nếu không thể truy cập LeaderElectionService
                }
            }

            // Thêm 5 sản phẩm mẫu khi khởi động để đảm bảo ứng dụng có thể chạy
            var initialSampleProducts = GetSampleProducts();
            session.Store<Product>(initialSampleProducts);
            await session.SaveChangesAsync(cancellation);
        }

        // Method to initialize data from service provider, will be called after migration
        public async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var store = serviceProvider.GetRequiredService<IDocumentStore>();
            using var session = store.LightweightSession();

            if (await session.Query<Product>().AnyAsync())
            {
                return;
            }
            
            try
            {
                // Lấy LeaderElectionService để kiểm tra nếu có
                var leaderElectionService = serviceProvider.GetService<ILeaderElectionService>();
                
                // Nếu dịch vụ Leader Election tồn tại và không phải là leader, bỏ qua
                if (leaderElectionService != null && !await leaderElectionService.IsLeaderAsync())
                {
                    return;
                }
            }
            catch (Exception)
            {
                // Bỏ qua lỗi, tiếp tục seed data nếu không thể truy cập LeaderElectionService
            }

            // Thêm 5 sản phẩm mẫu khi khởi động để đảm bảo ứng dụng có thể chạy
            var initialSampleProducts = GetSampleProducts();
            session.Store<Product>(initialSampleProducts);
            await session.SaveChangesAsync();
        }

        private static IEnumerable<Product> GetSampleProducts()
        {
            return new List<Product>()
            {
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Điện thoại thông minh - Mẫu 1",
                    Category = new List<string> { "Điện tử", "Công nghệ" },
                    Description = "Sản phẩm cao cấp với nhiều tính năng hiện đại.",
                    ImageFile = "product1.jpg",
                    Price = 9990000
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Máy tính xách tay - Mẫu 2",
                    Category = new List<string> { "Điện tử", "Máy tính" },
                    Description = "Thiết kế tinh tế, hiệu suất mạnh mẽ, đáp ứng mọi nhu cầu.",
                    ImageFile = "product2.jpg",
                    Price = 22500000
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Tủ lạnh - Mẫu 3",
                    Category = new List<string> { "Điện gia dụng" },
                    Description = "Công nghệ tiên tiến, tiết kiệm năng lượng và thân thiện với môi trường.",
                    ImageFile = "product3.jpg",
                    Price = 12750000
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Máy giặt - Mẫu 4",
                    Category = new List<string> { "Điện gia dụng" },
                    Description = "Phù hợp cho mọi gia đình, dễ sử dụng và bền bỉ theo thời gian.",
                    ImageFile = "product4.jpg",
                    Price = 7500000
                },
                new Product
                {
                    Id = Guid.NewGuid(),
                    Name = "Tai nghe không dây - Mẫu 5",
                    Category = new List<string> { "Điện tử", "Âm thanh" },
                    Description = "Chất lượng âm thanh vượt trội, thiết kế hiện đại và thoải mái khi đeo.",
                    ImageFile = "product5.jpg",
                    Price = 3450000
                }
            };
        }
    }
}
