using Microsoft.AspNetCore.Mvc;
using ProjectMaker.Base;
using ProjectMaker.Dtos.RelationShipCreator;
using ProjectMaker.Featueres.RelationShipCreator.Contracts;

namespace ProjectMaker.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RelationShipController(IRelationShipService relationShipService) : AppBaseController
    {
        [HttpPost("add_one_to_one_relationship")]
        public async Task<IActionResult> AddOneToOneRelationShip([FromBody] OneToOneRelationshipDto dto)
        {
            try
            {
                var response = await relationShipService.AddOneToOneRelationship(dto);
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
        [HttpPost("remove_one_to_one_relationship")]
        public async Task<IActionResult> RemoveOneToOneRelationShip([FromBody] OneToOneRelationshipDto dto)
        {
            try
            {
                var response = await relationShipService.RemoveOneToOneRelationship(dto);
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
        [HttpPost("add_one_to_many_relationship")]
        public async Task<IActionResult> AddOneToManyRelationShip([FromBody] OneToManyRelationshipDto dto)
        {
            try
            {
                var response = await relationShipService.AddOneToManyRelationship(dto);
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
        [HttpPost("remove_one_to_many_relationship")]
        public async Task<IActionResult> RemoveOneToManyRelationShip([FromBody] OneToManyRelationshipDto dto)
        {
            try
            {
                var response = await relationShipService.RemoveOneToManyRelationship(dto);
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
        [HttpPost("add_many_to_many_relationship")]
        public async Task<IActionResult> AddManyToManyRelationShip([FromBody] ManyToManyRelationshipDto dto)
        {
            try
            {
                var response = await relationShipService.AddManyToManyRelationship(dto);
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
        [HttpPost("remove_many_to_many_relationship")]
        public async Task<IActionResult> RemoveManyToManyRelationShip([FromBody] ManyToManyRelationshipDto dto)
        {
            try
            {
                var response = await relationShipService.RemoveManyToManyRelationship(dto);
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
