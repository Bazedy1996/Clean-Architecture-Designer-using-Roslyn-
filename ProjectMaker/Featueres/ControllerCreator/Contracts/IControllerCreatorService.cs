using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;

namespace ProjectMaker.Featueres.ControllerCreator.Contracts
{
    public interface IControllerCreatorService
    {
        public Task<Response<string>> CreateControllers(ServiceDto dto);
    }
}
