using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.ControllerCreator.Contracts;
using ProjectMaker.Featueres.DataCreator.Contracts;
using ProjectMaker.Featueres.ProjectCreator.Contracts;

namespace ProjectMaker.Featueres.ControllerCreator.Services
{
    public class ControllerCreatorService(IProjectService projectService, IDataCreator dataCreator, ResponseHandler responseHandler) : IControllerCreatorService
    {
        public async Task<Response<string>> CreateControllers(ServiceDto dto)
        {
            var controllerPath = Path.Combine(projectService.GetServicePath(dto), "Controllers");
            var entities = dataCreator.GetEntitiesFromModels(dto).Data;
            foreach (var entity in entities)
            {
                var iEntityServicePath = Path.Combine(projectService.GetServicePath(dto), "Services", "Contracts", $"I{entity}Service.cs");
                if (!File.Exists(iEntityServicePath))
                {
                    continue;
                }
                var code = await File.ReadAllTextAsync(iEntityServicePath);
                var tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot() as CompilationUnitSyntax;
                var interfaceDeclaration = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().FirstOrDefault();
                if (interfaceDeclaration == null)
                {
                    continue;
                }

                var methods = interfaceDeclaration.Members.OfType<MethodDeclarationSyntax>();
                var controllerCode = GenerateControllerCode(dto, entity, methods);
                var controllerFilePath = Path.Combine(controllerPath, $"{entity}Controller.cs");
                await File.WriteAllTextAsync(controllerFilePath, controllerCode);

            }
            return responseHandler.Created("Controllers Created Successfully");
        }
        private string GenerateControllerCode(ServiceDto dto, string entity, IEnumerable<MethodDeclarationSyntax> methods)
        {
            var controllerNamespace = $"{dto.ProjectName}.{dto.ServiceName}.Controllers";
            var entityService = $"I{entity}Service";
            var entityServiceField = $"_{entity.ToLower()}Service";

            // Create the namespace
            var namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(controllerNamespace));

            // Create the class
            var classDeclaration = SyntaxFactory.ClassDeclaration($"{entity}Controller")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddBaseListTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseTypeName("AppBaseController")))
                .AddAttributeLists(
                    SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(SyntaxFactory.ParseName("Route"))
                            .AddArgumentListArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("api/[controller]"))))
                    )),
                    SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Attribute(SyntaxFactory.ParseName("ApiController"))
                    ))
                );

            // Create the service field
            var fieldDeclaration = SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName(entityService))
                    .AddVariables(SyntaxFactory.VariableDeclarator(entityServiceField))
            ).AddModifiers(SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword));

            // Create the constructor
            var constructorDeclaration = SyntaxFactory.ConstructorDeclaration($"{entity}Controller")
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddParameterListParameters(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(entityService.ToLower()))
                        .WithType(SyntaxFactory.ParseTypeName(entityService))
                )
                .WithBody(SyntaxFactory.Block(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            SyntaxFactory.IdentifierName(entityServiceField),
                            SyntaxFactory.IdentifierName(entityService.ToLower())
                        )
                    )
                ));

            // Filter out unwanted methods
            var filteredMethods = methods.Where(method =>
            {
                var methodName = method.Identifier.Text;
                return methodName != "SaveChangesAsync" && methodName != "GetTableAsTracking";
            });

            // Create methods
            var methodDeclarations = filteredMethods.Select(method =>
            {
                var methodName = method.Identifier.Text;
                var httpMethod = GetHttpMethod(methodName);
                var parameterList = method.ParameterList.Parameters;
                var hasParameters = parameterList.Any();
                var parameterType = hasParameters ? parameterList.First().Type.ToString() : null;
                var parameterName = hasParameters ? parameterList.First().Identifier.Text : null;

                var methodDeclaration = SyntaxFactory.MethodDeclaration(SyntaxFactory.ParseTypeName("Task<IActionResult>"), methodName)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
                    .AddAttributeLists(
                        SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(SyntaxFactory.ParseName(httpMethod))
                                .AddArgumentListArguments(SyntaxFactory.AttributeArgument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(methodName))))
                        ))
                    );

                if (hasParameters)
                {
                    methodDeclaration = methodDeclaration.AddParameterListParameters(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameterName))
                            .WithType(SyntaxFactory.ParseTypeName(parameterType))
                            .AddAttributeLists(
                                SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Attribute(SyntaxFactory.ParseName("FromBody"))
                                ))
                            )
                    );
                }

                return methodDeclaration.WithBody(SyntaxFactory.Block(
                    SyntaxFactory.TryStatement()
                        .WithBlock(SyntaxFactory.Block(
                            SyntaxFactory.LocalDeclarationStatement(
                                SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName("var"))
                                    .AddVariables(SyntaxFactory.VariableDeclarator("response")
                                        .WithInitializer(SyntaxFactory.EqualsValueClause(
                                            SyntaxFactory.AwaitExpression(
                                                SyntaxFactory.InvocationExpression(
                                                    SyntaxFactory.MemberAccessExpression(
                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                        SyntaxFactory.IdentifierName(entityServiceField),
                                                        SyntaxFactory.IdentifierName(methodName)
                                                    )
                                                ).WithArgumentList(hasParameters ? SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameterName)))) : SyntaxFactory.ArgumentList())
                                            )
                                        ))
                                    )
                            ),
                            SyntaxFactory.ReturnStatement(
                                SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("NewResult"))
                                    .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("response")))))
                            )
                        ))
                        .WithCatches(SyntaxFactory.SingletonList<CatchClauseSyntax>(
                            SyntaxFactory.CatchClause()
                                .WithDeclaration(SyntaxFactory.CatchDeclaration(SyntaxFactory.ParseTypeName("Exception"), SyntaxFactory.Identifier("ex")))
                                .WithBlock(SyntaxFactory.Block(
                                    SyntaxFactory.ReturnStatement(
                                        SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("NewResult"))
                                            .WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(
                                                SyntaxFactory.Argument(
                                                    SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("Response<ErrorResponse>"))
                                                        .WithInitializer(SyntaxFactory.InitializerExpression(SyntaxKind.ObjectInitializerExpression)
                                                            .AddExpressions(
                                                                                                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("Message"), SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("ex"), SyntaxFactory.IdentifierName("Message"))),
                                SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("Errors"),
                                    SyntaxFactory.InitializerExpression(SyntaxKind.ArrayInitializerExpression, SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.InvocationExpression(
                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.IdentifierName("ex"), SyntaxFactory.IdentifierName("GetType"))).WithArgumentList(SyntaxFactory.ArgumentList()),
                                    SyntaxFactory.IdentifierName("Name")))))
                                , SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, SyntaxFactory.IdentifierName("Succeeded"), SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression))
                                                            )
                                                        )
                                                )
                                            )))
                                    )
                                ))
                        ))
                ));
            });

            // Add members to the class
            classDeclaration = classDeclaration.AddMembers(fieldDeclaration, constructorDeclaration);
            classDeclaration = classDeclaration.AddMembers(methodDeclarations.ToArray());

            // Add the class to the namespace
            namespaceDeclaration = namespaceDeclaration.AddMembers(classDeclaration);

            // Format the syntax tree
            var formattedNode = namespaceDeclaration.NormalizeWhitespace();

            // Convert the syntax tree to a string
            return formattedNode.ToFullString();
        }
        private string GetHttpMethod(string methodName)
        {
            if (methodName.StartsWith("Get", StringComparison.OrdinalIgnoreCase))
            {
                return "HttpGet";
            }
            else if (methodName.StartsWith("Add", StringComparison.OrdinalIgnoreCase) || methodName.StartsWith("Create", StringComparison.OrdinalIgnoreCase))
            {
                return "HttpPost";
            }
            else if (methodName.StartsWith("Update", StringComparison.OrdinalIgnoreCase))
            {
                return "HttpPut";
            }
            else if (methodName.StartsWith("Delete", StringComparison.OrdinalIgnoreCase))
            {
                return "HttpDelete";
            }
            return "HttpPost";
        }
    }
}
