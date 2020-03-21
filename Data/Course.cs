using System.ComponentModel.DataAnnotations.Schema;

namespace Claudia.Data
{
    public class Course
    {
        [Column("id")]
        public int CourseId { get; set; }
        
        [Column("course_name")]
        public string CourseName { get; set; }
        
        [Column("lecturer_id")]
        public string LecturerId { get; set; }
    }
}