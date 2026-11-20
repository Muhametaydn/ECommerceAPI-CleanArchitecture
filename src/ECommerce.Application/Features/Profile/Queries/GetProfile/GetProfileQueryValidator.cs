using FluentValidation;

namespace ECommerce.Application.Features.Profiles.Queries.GetProfile;

public sealed class GetProfileQueryValidator : AbstractValidator<GetProfileQuery>
{
    public GetProfileQueryValidator() => RuleFor(query => query.UserId).NotEmpty();
}
