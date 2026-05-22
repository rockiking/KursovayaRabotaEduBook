using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduBook.API_New.Models
{
    public class Author
    {
        [Key]
        public int Author_Id { get; set; }  

        public int User_id { get; set; }  

        public int? Subscribers { get; set; }

        // Навигационные свойства
        [ForeignKey("User_id")]
        public virtual User? User { get; set; }

        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
    }
}
