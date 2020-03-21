using System.Collections.Generic;
using Claudia.Data;

namespace Claudia.Models.SearchViewModels
{
    public class SearchViewModel
    {
        public List<Lecture> Videos { get; set; }
        public string SearchPhrase { get; set; }
        public int Order { get; set; }
        public string Course { get; set; }
    }
}