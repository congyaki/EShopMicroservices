using System;
using System.Collections.Generic;

namespace Catalog.API.Data
{
    public static class CatalogSeedData
    {
        public static IEnumerable<Product> GetPreconfiguredProducts()
        {
            var products = new List<Product>();
            var random = new Random();
            
            // Danh sách các loại sản phẩm
            var categories = new List<string>[]
            {
                new List<string> { "Điện tử", "Công nghệ" },
                new List<string> { "Điện gia dụng" },
                new List<string> { "Điện tử", "Âm thanh" },
                new List<string> { "Điện tử", "Máy tính" },
                new List<string> { "Điện gia dụng", "Nhà bếp" },
                new List<string> { "Điện tử", "Giải trí" },
                new List<string> { "Thiết bị thông minh", "Công nghệ" },
                new List<string> { "Điện gia dụng", "Chăm sóc cá nhân" },
                new List<string> { "Phụ kiện", "Công nghệ" },
                new List<string> { "Điện tử", "Camera" }
            };
            
            // Danh sách tên sản phẩm
            var productNames = new string[]
            {
                "Điện thoại thông minh", "Máy tính xách tay", "Tủ lạnh", "Máy giặt", 
                "Điều hòa không khí", "Quạt điện", "Bếp từ", "Máy hút bụi", 
                "Tivi 4K", "Loa bluetooth", "Máy ảnh", "Máy tính bảng", 
                "Tai nghe không dây", "Đồng hồ thông minh", "Máy lọc không khí",
                "Robot hút bụi", "Nồi cơm điện", "Lò vi sóng", "Máy rửa bát",
                "Máy chiếu", "Màn hình máy tính", "Bàn phím cơ", "Chuột gaming",
                "Máy in", "Máy chơi game", "Máy sấy tóc", "Máy xay sinh tố",
                "Bàn ủi", "Máy sưởi", "Máy lọc nước", "Máy đánh trứng",
                "Máy ép trái cây", "Máy làm mát", "Quạt sưởi", "Đèn thông minh",
                "Khóa cửa thông minh", "Camera an ninh", "Máy phát wifi",
                "Thiết bị theo dõi sức khỏe", "Máy đọc sách", "Máy chụp ảnh lấy ngay",
                "Máy quay phim", "Đầu đĩa Blu-ray", "Loa soundbar", "Ampli",
                "Micro karaoke", "Máy nghe nhạc", "Đồng hồ treo tường thông minh",
                "Máy tăm nước"
            };
            
            // Danh sách các mô tả
            var descriptions = new string[]
            {
                "Sản phẩm cao cấp với nhiều tính năng hiện đại.",
                "Thiết kế tinh tế, hiệu suất mạnh mẽ, đáp ứng mọi nhu cầu.",
                "Công nghệ tiên tiến, tiết kiệm năng lượng và thân thiện với môi trường.",
                "Phù hợp cho mọi gia đình, dễ sử dụng và bền bỉ theo thời gian.",
                "Sản phẩm mới nhất trên thị trường với nhiều cải tiến vượt trội.",
                "Chất lượng hàng đầu, được tin dùng bởi hàng triệu khách hàng.",
                "Thiết kế nhỏ gọn, tiện lợi sử dụng trong không gian hạn chế.",
                "Kết nối thông minh, điều khiển từ xa qua ứng dụng điện thoại.",
                "Công nghệ mới nhất, mang đến trải nghiệm tuyệt vời cho người dùng.",
                "Sản phẩm chính hãng, bảo hành toàn quốc với dịch vụ hậu mãi tốt.",
                "Hiệu suất vượt trội, đáp ứng nhu cầu sử dụng chuyên nghiệp.",
                "Thiết kế sang trọng, phù hợp với mọi không gian nội thất.",
                "Tính năng độc đáo không thể tìm thấy ở các sản phẩm tương tự.",
                "Sản xuất theo tiêu chuẩn quốc tế, đảm bảo an toàn cho người dùng.",
                "Công nghệ tiên tiến nhập khẩu từ các quốc gia phát triển."
            };
            
            // Danh sách ảnh
            var imageFiles = new string[]
            {
                "product1.jpg", "product2.jpg", "product3.jpg", "product4.jpg", "product5.jpg",
                "product6.jpg", "product7.jpg", "product8.jpg", "product9.jpg", "product10.jpg",
                "product11.jpg", "product12.jpg", "product13.jpg", "product14.jpg", "product15.jpg",
                "product16.jpg", "product17.jpg", "product18.jpg", "product19.jpg", "product20.jpg"
            };
            
            // Tạo 10000 sản phẩm
            for (int i = 0; i < 10000; i++)
            {
                var categoryIndex = random.Next(categories.Length);
                var nameIndex = random.Next(productNames.Length);
                var descIndex = random.Next(descriptions.Length);
                var imageIndex = random.Next(imageFiles.Length);
                
                // Tạo giá từ 100.000 đến 50.000.000 VND
                var price = random.Next(100, 50001) * 1000m;
                
                // Tạo tên sản phẩm với mã để tránh trùng lặp
                var productName = $"{productNames[nameIndex]} - Mã SP{i + 1:D5}";
                
                products.Add(new Product
                {
                    Id = Guid.NewGuid(),
                    Name = productName,
                    Category = categories[categoryIndex],
                    Description = descriptions[descIndex],
                    ImageFile = imageFiles[imageIndex],
                    Price = price
                });
            }
            
            return products;
        }
    }
}