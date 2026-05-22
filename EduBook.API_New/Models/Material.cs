using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EduBook.API_New.Models
{
    public class Material
    {
        [Key]
        public int Material_Id { get; set; }  

        public string Material_title { get; set; } = string.Empty;

        public string Discipline { get; set; } = string.Empty;

        public int? Likes { get; set; }

        public int Author_id { get; set; }

        public string link_file { get; set; } = string.Empty;

        public int? Comments { get; set; }

        // Навигационные свойства
        [ForeignKey("Author_id")]
        public virtual Author Author { get; set; } = null!;

        public virtual ICollection<Comment>? CommentsList { get; set; }
    }
}
