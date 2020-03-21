using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Claudia.Data;
using Claudia.Models;
using Claudia.Models.SearchViewModels;
using Claudia.Models.WatchViewModels;
using Claudia.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Claudia.Controllers
{
    /**
     * <summary>This class is for controlling requests on endpoint: /watch.</summary>
     * 
     */
    [Authorize]
    public class WatchController : Controller
    {
        //Objects injected by a DI container.
        private readonly IVideoService _lectureService;
        private readonly SignInManager<User> _signInManager;

        public WatchController(IVideoService youTubeService, SignInManager<User> signInManager)
        {
            _lectureService = youTubeService;
            _lectureService.Init();
            _signInManager = signInManager;
        }

        //HTTP GET action, url: /video?v=id
        /**
         * <summary>This action is for actual purposes of viewing video on the website.</summary>
         * <remarks>Needed security policy: RequireBase.</remarks>
         */
        [HttpGet]
        [Authorize(Policy = "RequireBase")]
        public async Task<IActionResult> Index(string v)
        {
            if (v == null) return View();
            var video = await _lectureService.GetById(v);
            if (video != null)
            {
                var videoList = await _lectureService.List(null, 10, 1);
                var attachements = _lectureService.GetAttachments(v);

                var videoMap = new Dictionary<string, string>();
                videoList.ForEach(videoEntry => { videoMap.Add(videoEntry.Id, videoEntry.DisplayName); });
                var commentsList = await _lectureService.GetCommentsByVideoId(video.Id);
                var commentDictionary = new Dictionary<Comment, List<SubComment>>();

                commentsList.ForEach(comment =>
                {
                    commentDictionary.Add(comment, _lectureService.GetSubCommentsById(comment.CommentId).Result);
                });

                return View(new WatchViewModel() { CurrentVideo = video, Ids = videoMap, Comments = commentDictionary, Attachements = attachements });
            }
            else
            {
                return View();
            }
        }

        //HTTP GET verb; url: /watch/search?sp=[model]
        /**
         * <summary>This action is for searching videos using the pattern given in the model in query.</summary>
         * <param name="sp">Model with pattern</param>
         * <remarks>Security policy needed: RequireBase</remarks>
         */
        [HttpGet]
        [Authorize(Policy = "RequireBase")]
        public async Task<IActionResult> Search(SearchViewModel sp)
        {
            sp.SearchPhrase += ":";
            var criteria = ParseCriteria(sp.SearchPhrase);
            var list = await _lectureService.List(criteria, 5, sp.Order);
            
            if (!string.IsNullOrEmpty(sp.Course))
            {
                list = list.FindAll(predicate =>
                    (_lectureService.GetCourseById(predicate.CourseId)).Result.CourseName == sp.Course);
            }

            return
            View(new SearchViewModel(){Videos = list, SearchPhrase = sp.SearchPhrase.Replace(":","")});
        }
        
        /**
         * <summary>This action is for posting comments.</summary>
         * <param name="c">String c which is an actual content of the comment.</param>
         * <param name="id">String id which is an id of the video to which we add the comment.</param>
         * <remarks>Requires security policy: RequireBase</remarks>
         */
        [HttpPost]
        [Authorize(Policy = "RequireBase")]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Comment(string c, string id)
        {
            if (string.IsNullOrEmpty(c) || string.IsNullOrEmpty(id))
            {
                TempData["returnMessage"] = string.IsNullOrEmpty(c) ? "No comment content." : "Something went wrong, sorry for the inconvenience.";
                return RedirectToAction(nameof(Index), new {v = id});
            }

            var comment = new Comment
            {
                Content = c,
                UserId = _signInManager.UserManager.GetUserId(HttpContext.User),
                VideoId = id
            };

            await _lectureService.AddComment(comment);           
            return RedirectToAction(nameof(Index), new {v = id});
        }

        /**
         * <summary>This action is for sending a subcomment.</summary>
         * <param name="c">An actual content of the comment</param>
         * <param name="cid">Comment id to which we add the subcomment</param>
         * <param name="vid">A video id.</param>
         * <remarks>Requires security policy: RequireBase</remarks>
         */
        [HttpPost]
        [Authorize(Policy = "RequireBase")]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> SubComment(string c, string vid, string cid)
        {
            if (string.IsNullOrEmpty(c) || string.IsNullOrEmpty(vid) || string.IsNullOrEmpty(cid))
            {
                TempData["returnMessage"] = string.IsNullOrEmpty(c) ? "No comment content." : "Something went wrong, sorry for the inconvenience.";
                return RedirectToAction(nameof(Index), new {v = vid});
            }
            
            var subcomment = new SubComment()
            {
                UserId = _signInManager.UserManager.GetUserId(_signInManager.Context.User),
                SubContent = c,
                CommentId = cid,
                Comment = (await _lectureService.GetCommentsByVideoId(vid)).Find(comment => comment.CommentId == cid)
                
            };
            await _lectureService.AddSubComment(subcomment);
            TempData["cid"] = cid;
            return RedirectToAction(nameof(Index), new {v = vid});
        }

        /**
         * <summary>This action is for streaming video to the client.</summary>
         * <param name="path">A physical path of the video on the filesystem.</param>
         * <remarks>Requires security policy: RequireBase</remarks>
         */
        [HttpGet]
        [Authorize(Policy = "RequireBase")]
        public async Task<FileStreamResult> Stream(string path)
        {
            var fileStream = new FileStream(path, FileMode.Open);
            return await Task<FileStreamResult>.Factory.StartNew(() =>  File(fileStream, "video/mp4"));
        }

        //A helper method, parses criteria. It is used by the Search action.
        private static string[] ParseCriteria(string p)
        {
            var tmp = p.Split(":");
            var result = new string[2];

            result[0] = tmp[0];
            //TODO: Parsing tags here and then placing at 1st index of array.
            return result;
        }
    }
}