using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Claudia.Data
{
    public class Expiry
    {
        [Key]
        [Column("id")]
        public string Id { get; set; }
        
        [Column("expiry_date")]
        public DateTime ExpiryDate { get; set; }
    }
}