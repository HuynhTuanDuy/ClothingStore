using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClothingStore.Models.Entities
{
    [Table("PROVINCES")]
    public class Province
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // Using external IDs (e.g. 1 for Hanoi)
        public int ProvinceId { get; set; }
        
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;

        public ICollection<District> Districts { get; set; } = new List<District>();
    }

    [Table("DISTRICTS")]
    public class District
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DistrictId { get; set; }
        
        public int ProvinceId { get; set; }
        
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;

        public Province Province { get; set; } = null!;
        public ICollection<Ward> Wards { get; set; } = new List<Ward>();
    }

    [Table("WARDS")]
    public class Ward
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int WardId { get; set; }
        
        public int DistrictId { get; set; }
        
        [MaxLength(20)]
        public string Code { get; set; } = string.Empty;
        
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;

        public District District { get; set; } = null!;
    }
}
