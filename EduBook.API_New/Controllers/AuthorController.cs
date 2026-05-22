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
    public class AuthorController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthorController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/author - Доступно ВСЕМ
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var authors = await _context.Authors
                .Include(a => a.User)
                .Include(a => a.Materials)
                .Select(a => new
                {
                    a.Author_Id,
                    a.User_id,
                    a.Subscribers,
                    UserName = a.User != null ? a.User.User_name : "Unknown",
                    MaterialsCount = a.Materials != null ? a.Materials.Count : 0
                })
                .ToListAsync();
            return Ok(authors);
        }

        // GET: api/author/{id}/materials - Доступно ВСЕМ
        [HttpGet("{id}/materials")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAuthorMaterials(int id)
        {
            var materials = await _context.Materials
                .Where(m => m.Author_id == id)
                .Select(m => new
                {
                    m.Material_Id,
                    m.Material_title,
                    m.Discipline,
                    m.Likes,
                    m.Comments
                })
                .ToListAsync();
            return Ok(materials);
        }

        // POST: api/author/become - Стать автором (доступно всем пользователям)
        [HttpPost("become")]
        [Authorize]
        public async Task<IActionResult> BecomeAuthor()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Админ уже имеет права
            if (userRole == "Admin")
                return BadRequest(new { message = "Администратор уже имеет все права" });

            var existingAuthor = await _context.Authors.AnyAsync(a => a.User_id == userId);
            if (existingAuthor)
                return BadRequest(new { message = "Вы уже являетесь автором" });

            var author = new Author
            {
                User_id = userId,
                Subscribers = 0
            };

            _context.Authors.Add(author);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Поздравляем! Теперь вы можете создавать материалы" });
        }
    }
}