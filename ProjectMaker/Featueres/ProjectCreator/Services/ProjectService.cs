using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.ProjectCreator.Contracts;
using System.Diagnostics;

namespace ProjectMaker.Featueres.ProjectCreator.Services
{
    public class ProjectService(IConfiguration configuration, ResponseHandler responseHandler) : IProjectService
    {
        #region project Services
        public Response<string> CreateNewProject(ProjectDto dto)
        {
            var projectName = HelperMethods.IsValidName(dto.ProjectName) ? dto.ProjectName : HelperMethods.SanitizeName(dto.ProjectName);
            var projectPath = GetProjectPath(dto);
            if (Directory.Exists(projectPath))
            {
                return responseHandler.UnprocessableEntity<string>("Project Name is Exist");
            }
            var newProjectPath = createDirectories(projectName);
            return responseHandler.Success<string>("Project Created Succeffully");

        }
        public string GetProjectPath(ProjectDto dto)
        {
            var projectName = HelperMethods.IsValidName(dto.ProjectName) ? dto.ProjectName : HelperMethods.SanitizeName(dto.ProjectName);
            string baseDir = configuration["ProjectCreation:BasePath"] ??
                             Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects");
            string srcDir = Path.Combine(baseDir, "src");
            if (Directory.Exists(srcDir))
            {
                return Path.Combine(srcDir, projectName);
            }
            else
            {
                return "Project is not Exist";
            }

        }
        public Response<string> DeleteProject(ProjectDto dto)
        {
            var projectPath = GetProjectPath(dto);

            if (!Directory.Exists(projectPath))
            {
                return responseHandler.UnprocessableEntity<string>("project is not Exist");
            }

            Directory.Delete(projectPath, recursive: true);
            return responseHandler.Deleted<string>("Project Delete Succeffully");


        }
        public Response<IEnumerable<string?>> GetProjects()
        {
            string baseDir = configuration["ProjectCreation:BasePath"] ??
                   Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects");
            string srcDir = Path.Combine(baseDir, "src");

            if (!Directory.Exists(srcDir))
                return responseHandler.Success(Enumerable.Empty<string>());

            return responseHandler.Success(Directory.GetDirectories(srcDir).Select(Path.GetFileName));
        }
        #endregion
        #region Service Services
        public async Task<Response<string>> CreateNewService(ServiceDto dto)
        {
            var projectName = HelperMethods.IsValidName(dto.ProjectName) ? dto.ProjectName : HelperMethods.SanitizeName(dto.ProjectName);
            var serviceName = HelperMethods.IsValidName(dto.ServiceName) ? dto.ServiceName : HelperMethods.SanitizeName(dto.ServiceName);
            string projectPath = GetProjectPath(new ProjectDto { ProjectName = projectName });
            string servicePath = Path.Combine(projectPath, serviceName);
            if (Directory.Exists(servicePath))
                return responseHandler.UnprocessableEntity<string>($"Service {serviceName} already exists");
            var SanitizedService = new ServiceDto { ServiceName = serviceName, ProjectName = projectName, ServiceType = dto.ServiceType };
            await CreateServiceProcess(SanitizedService, projectPath, servicePath);

            return responseHandler.Success("Service Created Successfully");

        }
        public Response<string> DeleteService(ServiceDto dto)
        {
            var projectName = HelperMethods.IsValidName(dto.ProjectName) ? dto.ProjectName : HelperMethods.SanitizeName(dto.ProjectName);
            var serviceName = HelperMethods.IsValidName(dto.ServiceName) ? dto.ServiceName : HelperMethods.SanitizeName(dto.ServiceName);
            string projectPath = GetProjectPath(new ProjectDto { ProjectName = projectName });
            if (!Directory.Exists(projectPath))
                return responseHandler.UnprocessableEntity<string>($"Project {projectName} Is not exist ");
            string servicePath = Path.Combine(projectPath, serviceName);


            if (!Directory.Exists(servicePath))
                return responseHandler.UnprocessableEntity<string>($"Service {serviceName} Is not exist ");

            Directory.Delete(servicePath, recursive: true);
            return responseHandler.Deleted("service Deleted Successfully");

        }
        public Response<IEnumerable<string>> GetAllServices(ProjectDto dto)
        {

            var projectName = HelperMethods.IsValidName(dto.ProjectName) ? dto.ProjectName : HelperMethods.SanitizeName(dto.ProjectName);
            string projectPath = GetProjectPath(dto);
            if (!Directory.Exists(projectPath))
                throw new ArgumentException("Project is Not Exist");

            return responseHandler.Success(Directory.GetDirectories(projectPath).Select(Path.GetFileName));
        }
        public string GetServicePath(ServiceDto dto)
        {

            var projectPath = GetProjectPath(new ProjectDto { ProjectName = dto.ProjectName });
            var servicePath = Path.Combine(projectPath, dto.ServiceName);
            if (!Directory.Exists(servicePath))
            {
                return $"Service {dto.ServiceName} is not Exist";
            }
            return servicePath;

        }
        #endregion
        #region Helper Methods
        private string createDirectories(string projectName)
        {
            string baseDir = configuration["ProjectCreation:BasePath"] ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Projects");
            string srcDir = Path.Combine(baseDir, "src");
            string newProjectPath = Path.Combine(srcDir, projectName);
            Directory.CreateDirectory(srcDir);
            Directory.CreateDirectory(newProjectPath);
            return newProjectPath;
        }
        private async Task CreateServiceProcess(ServiceDto serviceDto, string srcDir, string newProjectPath)
        {
            var processInfo = new ProcessStartInfo();
            if (serviceDto.ServiceType == ServiceType.WepApiService)
            {
                processInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"new webapi -o {serviceDto.ServiceName} --use-controllers",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = srcDir
                };

            }
            if (serviceDto.ServiceType == ServiceType.None)
            {
                throw new ArgumentException("Service Type is invalid or Not Exist");
            }
            using (var process = Process.Start(processInfo))
            {
                process?.WaitForExit();
                string? output = process?.StandardOutput.ReadToEnd();
                string? error = process?.StandardError.ReadToEnd();

                if (process?.ExitCode != 0)
                {
                    throw new Exception($"Error Creating Project {error}");
                }

            }
            var IsInstalled = await InstallEFCorePackages(newProjectPath);

            var controllerfile = $@"{newProjectPath + "/Controllers/WeatherForecastController.cs"}";
            if (File.Exists(controllerfile))
            {
                File.Delete(controllerfile);
            }
            var defaultFile = $@"{newProjectPath + "/WeatherForecast.cs"}";
            if (File.Exists(defaultFile))
            {
                File.Delete(defaultFile);
            }

            if (!IsInstalled)
            {
                DeleteService(serviceDto);
                throw new Exception("Error Installing Packages");
            }
        }
        private async Task<bool> InstallEFCorePackages(string srcDir)
        {

            var packages = new List<string> { PackageInstaller.Package1.GetContent(), PackageInstaller.Package2.GetContent(),
                PackageInstaller.Package3.GetContent(), PackageInstaller.Package4.GetContent(),PackageInstaller.Package5.GetContent() };
            bool allSucceeded = true;

            foreach (var package in packages)
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"add package {package}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = srcDir
                };

                bool success = await Task.Run(() =>
                {
                    using (var process = new Process { StartInfo = processInfo })
                    {
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                Console.WriteLine(e.Data);
                            }
                        };

                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                Console.WriteLine(e.Data);
                            }
                        };

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            return false;
                        }
                    }

                    return true;
                });

                if (!success)
                {
                    allSucceeded = false;
                }
            }

            return allSucceeded;
        }


        #endregion
    }
}
