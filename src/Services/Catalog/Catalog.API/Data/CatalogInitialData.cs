using Marten.Schema;

namespace Catalog.API.Data
{
    public class CatalogInitialData : IInitialData
    {
        public async Task Populate(IDocumentStore store, CancellationToken cancellation)
        {
            using var session = store.LightweightSession();

            if (await session.Query<Product>().AnyAsync())
            {
                return;
            }

            session.Store<Product>(GetPreconfiguredProducts());
            await session.SaveChangesAsync();
        }

        private static IEnumerable<Product> GetPreconfiguredProducts() => new List<Product>()
        {
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Điện thoại thông minh",
                Category = new List<string> { "Điện tử", "Công nghệ" },
                Description = "Điện thoại thông minh cao cấp với màn hình OLED.",
                ImageFile = "dien_thoai.jpg",
                Price = 15000000m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Máy tính xách tay",
                Category = new List<string> { "Điện tử", "Công nghệ" },
                Description = "Máy tính xách tay hiệu suất cao cho lập trình và thiết kế đồ họa.",
                ImageFile = "may_tinh.jpg",
                Price = 25000000m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Tủ lạnh",
                Category = new List<string> { "Điện gia dụng" },
                Description = "Tủ lạnh tiết kiệm điện năng, dung tích lớn.",
                ImageFile = "tu_lanh.jpg",
                Price = 12000000m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Máy giặt",
                Category = new List<string> { "Điện gia dụng" },
                Description = "Máy giặt cửa trước với công nghệ giặt sạch nhanh.",
                ImageFile = "may_giat.jpg",
                Price = 8000000m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Điều hòa không khí",
                Category = new List<string> { "Điện gia dụng", "Điện tử" },
                Description = "Điều hòa không khí tiết kiệm điện, làm lạnh nhanh.",
                ImageFile = "dieu_hoa.jpg",
                Price = 10000000m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Quạt điện",
                Category = new List<string> { "Điện gia dụng" },
                Description = "Quạt điện đứng, hoạt động êm ái, tiết kiệm năng lượng.",
                ImageFile = "quat_dien.jpg",
                Price = 1200000m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Bếp từ",
                Category = new List<string> { "Điện gia dụng" },
                Description = "Bếp từ cao cấp, an toàn, tiết kiệm điện.",
                ImageFile = "bep_tu.jpg",
                Price = 5000000m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Máy hút bụi",
                Category = new List<string> { "Điện gia dụng" },
                Description = "Máy hút bụi công suất cao, hút sạch mọi ngóc ngách.",
                ImageFile = "may_hut_bui.jpg",
                Price = 3000000m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Tivi 4K",
                Category = new List<string> { "Điện tử", "Công nghệ" },
                Description = "Tivi 4K màn hình lớn, hình ảnh sắc nét, sống động.",
                ImageFile = "tivi.jpg",
                Price = 20000000m
            },
            new Product
            {
                Id = Guid.NewGuid(),
                Name = "Loa bluetooth",
                Category = new List<string> { "Điện tử", "Âm thanh" },
                Description = "Loa bluetooth âm thanh chất lượng cao, thiết kế gọn nhẹ.",
                ImageFile = "loa_bluetooth.jpg",
                Price = 2500000m
            }
        };
    }

}
