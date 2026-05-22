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
    public class CommentController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommentController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/comment/material/{materialId} - Доступно ВСЕМ
        [HttpGet("material/{materialId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByMaterial(int materialId)
        {
            var comments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.Material_id == materialId)
                .OrderByDescending(c => c.Date)
                .Select(c => new
                {
                    c.Comment_Id,
                    c.Text,
                    c.Date,
                    UserName = c.User.User_name
                })
                .ToListAsync();
            return Ok(comments);
        }

        // POST: api/comment - Доступно ВСЕМ авторизованным
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateCommentDto dto)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            if (string.IsNullOrWhiteSpace(dto.Text))
                return BadRequest(new { message = "Текст комментария обязателен" });

            var comment = new Comment
            {
                User_id = userId,
                Material_id = dto.Material_id,
                Text = dto.Text,
                Date = DateTime.Now
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Обновляем счетчик
            var material = await _context.Materials.FindAsync(dto.Material_id);
            if (material != null)
            {
                material.Comments = (material.Comments ?? 0) + 1;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Комментарий добавлен", commentId = comment.Comment_Id });
        }

        // DELETE: api/comment/{id} - Свой комментарий или АДМИН
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
                return NotFound(new { message = "Комментарий не найден" });

            // Admin или автор комментария могут удалять
            if (userRole != "Admin" && comment.User_id != userId)
                return Forbid("Только автор комментария или администратор могут удалять");

            var materialId = comment.Material_id;

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            // Обновляем счетчик комментариев у материала
            var material = await _context.Materials.FindAsync(materialId);
            if (material != null && material.Comments > 0)
            {
                material.Comments = material.Comments - 1;
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Комментарий удален" });
        }

        public class CreateCommentDto
        {
            public int Material_id { get; set; }
            public string Text { get; set; }
        }
    }
}