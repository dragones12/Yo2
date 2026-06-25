using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBN_Online.Models
{
    public class Usuario
    {
        [Key]
        public int id_usuario { get; set; }
        
        [Required]
        [StringLength(100)]
        public string nombre { get; set; }
        
        [Required]
        [StringLength(100)]
        public string email { get; set; }
        
        [Required]
        [StringLength(255)]
        public string password_hash { get; set; }
        
        [Required]
        [StringLength(20)]
        public string rol { get; set; }
        
        [StringLength(20)]
        public string telefono { get; set; }
        
        public string direccion { get; set; }
        
        [StringLength(255)]
        public string avatar_url { get; set; }
        
        public bool es_activo { get; set; } = true;
        
        public DateTime created_at { get; set; } = DateTime.Now;
        public virtual ICollection<Pedido> Pedidos { get; set; } = new List<Pedido>();
    }
}