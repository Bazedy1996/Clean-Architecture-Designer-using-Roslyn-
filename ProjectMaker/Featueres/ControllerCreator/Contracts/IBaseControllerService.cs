using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;

namespace ProjectMaker.Featueres.ControllerCreator.Contracts
{
    public interface IBaseControllerService
    {
        public Task<Response<string>> CreateAppBaseController(ServiceDto dto);
    }
}
