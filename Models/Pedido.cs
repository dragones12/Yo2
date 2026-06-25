using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBN_Online.Models
{
    public class Pedido
    {
        [Key]
        public int id_pedido { get; set; }
        
        [Required]
        public int id_usuario { get; set; }
        
        public DateTime fecha_pedido { get; set; } = DateTime.Now;
        
        [Required]
        public decimal total { get; set; }
        
        [Required]
        [StringLength(20)]
        public string estado { get; set; } = "Pendiente";
        
        [StringLength(30)]
        public string metodo_pago { get; set; }
        
        [Required]
        public string direccion_envio { get; set; }
        
        public string nota { get; set; }
        
        public DateTime created_at { get; set; } = DateTime.Now;
        
        [ForeignKey("id_usuario")]
        public virtual Usuario Usuario { get; set; }
        
        public virtual ICollection<Detalle_Pedido> Detalle_Pedidos { get; set; } = new List<Detalle_Pedido>();
    }
}