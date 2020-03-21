using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Claudia.Data
{
    public class Comment
    {
        [Key]
        [Column("comment_id")]
        public string CommentId { get; set; }

        [Required]
        [Column("video_id")]
        public string VideoId { get; set; }
        
        [Required]
        [Column("user_id")]
        public string UserId { get; set; }

        [Required]
        [Column("content")]
        public string Content { get; set; }
    }
}