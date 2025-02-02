using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;

namespace ProjectMaker.Featueres.ServiceCreator.Contracts
{
    public interface IServiceCreatorService
    {
        public Task<Response<string>> AddServicesInterfaces(ServiceDto dto);
        public Task<Response<string>> GenerateServiceClasses(ServiceDto dto);
        public Task<Response<string>> AddMapping(ServiceDto dto);
    }
}
