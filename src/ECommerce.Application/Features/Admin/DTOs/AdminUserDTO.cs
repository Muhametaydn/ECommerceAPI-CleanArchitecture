namespace ECommerce.Application.Features.Admin.DTOs
{
    public class AdminUserDTO
    {
        public Guid Id { get; init; }
        public string FirstName { get; init; } = string.Empty;
        public string LastName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public bool IsActive { get; init; }
        public DateTime CreatedAt { get; init; }
        public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
    }
}
