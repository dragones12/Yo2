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
    public class PedidosController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PedidosController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Pedidos
        public async Task<IActionResult> Index()
        {
            var pedidos = _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Detalle_Pedidos)
                .OrderByDescending(p => p.created_at);
            return View(await pedidos.ToListAsync());
        }

        // GET: Pedidos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Detalle_Pedidos)
                    .ThenInclude(dp => dp.Producto)
                        .ThenInclude(p => p.Marca)
                .FirstOrDefaultAsync(m => m.id_pedido == id);
            
            if (pedido == null)
                return NotFound();

            return View(pedido);
        }

        // GET: Pedidos/Create
        public IActionResult Create()
        {
            ViewData["id_usuario"] = new SelectList(_context.Usuarios, "id_usuario", "email");
            ViewData["estados"] = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pendiente", Text = "Pendiente" },
                new SelectListItem { Value = "Pagado", Text = "Pagado" },
                new SelectListItem { Value = "Enviado", Text = "Enviado" },
                new SelectListItem { Value = "Entregado", Text = "Entregado" },
                new SelectListItem { Value = "Cancelado", Text = "Cancelado" }
            };
            ViewData["metodos_pago"] = new List<SelectListItem>
            {
                new SelectListItem { Value = "Tarjeta", Text = "Tarjeta" },
                new SelectListItem { Value = "Transferencia", Text = "Transferencia" },
                new SelectListItem { Value = "Efectivo", Text = "Efectivo" },
                new SelectListItem { Value = "PayPal", Text = "PayPal" },
                new SelectListItem { Value = "Otro", Text = "Otro" }
            };
            return View();
        }

        // POST: Pedidos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("id_usuario,estado,metodo_pago,direccion_envio,nota")] Pedido pedido)
        {
            Console.WriteLine("=== CREATE PEDIDO POST ===");
            Console.WriteLine($"ID Usuario: {pedido.id_usuario}");
            Console.WriteLine($"Estado: {pedido.estado}");
            Console.WriteLine($"Método Pago: {pedido.metodo_pago}");

            // Limpiar errores de navegación
            ModelState.Remove("Usuario");
            ModelState.Remove("Detalle_Pedidos");

            if (ModelState.IsValid)
            {
                try
                {
                    // Asignar valores automáticos
                    pedido.fecha_pedido = DateTime.Now;
                    pedido.created_at = DateTime.Now;
                    pedido.total = 0; // Se calculará con los detalles

                    _context.Add(pedido);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Pedido creado correctamente.";
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

            // Recargar datos para la vista
            ViewData["id_usuario"] = new SelectList(_context.Usuarios, "id_usuario", "email", pedido.id_usuario);
            ViewData["estados"] = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pendiente", Text = "Pendiente" },
                new SelectListItem { Value = "Pagado", Text = "Pagado" },
                new SelectListItem { Value = "Enviado", Text = "Enviado" },
                new SelectListItem { Value = "Entregado", Text = "Entregado" },
                new SelectListItem { Value = "Cancelado", Text = "Cancelado" }
            };
            ViewData["metodos_pago"] = new List<SelectListItem>
            {
                new SelectListItem { Value = "Tarjeta", Text = "Tarjeta" },
                new SelectListItem { Value = "Transferencia", Text = "Transferencia" },
                new SelectListItem { Value = "Efectivo", Text = "Efectivo" },
                new SelectListItem { Value = "PayPal", Text = "PayPal" },
                new SelectListItem { Value = "Otro", Text = "Otro" }
            };
            return View(pedido);
        }

        // GET: Pedidos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
                return NotFound();
            
            ViewData["id_usuario"] = new SelectList(_context.Usuarios, "id_usuario", "email", pedido.id_usuario);
            ViewData["estados"] = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pendiente", Text = "Pendiente" },
                new SelectListItem { Value = "Pagado", Text = "Pagado" },
                new SelectListItem { Value = "Enviado", Text = "Enviado" },
                new SelectListItem { Value = "Entregado", Text = "Entregado" },
                new SelectListItem { Value = "Cancelado", Text = "Cancelado" }
            };
            ViewData["metodos_pago"] = new List<SelectListItem>
            {
                new SelectListItem { Value = "Tarjeta", Text = "Tarjeta" },
                new SelectListItem { Value = "Transferencia", Text = "Transferencia" },
                new SelectListItem { Value = "Efectivo", Text = "Efectivo" },
                new SelectListItem { Value = "PayPal", Text = "PayPal" },
                new SelectListItem { Value = "Otro", Text = "Otro" }
            };
            return View(pedido);
        }

        // POST: Pedidos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("id_pedido,id_usuario,estado,metodo_pago,direccion_envio,nota")] Pedido pedido)
        {
            Console.WriteLine("=== EDIT PEDIDO POST ===");
            Console.WriteLine($"ID: {pedido.id_pedido}");
            Console.WriteLine($"Estado: {pedido.estado}");
            Console.WriteLine($"Método Pago: {pedido.metodo_pago}");

            if (id != pedido.id_pedido)
                return NotFound();

            // Limpiar errores de navegación
            ModelState.Remove("Usuario");
            ModelState.Remove("Detalle_Pedidos");

            if (ModelState.IsValid)
            {
                try
                {
                    // Obtener el pedido existente
                    var existingPedido = await _context.Pedidos.FindAsync(id);
                    if (existingPedido == null)
                        return NotFound();

                    // Actualizar solo los campos permitidos
                    existingPedido.id_usuario = pedido.id_usuario;
                    existingPedido.estado = pedido.estado;
                    existingPedido.metodo_pago = pedido.metodo_pago;
                    existingPedido.direccion_envio = pedido.direccion_envio;
                    existingPedido.nota = pedido.nota;
                    // NO actualizar fecha_pedido, total, created_at

                    _context.Update(existingPedido);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Pedido actualizado correctamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PedidoExists(pedido.id_pedido))
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

            // Recargar datos para la vista
            ViewData["id_usuario"] = new SelectList(_context.Usuarios, "id_usuario", "email", pedido.id_usuario);
            ViewData["estados"] = new List<SelectListItem>
            {
                new SelectListItem { Value = "Pendiente", Text = "Pendiente" },
                new SelectListItem { Value = "Pagado", Text = "Pagado" },
                new SelectListItem { Value = "Enviado", Text = "Enviado" },
                new SelectListItem { Value = "Entregado", Text = "Entregado" },
                new SelectListItem { Value = "Cancelado", Text = "Cancelado" }
            };
            ViewData["metodos_pago"] = new List<SelectListItem>
            {
                new SelectListItem { Value = "Tarjeta", Text = "Tarjeta" },
                new SelectListItem { Value = "Transferencia", Text = "Transferencia" },
                new SelectListItem { Value = "Efectivo", Text = "Efectivo" },
                new SelectListItem { Value = "PayPal", Text = "PayPal" },
                new SelectListItem { Value = "Otro", Text = "Otro" }
            };
            return View(pedido);
        }

        // GET: Pedidos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.Detalle_Pedidos)
                .FirstOrDefaultAsync(m => m.id_pedido == id);
            
            if (pedido == null)
                return NotFound();

            return View(pedido);
        }

        // POST: Pedidos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var pedido = await _context.Pedidos
                .Include(p => p.Detalle_Pedidos)
                .FirstOrDefaultAsync(p => p.id_pedido == id);

            if (pedido == null)
                return NotFound();

            // Verificar si tiene detalles en pedidos
            if (pedido.Detalle_Pedidos != null && pedido.Detalle_Pedidos.Any())
            {
                TempData["ErrorMessage"] = "No se puede eliminar el pedido porque tiene detalles asociados. Elimine los detalles primero.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Pedidos.Remove(pedido);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Pedido eliminado correctamente.";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar: {ex.Message}");
                TempData["ErrorMessage"] = "Error al eliminar el pedido. Verifique que no tenga detalles asociados.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool PedidoExists(int id)
        {
            return _context.Pedidos.Any(e => e.id_pedido == id);
        }
    }
}