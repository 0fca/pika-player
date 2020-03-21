using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Claudia.Models.VideoViewModels;
using Claudia.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Claudia.Controllers
{
    /**
     * <summary>This is a video controller class, it is responsible for handling requests from client sent to url: /video/</summary>
     * <remarks>It uses <seealso cref="LectureService"/> class to process all of clients' requests.</remarks>
     */
    public class VideoController : Controller
    {
        //This is for actual managing video and its data on database, injected via constructor by DI container.
        private readonly LectureService _lectureService;
        private readonly IConfiguration _configuration;
        
        public VideoController(IVideoService lectureService,
                               IConfiguration configuration)
        {
            _lectureService = (LectureService)lectureService;
            _configuration = configuration;
        }

        //HTTP GET action, url: /video?v=id
        /**
         * <summary>This action is for viewing information of the lecture and editing it.</summary>
         * <remarks>Needed security policy: RequireElevated.</remarks>
         */
        [HttpGet]
        [Authorize(Policy = "RequireElevated")]
        public async Task<IActionResult> Index(string v)
        {
            var lecture = await _lectureService.GetById(v);
            if (lecture != null)
            {
                var indexViewModel = new IndexViewModel
                {
                    Id = lecture.Id,
                    DisplayName = lecture.DisplayName,
                    Description = lecture.Description,
                    Courses = await _lectureService.GetCoursesAsync(),
                    Attachements = await _lectureService.GetAttachementsAsync(v),
                    CourseId = lecture.CourseId
                };
                return View(indexViewModel);
            }
            else
            {
                TempData["returnMessage"] = "No video of such id.";
                return View();
            }
        }
        //HTTP GET verb; url: /video/add
        /**
         * <summary></summary>
         * <remarks>Needed security policy: RequireElevated.</remarks>
         */
        [HttpGet]
        [Authorize(Policy = "RequireElevated")]
        public IActionResult Add()
        {
            return View();
        }

        //HTTP POST verb; url: /video/edit
        /**
         * <summary></summary>
         * <param name="indexViewModel"></param>
         * <remarks>Needed security policy: RequireElevated.</remarks>
         */
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Policy = "RequireElevated")]
        public async Task<IActionResult> Edit(IndexViewModel indexViewModel)
        {
            var lecture = await _lectureService.GetById(indexViewModel.Id);
            var result = false;
            var attachementResult = false;

            if (lecture != null)
            {
                lecture.DisplayName = indexViewModel.DisplayName;
                lecture.Description = indexViewModel.Description;
                lecture.CourseId = indexViewModel.CourseId;
                attachementResult = await _lectureService.AddAttachementsAsync(indexViewModel.Files, indexViewModel.Id);
                result = await _lectureService.UpdateAsync(lecture);
            }

            indexViewModel.Courses = await _lectureService.GetCoursesAsync();

            TempData["returnMessage"] = (attachementResult && result) ? "Successfully updated." : "Video couldn't be updated.";
            return RedirectToAction(nameof(Index), new {v = indexViewModel.Id});
        }
        //HTTP POST verb; url: /video/upload
        /**
         * <summary>This action is for uploading videos to the system.</summary>
         * <param name="files">List<IFormFile> collection with all files to be uploaded.</param>
         * <remarks>Needed security policy: RequireElevated.</remarks>
         */
        [HttpPost]
        [AutoValidateAntiforgeryToken]
        [Authorize(Policy = "RequireElevated")]
        public async Task<IActionResult> Upload(List<IFormFile> files)
        {
                foreach (var formFile in files)
                {
                    if (formFile.ContentType.Contains("mp4"))
                    {
                        var id = await _lectureService.Add(formFile.OpenReadStream(), formFile.FileName, formFile.ContentType);

                        if (!string.IsNullOrEmpty(id))
                        {
                            return RedirectToAction(nameof(Index), new { v = id });
                        }

                        TempData["ReturnMessage"] = "An internal error occured while trying to upload the video.";
                        return RedirectToAction("add");
                    }

                    TempData["ReturnMessage"] = "Invalid file format: "+formFile.FileName;
                    return RedirectToAction("add");
                }
            
            TempData["ReturnMessage"] = "Nothing to be uploaded.";
            return RedirectToAction("add");
        }

        //HTTP GET verb; url: /video/delete?v=id
        /**
         * <summary>Views the delete view.</summary> 
         */
        [HttpGet]
        [Authorize(Policy = "RequireElevated")]
        public IActionResult Delete(string v)
        {
            ViewData["id"] = v;
            return View();
        }

        //HTTP POST verb; url: /video/deleteconfirmation?v=id
        /**
         * <summary>Used by Delete view to confirm deletion of the video.</summary>
         * <param name="v">Video id.</param>
         */
        [HttpPost]
        [Authorize(Policy = "RequireElevated")]
        public async Task<IActionResult> DeleteConfirmation(string v)
        {
            var lecture = await _lectureService.GetById(v);
            var state = await _lectureService.Delete(lecture);
            TempData["returnMessage"] = state ? "Deleted element successfully." : "Couldn't delete element.";
            return RedirectToAction(nameof(List));
        }

        //HTTP GET verb; url: /video/list
        /**
         * <summary>Lists all videos.</summary>
         * <remarks>Needed security policy: RequireElevated.</remarks> 
         */
        [HttpGet]
        [Authorize(Policy = "RequireElevated")]
        public async Task<IActionResult> List()
        {
            var listModel = new VideoListViewModel();
            var list = await _lectureService.ListFor();
            listModel.Lectures = list;
            return View(listModel);
        }

        //HTTP POST verb; url: /video/unlock?id=id
        /**
         * <summary>Unlocks the video of id passed in query.</summary>
         * <remarks>Uses method <seealso cref="LectureService.UnlockAsync(string)"/></remarks>
         */
        [HttpPost]
        [Authorize(Policy = "RequireElevated")]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Unlock(string id)
        {
            var msg = await _lectureService.UnlockAsync(id);
            TempData["returnMessage"] = msg;
            return RedirectToAction(nameof(List));
        }

        //HTTP GET verb; url: /video/thumb?id=id
        /**
         * <summary>Returns a thumbnail for video of id passed in query.</summary>
         * <returns>The file in JPG format.</returns>
         */
        [HttpGet]
        public async Task<FileResult> Thumb(string id)
        {
            var lecture = await _lectureService.GetById(id);
            var thumbData = lecture.Thumbnail;
            return File(thumbData, "image/png");
        }

        //HTTP GET verb; url: /video/downloadattachement?id=id&n=value
        /**
         * <summary>Action for downloading attachements.</summary>
         * <param name="id">Id of video of which we want attachements.</param>
         * <param name="n">String value indicating something.</param>
         */
        [HttpGet]
        public async Task<FileResult> DownloadAttachement(string id, [FromQuery] string n)
        {
            var attachements = _lectureService.GetAttachments(id);
            var attachementPath = attachements.Find(attachement => attachement.Equals(n));
            var fstream = new FileStream(attachementPath, FileMode.Open);
            var buffer = new byte[fstream.Length];
            await fstream.ReadAsync(buffer);
            return File(buffer, "unknown/unknown", Path.GetFileName(attachementPath));
        }
    }
}