using AutoMapper;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Features.Reviews.DTOs;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Interfaces;
using MediatR;

namespace ECommerce.Application.Features.Reviews.Commands.CreateReview
{
    public class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, ReviewDTO>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public CreateReviewCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ReviewDTO> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
        {
            // Ürün var mı?
            var product = await _unitOfWork.Product.GetByIdAsync(request.ProductId)
                ?? throw new NotFoundException("Ürün", request.ProductId);

            // Kullanıcı bu ürünü daha önce değerlendirmiş mi?
            var alreadyReviewed = await _unitOfWork.Review
                .HasUserReviewedProductAsync(request.UserId, request.ProductId);

            if (alreadyReviewed)
                throw new InvalidOperationException("Bu ürünü zaten değerlendirdiniz.");

            // Kullanıcı bu ürünü satın almış mı? (teslim edilmiş sipariş kontrolü)
            var hasPurchased = await _unitOfWork.Order.GetOrdersByUserAsync(request.UserId);
            var purchased = hasPurchased.Any(o =>
                o.Status == Domain.Enums.OrderStatus.Delivered &&
                o.OrderItems.Any(oi => oi.ProductId == request.ProductId));

            if (!purchased)
                throw new InvalidOperationException("Sadece satın aldığınız ürünleri değerlendirebilirsiniz.");

            var review = new Review
            {
                Id = Guid.NewGuid(),
                ProductId = request.ProductId,
                UserId = request.UserId,
                Title = request.Title,
                Comment = request.Comment
            };
            review.SetRating(request.Rating);

            await _unitOfWork.Review.AddAsync(review);
            await _unitOfWork.SaveChangesAsync();

            // Ürün ve kullanıcı bilgileriyle birlikte döndür
            var saved = await _unitOfWork.Review.GetByIdAsync(review.Id);
            return _mapper.Map<ReviewDTO>(saved);
        }
    }
}
