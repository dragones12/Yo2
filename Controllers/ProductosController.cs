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
    public class ProductosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductosController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Productos
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Productos
                .Include(p => p.Marca)
                .Where(p => p.es_activo == true);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Productos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Marca)
                .FirstOrDefaultAsync(m => m.id_producto == id);

            if (producto == null)
                return NotFound();

            return View(producto);
        }

        // GET: Productos/Create
        public IActionResult Create()
        {
            ViewData["id_marca"] = new SelectList(_context.Marcas.Where(m => m.es_activo == true), "id_marca", "nombre_marca");
            return View();
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_marca,nombre_producto,estilo,graduacion_alcohol,descripcion,precio,stock,es_activo")] Producto producto, IFormFile ImagenFile)
        {
            Console.WriteLine("=== CREATE PRODUCTO POST ===");
            Console.WriteLine($"Marca: {producto.id_marca}");
            Console.WriteLine($"Nombre: {producto.nombre_producto}");
            Console.WriteLine($"Precio: {producto.precio}");
            Console.WriteLine($"Stock: {producto.stock}");
            Console.WriteLine($"Graduación: {producto.graduacion_alcohol}");
            Console.WriteLine($"Imagen recibida: {ImagenFile?.FileName ?? "Sin imagen"}");

            // Limpiar errores de validación
            ModelState.Remove("Marca");
            ModelState.Remove("ImagenFile");
            ModelState.Remove("url_imagen");

            if (ModelState.IsValid)
            {
                try
                {
                    // Guardar la imagen si se subió
                    if (ImagenFile != null && ImagenFile.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "productos");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + ImagenFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImagenFile.CopyToAsync(fileStream);
                        }

                        producto.url_imagen = "/images/productos/" + uniqueFileName;
                    }
                    else
                    {
                        // Imagen por defecto
                        producto.url_imagen = "/images/productos/default-product.png";
                    }

                    producto.created_at = DateTime.Now;
                    producto.es_activo = true; // <--- NUEVA LÍNEA: Asegurar que esté activo
                    _context.Add(producto);
                    await _context.SaveChangesAsync();

                    // VERIFICACIÓN: Obtener el producto guardado
                    var productoGuardado = await _context.Productos
                        .Include(p => p.Marca)
                        .FirstOrDefaultAsync(p => p.id_producto == producto.id_producto);

                    Console.WriteLine($"=== PRODUCTO GUARDADO ===");
                    Console.WriteLine($"ID: {productoGuardado?.id_producto}");
                    Console.WriteLine($"Nombre: {productoGuardado?.nombre_producto}");
                    Console.WriteLine($"Marca: {productoGuardado?.Marca?.nombre_marca}");
                    Console.WriteLine($"Activo: {productoGuardado?.es_activo}");
                    Console.WriteLine($"URL Imagen: {productoGuardado?.url_imagen}");
                    Console.WriteLine($"========================");

                    TempData["SuccessMessage"] = "Producto creado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    // Mostrar el error completo
                    Console.WriteLine($"Error: {ex.Message}");

                    // Si hay inner exception, mostrarla
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                        Console.WriteLine($"Inner StackTrace: {ex.InnerException.StackTrace}");
                        ModelState.AddModelError("", $"Error interno: {ex.InnerException.Message}");
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Error al guardar: {ex.Message}");
                    }
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine($"ERROR DE VALIDACIÓN: {error.ErrorMessage}");
                }
            }

            ViewData["id_marca"] = new SelectList(_context.Marcas.Where(m => m.es_activo == true), "id_marca", "nombre_marca", producto.id_marca);
            return View(producto);
        }

        // GET: Productos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var producto = await _context.Productos.FindAsync(id);
            if (producto == null)
                return NotFound();

            ViewData["id_marca"] = new SelectList(_context.Marcas.Where(m => m.es_activo == true), "id_marca", "nombre_marca", producto.id_marca);
            return View(producto);
        }

        // POST: Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_producto,id_marca,nombre_producto,estilo,graduacion_alcohol,descripcion,precio,stock,es_activo")] Producto producto, IFormFile ImagenFile)
        {
            Console.WriteLine("=== EDIT PRODUCTO POST ===");
            Console.WriteLine($"ID: {producto.id_producto}");
            Console.WriteLine($"Marca: {producto.id_marca}");
            Console.WriteLine($"Nombre: {producto.nombre_producto}");
            Console.WriteLine($"Precio: {producto.precio}");
            Console.WriteLine($"Stock: {producto.stock}");
            Console.WriteLine($"Graduación: {producto.graduacion_alcohol}");
            Console.WriteLine($"Imagen recibida: {ImagenFile?.FileName ?? "Sin imagen"}");

            if (id != producto.id_producto)
                return NotFound();

            // Limpiar errores de validación
            ModelState.Remove("Marca");
            ModelState.Remove("ImagenFile");
            ModelState.Remove("url_imagen");

            if (ModelState.IsValid)
            {
                try
                {
                    var existingProducto = await _context.Productos.FindAsync(id);
                    if (existingProducto == null)
                        return NotFound();

                    // Actualizar campos
                    existingProducto.id_marca = producto.id_marca;
                    existingProducto.nombre_producto = producto.nombre_producto;
                    existingProducto.estilo = producto.estilo;
                    existingProducto.graduacion_alcohol = producto.graduacion_alcohol;
                    existingProducto.descripcion = producto.descripcion;
                    existingProducto.precio = producto.precio;
                    existingProducto.stock = producto.stock;
                    existingProducto.es_activo = producto.es_activo;

                    // Manejar la imagen
                    if (ImagenFile != null && ImagenFile.Length > 0)
                    {
                        // Eliminar imagen anterior si existe y no es la default
                        if (!string.IsNullOrEmpty(existingProducto.url_imagen) &&
                            !existingProducto.url_imagen.Contains("default-product.png"))
                        {
                            string oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                                existingProducto.url_imagen.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Guardar nueva imagen
                        string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "productos");
                        Directory.CreateDirectory(uploadsFolder);

                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + ImagenFile.FileName;
                        string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await ImagenFile.CopyToAsync(fileStream);
                        }

                        existingProducto.url_imagen = "/images/productos/" + uniqueFileName;
                    }

                    _context.Update(existingProducto);
                    await _context.SaveChangesAsync();

                    // VERIFICACIÓN: Mostrar producto actualizado
                    Console.WriteLine($"=== PRODUCTO ACTUALIZADO ===");
                    Console.WriteLine($"ID: {existingProducto.id_producto}");
                    Console.WriteLine($"Nombre: {existingProducto.nombre_producto}");
                    Console.WriteLine($"Activo: {existingProducto.es_activo}");
                    Console.WriteLine($"URL Imagen: {existingProducto.url_imagen}");

                    TempData["SuccessMessage"] = "Producto actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.id_producto))
                        return NotFound();
                    else
                        throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
                        ModelState.AddModelError("", $"Error interno: {ex.InnerException.Message}");
                    }
                    else
                    {
                        ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
                    }
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                foreach (var error in errors)
                {
                    Console.WriteLine($"ERROR DE VALIDACIÓN: {error.ErrorMessage}");
                }
            }

            ViewData["id_marca"] = new SelectList(_context.Marcas.Where(m => m.es_activo == true), "id_marca", "nombre_marca", producto.id_marca);
            return View(producto);
        }

        // GET: Productos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var producto = await _context.Productos
                .Include(p => p.Marca)
                .Include(p => p.Detalle_Pedidos)
                .FirstOrDefaultAsync(m => m.id_producto == id);

            if (producto == null)
                return NotFound();

            return View(producto);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Detalle_Pedidos)
                .FirstOrDefaultAsync(p => p.id_producto == id);

            if (producto == null)
                return NotFound();

            // Verificar si tiene detalles en pedidos
            if (producto.Detalle_Pedidos != null && producto.Detalle_Pedidos.Any())
            {
                TempData["ErrorMessage"] = "No se puede eliminar el producto porque tiene pedidos asociados. Desactívelo en su lugar.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Eliminar la imagen asociada
                if (!string.IsNullOrEmpty(producto.url_imagen) &&
                    !producto.url_imagen.Contains("default-product.png"))
                {
                    string imagePath = Path.Combine(_webHostEnvironment.WebRootPath,
                        producto.url_imagen.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Producto eliminado correctamente.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar: {ex.Message}");
                TempData["ErrorMessage"] = "Error al eliminar el producto. Verifique que no tenga pedidos asociados.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.id_producto == id);
        }

        // GET: Productos/GetImageUrls
        public async Task<IActionResult> GetImageUrls()
        {
            var productos = await _context.Productos
                .Where(p => p.es_activo == true)
                .Select(p => new
                {
                    p.id_producto,
                    p.nombre_producto,
                    p.url_imagen
                })
                .ToListAsync();

            return Json(productos);
        }
    }
}