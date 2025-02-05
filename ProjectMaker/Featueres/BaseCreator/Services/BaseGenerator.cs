using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.BaseCreator.Contracts;
using ProjectMaker.Featueres.ProjectCreator.Contracts;

namespace ProjectMaker.Featueres.BaseCreator.Services
{
    public class BaseGenerator(IProjectService projectService, ResponseHandler responseHandler) : IBaseGenerator
    {
        public async Task<Response<string>> BaseCreator(ServiceDto dto)
        {
            await ResponseClassCreator(dto);
            await ResponseHandlerClassCreator(dto);
            await GenerateGenericRepositoryInterface(dto);
            await GenerateGenericRepositoryClass(dto);
            await CreateErrorResponseClass(dto);
            return responseHandler.Created("Base Created Successfully");
        }
        #region ResponseCreator
        private async Task ResponseClassCreator(ServiceDto dto)
        {
            var serviceDto = new ServiceDto
            {
                ProjectName = HelperMethods.SanitizeName(dto.ProjectName),
                ServiceName = HelperMethods.SanitizeName(dto.ServiceName)
            };

            var servicePath = projectService.GetServicePath(serviceDto);
            var basePath = Path.Combine(servicePath, "Base");
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            var typeParameter = SyntaxFactory.TypeParameter("T");
            var classDeclaration = SyntaxFactory.ClassDeclaration("Response")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddTypeParameterListParameters(typeParameter)
                .AddMembers(
                    SyntaxFactory.ConstructorDeclaration("Response")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .WithBody(SyntaxFactory.Block()),
                    SyntaxFactory.ConstructorDeclaration("Response")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddParameterListParameters(
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("data"))
                                .WithType(SyntaxFactory.IdentifierName("T")),
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("message"))
                                .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))
                                .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)))
                        )
                        .WithBody(SyntaxFactory.Block(
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName("Succeeded"),
                                    SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression))),
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName("Message"),
                                    SyntaxFactory.IdentifierName("message"))),
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName("Data"),
                                    SyntaxFactory.IdentifierName("data")))
                        )),
                    SyntaxFactory.ConstructorDeclaration("Response")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddParameterListParameters(
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("message"))
                                .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))
                        )
                        .WithBody(SyntaxFactory.Block(
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName("Succeeded"),
                                    SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))),
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName("Message"),
                                    SyntaxFactory.IdentifierName("message")))
                        )),
                    SyntaxFactory.ConstructorDeclaration("Response")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddParameterListParameters(
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("message"))
                                .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))),
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("succeeded"))
                                .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)))
                        )
                        .WithBody(SyntaxFactory.Block(
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName("Succeeded"),
                                    SyntaxFactory.IdentifierName("succeeded"))),
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(
                                    SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName("Message"),
                                    SyntaxFactory.IdentifierName("message")))
                        )),

                    CreateProperty("System.Net.HttpStatusCode", "StatusCode"),
                    CreateProperty("object", "Meta"),
                    CreateProperty("bool", "Succeeded"),
                    CreateProperty("string", "Message"),
                    CreateProperty("List<string>", "Errors"),
                    CreateProperty("T", "Data")
                );

            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName($"{serviceDto.ProjectName}.{serviceDto.ServiceName}.Base")).AddMembers(classDeclaration);

            var compilationUnit = SyntaxFactory.CompilationUnit().AddMembers(namespaceDeclaration);

            string classCode = compilationUnit.NormalizeWhitespace().ToFullString();
            string filePath = Path.Combine(basePath, "Response.cs");
            await File.WriteAllTextAsync(filePath, classCode);
        }
        private static PropertyDeclarationSyntax CreateProperty(string typeName, string propertyName)
        {
            return SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName(typeName), propertyName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                );
        }
        #endregion

        #region ResponseHandlerCreator
        private async Task ResponseHandlerClassCreator(ServiceDto dto)
        {
            var serviceDto = new ServiceDto
            {
                ProjectName = HelperMethods.SanitizeName(dto.ProjectName),
                ServiceName = HelperMethods.SanitizeName(dto.ServiceName)
            };
            var servicePath = projectService.GetServicePath(serviceDto);
            var basePath = Path.Combine(servicePath, "Base");
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            var classDeclaration = SyntaxFactory.ClassDeclaration("ResponseHandler")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddMembers(
                    SyntaxFactory.ConstructorDeclaration("ResponseHandler")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .WithBody(SyntaxFactory.Block()),

                    CreateResponseMethod("Deleted", "System.Net.HttpStatusCode.OK", true, "Deleted Successfully"),
                    CreateResponseMethod("Success", "System.Net.HttpStatusCode.OK", true, "Added Successfully", true),
                    CreateResponseMethod("Unauthorized", "System.Net.HttpStatusCode.Unauthorized", true, "UnAuthorized"),
                    CreateResponseMethod("BadRequest", "System.Net.HttpStatusCode.BadRequest", false, "Bad Request", false, true),
                    CreateResponseMethod("NotFound", "System.Net.HttpStatusCode.NotFound", false, "Not Found", false, true),
                    CreateResponseMethod("Created", "System.Net.HttpStatusCode.Created", true, "Created", true)
                );

            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName($"{serviceDto.ProjectName}.{serviceDto.ServiceName}.Base"))
                .AddMembers(classDeclaration);

            var compilationUnit = SyntaxFactory.CompilationUnit().AddMembers(namespaceDeclaration);

            string classCode = compilationUnit.NormalizeWhitespace().ToFullString();
            string filePath = Path.Combine(basePath, "ResponseHandler.cs");
            await File.WriteAllTextAsync(filePath, classCode);
        }
        private static MethodDeclarationSyntax CreateResponseMethod(string methodName, string statusCode, bool succeeded, string message, bool hasEntity = false, bool hasMessageParameter = false)
        {
            var method = SyntaxFactory.MethodDeclaration(
                    SyntaxFactory.GenericName("Response")
                        .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName("T")),
                    methodName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddTypeParameterListParameters(SyntaxFactory.TypeParameter("T"));

            var parameters = new SeparatedSyntaxList<ParameterSyntax>();

            if (hasEntity)
            {
                parameters = parameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("entity"))
                    .WithType(SyntaxFactory.IdentifierName("T")));
                parameters = parameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("Meta"))
                    .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ObjectKeyword)))
                    .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))));
            }

            if (hasMessageParameter)
            {
                parameters = parameters.Add(SyntaxFactory.Parameter(SyntaxFactory.Identifier("message"))
                    .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))
                    .WithDefault(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression))));
            }

            method = method.AddParameterListParameters(parameters.ToArray());

            var objectCreationExpression = SyntaxFactory.ObjectCreationExpression(
                SyntaxFactory.GenericName("Response")
                    .AddTypeArgumentListArguments(SyntaxFactory.IdentifierName("T")))
                .WithInitializer(
                    SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression)
                        .AddExpressions(
                            SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.IdentifierName("StatusCode"),
                                SyntaxFactory.ParseExpression(statusCode)),
                            SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.IdentifierName("Succeeded"),
                                SyntaxFactory.LiteralExpression(succeeded ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression)),
                            SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                SyntaxFactory.IdentifierName("Message"),
                                hasMessageParameter
                                    ? SyntaxFactory.ConditionalExpression(
                                        SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                                            SyntaxFactory.IdentifierName("message"),
                                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                                        SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(message)),
                                        SyntaxFactory.IdentifierName("message"))
                                    : SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(message)))
                        ));

            if (hasEntity)
            {
                objectCreationExpression = objectCreationExpression.WithInitializer(
                    objectCreationExpression.Initializer!.AddExpressions(
                        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName("Data"),
                            SyntaxFactory.IdentifierName("entity")),
                        SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName("Meta"),
                            SyntaxFactory.IdentifierName("Meta"))
                    )
                );
            }

            var returnStatement = SyntaxFactory.ReturnStatement(objectCreationExpression);

            return method.WithBody(SyntaxFactory.Block(returnStatement));
        }
        #endregion

        #region GenericRepositoryCreator
        private async Task GenerateGenericRepositoryInterface(ServiceDto dto)
        {
            var serviceDto = new ServiceDto
            {
                ProjectName = HelperMethods.SanitizeName(dto.ProjectName),
                ServiceName = HelperMethods.SanitizeName(dto.ServiceName)
            };

            var servicePath = projectService.GetServicePath(serviceDto);
            var basePath = Path.Combine(servicePath, "Base");

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            var interfaceDeclaration = SyntaxFactory.InterfaceDeclaration("IGenericRepository")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddTypeParameterListParameters(SyntaxFactory.TypeParameter("T"))
                .AddConstraintClauses(
                    SyntaxFactory.TypeParameterConstraintClause("T")
                        .AddConstraints(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint)))
                .AddMembers(
                    CreateMethodDeclaration("IQueryable<T>", "GetTableNoTracking"),
                    CreateMethodDeclaration("IQueryable<T>", "GetTableAsTracking"),
                    CreateMethodDeclaration("Task<T>", "AddAsync", "T", "entity"),
                    CreateMethodDeclaration("Task", "AddRangeAsync", "ICollection<T>", "entities"),
                    CreateMethodDeclaration("Task", "UpdateAsync", "T", "entity"),
                    CreateMethodDeclaration("Task", "UpdateRangeAsync", "ICollection<T>", "entities"),
                    CreateMethodDeclaration("Task", "DeleteAsync", "T", "entity"),
                    CreateMethodDeclaration("Task", "DeleteRangeAsync", "ICollection<T>", "entities"),
                    CreateMethodDeclaration("Task<T>", "GetByIdAsync", "Guid", "id"),
                    CreateMethodDeclaration("Task", "SaveChangesAsync"),
                    CreateMethodDeclaration("IDbContextTransaction", "BeginTransaction"),
                    CreateMethodDeclaration("void", "Commit"),
                    CreateMethodDeclaration("void", "RollBack")
                );

            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName($"{serviceDto.ProjectName}.{serviceDto.ServiceName}.Base"))
                .AddMembers(interfaceDeclaration);

            var compilationUnit = SyntaxFactory.CompilationUnit().AddMembers(namespaceDeclaration);

            string interfaceCode = compilationUnit.NormalizeWhitespace().ToFullString();
            string filePath = Path.Combine(basePath, "IGenericRepository.cs");
            await File.WriteAllTextAsync(filePath, interfaceCode);

        }
        private static MethodDeclarationSyntax CreateMethodDeclaration(string returnType, string methodName, string parameterType = null, string parameterName = null)
        {
            var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(returnType), methodName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.None));

            if (parameterType != null && parameterName != null)
            {
                methodDeclaration = methodDeclaration.AddParameterListParameters(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                        .WithType(SyntaxFactory.ParseTypeName(parameterType)));
            }

            return methodDeclaration.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }
        private async Task GenerateGenericRepositoryClass(ServiceDto dto)
        {
            var serviceDto = new ServiceDto
            {
                ProjectName = HelperMethods.SanitizeName(dto.ProjectName),
                ServiceName = HelperMethods.SanitizeName(dto.ServiceName)
            };

            var servicePath = projectService.GetServicePath(serviceDto);
            var basePath = Path.Combine(servicePath, "Base");
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            var classDeclaration = SyntaxFactory.ClassDeclaration("GenericRepository")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddTypeParameterListParameters(SyntaxFactory.TypeParameter("T"))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("IGenericRepository<T>")))
                .AddConstraintClauses(
                    SyntaxFactory.TypeParameterConstraintClause("T")
                        .AddConstraints(SyntaxFactory.ClassOrStructConstraint(SyntaxKind.ClassConstraint)))
                .AddMembers(
                    //field
                    SyntaxFactory.FieldDeclaration(
                        SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("ApplicationDbContext"))
                            .AddVariables(SyntaxFactory.VariableDeclarator("_dbContext")))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)),

                    // Constructor
                    SyntaxFactory.ConstructorDeclaration("GenericRepository")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddParameterListParameters(
                            SyntaxFactory.Parameter(SyntaxFactory.Identifier("dbContext"))
                                .WithType(SyntaxFactory.ParseTypeName("ApplicationDbContext")))
                        .WithBody(SyntaxFactory.Block(
                            SyntaxFactory.ExpressionStatement(
                                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    SyntaxFactory.IdentifierName("_dbContext"),
                                    SyntaxFactory.IdentifierName("dbContext"))))),

                    // Methods
                    CreateMethod("Task<T>", "GetByIdAsync", new[] { ("Guid", "id") }, true, new[]
                    {
                    "return await _dbContext.Set<T>().FindAsync(id);"
                    }),

                    CreateMethod("IQueryable<T>", "GetTableNoTracking", null, false, new[]
                    {
                    "return _dbContext.Set<T>().AsNoTracking().AsQueryable();"
                    }),

                    CreateMethod("Task", "AddRangeAsync", new[] { ("ICollection<T>", "entities") }, true, new[]
                    {
                    "await _dbContext.Set<T>().AddRangeAsync(entities);",
                    "await _dbContext.SaveChangesAsync();"
                    }),

                    CreateMethod("Task<T>", "AddAsync", new[] { ("T", "entity") }, true, new[]
                    {
                    "await _dbContext.Set<T>().AddAsync(entity);",
                    "await _dbContext.SaveChangesAsync();",
                    "return entity;"
                    }),

                    CreateMethod("Task", "UpdateAsync", new[] { ("T", "entity") }, true, new[]
                    {
                    "_dbContext.Set<T>().Update(entity);",
                    "await _dbContext.SaveChangesAsync();"
                    }),

                    CreateMethod("Task", "DeleteAsync", new[] { ("T", "entity") }, true, new[]
                    {
                    "_dbContext.Set<T>().Remove(entity);",
                    "await _dbContext.SaveChangesAsync();"
                    }),

                    CreateMethod("Task", "DeleteRangeAsync", new[] { ("ICollection<T>", "entities") }, true, new[]
                    {
                    "foreach (var entity in entities) {",
                    "    _dbContext.Entry(entity).State = EntityState.Deleted;",
                    "}",
                    "await _dbContext.SaveChangesAsync();"
                    }),

                    CreateMethod("Task", "SaveChangesAsync", null, true, new[]
                    {
                    "await _dbContext.SaveChangesAsync();"
                    }),

                    CreateMethod("IDbContextTransaction", "BeginTransaction", null, false, new[]
                    {
                    "return _dbContext.Database.BeginTransaction();"
                    }),

                    CreateMethod("void", "Commit", null, false, new[]
                    {
                    "_dbContext.Database.CommitTransaction();"
                    }),

                    CreateMethod("void", "RollBack", null, false, new[]
                    {
                    "_dbContext.Database.RollbackTransaction();"
                    }),

                    CreateMethod("IQueryable<T>", "GetTableAsTracking", null, false, new[]
                    {
                    "return _dbContext.Set<T>().AsQueryable();"
                    }),

                    CreateMethod("Task", "UpdateRangeAsync", new[] { ("ICollection<T>", "entities") }, true, new[]
                    {
                    "_dbContext.Set<T>().UpdateRange(entities);",
                    "await _dbContext.SaveChangesAsync();"
                    })
                );

            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName($"{serviceDto.ProjectName}.{serviceDto.ServiceName}.Base"))
                .AddMembers(classDeclaration);

            var compilationUnit = SyntaxFactory.CompilationUnit().AddMembers(namespaceDeclaration);

            string classCode = compilationUnit.NormalizeWhitespace().ToFullString();

            string filePath = Path.Combine(basePath, "GenericRepository.cs");

            await File.WriteAllTextAsync(filePath, classCode);

        }
        private static MethodDeclarationSyntax CreateMethod(string returnType, string methodName, (string Type, string Name)[] parameters, bool isAsync, string[] bodyLines)
        {
            var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName(returnType), methodName)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

            if (isAsync)
            {
                methodDeclaration = methodDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.VirtualKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword));
            }
            else
            {
                methodDeclaration = methodDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.VirtualKeyword));
            }

            if (parameters != null)
            {
                methodDeclaration = methodDeclaration.AddParameterListParameters(
                    parameters.Select(p => SyntaxFactory.Parameter(SyntaxFactory.Identifier(p.Name))
                        .WithType(SyntaxFactory.ParseTypeName(p.Type))).ToArray());
            }

            var statements = bodyLines.Select(line => SyntaxFactory.ParseStatement(line)).ToArray();
            return methodDeclaration.WithBody(SyntaxFactory.Block(statements));
        }
        #endregion
        #region Generate ErrorResponse
        private async Task CreateErrorResponseClass(ServiceDto dto)
        {
            var basePath = Path.Combine(projectService.GetServicePath(dto), "Base");
            Directory.CreateDirectory(basePath);
            var classCode = GenerateErrorResponseClass(dto);
            var filePath = Path.Combine(basePath, "ErrorResponse.cs");
            await File.WriteAllTextAsync(filePath, classCode);
        }

        private string GenerateErrorResponseClass(ServiceDto dto)
        {
            var classDeclaration = SyntaxFactory.ClassDeclaration("ErrorResponse")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddMembers(
                    SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("string"), "Message")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        )
                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(string.Empty))))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),

                    SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("string"), "ExceptionType")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        )
                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(string.Empty))))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),

                    SyntaxFactory.PropertyDeclaration(SyntaxFactory.ParseTypeName("string?"), "StackTrace")
                        .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                        .AddAccessorListAccessors(
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        )
                        .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(string.Empty))))
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                );

            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName($"{dto.ProjectName}.{dto.ServiceName}.Base"))
                .AddMembers(classDeclaration);

            var formattedNode = namespaceDeclaration.NormalizeWhitespace();

            return formattedNode.ToFullString();
        }
        #endregion
    }
}

