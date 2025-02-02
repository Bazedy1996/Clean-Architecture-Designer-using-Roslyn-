using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;

namespace ProjectMaker.Featueres.DataCreator.Contracts
{
    public interface IConfigurationDB
    {
        public Task<Response<string>> AddAppDbContext(ServiceDto dto);
        public string GetDbContextPath(ServiceDto dto);
    }
}
