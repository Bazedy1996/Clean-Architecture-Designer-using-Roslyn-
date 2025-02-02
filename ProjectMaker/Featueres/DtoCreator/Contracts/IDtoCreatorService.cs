using ProjectMaker.Base;
using ProjectMaker.Dtos.DataCreator;
using ProjectMaker.Dtos.DtoCreator;
using ProjectMaker.Dtos.ProjectCreator;

namespace ProjectMaker.Featueres.DtoCreator.Contracts
{
    public interface IDtoCreatorService
    {
        public Task<Response<string>> CreateDto(AddDtoProperties dto);
        public Response<List<string>> GetDto(ServiceDto dto);
        public Task<Response<List<PropertyDto>>> GetPropertiesFromDto(ModelDto dto);
        public Task<Response<string>> DeletePropertiesFromDto(DeleteDtoProperties dto);


    }
}
