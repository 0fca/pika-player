using System.Collections.Generic;
using System.Threading.Tasks;
using Claudia.Models.HomeViewModels;
using Claudia.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Claudia.Data;

namespace Claudia.Controllers
{
    public class HomeController : Controller
    {
        private readonly LectureService _lectureService;

        public HomeController(IVideoService lectureService)
        {
            _lectureService = (LectureService)lectureService;
            if (!_lectureService.IsInitiated)
            {
                _lectureService.Init().Wait();
            }
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var indexViewModel = new IndexViewModel()
            {
                LatestVideos = await _lectureService.List(null,3,1)
            };
            return View(indexViewModel);
        }

        [AllowAnonymous]
        public IActionResult Contact()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult About()
        {
            return View();
        }
    }
}