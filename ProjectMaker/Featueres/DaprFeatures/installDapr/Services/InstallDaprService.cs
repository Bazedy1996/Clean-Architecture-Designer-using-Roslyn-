using ProjectMaker.Base;
using ProjectMaker.Featueres.DaprFeatures.installDapr.Contracts;
using System.Diagnostics;

namespace ProjectMaker.Featueres.DaprFeatures.installDapr.Services
{
    public class InstallDaprService(ResponseHandler responseHandler) : IInstallDaprService
    {
        #region InstallDapr
        public Response<string> InstallDapr()
        {
            if (OperatingSystem.IsWindows())
            {
                InstallDaprOnWindows();

            }
            else if (OperatingSystem.IsLinux())
            {
                InstallDaprOnLinux();

            }
            else
            {
                throw new PlatformNotSupportedException("Current operating system is not supported.");
            }
            return responseHandler.Success<string>("Dapr Installed Successfully ");
        }
        private static void InstallDaprOnLinux()
        {

            var startInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = "-c \"wget -q https://raw.githubusercontent.com/dapr/cli/master/install/install.sh -O - | /bin/bash\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Installation failed with exit code: {process.ExitCode}");
                }
            }
        }
        private static void InstallDaprOnWindows()
        {

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-Command \"iwr -useb https://raw.githubusercontent.com/dapr/cli/master/install/install.ps1 | iex\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Installation failed with exit code: {process.ExitCode}");
                }
            }
        }
        #endregion
        public Response<string> InitializeDaprSlim()
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dapr",
                Arguments = "init --slim",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();
                if (process.ExitCode != 0)
                {
                    throw new Exception($"Installation failed with exit code: {process.ExitCode}");
                }
            }
            return responseHandler.Success<string>("Dapr Init successfully");
        }
    }



}
