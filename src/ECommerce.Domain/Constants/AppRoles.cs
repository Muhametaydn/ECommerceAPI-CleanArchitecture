using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Domain.Constants
{
    public class AppRoles
    {
        public const string Admin = "Admin";
        public const string Customer = "Customer";
        public const string Seller = "Seller";

        public static readonly IReadOnlyList<string> All = new[] { Admin, Customer , Seller};
    }
}
