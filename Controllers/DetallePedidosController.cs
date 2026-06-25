using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using CBN_Online.Data;
using CBN_Online.Models;

namespace CBN_Online.Controllers
{
    public class DetallePedidosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DetallePedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: DetallePedidos
        public async Task<IActionResult> Index()
        {
            var detalles = _context.Detalle_Pedidos
                .Include(d => d.Pedido)
                    .ThenInclude(p => p.Usuario)
                .Include(d => d.Producto)
                    .ThenInclude(p => p.Marca)
                .OrderByDescending(d => d.created_at);
            return View(await detalles.ToListAsync());
        }

        // GET: DetallePedidos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var detalle = await _context.Detalle_Pedidos
                .Include(d => d.Pedido)
                    .ThenInclude(p => p.Usuario)
                .Include(d => d.Producto)
                    .ThenInclude(p => p.Marca)
                .FirstOrDefaultAsync(m => m.id_detalle == id);
            
            if (detalle == null)
                return NotFound();

            return View(detalle);
        }

        // GET: DetallePedidos/Create
        public IActionResult Create()
        {
            ViewData["id_pedido"] = new SelectList(_context.Pedidos, "id_pedido", "id_pedido");
            ViewData["id_producto"] = new SelectList(
                _context.Productos.Where(p => p.es_activo == true), 
                "id_producto", 
                "nombre_producto"
            );
            return View();
        }

        // POST: DetallePedidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_pedido,id_producto,cantidad,precio_unitario")] Detalle_Pedido detalle)
        {
            Console.WriteLine("=== CREATE DETALLE POST ===");
            Console.WriteLine($"ID Pedido: {detalle.id_pedido}");
            Console.WriteLine($"ID Producto: {detalle.id_producto}");
            Console.WriteLine($"Cantidad: {detalle.cantidad}");
            Console.WriteLine($"Precio Unitario: {detalle.precio_unitario}");

            // Limpiar errores de navegación
            ModelState.Remove("Pedido");
            ModelState.Remove("Producto");

            if (ModelState.IsValid)
            {
                try
                {
                    // Calcular subtotal
                    detalle.subtotal = detalle.cantidad * detalle.precio_unitario;
                    detalle.created_at = DateTime.Now;

                    _context.Add(detalle);
                    await _context.SaveChangesAsync();

                    // Actualizar el total del pedido
                    await ActualizarTotalPedido(detalle.id_pedido);

                    TempData["SuccessMessage"] = "Detalle agregado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al guardar: {ex.Message}");
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

            ViewData["id_pedido"] = new SelectList(_context.Pedidos, "id_pedido", "id_pedido", detalle.id_pedido);
            ViewData["id_producto"] = new SelectList(
                _context.Productos.Where(p => p.es_activo == true), 
                "id_producto", 
                "nombre_producto", 
                detalle.id_producto
            );
            return View(detalle);
        }

        // GET: DetallePedidos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var detalle = await _context.Detalle_Pedidos.FindAsync(id);
            if (detalle == null)
                return NotFound();
            
            ViewData["id_pedido"] = new SelectList(_context.Pedidos, "id_pedido", "id_pedido", detalle.id_pedido);
            ViewData["id_producto"] = new SelectList(
                _context.Productos.Where(p => p.es_activo == true), 
                "id_producto", 
                "nombre_producto", 
                detalle.id_producto
            );
            return View(detalle);
        }

        // POST: DetallePedidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_detalle,id_pedido,id_producto,cantidad,precio_unitario")] Detalle_Pedido detalle)
        {
            Console.WriteLine("=== EDIT DETALLE POST ===");
            Console.WriteLine($"ID: {detalle.id_detalle}");
            Console.WriteLine($"ID Pedido: {detalle.id_pedido}");
            Console.WriteLine($"ID Producto: {detalle.id_producto}");
            Console.WriteLine($"Cantidad: {detalle.cantidad}");
            Console.WriteLine($"Precio Unitario: {detalle.precio_unitario}");

            if (id != detalle.id_detalle)
                return NotFound();

            // Limpiar errores de navegación
            ModelState.Remove("Pedido");
            ModelState.Remove("Producto");

            if (ModelState.IsValid)
            {
                try
                {
                    // Obtener el detalle existente
                    var existingDetalle = await _context.Detalle_Pedidos.FindAsync(id);
                    if (existingDetalle == null)
                        return NotFound();

                    // Guardar el id_pedido original para actualizar total después
                    int idPedidoOriginal = existingDetalle.id_pedido;

                    // Actualizar solo los campos permitidos
                    existingDetalle.id_pedido = detalle.id_pedido;
                    existingDetalle.id_producto = detalle.id_producto;
                    existingDetalle.cantidad = detalle.cantidad;
                    existingDetalle.precio_unitario = detalle.precio_unitario;
                    existingDetalle.subtotal = detalle.cantidad * detalle.precio_unitario;
                    // NO actualizar created_at

                    _context.Update(existingDetalle);
                    await _context.SaveChangesAsync();

                    // Actualizar el total del pedido (nuevo y viejo si cambiaron)
                    if (idPedidoOriginal != detalle.id_pedido)
                    {
                        await ActualizarTotalPedido(idPedidoOriginal);
                    }
                    await ActualizarTotalPedido(detalle.id_pedido);

                    TempData["SuccessMessage"] = "Detalle actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!Detalle_PedidoExists(detalle.id_detalle))
                        return NotFound();
                    else
                        throw;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al actualizar: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"INNER: {ex.InnerException.Message}");
                    
                    ModelState.AddModelError("", $"Error al actualizar: {ex.Message}");
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

            ViewData["id_pedido"] = new SelectList(_context.Pedidos, "id_pedido", "id_pedido", detalle.id_pedido);
            ViewData["id_producto"] = new SelectList(
                _context.Productos.Where(p => p.es_activo == true), 
                "id_producto", 
                "nombre_producto", 
                detalle.id_producto
            );
            return View(detalle);
        }

        // GET: DetallePedidos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var detalle = await _context.Detalle_Pedidos
                .Include(d => d.Pedido)
                .Include(d => d.Producto)
                    .ThenInclude(p => p.Marca)
                .FirstOrDefaultAsync(m => m.id_detalle == id);
            
            if (detalle == null)
                return NotFound();

            return View(detalle);
        }

        // POST: DetallePedidos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var detalle = await _context.Detalle_Pedidos.FindAsync(id);
            if (detalle == null)
                return NotFound();

            try
            {
                int idPedido = detalle.id_pedido;
                
                _context.Detalle_Pedidos.Remove(detalle);
                await _context.SaveChangesAsync();

                // Actualizar el total del pedido
                await ActualizarTotalPedido(idPedido);

                TempData["SuccessMessage"] = "Detalle eliminado correctamente.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar: {ex.Message}");
                TempData["ErrorMessage"] = "Error al eliminar el detalle.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool Detalle_PedidoExists(int id)
        {
            return _context.Detalle_Pedidos.Any(e => e.id_detalle == id);
        }

        // Método para actualizar el total del pedido
        private async Task ActualizarTotalPedido(int idPedido)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalle_Pedidos)
                .FirstOrDefaultAsync(p => p.id_pedido == idPedido);

            if (pedido != null)
            {
                pedido.total = pedido.Detalle_Pedidos?.Sum(d => d.subtotal) ?? 0;
                _context.Update(pedido);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Total del pedido {idPedido} actualizado a: {pedido.total}");
            }
        }
    }
}