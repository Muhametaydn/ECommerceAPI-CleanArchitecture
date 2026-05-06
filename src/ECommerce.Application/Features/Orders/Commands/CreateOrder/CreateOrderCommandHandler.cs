using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Orders.DTOs;
using ECommerce.Application.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Orders.Commands.CreateOrder
{
    public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDetailDTO>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartService _cartService;
        private readonly IMapper _mapper;

        public CreateOrderCommandHandler(
            IUnitOfWork unitOfWork,
            ICartService cartService,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _cartService = cartService;
            _mapper = mapper;
        }

        public async Task<OrderDetailDTO> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            // 1) Sepeti getir
            var cart = await _cartService.GetCartAsync(request.CartId)
                ?? throw new InvalidOperationException("Sepet bulunamadı veya boş.");

            if (!cart.Items.Any())
                throw new InvalidOperationException("Sepet boş. Sipariş oluşturulamaz.");

            // 2) Sipariş oluştur
            var order = new Order
            {
                Id = Guid.NewGuid(),
                OrderNumber = Order.GenerateOrderNumber(),
                UserId = request.UserId,
                ShippingAddressId = request.ShippingAddressId,
                Note = request.Note,
                Status = Domain.Enums.OrderStatus.Pending
            };

            // 3) Her sepet kalemi için stok kontrolü yap ve OrderItem oluştur
            foreach (var cartItem in cart.Items)
            {
                var product = await _unitOfWork.Product.GetByIdAsync(cartItem.ProductId)
                    ?? throw new NotFoundException("Ürün", cartItem.ProductId);

                if (!product.IsActive)
                    throw new InvalidOperationException($"'{product.Name}' ürünü artık satışta değil.");

                if (product.StockQuantity < cartItem.Quantity)
                    throw new InvalidOperationException(
                        $"'{product.Name}' için yeterli stok yok. Mevcut: {product.StockQuantity}, İstenen: {cartItem.Quantity}");

                // Stok düş
                product.DecreaseStock(cartItem.Quantity);
                _unitOfWork.Product.Update(product);

                // Sipariş kalemi oluştur (güncel fiyat üzerinden)
                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = cartItem.Quantity,
                    UnitPrice = product.Price
                };

                order.OrderItems.Add(orderItem);
            }

            // 4) Kupon uygula (varsa)
            if (!string.IsNullOrWhiteSpace(request.CouponCode))
            {
                var coupon = await _unitOfWork.Coupon.GetByCodeAsync(request.CouponCode.ToUpperInvariant())
                    ?? throw new InvalidOperationException("Kupon kodu bulunamadı.");

                var subTotal = order.OrderItems.Sum(oi => oi.TotalPrice);
                var discountAmount = coupon.CalculateDiscount(subTotal);

                order.CouponCode = coupon.Code;
                order.DiscountAmount = discountAmount;

                coupon.Use();
                _unitOfWork.Coupon.Update(coupon);
            }

            // 5) Toplam tutarı hesapla (indirim dahil)
            order.CalculateTotal();

            // 6) Ödeme kaydı oluştur
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Method = request.PaymentMethod,
                Status = Domain.Enums.PaymentStatus.Pending
            };

            // 7) Veritabanına kaydet
            await _unitOfWork.Order.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();

            // 8) Sepeti temizle
            await _cartService.DeleteCartAsync(request.CartId);

            // 9) Siparişi detaylarıyla birlikte getir ve döndür
            var savedOrder = await _unitOfWork.Order.GetOrderWithItemsAsync(order.Id);
            return _mapper.Map<OrderDetailDTO>(savedOrder);
        }
    }
}
