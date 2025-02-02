using Microsoft.AspNetCore.Mvc;
using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.BaseCreator.Contracts;

namespace ProjectMaker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseCreatorController(IBaseGenerator baseGeneratorService) : AppBaseController
    {
        [HttpPost("generate_bases")]
        public async Task<IActionResult> CreateModel([FromBody] ServiceDto dto)
        {
            try
            {
                var response = await baseGeneratorService.BaseCreator(dto);
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
