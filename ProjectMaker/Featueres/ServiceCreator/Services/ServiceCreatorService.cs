using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectMaker.Base;
using ProjectMaker.Dtos.DataCreator;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.DataCreator.Contracts;
using ProjectMaker.Featueres.DtoCreator.Contracts;
using ProjectMaker.Featueres.ProjectCreator.Contracts;
using ProjectMaker.Featueres.ServiceCreator.Contracts;

namespace ProjectMaker.Featueres.ServiceCreator.Services
{
    public class ServiceCreatorService(IProjectService projectService, IDataCreator dataCreatorService, ResponseHandler responseHandler, IDtoCreatorService dtoCreatorService) : IServiceCreatorService
    {
        #region Generate interfaces
        public async Task<Response<string>> AddServicesInterfaces(ServiceDto dto)
        {
            var interfacesPath = Path.Combine(projectService.GetServicePath(dto), "Services", "Contracts");

            if (!Directory.Exists(interfacesPath))
            {
                Directory.CreateDirectory(interfacesPath);
            }
            var entitiesResponse = dataCreatorService.GetEntitiesFromModels(dto);
            if (entitiesResponse.Data == null || !entitiesResponse.Succeeded)
            {
                return responseHandler.BadRequest<string>("Failed to retrieve entities.");
            }
            var entityNames = entitiesResponse.Data;

            foreach (var entityName in entityNames)
            {
                var interfaceName = $"I{entityName}Service";
                var interfaceCode = GenerateInterfaceCode(dto, entityName, interfaceName);
                var interfaceFilePath = Path.Combine(interfacesPath, $"{interfaceName}.cs");
                await File.WriteAllTextAsync(interfaceFilePath, interfaceCode);
            }
            return responseHandler.Success<string>("Contracts Created Successfully");

        }
        private string GenerateInterfaceCode(ServiceDto dto, string entityName, string interfaceName)
        {
            string namespaceName = $"{dto.ProjectName}.{dto.ServiceName}.Services.Interfaces";

            var interfaceDeclaration = SyntaxFactory.InterfaceDeclaration(interfaceName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            var methods = new List<MethodDeclarationSyntax>
            {
                CreateMethodDeclaration("GetTableNoTracking", $"Task<List<Read{entityName}Dto>>"),
                CreateMethodDeclaration("GetTableAsTracking", $"IQueryable<Read{entityName}Dto>"),
                CreateMethodDeclaration("AddAsync", $"Task<Response<Create{entityName}Dto>>", ($"create{entityName}Dto", $"Create{entityName}Dto")),
                CreateMethodDeclaration("AddRangeAsync", $"Task<Response<string>>", ($"create{entityName}Dtos", $"ICollection<Create{entityName}Dto>")),
                CreateMethodDeclaration("UpdateAsync", $"Task<Response<string>>", ($"update{entityName}Dto", $"Update{entityName}Dto")),
                CreateMethodDeclaration("UpdateRangeAsync", $"Task<Response<string>>", ($"update{entityName}Dtos", $"ICollection<Update{entityName}Dto>")),
                CreateMethodDeclaration("DeleteAsync", $"Task<Response<string>>", ($"delete{entityName}Dto", $"Delete{entityName}Dto")),
                CreateMethodDeclaration("DeleteRangeAsync", $"Task<Response<string>>", ($"delete{entityName}Dtos", $"ICollection<Delete{entityName}Dto>")),
                CreateMethodDeclaration("GetByIdAsync", $"Task<Response<Read{entityName}Dto>>", ("id", "Guid")),
                CreateMethodDeclaration("SaveChangesAsync", $"Task")
            };
            interfaceDeclaration = interfaceDeclaration.AddMembers(methods.ToArray());

            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName)).AddMembers(interfaceDeclaration);


            var compilationUnit = SyntaxFactory.CompilationUnit().AddMembers(namespaceDeclaration).NormalizeWhitespace();
            return compilationUnit.ToFullString();
        }
        private MethodDeclarationSyntax CreateMethodDeclaration(string methodName, string returnType, params (string Name, string Type)[] parameters)
        {
            var returnTypeSyntax = SyntaxFactory.ParseTypeName(returnType);
            var methodDeclaration = SyntaxFactory.MethodDeclaration(returnTypeSyntax, methodName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            foreach (var param in parameters)
            {
                var parameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(param.Name))
                    .WithType(SyntaxFactory.ParseTypeName(param.Type));
                methodDeclaration = methodDeclaration.AddParameterListParameters(parameter);
            }

            return methodDeclaration;
        }
        #endregion
        #region Generate implementations

        public async Task<Response<string>> GenerateServiceClasses(ServiceDto dto)
        {
            var implementationsPath = Path.Combine(projectService.GetServicePath(dto), "Services", "Implementations");
            if (!Directory.Exists(implementationsPath))
            {
                Directory.CreateDirectory(implementationsPath);
            }

            var entitiesResponse = dataCreatorService.GetEntitiesFromModels(dto);
            if (entitiesResponse.Data == null || !entitiesResponse.Succeeded)
            {
                return responseHandler.BadRequest<string>("Failed to retrieve entities.");
            }
            var entityNames = entitiesResponse.Data;

            foreach (var entityName in entityNames)
            {
                var className = $"{entityName}Service";
                var classCode = GenerateServiceClassCode(entityName, className);
                var filePath = Path.Combine(implementationsPath, $"{className}.cs");
                await File.WriteAllTextAsync(filePath, classCode);
            }
            return responseHandler.Created("Services implementations Created Successfully");
        }

        private string GenerateServiceClassCode(string entityName, string className)
        {
            var namespaceName = "Project1.Service1.Services.Implementations";

            var classDeclaration = SyntaxFactory.ClassDeclaration(className)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithParameterList(SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(new[]
                {
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("repository"))
                .WithType(SyntaxFactory.ParseTypeName($"IGenericRepository<{entityName}>")),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("mapper"))
                .WithType(SyntaxFactory.ParseTypeName("IMapper")),
            SyntaxFactory.Parameter(SyntaxFactory.Identifier("responseHandler"))
                .WithType(SyntaxFactory.ParseTypeName("ResponseHandler"))
                })))
                .AddBaseListTypes(
                    SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName($"I{entityName}Service")))
                .AddMembers(
                    GenerateAsyncMethod("GetTableNoTracking", $"List<Read{entityName}Dto>", "", $"var result = await repository.GetTableNoTracking().Select(entity => mapper.Map<Read{entityName}Dto>(entity)).ToListAsync();\nreturn result;\n"),
                    GenerateMethod("GetTableAsTracking", $"IQueryable<Read{entityName}Dto>", $"repository.GetTableAsTracking().Select(entity => mapper.Map<Read{entityName}Dto>(entity))"),
                    GenerateAsyncMethod("AddAsync", $"Response<Create{entityName}Dto>", $"Create{entityName}Dto create{entityName}Dto", $"var entity = mapper.Map<{entityName}>(create{entityName}Dto);\nawait repository.AddAsync(entity);\nawait repository.SaveChangesAsync();\nreturn responseHandler.Created(create{entityName}Dto);"),
                    GenerateAsyncMethod("AddRangeAsync", "Response<string>", $"ICollection<Create{entityName}Dto> create{entityName}Dtos", $"var entities = mapper.Map<ICollection<{entityName}>>(create{entityName}Dtos);\nawait repository.AddRangeAsync(entities);\nawait repository.SaveChangesAsync();\nreturn responseHandler.Success(\"Entities added successfully\");"),
                    GenerateAsyncMethod("UpdateAsync", "Response<string>", $"Update{entityName}Dto update{entityName}Dto", $"var entity = mapper.Map<{entityName}>(update{entityName}Dto);\nawait repository.UpdateAsync(entity);\nawait repository.SaveChangesAsync();\nreturn responseHandler.Success(\"Entity updated successfully\");"),
                    GenerateAsyncMethod("UpdateRangeAsync", "Response<string>", $"ICollection<Update{entityName}Dto> update{entityName}Dtos", $"var entities = mapper.Map<ICollection<{entityName}>>(update{entityName}Dtos);\nawait repository.UpdateRangeAsync(entities);\nawait repository.SaveChangesAsync();\nreturn responseHandler.Success(\"Entities updated successfully\");"),
                    GenerateDeleteMethod("DeleteAsync", entityName),
                    GenerateDeleteMethod("DeleteRangeAsync", entityName, true),
                    GenerateAsyncMethod("GetByIdAsync", $"Response<Read{entityName}Dto>", "Guid id", $"var entity = await repository.GetByIdAsync(id);\nif (entity == null)\n{{\n    return responseHandler.NotFound<Read{entityName}Dto>(\"Entity not found\");\n}}\nvar dto = mapper.Map<Read{entityName}Dto>(entity);\nreturn responseHandler.Success(dto);"),
                    GenerateAsyncTaskMethod("SaveChangesAsync", "await repository.SaveChangesAsync();\n")
                );

            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(namespaceName))
                .AddMembers(classDeclaration);

            var compilationUnit = SyntaxFactory.CompilationUnit()
                .AddMembers(namespaceDeclaration)
                .NormalizeWhitespace();

            return compilationUnit.ToFullString();
        }

        private MethodDeclarationSyntax GenerateMethod(string methodName, string returnType, string body)
        {
            return SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(returnType), methodName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.SingletonList<StatementSyntax>(
                        SyntaxFactory.ParseStatement($"return {body};\n"))));
        }

        private MethodDeclarationSyntax GenerateAsyncMethod(string methodName, string returnType, string parameters, string body)
        {
            var parameterList = parameters.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(param => param.Trim().Split(' '))
                                          .Select(parts => SyntaxFactory.Parameter(SyntaxFactory.Identifier(parts[1]))
                                                                          .WithType(SyntaxFactory.ParseTypeName(parts[0])))
                                          .ToArray();

            var statements = body.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(line => SyntaxFactory.ParseStatement(line.Trim() + "\n"))
                                 .ToArray();

            return SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName($"Task<{returnType}>"), methodName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
                .AddParameterListParameters(parameterList)
                .WithBody(SyntaxFactory.Block(statements));
        }

        private MethodDeclarationSyntax GenerateAsyncTaskMethod(string methodName, string body)
        {
            var statements = body.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(line => SyntaxFactory.ParseStatement(line.Trim() + "\n"))
                                 .ToArray();

            return SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("Task"), methodName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
                .WithBody(SyntaxFactory.Block(statements));
        }

        private MethodDeclarationSyntax GenerateDeleteMethod(string methodName, string entityName, bool isRange = false)
        {
            var parameterType = isRange ? $"ICollection<Delete{entityName}Dto>" : $"Delete{entityName}Dto";
            var parameterName = isRange ? "deleteEntities" : "deleteEntity";
            var mapType = isRange ? $"ICollection<{entityName}>" : entityName;
            var mapMethod = isRange ? "DeleteRangeAsync" : "DeleteAsync";

            return SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("Task<Response<string>>"), methodName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                        .WithType(SyntaxFactory.ParseTypeName(parameterType)))
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ParseStatement("using (var trans = repository.BeginTransaction())\n{"),
                    SyntaxFactory.Block(
                        SyntaxFactory.TryStatement()
                            .WithBlock(SyntaxFactory.Block(
                                SyntaxFactory.ParseStatement($"var entities = mapper.Map<{mapType}>({parameterName});\n"),
                                SyntaxFactory.ParseStatement($"await repository.{mapMethod}(entities);\n"),
                                SyntaxFactory.ParseStatement("trans.Commit();\n"),
                                SyntaxFactory.ParseStatement("return responseHandler.Deleted<string>();\n")))
                            .WithCatches(SyntaxFactory.SingletonList(
                                SyntaxFactory.CatchClause()
                                    .WithDeclaration(SyntaxFactory.CatchDeclaration(SyntaxFactory.ParseTypeName("Exception"))
                                        .WithIdentifier(SyntaxFactory.Identifier("ex")))
                                    .WithBlock(SyntaxFactory.Block(
                                        SyntaxFactory.ParseStatement("trans.Rollback();\n"),
                                        SyntaxFactory.ParseStatement("return responseHandler.BadRequest<string>(ex.Message);\n")))))),
                    SyntaxFactory.ParseStatement("}\n")));
        }

        #endregion

        #region Mapping Dtos and Entities
        public async Task<Response<string>> AddMapping(ServiceDto dto)
        {
            var entities = dataCreatorService.GetEntitiesFromModels(dto);
            var mapperPath = Path.Combine(projectService.GetServicePath(dto), "Mapper", "MappingRegister.cs");

            var mapperDirectory = Path.GetDirectoryName(mapperPath);
            if (!Directory.Exists(mapperDirectory))
            {
                Directory.CreateDirectory(mapperDirectory);
            }

            if (!File.Exists(mapperPath))
            {
                using (var fs = File.Create(mapperPath))
                {
                    fs.Close();
                }
            }

            var classDeclaration = SyntaxFactory.ClassDeclaration("MappingRegister")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            var registerMethod = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                "Register")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                             SyntaxFactory.Token(SyntaxKind.StaticKeyword))
                .WithBody(SyntaxFactory.Block());

            foreach (var entity in entities.Data)
            {
                var modelDto = new ModelDto
                {
                    ProjectName = dto.ProjectName,
                    ServiceName = dto.ServiceName,
                    ModelName = entity
                };

                var entityProperties = await dataCreatorService.GetPropertiesFromEntity(modelDto);
                var entityName = entity;
                var createDtoName = $"Create{entityName}Dto";
                var readDtoName = $"Read{entityName}Dto";
                var updateDtoName = $"Update{entityName}Dto";
                var deleteDtoName = $"Delete{entityName}Dto";

                var createMapExpression = SyntaxFactory.ParseStatement(
                    $"TypeAdapterConfig<{createDtoName}, {entityName}>.NewConfig();");
                var readMapExpression = SyntaxFactory.ParseStatement(
                    $"TypeAdapterConfig<{entityName}, {readDtoName}>.NewConfig();");
                var updateMapExpression = SyntaxFactory.ParseStatement(
                    $"TypeAdapterConfig<{updateDtoName}, {entityName}>.NewConfig();");
                var deleteMapExpression = SyntaxFactory.ParseStatement(
                    $"TypeAdapterConfig<{deleteDtoName}, {entityName}>.NewConfig();");

                registerMethod = registerMethod.WithBody(registerMethod.Body.AddStatements(
                    createMapExpression,
                    readMapExpression,
                    updateMapExpression,
                    deleteMapExpression));
            }
            classDeclaration = classDeclaration.AddMembers(registerMethod);
            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName($"{dto.ProjectName}.{dto.ServiceName}.Mapper"))
                                        .AddMembers(classDeclaration);

            var compilationUnit = SyntaxFactory.CompilationUnit().AddMembers(namespaceDeclaration);

            await File.WriteAllTextAsync(mapperPath, compilationUnit.NormalizeWhitespace().ToFullString());
            return responseHandler.Success<string>("Mapping Added Successfully");

        }

        #endregion

    }
}
