using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EduBook.API_New.Data;
using EduBook.API_New.Models;

namespace EduBook.API_New.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public RolesController(AppDbContext context)
        {
            _context = context;
        }

        // DTO классы
        public class RoleResponseDto
        {
            public int Role_Id { get; set; }
            public string Role { get; set; }
            public int UsersCount { get; set; }
        }

        public class CreateRoleDto
        {
            public string Role { get; set; }
        }

        public class UpdateRoleDto
        {
            public string Role { get; set; }
        }

        // GET: api/roles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleResponseDto>>> GetRoles()
        {
            var roles = await _context.Rols
                .Select(r => new RoleResponseDto
                {
                    Role_Id = r.Role_Id,
                    Role = r.Role,
                    UsersCount = _context.Users.Count(u => u.Role_id == r.Role_Id)
                })
                .ToListAsync();

            return Ok(roles);
        }

        // GET: api/roles/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<RoleResponseDto>> GetRole(int id)
        {
            var role = await _context.Rols
                .FirstOrDefaultAsync(r => r.Role_Id == id);

            if (role == null)
            {
                return NotFound(new { message = $"Роль с ID {id} не найдена" });
            }

            var usersCount = await _context.Users.CountAsync(u => u.Role_id == id);

            var roleDto = new RoleResponseDto
            {
                Role_Id = role.Role_Id,
                Role = role.Role,
                UsersCount = usersCount
            };

            return Ok(roleDto);
        }

        // GET: api/roles/{id}/users
        [HttpGet("{id}/users")]
        public async Task<IActionResult> GetUsersByRole(int id)
        {
            var role = await _context.Rols.FindAsync(id);
            if (role == null)
            {
                return NotFound(new { message = $"Роль с ID {id} не найдена" });
            }

            var users = await _context.Users
                .Where(u => u.Role_id == id)
                .Select(u => new
                {
                    u.User_Id,
                    u.User_name,
                    u.Email,
                    
                })
                .ToListAsync();

            return Ok(new
            {
                role_name = role.Role,
                users_count = users.Count,
                users = users
            });
        }

        // POST: api/roles
        [HttpPost]
        public async Task<ActionResult<RoleResponseDto>> CreateRole([FromBody] CreateRoleDto createDto)
        {
            // Проверка валидации
            if (string.IsNullOrWhiteSpace(createDto.Role))
            {
                return BadRequest(new { message = "Название роли обязательно" });
            }

            // Проверка на существование роли
            var existingRole = await _context.Rols
                .FirstOrDefaultAsync(r => r.Role == createDto.Role);

            if (existingRole != null)
            {
                return Conflict(new { message = $"Роль '{createDto.Role}' уже существует" });
            }

            var role = new Roles
            {
                Role = createDto.Role
            };

            _context.Rols.Add(role);
            await _context.SaveChangesAsync();

            var responseDto = new RoleResponseDto
            {
                Role_Id = role.Role_Id,
                Role = role.Role,
                UsersCount = 0
            };

            return CreatedAtAction(nameof(GetRole), new { id = role.Role_Id }, responseDto);
        }

        // PUT: api/roles/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateRoleDto updateDto)
        {
            // Проверка валидации
            if (string.IsNullOrWhiteSpace(updateDto.Role))
            {
                return BadRequest(new { message = "Название роли обязательно" });
            }

            var role = await _context.Rols.FindAsync(id);
            if (role == null)
            {
                return NotFound(new { message = $"Роль с ID {id} не найдена" });
            }

            // Проверка дубликата
            var duplicateRole = await _context.Rols
                .FirstOrDefaultAsync(r => r.Role == updateDto.Role && r.Role_Id != id);

            if (duplicateRole != null)
            {
                return Conflict(new { message = $"Роль '{updateDto.Role}' уже существует" });
            }

            role.Role = updateDto.Role;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Роль успешно обновлена", role_id = id, new_name = updateDto.Role });
        }

        // DELETE: api/roles/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var role = await _context.Rols.FindAsync(id);
            if (role == null)
            {
                return NotFound(new { message = $"Роль с ID {id} не найдена" });
            }

            // Проверяем, есть ли пользователи с этой ролью
            var usersWithRole = await _context.Users.AnyAsync(u => u.Role_id == id);

            if (usersWithRole)
            {
                var usersCount = await _context.Users.CountAsync(u => u.Role_id == id);
                return BadRequest(new
                {
                    message = $"Невозможно удалить роль, так как {usersCount} пользователь(ей) имеют эту роль",
                    users_count = usersCount
                });
            }

            _context.Rols.Remove(role);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Роль '{role.Role}' успешно удалена" });
        }
    }
}