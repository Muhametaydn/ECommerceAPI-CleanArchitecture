using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<CreateOrderCommandHandler> _logger;

        public CreateOrderCommandHandler(
            IUnitOfWork unitOfWork,
            ICartService cartService,
            IMapper mapper, ILogger<CreateOrderCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _cartService = cartService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OrderDetailDTO> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            var orderId = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"checkout:{request.UserId:D}:{request.CheckoutRequestId:D}")).AsSpan(0, 16));
            var previous = await _unitOfWork.Order.GetOrderWithItemsAsync(orderId);
            if (previous is not null)
                return await Replay(previous, request);

            // 1) Sepeti getir
            var cart = await _cartService.GetCartAsync(request.CartId)
                ?? throw new InvalidOperationException("Sepet bulunamadı veya boş.");

            if (!cart.Items.Any())
                throw new InvalidOperationException("Sepet boş. Sipariş oluşturulamaz.");

            var cartKey = CartKey(request, cart);
            previous = await _unitOfWork.Order.GetByCartCheckoutKeyAsync(cartKey);
            if (previous is not null)
                return await Replay(previous, request);

            var address = await _unitOfWork.Address.GetByIdAsync(request.ShippingAddressId)
                ?? throw new NotFoundException("Adres", request.ShippingAddressId);
            if (address.UserId != request.UserId)
                throw new ForbiddenException("Bu teslimat adresini kullanma yetkiniz yok.");

            // 2) Sipariş oluştur
            var order = new Order
            {
                Id = orderId,
                CartCheckoutKey = cartKey,
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

            // 5b) Sipariş oluşturuldu domain event'ini raise et
            order.RaiseOrderCreated();

            // 6) Ödeme kaydı oluştur
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Amount = order.TotalAmount,
                Method = request.PaymentMethod,
                Status = Domain.Enums.PaymentStatus.Pending
            };

            order.Payment = payment;

            // 7) Veritabanına kaydet
            await _unitOfWork.Order.AddAsync(order);
            try
            {
                await _unitOfWork.SaveChangesAsync();
            }
            catch (ConflictException)
            {
                var winner = await _unitOfWork.Order.GetByCartCheckoutKeyAsync(cartKey);
                if (winner is null) throw;
                return await Replay(winner, request);
            }

            // 8) Sepeti temizle
            await CleanupCart(request.CartId, cart, order.Id);

            // 9) Siparişi detaylarıyla birlikte getir ve döndür
            var savedOrder = await _unitOfWork.Order.GetOrderWithItemsAsync(order.Id);
            return _mapper.Map<OrderDetailDTO>(savedOrder);
        }
        private static string CartKey(CreateOrderCommand request, ECommerce.Domain.Entities.Cart cart)
        {
            var items = string.Join(";", cart.Items.OrderBy(i => i.ProductId).Select(i => $"{i.ProductId:D}:{i.Quantity}"));
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{request.UserId:D}|{request.CartId}|{cart.UpdatedAt.ToUniversalTime():O}|{items}")));
        }

        private async Task<OrderDetailDTO> Replay(Order order, CreateOrderCommand request)
        {
            var requestedId = new Guid(SHA256.HashData(Encoding.UTF8.GetBytes($"checkout:{request.UserId:D}:{request.CheckoutRequestId:D}")).AsSpan(0, 16));
            var differentRequest = order.Id != requestedId;
            if (order.UserId != request.UserId || order.ShippingAddressId != request.ShippingAddressId
                || order.Payment?.Method != request.PaymentMethod
                || (order.CouponCode ?? "") != (request.CouponCode?.ToUpperInvariant() ?? "")
                || (order.Note ?? "") != (request.Note ?? ""))
                throw new ConflictException();
            try
            {
                var currentCart = await _cartService.GetCartAsync(request.CartId);
                if (currentCart is not null && CartKey(request, currentCart) == order.CartCheckoutKey)
                    await CleanupCart(request.CartId, currentCart, order.Id);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Committed order {OrderId}: cart cleanup failed", order.Id);
            }
            if (differentRequest)
                throw new ConflictException("Bu sepet için sipariş zaten oluşturuldu. Siparişlerinizi kontrol edin.");
            return _mapper.Map<OrderDetailDTO>(order);
        }

        private async Task CleanupCart(string cartId, ECommerce.Domain.Entities.Cart snapshot, Guid orderId)
        {
            try { await _cartService.DeleteCartIfUnchangedAsync(cartId, snapshot); }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Committed order {OrderId}: cart cleanup failed", orderId);
            }
        }
    }
}
