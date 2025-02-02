using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectMaker.Base;
using ProjectMaker.Dtos.DBHandler;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Featueres.DBHandler.Contracts;
using ProjectMaker.Featueres.ProjectCreator.Contracts;
using System.Text.Json;

namespace ProjectMaker.Featueres.DBHandler.Services
{
    public class PropertyHandler(IProjectService projectService, ResponseHandler responseHandler) : IPropertyHandler
    {
        public async Task<Response<string>> AddValidationToProperty(AttributeDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };
            var classFile = Path.Combine(projectService.GetServicePath(serviceDto), "Dtos", $"{dto.ModelName}", $"Create{dto.ModelName}Dto.cs");
            var classCode = await File.ReadAllTextAsync(classFile);

            var tree = CSharpSyntaxTree.ParseText(classCode);
            var root = await tree.GetRootAsync();
            var RecordNode = root.DescendantNodes().OfType<RecordDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == $"Create{dto.ModelName}Dto");
            var property = RecordNode.DescendantNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == dto.PropertyName);

            var attributeArguments = new List<AttributeArgumentSyntax>();

            if (dto.Value != null)
            {
                var jsonElement = JsonSerializer.Deserialize<JsonElement>(dto.Value.ToString());
                object extractedValue = jsonElement.ValueKind switch
                {
                    JsonValueKind.Number when jsonElement.TryGetInt32(out int intValue) => intValue,
                    JsonValueKind.String => jsonElement.GetString(),
                    _ => throw new ArgumentException($"Unsupported additional value type: {jsonElement.ValueKind}")
                };

                var literalExpression = extractedValue switch
                {
                    int intValue => SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(intValue)),
                    string strValue => SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(strValue)),
                    _ => throw new ArgumentException($"Unsupported extracted value type: {extractedValue.GetType()}")
                };

                attributeArguments.Add(SyntaxFactory.AttributeArgument(literalExpression));
            }

            if (!string.IsNullOrEmpty(dto.ErrorMessage))
            {
                attributeArguments.Add(SyntaxFactory.AttributeArgument(
                    SyntaxFactory.NameEquals("ErrorMessage"),
                    null,
                    SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(dto.ErrorMessage))));
            }
            var attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName(dto.AnnotationType))
           .WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SeparatedList(attributeArguments)));
            var newProperty = property!.AddAttributeLists(SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute)));
            var newRoot = root.ReplaceNode(property, newProperty);
            await File.WriteAllTextAsync(classFile, newRoot.NormalizeWhitespace().ToFullString());
            return responseHandler.Success("Attribute Added Successfully");
        }
        public async Task<Response<AttributeDetails>> GetAttributeDetailsFromProperty(AttributeDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };
            var classFile = Path.Combine(projectService.GetServicePath(serviceDto), "Dtos", $"{dto.ModelName}", $"Create{dto.ModelName}Dto.cs");
            var classCode = await File.ReadAllTextAsync(classFile);

            var tree = CSharpSyntaxTree.ParseText(classCode);
            var root = await tree.GetRootAsync();
            var classNode = root.DescendantNodes().OfType<RecordDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == $"Create{dto.ModelName}Dto");
            var property = classNode?.DescendantNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == dto.PropertyName);
            var attribute = property.AttributeLists
                           .SelectMany(al => al.Attributes)
                           .FirstOrDefault(attr => attr.Name.ToString().Equals(dto.AnnotationType, StringComparison.OrdinalIgnoreCase));

            var attributeDetails = new AttributeDetails();
            var arguments = attribute.ArgumentList?.Arguments;

            if (arguments != null)
            {
                foreach (var argument in arguments)
                {
                    var argumentName = argument.NameEquals?.Name.Identifier.Text;
                    var argumentValue = argument.Expression.ToString().Trim('"'); // Remove quotes for string literals

                    if (string.IsNullOrEmpty(argumentName))
                    {
                        // Handle unnamed arguments (e.g., the main value)
                        attributeDetails.Value = argumentValue;
                    }
                    else if (argumentName.Equals("ErrorMessage", StringComparison.OrdinalIgnoreCase))
                    {
                        attributeDetails.ErrorMessage = argumentValue;
                    }
                }
            }

            return responseHandler.Success(attributeDetails);
        }
        public async Task<Response<string>> RemoveAttributeFromProperty(AttributeDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };
            var classFile = Path.Combine(projectService.GetServicePath(serviceDto), "Dtos", $"{dto.ModelName}", $"Create{dto.ModelName}Dto.cs");
            var classCode = await File.ReadAllTextAsync(classFile);

            var tree = CSharpSyntaxTree.ParseText(classCode);
            var root = await tree.GetRootAsync();
            var classNode = root.DescendantNodes().OfType<RecordDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == $"Create{dto.ModelName}Dto");
            var property = classNode?.DescendantNodes().OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == dto.PropertyName);


            var attributeList = property.AttributeLists
                .FirstOrDefault(al => al.Attributes.Any(attr => attr.Name.ToString().Equals(dto.AnnotationType, StringComparison.OrdinalIgnoreCase)));

            var attribute = attributeList.Attributes
                .FirstOrDefault(attr => attr.Name.ToString().Equals(dto.AnnotationType, StringComparison.OrdinalIgnoreCase));

            var updatedAttributeList = attributeList.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia);

            SyntaxList<AttributeListSyntax> updatedAttributeLists;
            if (updatedAttributeList.Attributes.Count == 0)
            {
                updatedAttributeLists = property.AttributeLists.Remove(attributeList);
            }
            else
            {
                updatedAttributeLists = property.AttributeLists.Replace(attributeList, updatedAttributeList);
            }

            var updatedProperty = property.WithAttributeLists(updatedAttributeLists);
            var newRoot = root.ReplaceNode(property, updatedProperty);

            await File.WriteAllTextAsync(classFile, newRoot.NormalizeWhitespace().ToFullString());

            return responseHandler.Success("Attribute removed successfully.");
        }
    }
}
