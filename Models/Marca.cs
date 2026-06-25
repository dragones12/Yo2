using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CBN_Online.Models
{
    public class Marca
    {
        [Key]
        public int id_marca { get; set; }

        [Required]
        public int id_empresa { get; set; }

        [Required]
        [StringLength(50)]
        public string nombre_marca { get; set; }

        [Required]
        [StringLength(30)]
        public string tipo_marca { get; set; }

        public string descripcion { get; set; }

        [StringLength(255)]
        public string url_imagen { get; set; }

        public bool es_activo { get; set; } = true;

        public DateTime created_at { get; set; } = DateTime.Now;  

        [ForeignKey("id_empresa")]
        public virtual Empresa Empresa { get; set; }
        public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
    }
}