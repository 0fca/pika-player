using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Claudia.Data
{
    public class Lecture
    {
        [Key]
        [Column("id")]
        public string Id { get; set; }
        
        [Required]
        [Column("display_name")]
        public string DisplayName { get; set; }
        
        [Required]
        [Column("description")]
        public string Description { get; set; }
        
        [Required]
        [Column("video_path")]
        public string VideoPath { get; set; }
        
        [Column("thumbnail")]
        public byte[] Thumbnail { get; set; }
        
        [Column("mime_type")]
        public string MimeType { get; set; }
        
        [Required]
        [Column("lecturer_id")]
        public string LecturerId { get; set; }

        [Required] 
        [Column("course_id")]
        public int CourseId { get; set; }

        [Required]
        [Column("date_added")]
        public DateTime DateAdded { get; set; }

        [Column("is_locked")] 
        public bool IsLocked { get; set; } = false;
    }
}