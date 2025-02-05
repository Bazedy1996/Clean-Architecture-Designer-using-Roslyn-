using Microsoft.AspNetCore.Mvc;
using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.ControllerCreator.Contracts;

namespace ProjectMaker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ControllerCreatorController(IBaseControllerService baseControllerService, IControllerCreatorService controllerCreatorService) : AppBaseController
    {
        [HttpPost("add_base_controller")]
        public async Task<IActionResult> AddBaseController([FromBody] ServiceDto dto)
        {
            try
            {
                var response = await baseControllerService.CreateAppBaseController(dto);
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
        [HttpPost("add_controllers")]
        public async Task<IActionResult> AddControllers([FromBody] ServiceDto dto)
        {
            try
            {
                var response = await controllerCreatorService.CreateControllers(dto);
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
