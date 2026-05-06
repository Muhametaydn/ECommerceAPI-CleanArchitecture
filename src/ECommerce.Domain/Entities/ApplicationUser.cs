using Microsoft.AspNetCore.Identity;

namespace ECommerce.Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public string FirstName { get; private set; } = default!;
        public string LastName { get; private set; } = default!;
        public string? ProfileImageUrl { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdateTime { get; private set; }
        public bool IsActive { get; private set; } = true;

        //Navigation Properties
        public ICollection<Address> Addresses { get; private set; } = new List<Address>();
        public ICollection<Order> Orders { get; private set; } = new List<Order>();
        public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

        public static ApplicationUser Create(string firstName, string lastName, string email, string userName)
        {
            return new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                UserName = userName,   // IdentityUser'da UserName (büyük N)
                CreatedAt = DateTime.UtcNow,
            };
        }

        public void UpdateProfile(string firstName, string lastName, string? profileImageUrl = null)
        {
            FirstName = firstName;
            LastName = lastName;
            if (profileImageUrl is not null)
                ProfileImageUrl = profileImageUrl;
            UpdateTime = DateTime.UtcNow;
        }

        public void Deactivate()
        {
            IsActive = false;
            UpdateTime = DateTime.UtcNow;
        }

        public string FullName => $"{FirstName} {LastName}";
    }
}
