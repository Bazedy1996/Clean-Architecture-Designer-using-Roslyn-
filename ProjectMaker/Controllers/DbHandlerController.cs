using Microsoft.AspNetCore.Mvc;
using ProjectMaker.Base;
using ProjectMaker.Dtos.DBHandler;
using ProjectMaker.Featueres.DBHandler.Contracts;

namespace ProjectMaker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DbHandlerController(IPropertyHandler propertyHandlerService) : AppBaseController
    {

        [HttpPost("add_valiadation_attribute")]
        public async Task<IActionResult> AddValidationAttributeToProperty([FromBody] AttributeDto dto)
        {
            try
            {
                var response = await propertyHandlerService.AddValidationToProperty(dto);
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
        [HttpPost("get_attribute_details")]
        public async Task<IActionResult> GetAttributeDetailsFromProperty([FromBody] AttributeDto dto)
        {
            try
            {
                var response = await propertyHandlerService.GetAttributeDetailsFromProperty(dto);
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
        [HttpPost("remove_attribute")]
        public async Task<IActionResult> RemoveAttribute([FromBody] AttributeDto dto)
        {
            try
            {
                var response = await propertyHandlerService.RemoveAttributeFromProperty(dto);
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
