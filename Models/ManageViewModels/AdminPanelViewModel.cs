using System.Collections.Generic;

namespace Claudia.Models
{
    public class AdminPanelViewModel
    {
        public Dictionary<User, IList<string>> UsersWithRoles { get; set; }
    }
}
