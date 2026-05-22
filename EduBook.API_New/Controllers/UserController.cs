using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using EduBook.API_New.Data;
using System.Security.Claims;

namespace EduBook.API_New.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/user - Только АДМИН
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin")
                return Forbid("Только администраторы могут просматривать всех пользователей");

            var users = await _context.Users
                .Include(u => u.Role)
                .Select(u => new
                {
                    u.User_Id,
                    u.User_name,
                    u.Login,
                    u.Email,
                    u.IsActive,
                    u.CreatedAt,
                    Role = u.Role != null ? u.Role.Role : "Unknown"
                })
                .ToListAsync();
            return Ok(users);
        }
        [HttpPut("{id}/make-admin")]
        [Authorize]
        public async Task<IActionResult> MakeAdmin(int id)
        {
            var currentUserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // Только текущий админ может назначать админов
            if (currentUserRole != "Admin")
                return Forbid("Только администраторы могут назначать админов");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            var adminRole = await _context.Rols.FirstOrDefaultAsync(r => r.Role == "Admin");
            if (adminRole == null)
                return BadRequest(new { message = "Роль Admin не найдена в базе данных" });

            user.Role_id = adminRole.Role_Id;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Пользователь {user.User_name} назначен администратором" });
        }

        // GET: api/user/profile - Свой профиль (доступно всем)
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.User_Id == userId);

            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            return Ok(new
            {
                user.User_Id,
                user.User_name,
                user.Login,
                user.Email,
                user.IsActive,
                user.CreatedAt,
                Role = user.Role?.Role ?? "Student"
            });
        }

        // DELETE: api/user/{id} - Только АДМИН
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != "Admin")
                return Forbid("Только администраторы могут удалять пользователей");

            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "Пользователь не найден" });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Пользователь удален" });
        }
    }
}