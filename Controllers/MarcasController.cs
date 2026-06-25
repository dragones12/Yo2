using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CBN_Online.Data;
using CBN_Online.Models;
using System.IO; // Para manejo de archivos
using Microsoft.AspNetCore.Hosting; // Para IWebHostEnvironment

namespace CBN_Online.Controllers
{
    public class MarcasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public MarcasController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Marcas
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Marcas.Include(m => m.Empresa);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Marcas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var marca = await _context.Marcas
                .Include(m => m.Empresa)
                .FirstOrDefaultAsync(m => m.id_marca == id);
            if (marca == null)
            {
                return NotFound();
            }

            return View(marca);
        }

        // GET: Marcas/Create
        public IActionResult Create()
        {
            ViewData["id_empresa"] = new SelectList(_context.Empresa, "id_empresa", "nombre_empresa");
            return View();
        }

        // POST: Marcas/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_empresa,nombre_marca,tipo_marca,descripcion,es_activo")] Marca marca, IFormFile ImagenFile)
        {
            Console.WriteLine("=== CREATE POST EJECUTADO ===");
            Console.WriteLine($"ID Empresa: {marca.id_empresa}");
            Console.WriteLine($"Nombre: {marca.nombre_marca}");
            Console.WriteLine($"Tipo: {marca.tipo_marca}");
            Console.WriteLine($"Descripción: {marca.descripcion}");
            Console.WriteLine($"Es Activo: {marca.es_activo}");
            Console.WriteLine($"Imagen recibida: {ImagenFile?.FileName ?? "Sin imagen"}");

            // Limpiar errores de validación de navegación
            ModelState.Remove("Empresa");
            ModelState.Remove("ImagenFile");
            ModelState.Remove("url_imagen");

            if (ModelState.IsValid)
            {
                try
                {
                    if (marca.id_empresa == 0)
                    {
                        ModelState.AddModelError("id_empresa", "Debe seleccionar una empresa.");
                        ViewData["id_empresa"] = new SelectList(_context.Empresa, "id_empresa", "nombre_empresa", marca.id_empresa);
                        return View(marca);
                    }

                    // Guardar la imagen si se subió
                    if (ImagenFile != null && ImagenFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "marcas");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + ImagenFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImagenFile.CopyToAsync(fileStream);
                        }

                        marca.url_imagen = "/images/marcas/" + uniqueFileName;
                    }
                    else
                    {
                        // Imagen por defecto
                        marca.url_imagen = "/images/marcas/default-brand.png";
                    }

                    marca.created_at = DateTime.Now;
                    Console.WriteLine($"Fecha asignada: {marca.created_at}");

                    Console.WriteLine("Guardando en BD...");
                    _context.Add(marca);
                    await _context.SaveChangesAsync();
                    Console.WriteLine("¡Guardado exitoso!");

                    TempData["SuccessMessage"] = "Marca creada correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"EXCEPCIÓN: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"INNER: {ex.InnerException.Message}");

                    ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                Console.WriteLine($"ModelState NO es válido. Errores: {errors.Count()}");
                foreach (var error in errors)
                {
                    Console.WriteLine($"ERROR: {error.ErrorMessage}");
                }
            }

            ViewData["id_empresa"] = new SelectList(_context.Empresa, "id_empresa", "nombre_empresa", marca.id_empresa);
            return View(marca);
        }

        // GET: Marcas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var marca = await _context.Marcas.FindAsync(id);
            if (marca == null)
                return NotFound();

            ViewData["id_empresa"] = new SelectList(_context.Empresa, "id_empresa", "nombre_empresa", marca.id_empresa);
            return View(marca);
        }

        // POST: Marcas/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_marca,id_empresa,nombre_marca,tipo_marca,descripcion,es_activo")] Marca marca, IFormFile ImagenFile)
        {
            Console.WriteLine("=== EDIT POST EJECUTADO ===");
            Console.WriteLine($"ID: {marca.id_marca}");
            Console.WriteLine($"ID Empresa: {marca.id_empresa}");
            Console.WriteLine($"Nombre: {marca.nombre_marca}");
            Console.WriteLine($"Tipo: {marca.tipo_marca}");
            Console.WriteLine($"Imagen recibida: {ImagenFile?.FileName ?? "Sin imagen"}");

            if (id != marca.id_marca)
                return NotFound();

            // Limpiar error de navegación
            ModelState.Remove("Empresa");
            ModelState.Remove("ImagenFile");
            ModelState.Remove("url_imagen");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingMarca = await _context.Marcas.FindAsync(id);
                    if (existingMarca == null)
                        return NotFound();

                    // Actualizar solo los campos permitidos
                    existingMarca.id_empresa = marca.id_empresa;
                    existingMarca.nombre_marca = marca.nombre_marca;
                    existingMarca.tipo_marca = marca.tipo_marca;
                    existingMarca.descripcion = marca.descripcion;
                    existingMarca.es_activo = marca.es_activo;

                    // Manejar la imagen
                    if (ImagenFile != null && ImagenFile.Length > 0)
                    {
                        // Eliminar imagen anterior si existe y no es la default
                        if (!string.IsNullOrEmpty(existingMarca.url_imagen) &&
                            !existingMarca.url_imagen.Contains("default-brand.png"))
                        {
                            string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                                existingMarca.url_imagen.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Guardar nueva imagen
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "marcas");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + ImagenFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImagenFile.CopyToAsync(fileStream);
                        }

                        existingMarca.url_imagen = "/images/marcas/" + uniqueFileName;
                    }
                    // Si no se sube nueva imagen, mantener la existente

                    _context.Update(existingMarca);
                    await _context.SaveChangesAsync();

                    Console.WriteLine("¡Actualizado exitosamente!");
                    TempData["SuccessMessage"] = "Marca actualizada correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MarcaExists(marca.id_marca))
                        return NotFound();
                    else
                        throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"INNER: {ex.InnerException.Message}");

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

            ViewData["id_empresa"] = new SelectList(_context.Empresa, "id_empresa", "nombre_empresa", marca.id_empresa);
            return View(marca);
        }

        // GET: Marcas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var marca = await _context.Marcas
                .Include(m => m.Empresa)
                .FirstOrDefaultAsync(m => m.id_marca == id);

            if (marca == null)
            {
                return NotFound();
            }

            // Verificar si tiene productos para mostrar advertencia
            var tieneProductos = await _context.Productos.AnyAsync(p => p.id_marca == id);
            ViewBag.TieneProductos = tieneProductos;

            return View(marca);
        }
        // POST: Marcas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var marca = await _context.Marcas
                .Include(m => m.Productos)
                .FirstOrDefaultAsync(m => m.id_marca == id);

            if (marca == null)
            {
                return NotFound();
            }

            // Verificar si tiene productos
            if (marca.Productos != null && marca.Productos.Any())
            {
                TempData["ErrorMessage"] = "No se puede eliminar la marca porque tiene productos asociados. Elimina los productos primero.";
                return RedirectToAction(nameof(Index));
            }

            // Eliminar la imagen asociada
            if (!string.IsNullOrEmpty(marca.url_imagen) &&
                !marca.url_imagen.Contains("default-brand.png"))
            {
                string imagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                    marca.url_imagen.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Marcas.Remove(marca);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Marca eliminada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        private bool MarcaExists(int id)
        {
            return _context.Marcas.Any(e => e.id_marca == id);
        }
    }
}