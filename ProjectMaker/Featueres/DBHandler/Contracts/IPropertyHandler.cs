using ProjectMaker.Base;
using ProjectMaker.Dtos.DBHandler;

namespace ProjectMaker.Featueres.DBHandler.Contracts
{
    public interface IPropertyHandler
    {
        public Task<Response<string>> AddValidationToProperty(AttributeDto dto);
        public Task<Response<AttributeDetails>> GetAttributeDetailsFromProperty(AttributeDto dto);
        public Task<Response<string>> RemoveAttributeFromProperty(AttributeDto dto);
    }
}
