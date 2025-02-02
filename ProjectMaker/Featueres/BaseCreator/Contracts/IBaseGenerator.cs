using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;

namespace ProjectMaker.Featueres.BaseCreator.Contracts
{
    public interface IBaseGenerator
    {
        public Task<Response<string>> BaseCreator(ServiceDto dto);
    }
}
