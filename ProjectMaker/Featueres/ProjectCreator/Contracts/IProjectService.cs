using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;

namespace ProjectMaker.Featueres.ProjectCreator.Contracts
{
    public interface IProjectService
    {
        public Response<string> CreateNewProject(ProjectDto dto);
        public string GetProjectPath(ProjectDto dto);
        public Response<IEnumerable<string?>> GetProjects();
        public Response<string> DeleteProject(ProjectDto dto);
        public Task<Response<string>> CreateNewService(ServiceDto dto);
        public Response<IEnumerable<string>> GetAllServices(ProjectDto dto);
        public Response<string> DeleteService(ServiceDto dto);
        public string GetServicePath(ServiceDto dto);

    }
}
