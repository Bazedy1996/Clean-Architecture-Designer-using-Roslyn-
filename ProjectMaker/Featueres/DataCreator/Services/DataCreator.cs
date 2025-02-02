using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectMaker.Base;
using ProjectMaker.Dtos.DataCreator;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.DataCreator.Contracts;
using ProjectMaker.Featueres.ProjectCreator.Contracts;
using System.Text.RegularExpressions;

namespace ProjectMaker.Featueres.DataCreator.Services
{
    public class DataCreator(IProjectService projectService, ResponseHandler responseHandler, IConfigurationDB configurationDBService) : IDataCreator
    {
        #region Models
        public async Task<Response<string>> CreateModels(ModelDto dto)
        {
            var projectName = HelperMethods.IsValidName(dto.ProjectName) ? dto.ProjectName : HelperMethods.SanitizeName(dto.ProjectName);
            var serviceName = HelperMethods.IsValidName(dto.ServiceName) ? dto.ServiceName : HelperMethods.SanitizeName(dto.ServiceName);
            var modelName = HelperMethods.IsValidName(dto.ModelName) ? dto.ModelName : HelperMethods.SanitizeName(dto.ModelName);
            var projectPath = projectService.GetProjectPath(new ProjectDto { ProjectName = projectName });
            if (!Directory.Exists(projectPath))
            {
                throw new ArgumentException("Project is Not Exist");
            }
            var serviceDto = new ServiceDto { ProjectName = projectName, ServiceName = serviceName, };
            var servicePath = projectService.GetServicePath(serviceDto);
            if (!Directory.Exists(servicePath))
            {
                throw new ArgumentException("Service is Not Exist");
            }
            var modelFolderPath = Path.Combine(servicePath, "Models");

            if (!Directory.Exists(modelFolderPath))
            {
                Directory.CreateDirectory(modelFolderPath);
            }
            var ExistingEntities = GetEntitiesFromModels(serviceDto).Data;
            var ExistingComplexTypes = GetComplexTypesFromModels(serviceDto).Data;
            if ((ExistingEntities ?? Enumerable.Empty<string>()).Contains(modelName) || (ExistingComplexTypes ?? Enumerable.Empty<string>()).Contains(modelName))
            {
                return responseHandler.UnprocessableEntity<string>("Model Name is Exist as Entity or ComplexType");
            }
            if (dto.ModelType == ModelType.Entity)
            {
                await CreateEntity(projectName, serviceName, modelName, modelFolderPath);
                return responseHandler.Created("Entity Created Successfully");
            }
            if (dto.ModelType == ModelType.ComplexType)
            {
                await CreateComplexType(projectName, serviceName, modelName, modelFolderPath);
                await AddDbSets(serviceDto, modelName);
                return responseHandler.Created("ComplexType Created Succssfully");
            }
            return responseHandler.BadRequest<string>("Model Type or Name is invalid ");
        }
        private async Task CreateEntity(string projectName, string serviceName, string entityName, string modelPath)
        {
            var entitiesFolderPath = Path.Combine(modelPath, "Entities");
            if (!Directory.Exists(entitiesFolderPath))
            {
                Directory.CreateDirectory(entitiesFolderPath);
            }
            var entityPath = Path.Combine(entitiesFolderPath, $"{entityName}.cs");
            var nameSpaceDeclartion = SyntaxFactory.NamespaceDeclaration(
                SyntaxFactory.ParseName($"{projectName}.{serviceName}.Models.Entities")
                ).NormalizeWhitespace();
            var classDeclaration = SyntaxFactory.ClassDeclaration(entityName).AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                    .AddMembers(
                                        SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("Guid"), "Id")
                                            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                            .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
                                            {
                                                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                                                SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                                            })))
                                    );
            nameSpaceDeclartion = nameSpaceDeclartion.AddMembers(classDeclaration);
            var compilationUnit = SyntaxFactory.CompilationUnit().AddMembers(nameSpaceDeclartion).NormalizeWhitespace();
            await File.WriteAllTextAsync(entityPath, compilationUnit.ToString());
        }
        private async Task CreateComplexType(string projectName, string serviceName, string ComplexTypeName, string modelPath)
        {
            var ComplexTypeFolderPath = Path.Combine(modelPath, "ComplexTypes");
            if (!Directory.Exists(ComplexTypeFolderPath))
            {
                Directory.CreateDirectory(ComplexTypeFolderPath);
            }
            var complexTypePath = Path.Combine(ComplexTypeFolderPath, $"{ComplexTypeName}.cs");
            var nameSpaceDeclartion = SyntaxFactory.NamespaceDeclaration(
                SyntaxFactory.ParseName($"{projectName}.{serviceName}.Models.ComplexTypes")
                ).NormalizeWhitespace();
            var recordDeclaration = SyntaxFactory.RecordDeclaration(SyntaxFactory.Token(SyntaxKind.RecordKeyword), ComplexTypeName)
                                         .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                                         .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                                         .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
                                         .AddAttributeLists(SyntaxFactory.AttributeList(
                                            SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("ComplexType"))
                                            )
                                         ));
            nameSpaceDeclartion = nameSpaceDeclartion.AddMembers(recordDeclaration);
            var compilationUnit = SyntaxFactory.CompilationUnit().AddMembers(nameSpaceDeclartion).NormalizeWhitespace();
            await File.WriteAllTextAsync(complexTypePath, compilationUnit.ToFullString());
        }
        public Response<List<string>> GetEntitiesFromModels(ServiceDto dto)
        {
            var projectName = HelperMethods.IsValidName(dto.ProjectName) ? dto.ProjectName : HelperMethods.SanitizeName(dto.ProjectName);
            var serviceName = HelperMethods.IsValidName(dto.ServiceName) ? dto.ServiceName : HelperMethods.SanitizeName(dto.ServiceName);
            var projecctPath = projectService.GetProjectPath(new ProjectDto { ProjectName = projectName });
            if (!Directory.Exists(projecctPath))
            {
                throw new ArgumentException("Project Is Not Exist");
            }
            var servicePath = projectService.GetServicePath(new ServiceDto { ProjectName = projectName, ServiceName = serviceName, });
            if (!Directory.Exists(servicePath))
            {
                throw new ArgumentException("Service Is Not Exist");
            }
            var EntitiesDir = Path.Combine(servicePath, "Models", "Entities");
            if (!Directory.Exists(EntitiesDir))
            {
                Directory.CreateDirectory(EntitiesDir);
            }
            var csFiles = Directory.GetFiles(EntitiesDir, "*.cs", SearchOption.AllDirectories);
            var Entities = csFiles.Select(file => Path.GetFileNameWithoutExtension(file)).ToList();
            return responseHandler.Success(Entities);
        }
        public Response<List<string>> GetComplexTypesFromModels(ServiceDto dto)
        {
            var projectName = HelperMethods.IsValidName(dto.ProjectName) ? dto.ProjectName : HelperMethods.SanitizeName(dto.ProjectName);
            var serviceName = HelperMethods.IsValidName(dto.ServiceName) ? dto.ServiceName : HelperMethods.SanitizeName(dto.ServiceName);
            var projecctPath = projectService.GetProjectPath(new ProjectDto { ProjectName = projectName });
            if (!Directory.Exists(projecctPath))
            {
                throw new ArgumentException("Project Is Not Exist");
            }
            var servicePath = projectService.GetServicePath(new ServiceDto { ProjectName = projectName, ServiceName = serviceName, });
            if (!Directory.Exists(servicePath))
            {
                throw new ArgumentException("Service Is Not Exist");
            }
            var ComplexTypesDir = Path.Combine(servicePath, "Models", "ComplexTypes");
            if (!Directory.Exists(ComplexTypesDir))
            {
                Directory.CreateDirectory(ComplexTypesDir);
            }
            var csFiles = Directory.GetFiles(ComplexTypesDir, "*.cs", SearchOption.AllDirectories);
            var ComplexTypes = csFiles.Select(file => Path.GetFileNameWithoutExtension(file)).ToList();
            return responseHandler.Success(ComplexTypes);
        }
        private string GetEntityPath(ModelDto entity)
        {
            var projectName = HelperMethods.IsValidName(entity.ProjectName) ? entity.ProjectName : HelperMethods.SanitizeName(entity.ProjectName);
            var serviceName = HelperMethods.IsValidName(entity.ServiceName) ? entity.ServiceName : HelperMethods.SanitizeName(entity.ServiceName);
            var modelName = HelperMethods.IsValidName(entity.ModelName) ? entity.ModelName : HelperMethods.SanitizeName(entity.ModelName);
            var servicePath = projectService.GetServicePath(new ServiceDto { ProjectName = projectName, ServiceName = serviceName, });
            var entityPath = Path.Combine(servicePath, "Models", "Entities", $"{modelName}.cs");
            if (!File.Exists(entityPath))
            {
                throw new FileNotFoundException("Entity Is Not Exist");
            }
            return entityPath;

        }
        private string GetComplexTypePath(ModelDto complexType)
        {
            var projectName = HelperMethods.IsValidName(complexType.ProjectName) ? complexType.ProjectName : HelperMethods.SanitizeName(complexType.ProjectName);
            var serviceName = HelperMethods.IsValidName(complexType.ServiceName) ? complexType.ServiceName : HelperMethods.SanitizeName(complexType.ServiceName);
            var modelName = HelperMethods.IsValidName(complexType.ModelName) ? complexType.ModelName : HelperMethods.SanitizeName(complexType.ModelName);
            var servicePath = projectService.GetServicePath(new ServiceDto { ProjectName = projectName, ServiceName = serviceName, });
            var entityPath = Path.Combine(servicePath, "Models", "ComplexTypes", $"{modelName}.cs");
            if (!File.Exists(entityPath))
            {
                throw new FileNotFoundException("ComplexType Is Not Exist");
            }
            return entityPath;

        }
        public async Task<Response<string>> DeleteEntity(ModelDto entity)
        {
            var serviceDto = new ServiceDto { ProjectName = entity.ProjectName, ServiceName = entity.ServiceName, };
            var projecctPath = projectService.GetProjectPath(new ProjectDto { ProjectName = entity.ProjectName });
            if (!Directory.Exists(projecctPath))
            {
                throw new ArgumentException("Project Is Not Exist");
            }
            var servicePath = projectService.GetServicePath(serviceDto);
            if (!Directory.Exists(servicePath))
            {
                throw new ArgumentException("Service Is Not Exist");
            }
            var entityPath = GetEntityPath(entity);
            File.Delete(entityPath);
            await RemoveDbSet(serviceDto, entity.ModelName);
            return responseHandler.Deleted("Entity Deleted Successfully");
        }
        public Response<string> DeleteComplexType(ModelDto complexType)
        {
            var complexTypePath = GetComplexTypePath(complexType);
            var projecctPath = projectService.GetProjectPath(new ProjectDto { ProjectName = complexType.ProjectName });
            if (!Directory.Exists(projecctPath))
            {
                throw new ArgumentException("Project Is Not Exist");
            }
            var servicePath = projectService.GetServicePath(new ServiceDto { ProjectName = complexType.ProjectName, ServiceName = complexType.ServiceName });
            if (!Directory.Exists(servicePath))
            {
                throw new ArgumentException("Service Is Not Exist");
            }
            File.Delete(complexTypePath);
            return responseHandler.Deleted("Entity Deleted Successfully");
        }
        private async Task AddDbSets(ServiceDto dto, string entityName)
        {
            var appDbContextClass = configurationDBService.GetDbContextPath(dto);
            if (appDbContextClass == null)
            {
                throw new Exception("DbContext file does not exist.");
            }

            var dbContextFile = await File.ReadAllTextAsync(appDbContextClass);
            var syntaxTree = CSharpSyntaxTree.ParseText(dbContextFile);
            var root = syntaxTree.GetRoot();
            var classDeclaration = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(cls => cls.Identifier.Text.Contains("ApplicationDbContext"));

            if (classDeclaration == null)
            {
                throw new Exception("ApplicationDbContext class not found in the file.");
            }

            var existingProperties = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .Where(prop => prop.Type is GenericNameSyntax type &&
                               type.Identifier.Text == "DbSet")
                .Select(prop => prop.Identifier.Text)
                .ToHashSet();


            var propertyName = $"{entityName}s";
            if (!existingProperties.Contains(propertyName))
            {
                var dbSetProperty = SyntaxFactory.PropertyDeclaration(
                        SyntaxFactory.GenericName("DbSet")
                            .WithTypeArgumentList(
                                SyntaxFactory.TypeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                        SyntaxFactory.IdentifierName(entityName)))),
                        SyntaxFactory.Identifier(propertyName))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

                classDeclaration = classDeclaration.AddMembers(dbSetProperty);
            }

            var updatedRoot = root.ReplaceNode(root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(), classDeclaration);
            var normalizedRoot = updatedRoot.NormalizeWhitespace();
            await File.WriteAllTextAsync(appDbContextClass, normalizedRoot.ToFullString());
        }
        private async Task RemoveDbSet(ServiceDto dto, string entityName)
        {
            var appDbContextClass = configurationDBService.GetDbContextPath(dto);
            if (appDbContextClass == null)
            {
                throw new Exception("DbContext file does not exist.");
            }

            var dbContextFile = await File.ReadAllTextAsync(appDbContextClass);
            var syntaxTree = CSharpSyntaxTree.ParseText(dbContextFile);
            var root = syntaxTree.GetRoot();
            var classDeclaration = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(cls => cls.Identifier.Text.Contains("ApplicationDbContext"));

            if (classDeclaration == null)
            {
                throw new Exception("ApplicationDbContext class not found in the file.");
            }

            var propertyName = $"{entityName}s";
            var propertyToRemove = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(prop => prop.Identifier.Text == propertyName &&
                                        prop.Type is GenericNameSyntax type &&
                                        type.Identifier.Text == "DbSet");

            if (propertyToRemove != null)
            {
                classDeclaration = classDeclaration.RemoveNode(propertyToRemove, SyntaxRemoveOptions.KeepNoTrivia);
            }
            else
            {
                throw new Exception($"DbSet property '{propertyName}' not found in ApplicationDbContext.");
            }

            var updatedRoot = root.ReplaceNode(root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(), classDeclaration);
            var normalizedRoot = updatedRoot.NormalizeWhitespace();
            await File.WriteAllTextAsync(appDbContextClass, normalizedRoot.ToFullString());
        }
        #endregion

        #region Properties
        public async Task<Response<string>> AddPropertiesToEntity(AddPropertyDto dto)
        {
            var projectName = HelperMethods.IsValidName(dto.ProjectName) ? dto.ProjectName : HelperMethods.SanitizeName(dto.ProjectName);
            var serviceName = HelperMethods.IsValidName(dto.Servicename) ? dto.Servicename : HelperMethods.SanitizeName(dto.Servicename);
            var entityName = HelperMethods.IsValidName(dto.ModelName) ? dto.ModelName : HelperMethods.SanitizeName(dto.ModelName);
            var entityPath = GetEntityPath(new ModelDto { ProjectName = projectName, ServiceName = serviceName, ModelName = entityName });
            var entityCode = await File.ReadAllTextAsync(entityPath);
            var syntaxTree = CSharpSyntaxTree.ParseText(entityCode);
            var root = await syntaxTree.GetRootAsync();

            var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == entityName);

            foreach (var property in dto.Properties)
            {
                var propertyName = HelperMethods.IsValidName(property.Name) ? property.Name : HelperMethods.SanitizeName(property.Name);
                if (!IsValidPropertyNameModel(propertyName, classNode!))
                {
                    return responseHandler.UnprocessableEntity<string>($"Property name '{propertyName}' is invalid.");
                }
                var serviceDto = new ServiceDto
                {
                    ProjectName = projectName,
                    ServiceName = serviceName
                };
                var (isValid, sanitizedType) = IsValidPropertyType(property.Type, serviceDto);

                var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName(sanitizedType), propertyName)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
                    {
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    })));
                classNode = classNode!.AddMembers(propertyDeclaration);
                var newRoot = root.ReplaceNode(root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First(c => c.Identifier.Text == entityName), classNode);

                await File.WriteAllTextAsync(entityPath, newRoot.NormalizeWhitespace().ToFullString());

            }
            return responseHandler.Created("property Created successfully");
        }
        public async Task<Response<List<PropertyDto>>> GetPropertiesFromEntity(ModelDto dto)
        {
            var entityFilePath = GetEntityPath(new ModelDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName, ModelName = dto.ModelName });
            var entityName = HelperMethods.IsValidName(dto.ModelName) ? dto.ModelName : HelperMethods.SanitizeName(dto.ModelName);
            var modelCode = await File.ReadAllTextAsync(entityFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(modelCode);
            var root = syntaxTree.GetRoot();
            var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == entityName);
            var properties = classNode!.Members.OfType<PropertyDeclarationSyntax>().Select(p => new PropertyDto { Name = p.Identifier.Text, Type = p.Type.ToString() }).ToList();
            return responseHandler.Success(properties);
        }
        public async Task<Response<string>> DeletePropertiesFromEntity(DeletePropertyDto dto)
        {



            var entityFilePath = GetEntityPath(new ModelDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName, ModelName = dto.ModelName });
            var modelName = HelperMethods.IsValidName(dto.ModelName) ? dto.ModelName : HelperMethods.SanitizeName(dto.ModelName);
            var modelCode = await File.ReadAllTextAsync(entityFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(modelCode);
            var root = syntaxTree.GetRoot();
            var classNode = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == modelName);
            var nonExistentProperties = new List<string>();
            var deletedProperties = new List<string>();


            foreach (var property in dto.PropertyNames)
            {
                var propertyName = HelperMethods.IsValidName(property) ? property : HelperMethods.SanitizeName(property);
                var propertyNode = classNode!.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == propertyName);
                if (propertyNode == null)
                {
                    nonExistentProperties.Add(propertyName);
                }
                else
                {
                    root = root.RemoveNode(propertyNode, SyntaxRemoveOptions.KeepNoTrivia);
                    classNode = root!.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == modelName);
                    deletedProperties.Add(propertyName);
                }
            }

            // Write updated code back to file
            await File.WriteAllTextAsync(entityFilePath, root.NormalizeWhitespace().ToFullString());

            // Throw exception if any properties weren't found
            if (nonExistentProperties.Any())
            {
                throw new InvalidOperationException(
                    $"The following properties were not found: {string.Join(", ", nonExistentProperties)}. " +
                    $"Successfully deleted properties: {string.Join(", ", deletedProperties)}");
            }

            return responseHandler.Success($"Successfully deleted properties: {string.Join(", ", deletedProperties)}");
        }
        private bool IsValidPropertyNameModel(string propertyName, ClassDeclarationSyntax classNode)
        {
            propertyName = HelperMethods.IsValidName(propertyName) ? propertyName : HelperMethods.SanitizeName(propertyName);
            if (string.IsNullOrWhiteSpace(propertyName) || (!char.IsLetter(propertyName[0]) && propertyName[0] != '_'))
                return false;
            if (propertyName.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
                return false;
            if (classNode.Members.OfType<PropertyDeclarationSyntax>().Any(p => p.Identifier.Text == propertyName))
                throw new InvalidOperationException($"Property '{propertyName}' already exists in the model.");
            var reservedKeywords = SyntaxFacts.GetContextualKeywordKinds()
                .Select(SyntaxFacts.GetText)
                .Concat(SyntaxFacts.GetReservedKeywordKinds().Select(SyntaxFacts.GetText));

            return !reservedKeywords.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
        }
        private bool IsValidPropertyNameRecord(string propertyName, RecordDeclarationSyntax recordNode)
        {
            propertyName = HelperMethods.IsValidName(propertyName) ? propertyName : HelperMethods.SanitizeName(propertyName);
            if (string.IsNullOrWhiteSpace(propertyName) || (!char.IsLetter(propertyName[0]) && propertyName[0] != '_'))
                return false;
            if (propertyName.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
                return false;
            if (recordNode.Members.OfType<PropertyDeclarationSyntax>().Any(p => p.Identifier.Text == propertyName))
                throw new InvalidOperationException($"Property '{propertyName}' already exists in the model.");
            var reservedKeywords = SyntaxFacts.GetContextualKeywordKinds()
                .Select(SyntaxFacts.GetText)
                .Concat(SyntaxFacts.GetReservedKeywordKinds().Select(SyntaxFacts.GetText));

            return !reservedKeywords.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
        }
        private bool IsCustomModelType(string typeName, ServiceDto serviceDto)
        {
            typeName = HelperMethods.IsValidName(typeName) ? typeName : HelperMethods.SanitizeName(typeName);
            var customModels = GetComplexTypesFromModels(serviceDto);
            return (customModels.Data ?? Enumerable.Empty<string>()).Any(m => m == typeName);
        }
        private (bool isValid, string sanitizedType) IsValidPropertyType(string type, ServiceDto serviceDto)
        {
            var systemTypes = new HashSet<string>
            {
                "int", "long", "short", "byte", "float", "double", "decimal",
                "bool", "char", "DateTime", "TimeSpan", "Guid", "string"
            };

            var arrayPattern = new Regex(@"^(\w+)\[\]$");
            var collectionPattern = new Regex(@"^(List|Dictionary|ICollection)<(.+?)>$");

            if (systemTypes.Contains(type))
                return (true, type);
            if (arrayPattern.IsMatch(type))
            {
                var elementType = arrayPattern.Match(type).Groups[1].Value;
                if (systemTypes.Contains(elementType))
                    return (true, type);

                var sanitizedElementType = !HelperMethods.IsValidName(elementType) ? HelperMethods.SanitizeName(elementType) : elementType;

                var isValid = IsCustomModelType(sanitizedElementType, serviceDto);
                if (!isValid)
                {
                    throw new InvalidOperationException($"Invalid array element type: {elementType}. " +
                        $"Type should match an existing model name with proper capitalization.");
                }
                return (true, $"{sanitizedElementType}[]");
            }

            var collectionMatch = collectionPattern.Match(type);
            if (collectionMatch.Success)
            {
                var collectionType = collectionMatch.Groups[1].Value;
                var innerTypes = collectionMatch.Groups[2].Value.Split(',').Select(t => t.Trim()).ToArray();
                var sanitizedInnerTypes = new List<string>();

                foreach (var innerType in innerTypes)
                {
                    if (systemTypes.Contains(innerType))
                    {
                        sanitizedInnerTypes.Add(innerType);
                        continue;
                    }

                    var sanitizedInnerType = !HelperMethods.IsValidName(innerType) ? HelperMethods.SanitizeName(innerType) : innerType;

                    var isValid = IsCustomModelType(sanitizedInnerType, serviceDto);
                    if (!isValid)
                    {
                        throw new InvalidOperationException($"Invalid generic type parameter: {innerType}. " +
                            $"Type should match an existing model name.");
                    }
                    sanitizedInnerTypes.Add(sanitizedInnerType);
                }
                return (true, $"{collectionType}<{string.Join(", ", sanitizedInnerTypes)}>");
            }

            var sanitizedType = !HelperMethods.IsValidName(type) ? HelperMethods.SanitizeName(type) : type;

            var isModelValid = IsCustomModelType(sanitizedType, serviceDto);
            if (!isModelValid)
            {
                throw new InvalidOperationException($"Invalid type: {type}. " +
                    $"Type should match an existing model.");
            }
            return (true, sanitizedType);
        }

        public async Task<Response<string>> AddPropertiesToComplexType(AddPropertyDto dto)
        {


            var complexTypeFilePath = GetComplexTypePath(new ModelDto { ProjectName = dto.ProjectName, ServiceName = dto.Servicename, ModelName = dto.ModelName });
            var recordName = HelperMethods.IsValidName(dto.ModelName) ? dto.ModelName : HelperMethods.SanitizeName(dto.ModelName);
            // Load and parse existing model file
            var modelCode = await File.ReadAllTextAsync(complexTypeFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(modelCode);
            var root = syntaxTree.GetRoot();
            var recordNode = root.DescendantNodes().OfType<RecordDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == recordName);

            if (recordNode == null)
            {
                throw new InvalidOperationException($"Class '{recordName}' not found in the file.");
            }

            foreach (var property in dto.Properties)
            {
                var capitalizedPropertyName = HelperMethods.SanitizeName(property.Name);
                if (!IsValidPropertyNameRecord(capitalizedPropertyName, recordNode))
                {
                    throw new InvalidOperationException($"Property name '{capitalizedPropertyName}' is invalid.");
                }

                var serviceDto = new ServiceDto
                {
                    ProjectName = dto.ProjectName,
                    ServiceName = dto.Servicename
                };

                var (isValid, sanitizedType) = IsValidPropertyType(property.Type, serviceDto);

                var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName(sanitizedType), capitalizedPropertyName)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
                    {
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration)
                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    })));

                recordNode = recordNode.AddMembers(propertyDeclaration);
            }

            var newRoot = root.ReplaceNode(root.DescendantNodes().OfType<RecordDeclarationSyntax>().First(c => c.Identifier.Text == recordName), recordNode);

            await File.WriteAllTextAsync(complexTypeFilePath, newRoot.NormalizeWhitespace().ToFullString());
            return responseHandler.Created("Properties Created Successfully");
        }
        public async Task<Response<List<PropertyDto>>> GetPropertiesFromComplexTypes(ModelDto dto)
        {

            var modelFilePath = GetComplexTypePath(new ModelDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName, ModelName = dto.ModelName });
            var modelName = HelperMethods.IsValidName(dto.ModelName) ? dto.ModelName : HelperMethods.SanitizeName(dto.ModelName);
            var modelCode = await File.ReadAllTextAsync(modelFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(modelCode);
            var root = syntaxTree.GetRoot();
            var classNode = root.DescendantNodes().OfType<RecordDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == modelName);
            // Extract property names and types
            var properties = classNode!.Members.OfType<PropertyDeclarationSyntax>().Select(p => new PropertyDto { Name = p.Identifier.Text, Type = p.Type.ToString() }).ToList();

            return responseHandler.Success(properties);
        }
        public async Task<Response<string>> DeletePropertiesFromComplexTypes(DeletePropertyDto dto)
        {
            var modelFilePath = GetComplexTypePath(new ModelDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName, ModelName = dto.ModelName });
            var modelName = HelperMethods.IsValidName(dto.ModelName) ? dto.ModelName : HelperMethods.SanitizeName(dto.ModelName);
            var modelCode = await File.ReadAllTextAsync(modelFilePath);
            var syntaxTree = CSharpSyntaxTree.ParseText(modelCode);
            var root = syntaxTree.GetRoot();
            var classNode = root.DescendantNodes().OfType<RecordDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == modelName);


            var nonExistentProperties = new List<string>();
            var deletedProperties = new List<string>();

            foreach (var property in dto.PropertyNames)
            {
                var sanitizedPropertyName = HelperMethods.SanitizeName(property);
                var propertyNode = classNode!.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == sanitizedPropertyName);

                if (propertyNode == null)
                {
                    nonExistentProperties.Add(sanitizedPropertyName);
                }
                else
                {
                    root = root.RemoveNode(propertyNode, SyntaxRemoveOptions.KeepNoTrivia);
                    classNode = root.DescendantNodes()
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
        #endregion
    }
}
