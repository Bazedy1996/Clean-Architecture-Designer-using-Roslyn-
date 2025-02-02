using Microsoft.AspNetCore.Mvc;
using ProjectMaker.Base;
using ProjectMaker.Dtos.DataCreator;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.DataCreator.Contracts;

namespace ProjectMaker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DataController(IConfigurationDB configurationService, IDataCreator dataService) : AppBaseController
    {
        [HttpPost("add_db_context")]
        public async Task<IActionResult> AddDbContext([FromBody] ServiceDto dto)
        {
            try
            {
                var response = await configurationService.AddAppDbContext(dto);
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
        [HttpPost("create_model")]
        public async Task<IActionResult> CreateModel([FromBody] ModelDto dto)
        {
            try
            {
                var response = await dataService.CreateModels(dto);
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
        [HttpPost("get_entities")]
        public IActionResult GetEntities([FromBody] ServiceDto dto)
        {
            try
            {
                var response = dataService.GetEntitiesFromModels(dto);
                return NewResult(response);
            }
            catch (Exception ex)
            {
                return NewResult(new Response<ErrorResponse>
                {
                    Message = ex.Message,
                    Errors = [ex.GetType().Name],
                    Succeeded = false
                });
            }
        }
        [HttpPost("get_complex_types")]
        public IActionResult GetComplexTypes([FromBody] ServiceDto dto)
        {
            try
            {
                var response = dataService.GetComplexTypesFromModels(dto);
                return NewResult(response);
            }
            catch (Exception ex)
            {
                return NewResult(new Response<ErrorResponse>
                {
                    Message = ex.Message,
                    Errors = [ex.GetType().Name],
                    Succeeded = false
                });
            }
        }
        [HttpPost("remove_entity")]
        public async Task<IActionResult> RemoveEntity([FromBody] ModelDto dto)
        {
            try
            {
                var response = await dataService.DeleteEntity(dto);
                return NewResult(response);
            }
            catch (Exception ex)
            {
                return NewResult(new Response<ErrorResponse>
                {
                    Message = ex.Message,
                    Errors = [ex.GetType().Name],
                    Succeeded = false
                });
            }
        }
        [HttpPost("remove_complex_type")]

        public IActionResult RemoveComplexTypes([FromBody] ModelDto dto)
        {
            try
            {
                var response = dataService.DeleteComplexType(dto);
                return NewResult(response);
            }
            catch (Exception ex)
            {
                return NewResult(new Response<ErrorResponse>
                {
                    Message = ex.Message,
                    Errors = [ex.GetType().Name],
                    Succeeded = false
                });
            }
        }
        //Properties*------------------------------------------------------*

        [HttpPost("add_entity_properties")]

        public async Task<IActionResult> AddPropertiesToEntity([FromBody] AddPropertyDto dto)
        {
            try
            {
                var response = await dataService.AddPropertiesToEntity(dto);
                return NewResult(response);
            }
            catch (Exception ex)
            {
                return NewResult(new Response<ErrorResponse>
                {
                    Message = ex.Message,
                    Errors = [ex.GetType().Name],
                    Succeeded = false
                });
            }
        }
        [HttpPost("get_entity_properties")]

        public async Task<IActionResult> GetPropertiesFromEntity([FromBody] ModelDto dto)
        {
            try
            {
                var response = await dataService.GetPropertiesFromEntity(dto);
                return NewResult(response);

            }
            catch (Exception ex)
            {
                return NewResult(new Response<ErrorResponse>
                {
                    Message = ex.Message,
                    Errors = [ex.GetType().Name],
                    Succeeded = false
                });
            }
        }
        [HttpPost("remove_entity_properties")]

        public async Task<IActionResult> RemovePropertiesFromEntity([FromBody] DeletePropertyDto dto)
        {
            try
            {
                var response = await dataService.DeletePropertiesFromEntity(dto);
                return NewResult(response);
            }
            catch (Exception ex)
            {
                return NewResult(new Response<ErrorResponse>
                {
                    Message = ex.Message,
                    Errors = [ex.GetType().Name],
                    Succeeded = false
                });
            }
        }

        [HttpPost("add_complex_type_properties")]

        public async Task<IActionResult> AddPropertiesToComplexType([FromBody] AddPropertyDto dto)
        {
            try
            {
                var response = await dataService.AddPropertiesToComplexType(dto);
                return NewResult(response);
            }
            catch (Exception ex)
            {
                return NewResult(new Response<ErrorResponse>
                {
                    Message = ex.Message,
                    Errors = [ex.GetType().Name],
                    Succeeded = false
                });
            }
        }
        [HttpPost("get_complex_type_properties")]

        public async Task<IActionResult> GetPropertiesFromComplexType([FromBody] ModelDto dto)
        {
            try
            {
                var response = await dataService.GetPropertiesFromComplexTypes(dto);
                return NewResult(response);

            }
            catch (Exception ex)
            {
                return NewResult(new Response<ErrorResponse>
                {
                    Message = ex.Message,
                    Errors = [ex.GetType().Name],
                    Succeeded = false
                });
            }
        }
        [HttpPost("remove_complex_type_properties")]

        public async Task<IActionResult> RemovePropertiesFromComplexType([FromBody] DeletePropertyDto dto)
        {
            try
            {
                var response = await dataService.DeletePropertiesFromComplexTypes(dto);
                return NewResult(response);
            }
            catch (Exception ex)
            {
                return NewResult(new Response<ErrorResponse>
                {
                    Message = ex.Message,
                    Errors = [ex.GetType().Name],
                    Succeeded = false
                });
            }
        }
    }
}
