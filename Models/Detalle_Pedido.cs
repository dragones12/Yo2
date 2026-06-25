using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBN_Online.Models
{
    public class Detalle_Pedido
    {
        [Key]
        public int id_detalle { get; set; }
        
        [Required]
        public int id_pedido { get; set; }
        
        [Required]
        public int id_producto { get; set; }
        
        [Required]
        public int cantidad { get; set; }
        
        [Required]
        public decimal precio_unitario { get; set; }
        
        [Required]
        public decimal subtotal { get; set; }
        
        public DateTime created_at { get; set; } = DateTime.Now;
        
        [ForeignKey("id_pedido")]
        public virtual Pedido Pedido { get; set; }
        
        [ForeignKey("id_producto")]
        public virtual Producto Producto { get; set; }
    }
}