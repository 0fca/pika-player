using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Claudia.Data;
using Claudia.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Claudia.Services
{
    /**
     * <summary>
     *  This class is responsible for handling all operations on lectures.
     * </summary>
     *
     * <remarks>
     *  This class contains methods, mostly async, for operating on lectures' records in database and on files placed physically on the disk.
     *  All paths are stored in configuration file.
     *  Main features are: add video, add attachement to a video, remove video, edit video.
     * </remarks>
     **/

    public class LectureService : IVideoService
    {
        private readonly LecturesContext _lecturesContext;
        private readonly IConfiguration _configuration;
        private readonly HashGeneratorService _generator;
        private readonly SignInManager<User> _userManager;
        private readonly ILogger<LectureService> _log;

        public bool IsInitiated = false;
        
        /**
         * <summary>
         * The main constructor of the class, it is used by the DI container of Core framework to inject all objects on which this class depends.
         * </summary>
         * <param name="configuration">IConfiguration object responsible for handling actual app configuration</param>
         * <param name="generator">IGenerator object responsible for generating </param>
         * <param name="lecturesContext">LecturesContext object responsible for accessing database for lectures and all related data.</param>
         * <param name="log">ILogger object responsible for logging.</param>
         * <param name="userManager">UserManager object used to easily access users and their information.</param>
         **/
        public LectureService(LecturesContext lecturesContext, 
            IConfiguration configuration, 
            IGenerator generator,
            SignInManager<User> userManager,
            ILogger<LectureService> log)
        {
            _lecturesContext = lecturesContext;
            _configuration = configuration;
            _generator = (HashGeneratorService)generator;
            _userManager = userManager;
            _log = log;
        }

        /**
         * <summary>This method is responsible for getting list of attachments for an vid.</summary>
         * <param name="v">String which is an video id.</param>
         * <returns>Returns an List<string> object representing names of attachements.</returns>
         * */
        internal async Task<List<string>> GetAttachementsAsync(string v)
        {
            var attachementDir = Path.Combine(_configuration.GetSection("Storage")["AttachementsStorage"], v);
            var files = new List<string>();
            if (!Directory.Exists(attachementDir)) return await Task.Factory.StartNew(() => files);
            
            files = Directory.EnumerateFiles(attachementDir).ToList();
            files.ForEach(file =>
            {
                Path.GetFileName(file);
            });
            return await Task.Factory.StartNew(() => files);
        }

        public async Task Init()//Unused here.
        {
            
        }

        /**
         * <summary>This method is responsible for adding subcomment to an existing comment related to it via its id. It uses a relation in database.</summary>
         * <param name="subComment">A SubComment object passed from front-end prepared by <see cref="Claudia.Controllers.WatchController"/></param>
         */
        public async Task AddSubComment(SubComment subComment)
        {
            _lecturesContext.SubComments.Add(subComment);
            await _lecturesContext.SaveChangesAsync();
        }

        /**
         * <summary>This method is responsible for listing lectures using passed to it criterias, limit and order.</summary>
         * <param name="criterias">String array which contains criterias to be searched for in lectures' names.</param>
         * <param name="limit">Integer value telling the method how many records should it contain in returned value. By default limit = 10.</param>
         * <param name="order">Integer value, by default set to 0, used to determine if the order is ASC(0) or DESC(1).</param>
         * <returns>Returns an List<Lecture> object which is a result list of needed lectures.<seealso cref="Claudia.Data.Lecture"/></returns>
         * <remarks>This method validates files existence.</remarks>
         */

        public async Task<List<Lecture>> List(string[] criterias, int limit = 10, int order = 0)
        {
            var list = _lecturesContext.Lectures.ToList();
            if (criterias != null)
            {
                var searchPhrase = criterias[0].Split(" ").ToList();

                //var tagsPhrase = criterias[1].Split(",").ToList();

                list.RemoveAll(predicate =>
                {
                    return searchPhrase.Any(part => (!predicate.DisplayName.ToLower().Contains(part.ToLower()) 
                                                     && !predicate.Description.ToLower().Contains(part.ToLower())) 
                                                     &&!(_lecturesContext.Courses.Find(predicate.CourseId)).CourseName.ToLower().Contains(part.ToLower()));
                });
            }

            if (order == 1)
            {
                list.Reverse();
            }

            ValidateFileExistence(list);
            
            return await Task.Factory.StartNew(() => list.GetRange(0, list.Count < limit ? list.Count : limit));
        }

        /**
         * <summary>This method lists <seealso cref="Claudia.Data.Lecture"/> objects for the logged in user if and only if the logged in user is in Admin or/and Lecture role. 
         * It uses passed parameters to prepare result </summary>
         * <param name="limit">Limit limits length of result set. By default it is = 10.</param>
         * <param name="order">Determines the order of result set: ASC = 0 and DESC = 1;</param>
         * <returns>Returns an List<Lecture> object which is a result list of needed lectures.<seealso cref="Claudia.Data.Lecture"/></returns>
         * <remarks>This method does NOT validates file physical existence.</remarks>
         */
        public async Task<List<Lecture>> ListFor(int limit = 10, int order = 0)
        {
            var list = await List(null, limit, order);
            if (order == 1)
            {
                list.Reverse();
            }

            ValidateFileExistence(list);
            return await Task.Factory.StartNew(() => list.GetRange(0, list.Count < limit ? list.Count : limit));
        }

        /**
         * <summary>This method returns a <seealso cref="Claudia.Data.Lecture"/> object using given id.</summary>
         * <param name="id">String object being an id of lecture in database.</param>
         * <returns>Object of Lecture class.</returns>
         */
        public async Task<Lecture> GetById(string id)
        {
            return await _lecturesContext.Lectures.FindAsync(id);
        }

        /**
         * <summary>This method gets comments related to the video of given id.</summary>
         * <param name="id">Video id of which comments you want to have.</param>
         * <returns>List<Comment> - list of comments related to that video.</returns>
         */
        public async Task<List<Comment>> GetCommentsByVideoId(string id)
        {
            return await Task.Factory.StartNew(() => 
                _lecturesContext.Comments.Where(comment => 
                comment.VideoId.Equals(id)).ToList());
        }

        /**
         * <summary>This method gets the subcomments for the comment specified with given cid.</summary>
         * <param name="cid">String which is an id of comment.</param>
         * <returns>Returns List<<seealso cref="Claudia.Data.SubComment"/>></returns>
         */
        public async Task<List<SubComment>> GetSubCommentsById(string cid)
        {
            return await Task.Factory.StartNew(() =>_lecturesContext.SubComments.Where(c => c.CommentId == cid).ToList());
        }

        /**
         * <summary>Adds the comment.</summary>
         * <param name="comment">Comment object which contains all of the comment data.</param>
         */
        public async Task AddComment(Comment comment)
        {
            await Task.Factory.StartNew(() => _lecturesContext.Comments.Add(comment));
            await _lecturesContext.SaveChangesAsync();
        }

        /**
         * <summary>Adds the lecture to the database and writes a file to a physical drive to a predefined path.</summary>
         * <param name="s">Stream s contains binary data of a video file.</param>
         * <param name="fileName">The actual file name of the video file.</param>
         * <param name="mime">Contains an actual MIME type of the file.</param>
         * <returns>Returns a string which is an id of the video.</returns>
         */
        public async Task<string> Add(Stream s, string fileName, string mime)
        {
            var filePath = $"{_configuration.GetSection("Storage")["VideoPath"]}/{fileName}";
            var id = "";
            var assumedHiddenFilePath = Path.Combine(filePath, string.Concat(".", Path.GetFileName(filePath)));
            if (File.Exists(filePath) || File.Exists(assumedHiddenFilePath))
            {
                if (!await _lecturesContext.Lectures.AnyAsync(x => x.VideoPath.Equals(filePath))) return null;
                
                {
                    var tmpLect = await _lecturesContext.Lectures.FirstAsync(x => x.VideoPath.Equals(filePath));
                    return tmpLect.Id;
                }
            }
            
            if (!s.CanRead) return id;
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await s.CopyToAsync(stream);
            }
            s.Close();
            id = CreateId(fileName);
            
            var lecture = new Lecture
            {
                Id = id,
                VideoPath = filePath,
                MimeType = mime,
                LecturerId = _userManager.UserManager.GetUserId(_userManager.Context.User),
                DisplayName = fileName,
                Description = fileName,
                DateAdded = DateTime.Now
            };
            if (File.Exists(lecture.VideoPath))
            {
                var outputPath = await CreateThumbnail(lecture.VideoPath);
                if (File.Exists(outputPath))
                {
                    var fStream = new FileStream(outputPath, FileMode.Open);
                    var data = new byte[fStream.Length];
                    await fStream.ReadAsync(data);
                    lecture.Thumbnail = data;
                }
            }
            if (File.Exists(assumedHiddenFilePath))
            {
                File.Delete(assumedHiddenFilePath);
            }

            _lecturesContext.Lectures.Add(lecture);
            var expiry = new Expiry {Id = lecture.Id, ExpiryDate = DateTime.Now.AddMilliseconds(1000d)};
            await _lecturesContext.Expiries.AddAsync(expiry);
            await _lecturesContext.SaveChangesAsync();

            return id;
        }

        /**
         * <summary>This method adds attachments to the video of the given id.</summary>
         * <param name="attachments">Content of the file form passed here from the controller.</param>
         * <param name="id">The actual id of the video to which attachment/-s should be added.</param>
         * <returns>Returns bool value which says if actions succeeded or not.</returns>
         */
        public async Task<bool> AddAttachementsAsync(List<IFormFile> attachements, string id)
        {

            if (attachements == null || attachements.Count <= 0) return true;
            var attachementPath = _configuration.GetSection("Storage")["AttachementsStorage"];
            
            var result = false;

            return await Task<bool>.Factory.StartNew(() => {
                attachements.ForEach(async attachement =>
                {
                    var outputPath = Path.Combine(attachementPath, id, attachement.FileName);
                    if (!Directory.Exists(Path.Combine(attachementPath, id)))
                    {
                        Directory.CreateDirectory(Path.Combine(attachementPath, id));
                    }

                    if (File.Exists(outputPath))
                    {
                        _log.LogInformation($"File {outputPath} already exists, overwriting.");
                    }

                    var buffer = new byte[attachement.Length];
                    await attachement.OpenReadStream().ReadAsync(buffer);
                    var task = File.WriteAllBytesAsync(outputPath, buffer);
                    task.Wait();
                    result = File.Exists(outputPath);
                });
                return result;
            });
        }

        /**
         * <summary>Updates the record for the lecture in the database basing on the Lecture object passed.</summary>
         * <param name="lecture">Lecture object passed from the controller.</param>
         * 
         */
        public async Task<bool> UpdateAsync(Lecture lecture)
        {
            var oldLecture = await _lecturesContext.Lectures.FindAsync(lecture.Id);
            oldLecture.DisplayName = lecture.DisplayName;
            oldLecture.Description = lecture.Description;

            _lecturesContext.Update(oldLecture);
            return await _lecturesContext.SaveChangesAsync() != -1;
        }

        /**
         * <summary>Deletes the lecture - the record from the database and the physical file.</summary>
         * <param name="toBeDeleted">Lecture object which is to be deleted.</param>
         * <returns>Returns bool value as an indication if action succeeded or not.</returns>
         */
        public async Task<bool> Delete(Lecture toBeDeleted)
        {
            if (File.Exists(toBeDeleted.VideoPath))
            {
                File.Delete(toBeDeleted.VideoPath);
            }

            var assumedHiddenFilePath = Path.Combine(Path.GetDirectoryName(toBeDeleted.VideoPath), 
                string.Concat(".", Path.GetFileName(toBeDeleted.VideoPath)));
            
            if (File.Exists(assumedHiddenFilePath))
            {
                File.Delete(assumedHiddenFilePath);
            }

            var entry = _lecturesContext.Remove(toBeDeleted);
            
            await _lecturesContext.SaveChangesAsync();
            return entry.State == EntityState.Detached && !File.Exists(toBeDeleted.VideoPath);
        }

        /**
         * <summary>Gets all courses from database and loads the data to the dictionary collection.</summary>
         * <returns><see cref="Dictionary{TKey, TValue}"/> -
         * returns the Dictionary<int, string> object which contains data of all courses.</returns>
         * 
         */
        public async Task<Dictionary<int, string>> GetCoursesAsync()
        {
            var courseResultEnumerable = 
                (await _lecturesContext.Courses.ToListAsync()).Where(course => (string.IsNullOrEmpty(course.LecturerId)) 
                                                                               || course.LecturerId.Equals(_userManager.UserManager.GetUserId(_userManager.Context.User)));

            return courseResultEnumerable.ToDictionary(x => x.CourseId, x => x.CourseName);
        }

        /**
         * <summary>Gets the course object by id.</summary>
         * <param name="id">Course id.</param>
         * <returns>Returns the Course object matching given id value.</returns>
         */
        public async Task<Course> GetCourseById(int id)
        {
            return await _lecturesContext.Courses.FindAsync(id);
        }

        /**
         * <summary>Gets all expiries information from database.</summary>
         * <returns>List of Expiry objects.</returns>
         * <remarks>Not used.</remarks>
         */
        public async Task<List<Expiry>> GetAllExpiriesAsync()
        {
            return await _lecturesContext.Expiries.ToListAsync();
        }

        /**
         * <summary>Dispose <code>this</code> instance of LectureService. Disposes the <seealso cref="LecturesContext"/></summary>
         */
        public void Dispose()
        {
            _lecturesContext.Dispose();
        }

        /**
         * <summary>Unlocks the video of the given id.</summary>
         * <returns>String value which is a human-readable message.</returns>
         * <remarks>This method operates on physical files and has no influence on data stored in database.</remarks>
         */
        public async Task<string> UnlockAsync(string id)
        {
            string msg;

            try
            {
                if (id.Equals("all"))
                {
                    var videoList = _lecturesContext.Lectures.Select(l => l.VideoPath);
                    foreach (var videoName in videoList)
                    {
                        await UnlockVideo(videoName);
                    }
                    
                    msg = "All videos unlocked successfully.";
                }
                else
                {
                    var absoluteVideoPath = (await _lecturesContext.Lectures.FindAsync(id)).VideoPath;

                    await UnlockVideo(absoluteVideoPath);
                    msg = $"Video of id: {id} unlocked successfully.";
                }
            }
            catch(Exception e)
            {
                msg = $"Couldn't unlock the video of id {id}.";
                _log.LogError(e, e.Message);
            }
            return msg;
        }

        /**
         * <summary>Gets attachements for the video of id passed to the method.</summary>
         * <param name="id">Id of the video from which to get attachements.</param>
         * <returns>List of strings representing file names.</returns>
         * <remarks>There was no need to model the entity model for this purpose as each file can be mapped using video id.</remarks>
         */
        public List<string> GetAttachments(string id)
        {
            var list = new List<string>();
            var path = Path.Combine(_configuration.GetSection("Storage")["AttachementsStorage"], id);

            if (!Directory.Exists(path)) return list;
            var files = Directory.EnumerateFiles(path);
            list = files.ToList();
            return list;
        }
        //Helper methods, all are private not documented oficially.

        #region HelperMethods

        /*This method is reponsible for creating thumbnail using ffmpeg executable installed on the system.
         * ffmpeg was installed on host OS using aptitude,
         * it is accessible under the path: /usr/bin/ however the path is loaded from config so it can be changed.
         * It returns string which is a path to the thumbnail image.
        */
        private async Task<string> CreateThumbnail(string filename)
        {
            var output = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{".png"}");

            await Task.Factory.StartNew(() =>
            {
                var process = new Process()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = _configuration.GetSection("Ffmpeg")["FfmpegPath"],
                        Arguments =
                            $"-i {filename} -ss {TimeSpan.FromSeconds(10d)} -vframes 1 {output}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                };

                if (!File.Exists(process.StartInfo.FileName)) return;
                
                process.Start();
                process.WaitForExit();
            });
            return output;
        }

        //This method creates an id using IGeneratorService.
        private string CreateId(string displayName)
        {
            return _generator.GenerateId(displayName).Substring(0,10);
        }

        //This method validates existence of each video file to which refers Lecture object.
        private static void ValidateFileExistence(List<Lecture> list)
        {
            list.RemoveAll(video => {
                try
                {
                    return File.GetAttributes(video.VideoPath) == FileAttributes.Hidden;
                }
                catch
                {
                    return true;
                }
            });
        }

        //TODO: Change description. 
        private async Task UnlockVideo(string videoName)
        {
            if (Path.IsPathFullyQualified(videoName)
                && File.Exists(videoName))
            {
                var lecture = await _lecturesContext.Lectures.SingleAsync(l => 
                    l.IsLocked && l.VideoPath.Equals(videoName));
                lecture.IsLocked = false;
                lecture.DateAdded = DateTime.Now.AddDays(7);
                await UpdateAsync(lecture);
            }
        }
        #endregion
    }
}