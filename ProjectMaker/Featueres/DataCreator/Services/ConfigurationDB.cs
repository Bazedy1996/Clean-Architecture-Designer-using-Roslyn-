using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.DataCreator.Contracts;
using ProjectMaker.Featueres.ProjectCreator.Contracts;

namespace ProjectMaker.Featueres.DataCreator.Services
{
    public class ConfigurationDB(IProjectService projectService, ResponseHandler responseHandler) : IConfigurationDB
    {
        public async Task<Response<string>> AddAppDbContext(ServiceDto dto)
        {
            string projectName = HelperMethods.IsValidName(dto.ProjectName) ? dto.ProjectName : HelperMethods.SanitizeName(dto.ProjectName);
            string serviceName = HelperMethods.IsValidName(dto.ServiceName) ? dto.ServiceName : HelperMethods.SanitizeName(dto.ServiceName);
            var serviceDto = new ServiceDto
            {
                ProjectName = projectName,
                ServiceName = serviceName,
            };
            string servicePath = projectService.GetServicePath(serviceDto);
            if (!Directory.Exists(servicePath))
            {
                throw new ArgumentException($"Service '{serviceName}' does not exist ");
            }

            string directoryPath = Path.Combine(servicePath, "Data");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
            var classCode = SyntaxFactory.ClassDeclaration("ApplicationDbContext")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("DbContext")))
                .AddMembers(
                    SyntaxFactory.ConstructorDeclaration("ApplicationDbContext")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddParameterListParameters(SyntaxFactory.Parameter(SyntaxFactory.Identifier("options"))
                            .WithType(SyntaxFactory.ParseTypeName("DbContextOptions<ApplicationDbContext>")))
                        .WithInitializer(SyntaxFactory.ConstructorInitializer(
                            SyntaxKind.BaseConstructorInitializer,
                            SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.Argument(SyntaxFactory.IdentifierName("options"))
                            ))
                        ))
                        .WithBody(SyntaxFactory.Block())
                );

            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(
                SyntaxFactory.ParseName($"{projectName}.{serviceName}.Data"))
                .AddMembers(classCode);
            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("Microsoft.EntityFrameworkCore")))
                .AddMembers(namespaceDeclaration)
                .NormalizeWhitespace();

            string filePath = Path.Combine(directoryPath, "ApplicationDbContext.cs");
            await File.WriteAllTextAsync(filePath, compilationUnit.ToFullString());
            await AddOnConfiguring(serviceDto);
            await AddOnModelCreating(serviceDto);
            return responseHandler.Success<string>("ApplicationDbContext created successfully");
        }
        public string GetDbContextPath(ServiceDto dto)
        {
            var dbContextPath = Path.Combine(projectService.GetServicePath(dto), "Data", "ApplicationDbContext.cs");
            return dbContextPath;

        }
        private async Task AddOnConfiguring(ServiceDto dto)
        {

            var servicePath = projectService.GetServicePath(dto);
            var dbContextPath = Path.Combine(servicePath, "Data", "ApplicationDbContext.cs");
            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(dbContextPath));
            var root = syntaxTree.GetRoot();
            var dbContextClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == "ApplicationDbContext");

            if (dbContextClass == null)
            {
                throw new ArgumentException($"ApplicationDbContext class not found in {dbContextPath}");
            }

            var onConfiguringExists = dbContextClass.Members.OfType<MethodDeclarationSyntax>()
                .Any(m => m.Identifier.Text == "OnConfiguring");

            if (!onConfiguringExists)
            {
                var onConfiguringMethod = SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                        "OnConfiguring")
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
                    .AddParameterListParameters(SyntaxFactory.Parameter(
                        SyntaxFactory.Identifier("optionsBuilder"))
                        .WithType(SyntaxFactory.ParseTypeName("DbContextOptionsBuilder")))
                    .WithBody(SyntaxFactory.Block(
                        SyntaxFactory.ParseStatement("optionsBuilder.UseSqlServer(\"your-connection-string\");")
                    ));

                var newDbContextClass = dbContextClass.AddMembers(onConfiguringMethod);
                var newRoot = root.ReplaceNode(dbContextClass, newDbContextClass);
                await File.WriteAllTextAsync(dbContextPath, newRoot.NormalizeWhitespace().ToFullString());
            }
        }
        private async Task AddOnModelCreating(ServiceDto dto)
        {

            var servicePath = projectService.GetServicePath(dto);
            var dbContextPath = Path.Combine(servicePath, "Data", "ApplicationDbContext.cs");
            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(dbContextPath));
            var root = syntaxTree.GetRoot();
            var dbContextClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == "ApplicationDbContext");

            if (dbContextClass == null)
            {
                throw new ArgumentException($"ApplicationDbContext class not found in {dbContextPath}");
            }

            var onModelCreatingExists = dbContextClass.Members.OfType<MethodDeclarationSyntax>()
                .Any(m => m.Identifier.Text == "OnModelCreating");

            if (!onModelCreatingExists)
            {
                var onConfiguringMethod = SyntaxFactory.MethodDeclaration(
                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                        "OnModelCreating")
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.OverrideKeyword))
                    .AddParameterListParameters(SyntaxFactory.Parameter(
                        SyntaxFactory.Identifier("modelBuilder"))
                        .WithType(SyntaxFactory.ParseTypeName("ModelBuilder")))
                    .WithBody(SyntaxFactory.Block());

                var newDbContextClass = dbContextClass.AddMembers(onConfiguringMethod);
                var newRoot = root.ReplaceNode(dbContextClass, newDbContextClass);
                await File.WriteAllTextAsync(dbContextPath, newRoot.NormalizeWhitespace().ToFullString());
            }
        }
    }
}
