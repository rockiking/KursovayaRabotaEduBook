using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduBook.API_New.Models
{
    public class Comment
    {
        [Key]
        public int Comment_Id { get; set; }  // Убрал required

        public int User_id { get; set; }

        public int Material_id { get; set; }

        public string Text { get; set; } = string.Empty;

        public DateTime Date { get; set; } = DateTime.Now;

        // Навигационные свойства
        [ForeignKey("User_id")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("Material_id")]
        public virtual Material Material { get; set; } = null!;
    }
}