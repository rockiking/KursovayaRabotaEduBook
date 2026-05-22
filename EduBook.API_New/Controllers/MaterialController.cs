using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduBook.API_New.Data;
using EduBook.API_New.Models;
using System.Security.Claims;

namespace EduBook.API_New.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaterialController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MaterialController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/material - Доступно ВСЕМ
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var materials = await _context.Materials
                .Include(m => m.Author)
                .ThenInclude(a => a.User)
                .Select(m => new
                {
                    m.Material_Id,
                    m.Material_title,
                    m.Discipline,
                    m.Likes,
                    m.link_file,
                    AuthorName = m.Author != null && m.Author.User != null ? m.Author.User.User_name : "Unknown",
                    CommentsCount = m.Comments ?? 0
                })
                .ToListAsync();
            return Ok(materials);
        }
        [HttpPatch("{id}/like")]
        [AllowAnonymous]
        public async Task<IActionResult> Like(int id)
        {
            try
            {
                var material = await _context.Materials.FindAsync(id);
                if (material == null)
                    return NotFound(new { message = "Материал не найден" });

                material.Likes = (material.Likes ?? 0) + 1;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Лайк добавлен", likes = material.Likes });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET: api/material/{id} - Доступно ВСЕМ
        [HttpGet("{id}")]
        [AllowAnonymous]
       
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var material = await _context.Materials
                    .Include(m => m.Author)
                    .ThenInclude(a => a.User)
                    .FirstOrDefaultAsync(m => m.Material_Id == id);

                if (material == null)
                    return NotFound(new { message = "Материал не найден" });

                return Ok(new
                {
                    material.Material_Id,
                    material.Material_title,
                    material.Discipline,
                    material.Likes,
                    material.link_file,
                    material.Comments,
                    AuthorName = material.Author?.User?.User_name ?? "Unknown"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // PUT: api/material/{id} - Только автор материала или админ
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMaterialDto dto)
        {
            var material = await _context.Materials.FindAsync(id);
            if (material == null)
                return NotFound(new { message = "Материал не найден" });

            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Admin или автор материала могут редактировать
            if (userRole != "Admin")
            {
                var author = await _context.Authors.FirstOrDefaultAsync(a => a.User_id == userId);
                if (author == null || author.Author_Id != material.Author_id)
                    return Forbid("Только автор материала или администратор могут редактировать");
            }

            if (!string.IsNullOrWhiteSpace(dto.Material_title))
                material.Material_title = dto.Material_title;
            if (!string.IsNullOrWhiteSpace(dto.Discipline))
                material.Discipline = dto.Discipline;
            if (!string.IsNullOrWhiteSpace(dto.link_file))
                material.link_file = dto.link_file;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Материал обновлен" });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin")
                return Forbid("Только администраторы могут удалять материалы");

            var material = await _context.Materials
                .Include(m => m.CommentsList)  // Загружаем связанные комментарии
                .FirstOrDefaultAsync(m => m.Material_Id == id);

            if (material == null)
                return NotFound(new { message = "Материал не найден" });

            // Сначала удаляем все комментарии к этому материалу
            if (material.CommentsList != null && material.CommentsList.Any())
            {
                _context.Comments.RemoveRange(material.CommentsList);
            }

            // Затем удаляем сам материал
            _context.Materials.Remove(material);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Ошибка при удалении: {ex.Message}" });
            }

            return Ok(new { message = "Материал и все связанные комментарии удалены" });
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateMaterialDto dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrWhiteSpace(dto.Material_title))
                    return BadRequest(new { message = "Название материала обязательно" });

                // Проверяем, является ли пользователь автором
                var author = await _context.Authors.FirstOrDefaultAsync(a => a.User_id == userId);

                // Если не автор и не админ
                if (author == null && userRole != "Admin")
                {
                    // Автоматически становимся автором
                    author = new Author
                    {
                        User_id = userId,
                        Subscribers = 0
                    };
                    _context.Authors.Add(author);
                    await _context.SaveChangesAsync();

                    // Обновляем роль пользователя
                    var user = await _context.Users.FindAsync(userId);
                    if (user != null)
                    {
                        var authorRole = await _context.Rols.FirstOrDefaultAsync(r => r.Role == "Author");
                        if (authorRole != null)
                        {
                            user.Role_id = authorRole.Role_Id;
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                // Получаем ID автора
                var currentAuthor = await _context.Authors.FirstOrDefaultAsync(a => a.User_id == userId);
                if (currentAuthor == null && userRole != "Admin")
                    return BadRequest(new { message = "Ошибка: не удалось определить автора" });

                var material = new Material
                {
                    Material_title = dto.Material_title,
                    Discipline = dto.Discipline,
                    link_file = dto.link_file,
                    Likes = 0,
                    Comments = 0,
                    Author_id = currentAuthor?.Author_Id ?? 1
                };

                _context.Materials.Add(material);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Материал создан", materialId = material.Material_Id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // DTO класс
        public class CreateMaterialDto
        {
            public string Material_title { get; set; } = "";
            public string Discipline { get; set; } = "";
            public string link_file { get; set; } = "";
        }

       
      

        public class UpdateMaterialDto
        {
            public string Material_title { get; set; } = "";
            public string Discipline { get; set; } = "";
            public string link_file { get; set; } = "";
        }
    }
}
