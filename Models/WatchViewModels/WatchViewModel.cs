using System.Collections.Generic;
using Claudia.Data;

namespace Claudia.Models.WatchViewModels
{
    public class WatchViewModel
    {
        public Lecture CurrentVideo { get; set; }
        public Dictionary<string,string> Ids { get; set; }
        public Dictionary<Comment,List<SubComment>> Comments { get; set; }
        public List<string> Attachements { get; set; }
    }
}