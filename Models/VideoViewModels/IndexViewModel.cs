using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace Claudia.Models.VideoViewModels
{
    public class IndexViewModel
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string ThumbPath { get; set; }
        public int CourseId { get; set; }
        public Dictionary<int, string> Courses { get; set; }
        public List<string> Attachements { get; set; }
        public List<IFormFile> Files { get; set; }
    }
}