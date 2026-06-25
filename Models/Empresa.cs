using System.ComponentModel.DataAnnotations;

namespace CBN_Online.Models
{
    public class Empresa
    {
        [Key]
        public int id_empresa { get; set; }
        
        [Required]
        [StringLength(100)]
        public string nombre_empresa { get; set; }
        
        public string descripcion { get; set; }
        
        [StringLength(255)]
        public string url_logo { get; set; }
        
        public DateTime created_at { get; set; } = DateTime.Now;
         public virtual ICollection<Marca> Marcas { get; set; } = new List<Marca>();
    }
}