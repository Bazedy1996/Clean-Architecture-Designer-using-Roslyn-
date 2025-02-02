using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectMaker.Base;
using ProjectMaker.Dtos.DataCreator;
using ProjectMaker.Dtos.DtoCreator;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.DataCreator.Contracts;
using ProjectMaker.Featueres.DtoCreator.Contracts;
using ProjectMaker.Featueres.ProjectCreator.Contracts;

namespace ProjectMaker.Featueres.DtoCreator.Services
{
    public class DtoCreatorService(IDataCreator dataCreatorService, IProjectService projectService, ResponseHandler responseHandler) : IDtoCreatorService
    {
        public async Task<Response<string>> CreateDto(AddDtoProperties dto)
        {
            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };
            var models = dataCreatorService.GetEntitiesFromModels(serviceDto);
            if (!models.Data.Any(m => m == dto.ModelName))
            {
                return responseHandler.UnprocessableEntity<string>("Model does not exist");
            }

            var modelDto = new ModelDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName, ModelName = dto.ModelName };
            var properties = dataCreatorService.GetPropertiesFromEntity(modelDto);
            var servicePath = projectService.GetServicePath(serviceDto);
            var modelFolderPath = Path.Combine(servicePath, "Dtos", HelperMethods.SanitizeName(dto.ModelName));

            if (!Directory.Exists(modelFolderPath))
            {
                Directory.CreateDirectory(modelFolderPath);
            }

            var dtoFileName = $"{dto.DtoType}{HelperMethods.SanitizeName(dto.ModelName)}Dto.cs";
            var dtoFilePath = Path.Combine(modelFolderPath, dtoFileName);

            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(
                SyntaxFactory.ParseName($"{dto.ProjectName}.{dto.ServiceName}.Dtos.{HelperMethods.SanitizeName(dto.ModelName)}")
            ).NormalizeWhitespace();

            var dtoRecordDeclaration = SyntaxFactory.RecordDeclaration(SyntaxFactory.Token(SyntaxKind.RecordKeyword), $"{dto.DtoType}{HelperMethods.SanitizeName(dto.ModelName)}Dto")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken));

            foreach (var property in dto.properties)
            {
                var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName(property.Type), HelperMethods.SanitizeName(property.Name))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
                    {
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    })));

                dtoRecordDeclaration = dtoRecordDeclaration.AddMembers(propertyDeclaration);
            }

            namespaceDeclaration = namespaceDeclaration.AddMembers(dtoRecordDeclaration);
            var compilationUnit = SyntaxFactory.CompilationUnit().AddMembers(namespaceDeclaration).NormalizeWhitespace();

            await File.WriteAllTextAsync(dtoFilePath, compilationUnit.ToString());
            return responseHandler.Created("DTO created successfully");
        }
        public Response<List<string>> GetDto(ServiceDto dto)
        {
            var projectName = HelperMethods.IsValidName(dto.ProjectName) ? dto.ProjectName : HelperMethods.SanitizeName(dto.ProjectName);
            var serviceName = HelperMethods.IsValidName(dto.ServiceName) ? dto.ServiceName : HelperMethods.SanitizeName(dto.ServiceName);
            var servicePath = projectService.GetServicePath(new ServiceDto { ProjectName = projectName, ServiceName = serviceName, });
            var DtoDir = Path.Combine(servicePath, "Dtos");
            if (!Directory.Exists(DtoDir))
            {
                Directory.CreateDirectory(DtoDir);
            }
            var csFiles = Directory.GetFiles(DtoDir, "*.cs", SearchOption.AllDirectories);
            var ComplexTypes = csFiles.Select(file => Path.GetFileNameWithoutExtension(file)).ToList();
            return responseHandler.Success(ComplexTypes);
        }
        public async Task<Response<List<PropertyDto>>> GetPropertiesFromDto(ModelDto dto)
        {

            var modelFilePath = GetDtoPath(new ModelDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName, ModelName = dto.ModelName });
            var modelName = HelperMethods.IsValidName(dto.ModelName) ? dto.ModelName : HelperMethods.SanitizeName(dto.ModelName);
            var modelCode = await File.ReadAllTextAsync(modelFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(modelCode);
            var root = syntaxTree.GetRoot();
            var recordNode = root.DescendantNodes().OfType<RecordDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == modelName);
            // Extract property names and types
            var properties = recordNode!.Members.OfType<PropertyDeclarationSyntax>().Select(p => new PropertyDto { Name = p.Identifier.Text, Type = p.Type.ToString() }).ToList();

            return responseHandler.Success(properties);
        }
        private string GetDtoPath(ModelDto complexType)
        {
            var projectName = HelperMethods.IsValidName(complexType.ProjectName) ? complexType.ProjectName : HelperMethods.SanitizeName(complexType.ProjectName);
            var serviceName = HelperMethods.IsValidName(complexType.ServiceName) ? complexType.ServiceName : HelperMethods.SanitizeName(complexType.ServiceName);
            var modelName = HelperMethods.IsValidName(complexType.ModelName) ? complexType.ModelName : HelperMethods.SanitizeName(complexType.ModelName);
            var servicePath = projectService.GetServicePath(new ServiceDto { ProjectName = projectName, ServiceName = serviceName, });
            var DtoPath = Path.Combine(servicePath, "Dtos", $"{modelName}.cs");
            if (!File.Exists(DtoPath))
            {
                throw new FileNotFoundException("Dto Is Not Exist");
            }
            return DtoPath;

        }
        public async Task<Response<string>> DeletePropertiesFromDto(DeleteDtoProperties dto)
        {
            var modelFilePath = GetDtoPath(new ModelDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName, ModelName = dto.ModelName });
            var modelName = HelperMethods.IsValidName(dto.ModelName) ? dto.ModelName : HelperMethods.SanitizeName(dto.ModelName);
            var modelCode = await File.ReadAllTextAsync(modelFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(modelCode);
            var root = syntaxTree.GetRoot();
            var recordNode = root.DescendantNodes().OfType<RecordDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == modelName);


            var nonExistentProperties = new List<string>();
            var deletedProperties = new List<string>();

            // Remove specified properties
            foreach (var property in dto.PropertyNames)
            {
                var sanitizedPropertyName = HelperMethods.SanitizeName(property);
                var propertyNode = recordNode!.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == sanitizedPropertyName);

                if (propertyNode == null)
                {
                    nonExistentProperties.Add(sanitizedPropertyName);
                }
                else
                {
                    root = root.RemoveNode(propertyNode, SyntaxRemoveOptions.KeepNoTrivia);
                    recordNode = root.DescendantNodes()
                        .OfType<RecordDeclarationSyntax>()
                        .First(c => c.Identifier.Text == modelName);
                    deletedProperties.Add(sanitizedPropertyName);
                }
            }

            // Write updated code back to file
            await File.WriteAllTextAsync(modelFilePath, root.NormalizeWhitespace().ToFullString());

            // Throw exception if any properties weren't found
            if (nonExistentProperties.Any())
            {
                throw new InvalidOperationException(
                    $"The following properties were not found: {string.Join(", ", nonExistentProperties)}. " +
                    $"Successfully deleted properties: {string.Join(", ", deletedProperties)}");
            }

            return responseHandler.Deleted($"Successfully deleted properties: {string.Join(", ", deletedProperties)}");
        }
    }
}
