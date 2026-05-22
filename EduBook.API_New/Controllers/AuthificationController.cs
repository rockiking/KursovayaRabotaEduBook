using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EduBook.API_New.Data;
using EduBook.API_New.Models;

namespace EduBook.API_New.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthificationController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AuthificationController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.email) || string.IsNullOrWhiteSpace(dto.password))
                return BadRequest(new { message = "Email и пароль обязательны" });

            if (await _context.Users.AnyAsync(u => u.Email == dto.email))
                return BadRequest(new { message = "Пользователь уже существует" });

            // Используем role_id из запроса, если он передан
            int roleId = dto.role_id;
            if (roleId == 0)
            {
                // Если role_id не передан, ищем роль "Student"
                var studentRole = await _context.Rols.FirstOrDefaultAsync(r => r.Role == "Student");
                roleId = studentRole?.Role_Id ?? 3;
            }

            // Проверяем, существует ли роль
            var roleExists = await _context.Rols.AnyAsync(r => r.Role_Id == roleId);
            if (!roleExists)
            {
                return BadRequest(new { message = $"Роль с ID {roleId} не существует" });
            }

            var user = new User
            {
                User_name = dto.name,
                Login = dto.email,
                Email = dto.email,
                Role_id = roleId,
                Password = HashPassword(dto.password),
                CreatedAt = DateTime.Now,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Загружаем роль пользователя
            await _context.Entry(user).Reference(u => u.Role).LoadAsync();

            // Создаем токен
            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Регистрация успешна!",
                userId = user.User_Id,
                token = token,
                user = new
                {
                    user.User_Id,
                    user.User_name,
                    user.Login,
                    user.Email,
                    user.Role_id,
                    Role = user.Role?.Role ?? "Student",
                    user.IsActive
                }
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == dto.email || u.Login == dto.email);

            if (user == null || user.Password != HashPassword(dto.password))
                return Unauthorized(new { message = "Неверный email или пароль" });

            if (!user.IsActive)
                return Unauthorized(new { message = "Аккаунт заблокирован" });

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                message = "Вход выполнен",
                token = token,
                user = new
                {
                    user.User_Id,
                    user.User_name,
                    user.Login,
                    user.Email,
                    user.Role_id,
                    Role = user.Role?.Role ?? "Student",
                    user.IsActive
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-super-secret-key-for-jwt-minimum-32-characters"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.User_Id.ToString()),
                new Claim(ClaimTypes.Name, user.User_name ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? ""),
                new Claim(ClaimTypes.Role, user.Role?.Role ?? "Student"),
                new Claim("UserId", user.User_Id.ToString()),
                new Claim("RoleId", user.Role_id.ToString())
            };

            var token = new JwtSecurityToken(
                expires: DateTime.Now.AddDays(7),
                claims: claims,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }
    }

    public class RegisterDto
    {
        public string name { get; set; } = "";
        public string email { get; set; } = "";
        public string password { get; set; } = "";
        public int role_id { get; set; } = 3;  // По умолчанию Student
    }

    public class LoginDto
    {
        public string email { get; set; } = "";
        public string password { get; set; } = "";
    }
}