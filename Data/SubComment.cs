using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Claudia.Data
{
    public sealed class SubComment
    {
        [Key]
        [Column("id")]
        public int SubCommentId { get; set; }

        [Required]
        [Column("user_id")]
        public string UserId { get; set; }

        [Required]
        [Column("subcontent")]
        public string SubContent { get; set; }

        [Column("comment_id")]
        public string CommentId { get; set; }
        
        public Comment Comment { get; set; }
    }
}