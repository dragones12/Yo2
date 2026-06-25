using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CBN_Online.Models;
using CBN_Online.Data;
using System.IO; // Para manejo de archivos
using Microsoft.AspNetCore.Hosting; // Para IWebHostEnvironment

namespace CBN_Online.Controllers
{
    public class EmpresaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public EmpresaController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Empresa
        public async Task<IActionResult> Index()
        {
            var empresas = await _context.Empresa.ToListAsync();
            return View(empresas);
        }

        // GET: Empresa/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var empresa = await _context.Empresa
                .Include(e => e.Marcas)
                .ThenInclude(m => m.Productos)
                .FirstOrDefaultAsync(m => m.id_empresa == id);

            if (empresa == null) return NotFound();

            return View(empresa);
        }

        // GET: Empresa/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Empresa/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("nombre_empresa,descripcion")] Empresa empresa, IFormFile LogoFile)
        {
            Console.WriteLine("=== CREATE EMPRESA POST ===");
            Console.WriteLine($"Nombre: {empresa.nombre_empresa}");
            Console.WriteLine($"Descripción: {empresa.descripcion}");
            Console.WriteLine($"Logo recibido: {LogoFile?.FileName ?? "Sin logo"}");

            // Limpiar errores de validación
            ModelState.Remove("LogoFile");
            ModelState.Remove("url_logo");

            if (ModelState.IsValid)
            {
                try
                {
                    // Guardar el logo si se subió
                    if (LogoFile != null && LogoFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "empresas");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + LogoFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await LogoFile.CopyToAsync(fileStream);
                        }

                        empresa.url_logo = "/images/empresas/" + uniqueFileName;
                    }
                    else
                    {
                        // Logo por defecto
                        empresa.url_logo = "/img/imagen.png";
                    }

                    empresa.created_at = DateTime.Now;
                    _context.Add(empresa);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "✅ Empresa creada correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                    
                    ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine($"ERROR: {error.ErrorMessage}");
                }
            }

            return View(empresa);
        }

        // GET: Empresa/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var empresa = await _context.Empresa.FindAsync(id);
            if (empresa == null) return NotFound();

            return View(empresa);
        }

        // POST: Empresa/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_empresa,nombre_empresa,descripcion")] Empresa empresa, IFormFile LogoFile)
        {
            Console.WriteLine("=== EDIT EMPRESA POST ===");
            Console.WriteLine($"ID: {empresa.id_empresa}");
            Console.WriteLine($"Nombre: {empresa.nombre_empresa}");
            Console.WriteLine($"Descripción: {empresa.descripcion}");
            Console.WriteLine($"Logo recibido: {LogoFile?.FileName ?? "Sin logo"}");

            if (id != empresa.id_empresa) return NotFound();

            // Limpiar errores de validación
            ModelState.Remove("LogoFile");
            ModelState.Remove("url_logo");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingEmpresa = await _context.Empresa.FindAsync(id);
                    if (existingEmpresa == null)
                        return NotFound();

                    // Actualizar campos
                    existingEmpresa.nombre_empresa = empresa.nombre_empresa;
                    existingEmpresa.descripcion = empresa.descripcion;

                    // Manejar el logo
                    if (LogoFile != null && LogoFile.Length > 0)
                    {
                        // Eliminar logo anterior si existe y no es la default
                        if (!string.IsNullOrEmpty(existingEmpresa.url_logo) && 
                            !existingEmpresa.url_logo.Contains("imagen.png"))
                        {
                            string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, 
                                existingEmpresa.url_logo.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Guardar nuevo logo
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "empresas");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + LogoFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await LogoFile.CopyToAsync(fileStream);
                        }

                        existingEmpresa.url_logo = "/images/empresas/" + uniqueFileName;
                    }

                    _context.Update(existingEmpresa);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "✅ Empresa actualizada correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmpresaExists(empresa.id_empresa))
                        return NotFound();
                    else
                        throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                    
                    ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine($"ERROR: {error.ErrorMessage}");
                }
            }

            return View(empresa);
        }

        // GET: Empresa/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var empresa = await _context.Empresa
                .Include(e => e.Marcas)
                .FirstOrDefaultAsync(m => m.id_empresa == id);

            if (empresa == null) return NotFound();

            // Verificar si tiene marcas
            ViewBag.TieneMarcas = empresa.Marcas != null && empresa.Marcas.Any();

            return View(empresa);
        }

        // POST: Empresa/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var empresa = await _context.Empresa
                    .Include(e => e.Marcas)
                    .FirstOrDefaultAsync(e => e.id_empresa == id);

                if (empresa == null)
                {
                    TempData["ErrorMessage"] = "Empresa no encontrada";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si tiene marcas
                if (empresa.Marcas != null && empresa.Marcas.Any())
                {
                    TempData["ErrorMessage"] = "❌ No se puede eliminar la empresa porque tiene marcas asociadas. Elimina las marcas primero.";
                    return RedirectToAction(nameof(Index));
                }

                // Verificar si tiene pedidos
                var tienePedidos = await _context.Detalle_Pedidos
                    .AnyAsync(dp => dp.Producto.Marca.id_empresa == id);

                if (tienePedidos)
                {
                    TempData["ErrorMessage"] = "❌ No se puede eliminar la empresa porque tiene productos con pedidos asociados.";
                    return RedirectToAction(nameof(Index));
                }

                // Eliminar el logo asociado
                if (!string.IsNullOrEmpty(empresa.url_logo) && 
                    !empresa.url_logo.Contains("imagen.png"))
                {
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath, 
                        empresa.url_logo.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Empresa.Remove(empresa);
                await _context.SaveChangesAsync();
                
                TempData["SuccessMessage"] = "✅ Empresa eliminada correctamente.";
            }
            catch (DbUpdateException ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                
                if (innerMessage.Contains("REFERENCE constraint"))
                {
                    TempData["ErrorMessage"] = "❌ No se puede eliminar porque tiene registros relacionados.";
                }
                else
                {
                    TempData["ErrorMessage"] = $"❌ Error de base de datos: {innerMessage}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Error inesperado: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool EmpresaExists(int id)
        {
            return _context.Empresa.Any(e => e.id_empresa == id);
        }
    }
}