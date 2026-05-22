using System.ComponentModel.DataAnnotations;

namespace EduBook.API_New.Models
{
    public class Roles
    {
        [Key]
        public int Role_Id { get; set; }  // Убрал required

        public string Role { get; set; } = string.Empty;

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}
