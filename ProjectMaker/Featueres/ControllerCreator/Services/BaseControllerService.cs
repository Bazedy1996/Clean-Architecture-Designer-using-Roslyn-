using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.ControllerCreator.Contracts;
using ProjectMaker.Featueres.ProjectCreator.Contracts;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ProjectMaker.Featueres.ControllerCreator.Services
{
    public class BaseControllerService(ResponseHandler responseHandler, IProjectService projectService) : IBaseControllerService
    {
        public async Task<Response<string>> CreateAppBaseController(ServiceDto dto)
        {
            var controllerPath = Path.Combine(projectService.GetServicePath(dto), "Controllers");
            if (!Directory.Exists(controllerPath))
            {
                Directory.CreateDirectory(controllerPath);
            }
            var appbaseFile = Path.Combine(controllerPath, "AppBaseController.cs");
            var classDeclaration = ClassDeclaration("AppBaseController").AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SimpleBaseType(ParseTypeName("ControllerBase")));
            var routeAttribute = Attribute(ParseName("Route"), AttributeArgumentList(
                SingletonSeparatedList(AttributeArgument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal("api/[controller]"))))));
            var apiControllerAttribute = Attribute(ParseName("ApiController"));
            classDeclaration = classDeclaration.AddAttributeLists(AttributeList(SingletonSeparatedList(routeAttribute)), AttributeList(SingletonSeparatedList(apiControllerAttribute)));
            var methodDeclaration = MethodDeclaration(ParseTypeName("ObjectResult"), Identifier("NewResult"))
            .AddModifiers(Token(SyntaxKind.PublicKeyword)).AddTypeParameterListParameters(TypeParameter("T"))
            .AddParameterListParameters(Parameter(Identifier("response")).WithType(ParseTypeName("Response<T>")));
            var switchStatement = SwitchStatement(
                    ParseExpression("response.StatusCode"),
                    List(new[]
                    {
                        // OK case
                        SwitchSection()
                            .AddLabels(CaseSwitchLabel(ParseExpression("HttpStatusCode.OK")))
                            .AddStatements(ReturnStatement(
                                ObjectCreationExpression(ParseTypeName("OkObjectResult"))
                                    .AddArgumentListArguments(Argument(IdentifierName("response"))))),

                        // Created case
                        SwitchSection()
                            .AddLabels(CaseSwitchLabel(ParseExpression("HttpStatusCode.Created")))
                            .AddStatements(ReturnStatement(
                                ObjectCreationExpression(ParseTypeName("CreatedResult"))
                                    .AddArgumentListArguments(
                                        Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(string.Empty))),
                                        Argument(IdentifierName("response"))))),

                        // Unauthorized case
                        SwitchSection()
                            .AddLabels(CaseSwitchLabel(ParseExpression("HttpStatusCode.Unauthorized")))
                            .AddStatements(ReturnStatement(
                                ObjectCreationExpression(ParseTypeName("UnauthorizedObjectResult"))
                                    .AddArgumentListArguments(Argument(IdentifierName("response"))))),

                        // BadRequest case
                        SwitchSection()
                            .AddLabels(CaseSwitchLabel(ParseExpression("HttpStatusCode.BadRequest")))
                            .AddStatements(ReturnStatement(
                                ObjectCreationExpression(ParseTypeName("BadRequestObjectResult"))
                                    .AddArgumentListArguments(Argument(IdentifierName("response"))))),

                        // NotFound case
                        SwitchSection()
                            .AddLabels(CaseSwitchLabel(ParseExpression("HttpStatusCode.NotFound")))
                            .AddStatements(ReturnStatement(
                                ObjectCreationExpression(ParseTypeName("NotFoundObjectResult"))
                                    .AddArgumentListArguments(Argument(IdentifierName("response"))))),

                        // Accepted case
                        SwitchSection()
                            .AddLabels(CaseSwitchLabel(ParseExpression("HttpStatusCode.Accepted")))
                            .AddStatements(ReturnStatement(
                                ObjectCreationExpression(ParseTypeName("AcceptedResult"))
                                    .AddArgumentListArguments(
                                        Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(string.Empty))),
                                        Argument(IdentifierName("response"))))),

                        // UnprocessableEntity case
                        SwitchSection()
                            .AddLabels(CaseSwitchLabel(ParseExpression("HttpStatusCode.UnprocessableEntity")))
                            .AddStatements(ReturnStatement(
                                ObjectCreationExpression(ParseTypeName("UnprocessableEntityObjectResult"))
                                    .AddArgumentListArguments(Argument(IdentifierName("response"))))),

                        // Default case
                        SwitchSection()
                            .AddLabels(DefaultSwitchLabel())
                            .AddStatements(ReturnStatement(
                                ObjectCreationExpression(ParseTypeName("BadRequestObjectResult"))
                                    .AddArgumentListArguments(Argument(IdentifierName("response")))))
                    }));

            methodDeclaration = methodDeclaration.AddBodyStatements(switchStatement);
            classDeclaration = classDeclaration.AddMembers(methodDeclaration);
            var namespaceDeclaration = NamespaceDeclaration(ParseName($"{dto.ProjectName}.{dto.ServiceName}.Controllers")).AddMembers(classDeclaration);
            var compilationUnit = CompilationUnit().AddMembers(namespaceDeclaration).NormalizeWhitespace();
            var code = compilationUnit.ToFullString();
            await File.WriteAllTextAsync(appbaseFile, code);
            return responseHandler.Success("AppBase Controller Created Successfully");

        }
    }
}
