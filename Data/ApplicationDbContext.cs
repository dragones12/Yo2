using Microsoft.EntityFrameworkCore;
using CBN_Online.Models;

namespace CBN_Online.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Empresa> Empresa { get; set; }
        public DbSet<Marca> Marcas { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<Detalle_Pedido> Detalle_Pedidos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuraciones
            modelBuilder.Entity<Usuario>()
                .HasIndex(u => u.email)
                .IsUnique();

            modelBuilder.Entity<Pedido>()
                .Property(p => p.estado)
                .HasConversion<string>();

            modelBuilder.Entity<Marca>()
                .Property(m => m.tipo_marca)
                .HasConversion<string>();

            // =========================================
            // CONFIGURACIÓN DE ELIMINACIÓN EN CASCADA
            // =========================================
            
            // Empresa -> Marcas
            modelBuilder.Entity<Marca>()
                .HasOne(m => m.Empresa)
                .WithMany(e => e.Marcas)
                .HasForeignKey(m => m.id_empresa)
                .OnDelete(DeleteBehavior.Cascade);

            // Marca -> Productos
            modelBuilder.Entity<Producto>()
                .HasOne(p => p.Marca)
                .WithMany(m => m.Productos)
                .HasForeignKey(p => p.id_marca)
                .OnDelete(DeleteBehavior.Cascade);

            // Pedido -> Detalle_Pedidos
            modelBuilder.Entity<Detalle_Pedido>()
                .HasOne(dp => dp.Pedido)
                .WithMany(p => p.Detalle_Pedidos)
                .HasForeignKey(dp => dp.id_pedido)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Producto -> Detalle_Pedidos (NO eliminar en cascada para proteger datos)
            modelBuilder.Entity<Detalle_Pedido>()
                .HasOne(dp => dp.Producto)
                .WithMany(p => p.Detalle_Pedidos)
                .HasForeignKey(dp => dp.id_producto)
                .OnDelete(DeleteBehavior.Restrict);  // ← Evita eliminar productos con pedidos
        }
    }
}