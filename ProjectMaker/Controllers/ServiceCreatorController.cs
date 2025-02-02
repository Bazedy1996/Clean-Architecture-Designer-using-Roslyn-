using Microsoft.AspNetCore.Mvc;
using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.ServiceCreator.Contracts;

namespace ProjectMaker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceCreatorController(IServiceCreatorService serviceCreatorService) : AppBaseController
    {
        [HttpPost("create_service_contracts")]
        public async Task<IActionResult> CreateServiceContracts([FromBody] ServiceDto dto)
        {
            try
            {
                var response = await serviceCreatorService.AddServicesInterfaces(dto);
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
        [HttpPost("create_service_implementations")]
        public async Task<IActionResult> CreateServiceImplementations([FromBody] ServiceDto dto)
        {
            try
            {
                var response = await serviceCreatorService.GenerateServiceClasses(dto);
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
        [HttpPost("add_mapping")]
        public async Task<IActionResult> AddMapping([FromBody] ServiceDto dto)
        {
            try
            {
                var response = await serviceCreatorService.AddMapping(dto);
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
