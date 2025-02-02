using Microsoft.AspNetCore.Mvc;
using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.ProjectCreator.Contracts;

namespace ProjectMaker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController(IProjectService projectService) : AppBaseController
    {


        [HttpPost("create_new_project")]
        public IActionResult CreateNewWProject([FromBody] ProjectDto dto)
        {

            var response = projectService.CreateNewProject(dto);
            return NewResult<string>(response);
        }
        [HttpPost("remove_project")]
        public IActionResult RemoveProject([FromBody] ProjectDto dto)
        {
            var response = projectService.DeleteProject(dto);
            return NewResult(response);
        }
        [HttpPost("get_projects")]
        public IActionResult GetAllProjects()
        {
            var response = projectService.GetProjects();
            return NewResult(response);
        }
        [HttpPost("create_new_service")]
        public async Task<IActionResult> CreateNewService([FromBody] ServiceDto dto)
        {
            try
            {
                var response = await projectService.CreateNewService(dto);
                return NewResult<string>(response);
            }
            catch (Exception ex)
            {
                return NewResult(new Response<ErrorResponse>
                {
                    Message = ex.Message,
                    Errors = [ex.GetType().Name],
                    Succeeded = false,
                });
            }


        }
        [HttpPost("remove_service")]
        public IActionResult RemoveService([FromBody] ServiceDto dto)
        {
            try
            {
                var response = projectService.DeleteService(dto);
                return NewResult(response);
            }
            catch (Exception ex)
            {
                return NewResult(new Response<ErrorResponse>
                {
                    Message = ex.Message,
                    Errors = [ex.GetType().Name],
                    Succeeded = false,
                });
            }

        }
        [HttpPost("get_services")]
        public IActionResult GetservicesByProject([FromBody] ProjectDto dto)
        {
            try
            {
                var response = projectService.GetAllServices(dto);
                return NewResult(response);
            }
            catch (Exception ex)
            {
                return NewResult(new Response<ErrorResponse>
                {
                    Message = ex.Message,
                    Errors = [ex.GetType().Name],
                    Succeeded = false,
                });
            }

        }
    }
}
