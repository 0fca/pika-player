using System.Collections.Generic;
using System.Threading.Tasks;
using Claudia.Data;

namespace Claudia.Services
{
    /**
     * <summary>Base interface for LectureService used to register service in DI.</summary>
     */
    public interface IVideoService
    {
        Task Init();
        Task AddComment(Comment c);
        Task AddSubComment(SubComment subComment);
        Task<List<Lecture>> List(string[] criterias, int limit, int order);
        Task<List<Lecture>> ListFor(int limit, int order);
        Task<Lecture> GetById(string id);
        Task<List<Comment>> GetCommentsByVideoId(string id);
        Task<List<SubComment>> GetSubCommentsById(string cid);
        Task<Course> GetCourseById(int id);
        List<string> GetAttachments(string id);
        
        void Dispose();
    }
}