using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Security.Claims;
using BCrypt.Net;

namespace loguin_A.Controllers;

public class CuentaController : Controller
{
    private readonly IConfiguration _configuration;

    public CuentaController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        ViewBag.Error = "Por favor, complete todos los campos";
        return View();
    }
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT id_usuario,
                   nombre,
                   email,
                   password_hash,
                   rol,
                   avatar_url
            FROM Usuarios
            WHERE email = @email
            AND es_activo = 1";

        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@email", email);

        using var reader = await command.ExecuteReaderAsync();

        if (!reader.Read())
        {
            ViewBag.Error = "Usuario no encontrado";
            return View();
        }

        var hash = reader["password_hash"].ToString();

        // Verificar contraseña (soporta BCrypt y texto plano)
        bool passwordValid;
        if (hash.StartsWith("$2"))
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(password, hash);
        }
        else
        {
            passwordValid = hash == password;
        }

        if (!passwordValid)
        {
            ViewBag.Error = "Contraseña incorrecta";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, reader["nombre"].ToString()!),
            new Claim(ClaimTypes.Email, reader["email"].ToString()!),
            new Claim(ClaimTypes.Role, reader["rol"].ToString()!),
            new Claim("Avatar", reader["avatar_url"]?.ToString() ?? "/img/usuario.png")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        var rol = reader["rol"].ToString();

        return rol switch
        {
            "Admin" => RedirectToAction("Index", "Admin"),
            "Cliente" => RedirectToAction("Index", "Cliente"),
            "Usuario" => RedirectToAction("Index", "Usuario"),
            _ => RedirectToAction("Index", "Home")
        };
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    public IActionResult Perfil()
    {
        var nombre = User.Identity?.Name;
        var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        var rol = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;
        var avatar = User.Claims.FirstOrDefault(c => c.Type == "Avatar")?.Value;

        ViewBag.Nombre = nombre;
        ViewBag.Email = email;
        ViewBag.Rol = rol;
        ViewBag.Avatar = avatar ?? "/img/usuario.png";

        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult EditarPerfil()
    {
        var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login");
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        using var connection = new SqlConnection(connectionString);
        connection.Open();

        var sql = "SELECT nombre, telefono, direccion, avatar_url FROM Usuarios WHERE email = @email";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@email", email);

        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            ViewBag.Nombre = reader["nombre"]?.ToString() ?? "";
            ViewBag.Telefono = reader["telefono"]?.ToString() ?? "";
            ViewBag.Direccion = reader["direccion"]?.ToString() ?? "";
            ViewBag.Avatar = reader["avatar_url"]?.ToString() ?? "/img/usuario.png";
        }

        return View();
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> EditarPerfil(
        string nombre,
        string telefono,
        string direccion,
        string password,
        string confirmarPassword,
        IFormFile avatarFile) // Eliminé avatar_url de los parámetros
    {
        var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction("Login");
        }

        // Validar que el nombre no esté vacío
        if (string.IsNullOrWhiteSpace(nombre))
        {
            ViewBag.Error = "El nombre es obligatorio";
            ViewBag.Nombre = nombre;
            ViewBag.Telefono = telefono;
            ViewBag.Direccion = direccion;
            return View();
        }

        // Validar contraseñas
        if (!string.IsNullOrEmpty(password) && password != confirmarPassword)
        {
            ViewBag.Error = "Las contraseñas no coinciden";
            ViewBag.Nombre = nombre;
            ViewBag.Telefono = telefono;
            ViewBag.Direccion = direccion;
            return View();
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        // OBTENER AVATAR ACTUAL
        string currentAvatar = await GetCurrentAvatarAsync(email, connection);
        string avatarPath = currentAvatar; // Mantener el actual por defecto

        // SI SUBE NUEVA IMAGEN
        if (avatarFile != null && avatarFile.Length > 0)
        {
            // Validar tipo de archivo
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" };
            var extension = Path.GetExtension(avatarFile.FileName).ToLower();
            
            if (!allowedExtensions.Contains(extension))
            {
                ViewBag.Error = "Solo se permiten imágenes (jpg, jpeg, png, gif, bmp)";
                ViewBag.Nombre = nombre;
                ViewBag.Telefono = telefono;
                ViewBag.Direccion = direccion;
                ViewBag.Avatar = currentAvatar;
                return View();
            }

            // Validar tamaño (máximo 5MB)
            if (avatarFile.Length > 5 * 1024 * 1024)
            {
                ViewBag.Error = "La imagen no puede superar los 5MB";
                ViewBag.Nombre = nombre;
                ViewBag.Telefono = telefono;
                ViewBag.Direccion = direccion;
                ViewBag.Avatar = currentAvatar;
                return View();
            }

            var fileName = Guid.NewGuid().ToString() + extension;
            var folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img");
            
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var fullPath = Path.Combine(folder, fileName);
            
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await avatarFile.CopyToAsync(stream);
            }
            
            avatarPath = "/img/" + fileName;

            // ELIMINAR AVATAR ANTERIOR SI NO ES EL DEFAULT
            if (!string.IsNullOrEmpty(currentAvatar) && 
                currentAvatar != "/img/usuario.png" &&
                !currentAvatar.Contains("default"))
            {
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", 
                    currentAvatar.TrimStart('/'));
                if (System.IO.File.Exists(oldFilePath))
                {
                    try
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                    catch
                    {
                        // Si no se puede eliminar, continuar
                    }
                }
            }
        }

        // ACTUALIZAR CONTRASEÑA
        string passwordHash;
        if (!string.IsNullOrEmpty(password))
        {
            passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        }
        else
        {
            passwordHash = await GetCurrentPasswordAsync(email, connection);
        }

        // ACTUALIZAR EN BD
        var sql = @"
            UPDATE Usuarios
            SET nombre = @nombre,
                telefono = @telefono,
                direccion = @direccion,
                avatar_url = @avatar,
                password_hash = @passwordHash
            WHERE email = @email";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@nombre", nombre);
        cmd.Parameters.AddWithValue("@telefono", string.IsNullOrEmpty(telefono) ? (object)DBNull.Value : telefono);
        cmd.Parameters.AddWithValue("@direccion", string.IsNullOrEmpty(direccion) ? (object)DBNull.Value : direccion);
        cmd.Parameters.AddWithValue("@avatar", avatarPath);
        cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
        cmd.Parameters.AddWithValue("@email", email);

        await cmd.ExecuteNonQueryAsync();

        // ACTUALIZAR LA COOKIE CON LOS NUEVOS DATOS
        await RefreshUserClaimsAsync(email);

        TempData["Success"] = "Perfil actualizado correctamente";
        return RedirectToAction("Perfil");
    }

    // MÉTODOS AUXILIARES PRIVADOS
    private async Task<string> GetCurrentPasswordAsync(string email, SqlConnection connection)
    {
        var cmd = new SqlCommand("SELECT password_hash FROM Usuarios WHERE email = @e", connection);
        cmd.Parameters.AddWithValue("@e", email);
        
        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString() ?? "";
    }

    private async Task<string> GetCurrentAvatarAsync(string email, SqlConnection connection)
    {
        var cmd = new SqlCommand("SELECT avatar_url FROM Usuarios WHERE email = @e", connection);
        cmd.Parameters.AddWithValue("@e", email);
        
        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString() ?? "/img/usuario.png";
    }

    private async Task RefreshUserClaimsAsync(string email)
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        
        using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var sql = @"
            SELECT id_usuario, nombre, email, rol, avatar_url 
            FROM Usuarios 
            WHERE email = @email";

        using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.AddWithValue("@email", email);

        using var reader = await cmd.ExecuteReaderAsync();
        
        if (await reader.ReadAsync())
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, reader["nombre"]?.ToString() ?? ""),
                new Claim(ClaimTypes.Email, reader["email"]?.ToString() ?? ""),
                new Claim(ClaimTypes.Role, reader["rol"]?.ToString() ?? ""),
                new Claim("Avatar", reader["avatar_url"]?.ToString() ?? "/img/usuario.png")
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal);
        }
    }
}