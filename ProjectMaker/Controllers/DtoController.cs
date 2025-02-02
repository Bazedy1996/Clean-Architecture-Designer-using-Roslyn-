using Microsoft.AspNetCore.Mvc;
using ProjectMaker.Base;
using ProjectMaker.Dtos.DataCreator;
using ProjectMaker.Dtos.DtoCreator;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.DtoCreator.Contracts;

namespace ProjectMaker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DtoController(IDtoCreatorService service) : AppBaseController
    {
        [HttpPost("create_dto")]

        public async Task<IActionResult> CreateDto([FromBody] AddDtoProperties dto)
        {
            try
            {
                var response = await service.CreateDto(dto);
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
        [HttpPost("get_dtos")]

        public IActionResult GetDtos([FromBody] ServiceDto dto)
        {
            try
            {
                var response = service.GetDto(dto);
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
        [HttpPost("get_dto_properties")]

        public async Task<IActionResult> GetDtoProperties([FromBody] ModelDto dto)
        {
            try
            {
                var response = await service.GetPropertiesFromDto(dto);
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
        [HttpPost("remove_dto_properties")]

        public async Task<IActionResult> RemoveDtoProperties([FromBody] DeleteDtoProperties dto)
        {
            try
            {
                var response = await service.DeletePropertiesFromDto(dto);
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
