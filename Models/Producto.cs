using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBN_Online.Models
{
    public class Producto
    {
        [Key]
        public int id_producto { get; set; }
        
        [Required]
        public int id_marca { get; set; }
        
        [Required]
        [StringLength(100)]
        public string nombre_producto { get; set; }
        
        [StringLength(50)]
        public string estilo { get; set; }
        
        public decimal? graduacion_alcohol { get; set; }
        
        public string descripcion { get; set; }
        
        [StringLength(255)]
        public string url_imagen { get; set; } 
        
        [Required]
        public decimal precio { get; set; }
        
        public int stock { get; set; } = 0;
        
        public bool es_activo { get; set; } = true;
        
        public DateTime created_at { get; set; } = DateTime.Now;
        
         [ForeignKey("id_marca")]
        public virtual Marca Marca { get; set; }
        
        public virtual ICollection<Detalle_Pedido> Detalle_Pedidos { get; set; } = new List<Detalle_Pedido>();
    }
}