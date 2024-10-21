using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Ordering.Domain.Enums;
using Ordering.Domain.Models;
using Ordering.Domain.ValueObjects;

namespace Ordering.Infrastructure.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            builder.HasKey(o => o.Id);
            builder.Property(o => o.Id).HasConversion(
                            orderId => orderId.Value,
                            dbId => OrderId.Of(dbId));

            builder.HasOne<Customer>()
                .WithMany()
                .HasForeignKey(o => o.CustomerId)
                .IsRequired();

            builder.HasMany(o => o.OrderItems)
                .WithOne()
                .HasForeignKey(oi => oi.OrderId);

            builder.OwnsOne(o => o.OrderName, nameBuilder =>
            {
                nameBuilder.Property(n => n.Value)
                .HasColumnName(nameof(Order.OrderName))
                .HasMaxLength(100)
                .IsRequired();
            });

            builder.OwnsOne(o => o.ShippingAddress, addressBuilder =>
            {
                addressBuilder.Property(s => s.FirstName)
                .HasMaxLength(50)
                .IsRequired();

                addressBuilder.Property(s => s.LastName)
                .HasMaxLength(50)
                .IsRequired();

                addressBuilder.Property(s => s.EmailAddress)
                .HasMaxLength(50);

                addressBuilder.Property(s => s.AddressLine)
                .HasMaxLength(180)
                .IsRequired();

                addressBuilder.Property(s => s.Country)
                .HasMaxLength(50);

                addressBuilder.Property(s => s.State)
                .HasMaxLength(50);

                addressBuilder.Property(s => s.ZipCode)
                .HasMaxLength(5)
                .IsRequired();
            });

            builder.OwnsOne(o => o.BillingAddress, addressBuilder =>
            {
                addressBuilder.Property(b => b.FirstName)
                .HasMaxLength(50)
                .IsRequired();

                addressBuilder.Property(b => b.LastName)
                .HasMaxLength(50)
                .IsRequired();

                addressBuilder.Property(b => b.EmailAddress)
                .HasMaxLength(50);

                addressBuilder.Property(b => b.AddressLine)
                .HasMaxLength(180)
                .IsRequired();

                addressBuilder.Property(b => b.Country)
                .HasMaxLength(50);

                addressBuilder.Property(b => b.State)
                .HasMaxLength(50);

                addressBuilder.Property(b => b.ZipCode)
                .HasMaxLength(5)
                .IsRequired();
            });

            builder.OwnsOne(o => o.Payment, paymentBuilder =>
            {
                paymentBuilder.Property(p => p.CardName)
                .HasMaxLength(50);

                paymentBuilder.Property(p => p.CardNumber)
                .HasMaxLength(24)
                .IsRequired();

                paymentBuilder.Property(p => p.Expiration)
                .HasMaxLength(10);

                paymentBuilder.Property(p => p.CVV)
                .HasMaxLength(3);

                paymentBuilder.Property(p => p.PaymentMethod);
            });

            builder.Property(o => o.Status)
                .HasDefaultValue(OrderStatus.Draft)
                .HasConversion(
                    s => s.ToString(),
                    dbStatus => (OrderStatus)Enum.Parse(typeof(OrderStatus), dbStatus) 
                    );

            builder.Property(o => o.TotalPrice)
                .HasPrecision(18, 2);
        }
    }
}
