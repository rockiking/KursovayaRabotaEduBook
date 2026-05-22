using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;

namespace EduBook.API_New.Models
{
    public class User
    {
        [Key]
        public int User_Id { get; set; }  // Убрал required

        public string User_name { get; set; } = string.Empty;

        public int? Favorite_material_id { get; set; }

        public string Login { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public int Role_id { get; set; }

        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Навигационные свойства
        [ForeignKey("Role_id")]
        public virtual Roles Role { get; set; } = null!;

        public virtual ICollection<Comment>? Comments { get; set; }
        public virtual ICollection<Author>? Authors { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryTime { get; set; }

    }
}
