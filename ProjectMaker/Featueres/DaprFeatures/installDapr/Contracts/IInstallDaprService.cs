using ProjectMaker.Base;

namespace ProjectMaker.Featueres.DaprFeatures.installDapr.Contracts
{
    public interface IInstallDaprService
    {
        public Response<string> InstallDapr();
        public Response<string> InitializeDaprSlim();
    }
}
