using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Claudia.Pages
{
    public class SecurityPolicy : PageModel
    {
        public string Rules { get; private set; }

        public void OnGet()
        {
            
            Rules = System.IO.File.ReadAllText("rules");
        }
    }
}