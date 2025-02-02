using ProjectMaker.Base;
using ProjectMaker.Dtos.DataCreator;
using ProjectMaker.Dtos.ProjectCreator;

namespace ProjectMaker.Featueres.DataCreator.Contracts
{
    public interface IDataCreator
    {
        public Task<Response<string>> CreateModels(ModelDto dto);
        public Response<List<string>> GetEntitiesFromModels(ServiceDto dto);
        public Response<List<string>> GetComplexTypesFromModels(ServiceDto dto);
        public Task<Response<string>> DeleteEntity(ModelDto entity);
        public Response<string> DeleteComplexType(ModelDto complexType);

        //Properties------------------------------------------------------------

        public Task<Response<string>> AddPropertiesToEntity(AddPropertyDto dto);
        public Task<Response<List<PropertyDto>>> GetPropertiesFromEntity(ModelDto dto);
        public Task<Response<string>> DeletePropertiesFromEntity(DeletePropertyDto dto);
        public Task<Response<string>> AddPropertiesToComplexType(AddPropertyDto dto);
        public Task<Response<List<PropertyDto>>> GetPropertiesFromComplexTypes(ModelDto dto);
        public Task<Response<string>> DeletePropertiesFromComplexTypes(DeletePropertyDto dto);

    }
}
