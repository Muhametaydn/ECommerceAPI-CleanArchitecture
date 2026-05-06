using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Entities
{
    public class RefreshToken
    {
        public Guid Id { get; private set; }
        public string Token { get; private set; } = default!;
        public DateTime ExpiresAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? RevokedAt { get; private set; }
        public string? ReplacedByToken { get; private set; }
        public string CreatedByIp { get; private set; } = default!;

        //FK
        public Guid UserId { get; private set; }
        public ApplicationUser User { get; private set; } = default!;

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsRevoked => RevokedAt is not null;
        public bool IsActive => !IsRevoked && !IsExpired;

        private RefreshToken() { }

        public static RefreshToken Create(Guid userId, string token, int expireationDays, string createdByIp)
        {
            return new RefreshToken()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(expireationDays),
                CreatedAt = DateTime.UtcNow,
                CreatedByIp = createdByIp
            };
        }

        public void Revoke(string? replacedByToken = null)
        {
            RevokedAt = DateTime.UtcNow;
            ReplacedByToken = replacedByToken; // field'a atama (önceki kodda local değişkene atıyordu - bug)
        }

    }
}
