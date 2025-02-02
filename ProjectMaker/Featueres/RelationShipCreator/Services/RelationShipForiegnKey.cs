using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectMaker.Base;
using ProjectMaker.Dtos.DataCreator;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Dtos.RelationShipCreator;
using ProjectMaker.Featueres.DataCreator.Contracts;
using ProjectMaker.Featueres.ProjectCreator.Contracts;
using ProjectMaker.Featueres.RelationShipCreator.Contracts;

namespace ProjectMaker.Featueres.RelationShipCreator.Services
{
    public class RelationShipForiegnKey(IProjectService projectService, IConfigurationDB configurationDBService, IDataCreator dataCreatorService) : IRelationShipForiegnKey
    {
        #region One To One
        public async Task AddForeignKeyProperty(OneToOneRelationshipDto dto)
        {
            // First get the primary key info from source entity
            var (keyPropertyName, keyPropertyType) = await GetPrimaryKeyInfoSourceEntity(dto);

            var targetModelPath = Path.Combine(projectService.GetServicePath(new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName }), "Models", "Entities", $"{dto.TargetEntity}.cs");
            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(targetModelPath));
            var root = await syntaxTree.GetRootAsync();
            var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.Text == dto.TargetEntity);
            if (classDeclaration == null)
            {
                throw new Exception($"Class {dto.TargetEntity} not found in the file.");
            }

            var foreignKeyProperty = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName(dto.IsMandatory ? keyPropertyType : $"{keyPropertyType}?"),
                    SyntaxFactory.Identifier($"{dto.SourceEntity}{keyPropertyName}"))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

            var navigationProperty = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName(dto.SourceEntity),
                    SyntaxFactory.Identifier(dto.SourceEntity))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

            // Add ForeignKey attribute
            var foreignKeyAttribute = SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("ForeignKey"))
                        .WithArgumentList(
                            SyntaxFactory.AttributeArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.AttributeArgument(
                                        SyntaxFactory.LiteralExpression(
                                            SyntaxKind.StringLiteralExpression,
                                            SyntaxFactory.Literal($"{dto.SourceEntity}{keyPropertyName}"))))))));

            navigationProperty = navigationProperty.AddAttributeLists(foreignKeyAttribute);

            // Add the new members to the specified class (e.g., Model3)
            var updatedClassDeclaration = classDeclaration.AddMembers(foreignKeyProperty, navigationProperty);

            // Replace the class in the original syntax tree
            var updatedRoot = root.ReplaceNode(classDeclaration, updatedClassDeclaration);

            // Write the updated syntax tree back to the file
            await File.WriteAllTextAsync(targetModelPath, updatedRoot.NormalizeWhitespace().ToFullString());
        }
        public async Task<(string KeyPropertyName, string KeyPropertyType)> GetPrimaryKeyInfoSourceEntity(OneToOneRelationshipDto dto)
        {

            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };
            var dbContextPath = configurationDBService.GetDbContextPath(serviceDto);
            var customKeyPropertyTargetEntity = await GetCustomKeyProperty(dbContextPath, dto.TargetEntity);
            var customKeyPropertySourceEntity = await GetCustomKeyProperty(dbContextPath, dto.SourceEntity);
            // Get source model properties
            var modelProperties = await dataCreatorService.GetPropertiesFromEntity(new ModelDto
            {
                ProjectName = dto.ProjectName,
                ServiceName = dto.ServiceName,
                ModelName = dto.SourceEntity
            });
            var targetModelProperties = await dataCreatorService.GetPropertiesFromEntity(new ModelDto
            {
                ProjectName = dto.ProjectName,
                ServiceName = dto.ServiceName,
                ModelName = dto.TargetEntity

            });

            if (!string.IsNullOrEmpty(customKeyPropertyTargetEntity) || targetModelProperties.Data.FirstOrDefault(t => t.Name == "Id") != null)
            {// If custom key exists, get its type
                if (!string.IsNullOrEmpty(customKeyPropertySourceEntity))
                {
                    var keyProperty = modelProperties.Data.First(p => p.Name == customKeyPropertySourceEntity);
                    return (customKeyPropertySourceEntity, keyProperty.Type);
                }

                // If no custom key, look for Id property
                var idProperty = modelProperties.Data.FirstOrDefault(p => p.Name == "Id");
                if (idProperty != null)
                {
                    return ("Id", idProperty.Type);
                }
            }

            throw new ArgumentException($"No primary key found for entity");
        }
        public async Task RemoveRelationshipForiegnKey(OneToOneRelationshipDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = HelperMethods.SanitizeName(dto.ProjectName), ServiceName = HelperMethods.SanitizeName(dto.ServiceName) };
            var targetModelPath = Path.Combine(projectService.GetServicePath(serviceDto), "Models", "Entities", $"{HelperMethods.SanitizeName(dto.TargetEntity)}.cs");
            var (keyPropertyName, keyPropertyType) = await GetPrimaryKeyInfoSourceEntity(dto);
            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(targetModelPath));
            var root = await syntaxTree.GetRootAsync();

            var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == dto.TargetEntity);
            var newClass = classDeclaration;
            var navigationProperty = newClass.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == dto.SourceEntity);

            if (navigationProperty != null)
            {
                newClass = newClass.RemoveNode(navigationProperty, SyntaxRemoveOptions.KeepNoTrivia);
            }

            var foreignKeyProperty = newClass.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == $"{dto.SourceEntity}{keyPropertyName}");

            if (foreignKeyProperty != null)
            {
                newClass = newClass.RemoveNode(foreignKeyProperty, SyntaxRemoveOptions.KeepNoTrivia);
            }
            var updatedRoot = root.ReplaceNode(classDeclaration, newClass);
            await File.WriteAllTextAsync(targetModelPath, updatedRoot.ToFullString());
        }
        private async Task<string> GetCustomKeyProperty(string dbContextPath, string entityName)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(dbContextPath));
            var root = await syntaxTree.GetRootAsync();

            var onModelCreatingMethod = root.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == "OnModelCreating");
            var keyConfig = onModelCreatingMethod!.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault(invocation =>
                            {
                                var methodChain = invocation.ToString();
                                return methodChain.Contains($"Entity<{entityName}>().HasKey");
                            });

            if (keyConfig == null) return null;

            var lambda = keyConfig.DescendantNodes().OfType<SimpleLambdaExpressionSyntax>().FirstOrDefault();
            if (lambda == null) return null;
            var memberAccess = lambda.DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .FirstOrDefault();

            return memberAccess?.Name.Identifier.Text;
        }
        #endregion
        #region One To Many
        public async Task AddOneToManyProperties(OneToManyRelationshipDto dto)
        {
            // Get primary key info from the One side
            var primaryKeyInfo = await GetPrimaryKeyInfoForOneToMany(dto);
            string keyPropertyName = primaryKeyInfo.KeyPropertyName;
            string keyPropertyType = primaryKeyInfo.KeyPropertyType;
            var manyModelPath = Path.Combine(projectService.GetServicePath(new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName }), "Models", "Entities", $"{dto.ManyEntity}.cs");
            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(manyModelPath));
            var root = await syntaxTree.GetRootAsync();
            var classDeclaration = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == dto.ManyEntity);

            if (classDeclaration == null)
                throw new Exception($"Class {dto.ManyEntity} not found in Models.cs");

            // Define foreign key property
            var foreignKeyProperty = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName(dto.IsMandatory ? keyPropertyType : $"{keyPropertyType}?"),
                    SyntaxFactory.Identifier($"{dto.OneEntity}{keyPropertyName}"))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

            // Define navigation property
            var navigationProperty = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.ParseTypeName(dto.OneEntity),
                    SyntaxFactory.Identifier(dto.OneEntity))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                .AddAttributeLists(
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(SyntaxFactory.IdentifierName("ForeignKey"))
                                .WithArgumentList(
                                    SyntaxFactory.AttributeArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.AttributeArgument(
                                                SyntaxFactory.LiteralExpression(
                                                    SyntaxKind.StringLiteralExpression,
                                                    SyntaxFactory.Literal($"{dto.OneEntity}{keyPropertyName}")))))))));

            // Add the properties to the class
            var updatedClassDeclaration = classDeclaration.AddMembers(foreignKeyProperty, navigationProperty);

            // Replace the original class
            var updatedRoot = root.ReplaceNode(classDeclaration, updatedClassDeclaration);

            // Write changes back to the file
            await File.WriteAllTextAsync(manyModelPath, updatedRoot.NormalizeWhitespace().ToFullString());

            // Add collection property to the One side
            await AddCollectionToOneEntity(dto);
        }
        public async Task<(string KeyPropertyName, string KeyPropertyType)> GetPrimaryKeyInfoForOneToMany(OneToManyRelationshipDto dto)
        {
            // First check for custom key in DbContext
            var dbContextPath = configurationDBService.GetDbContextPath(new ServiceDto { ProjectName = HelperMethods.SanitizeName(dto.ProjectName), ServiceName = HelperMethods.SanitizeName(dto.ServiceName) });

            var customKeyProperty = await GetCustomKeyProperty(dbContextPath, dto.OneEntity);

            // Get source model properties
            var modelProperties = await dataCreatorService.GetPropertiesFromEntity(new ModelDto
            {
                ProjectName = dto.ProjectName,
                ServiceName = dto.ServiceName,
                ModelName = dto.OneEntity
            });

            // If custom key exists, get its type
            if (!string.IsNullOrEmpty(customKeyProperty))
            {
                var keyProperty = modelProperties.Data.First(p => p.Name == customKeyProperty);
                return (customKeyProperty, keyProperty.Type);
            }

            // If no custom key, look for Id property
            var idProperty = modelProperties.Data.FirstOrDefault(p => p.Name == "Id");
            if (idProperty != null)
            {
                return ("Id", idProperty.Type);
            }

            throw new ArgumentException($"No primary key found for entity {dto.OneEntity}");
        }
        private async Task AddCollectionToOneEntity(OneToManyRelationshipDto dto)
        {
            var oneModelPath = Path.Combine(projectService.GetServicePath(new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName }), "Models", "Entities", $"{dto.OneEntity}.cs");

            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(oneModelPath));
            var root = await syntaxTree.GetRootAsync();

            var classDeclaration = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First(c => c.Identifier.Text == dto.OneEntity);

            var collectionProperty = SyntaxFactory.PropertyDeclaration(
                    SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("ICollection"))
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    SyntaxFactory.IdentifierName(dto.ManyEntity)))),
                    SyntaxFactory.Identifier($"{dto.ManyEntity}s"))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                        .NormalizeWhitespace();

            var updatedClassDeclaration = classDeclaration.AddMembers(collectionProperty);
            var updatedRoot = root.ReplaceNode(classDeclaration, updatedClassDeclaration);
            await File.WriteAllTextAsync(oneModelPath, updatedRoot.NormalizeWhitespace().ToFullString());
        }
        public async Task RemoveOneToManyProperties(OneToManyRelationshipDto dto)
        {
            // Get the primary key info for the Many entity
            var primaryKeyInfo = await GetPrimaryKeyInfoForOneToMany(dto);
            string keyPropertyName = primaryKeyInfo.KeyPropertyName;

            var manyEntityPath = Path.Combine(projectService.GetServicePath(new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName }), "Models", "Entities", $"{dto.ManyEntity}.cs");
            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(manyEntityPath));
            var root = await syntaxTree.GetRootAsync();
            var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == dto.ManyEntity);
            var navigationProperty = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == dto.OneEntity);

            if (navigationProperty != null)
            {
                classDeclaration = classDeclaration.RemoveNode(navigationProperty, SyntaxRemoveOptions.KeepNoTrivia);
            }
            var foreignKeyProperty = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == $"{dto.OneEntity}{keyPropertyName}");

            if (foreignKeyProperty != null)
            {
                classDeclaration = classDeclaration.RemoveNode(foreignKeyProperty, SyntaxRemoveOptions.KeepNoTrivia);
            }

            var updatedRoot = root.ReplaceNode(root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == dto.ManyEntity), classDeclaration);
            await File.WriteAllTextAsync(manyEntityPath, updatedRoot.NormalizeWhitespace().ToFullString());
            var oneEntityPath = Path.Combine(projectService.GetServicePath(new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName }), "Models", "Entities", $"{dto.OneEntity}.cs");

            syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(oneEntityPath));
            root = await syntaxTree.GetRootAsync();

            classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == dto.OneEntity);
            var collectionProperty = classDeclaration.Members.OfType<PropertyDeclarationSyntax>().FirstOrDefault(p => p.Identifier.Text == $"{dto.ManyEntity}s");

            if (collectionProperty != null)
            {
                classDeclaration = classDeclaration.RemoveNode(collectionProperty, SyntaxRemoveOptions.KeepNoTrivia);
            }
            updatedRoot = root.ReplaceNode(root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == dto.OneEntity), classDeclaration);
            await File.WriteAllTextAsync(oneEntityPath, updatedRoot.NormalizeWhitespace().ToFullString());
        }

        #endregion
        #region Many To Many
        public async Task CreateJoinEntity(ManyToManyRelationshipDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };
            var joinEntityName = $"{dto.FirstEntity}{dto.SecondEntity}";
            await dataCreatorService.CreateModels(new ModelDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName, ModelName = joinEntityName, ModelType = ModelType.Entity });
            var modelPath = Path.Combine(projectService.GetServicePath(serviceDto), "Models", "Entities", $"{joinEntityName}.cs");

            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(modelPath));
            var root = await syntaxTree.GetRootAsync();
            var reversedDto = new ManyToManyRelationshipDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName, FirstEntity = dto.SecondEntity, SecondEntity = dto.FirstEntity, DeleteRule = dto.DeleteRule };
            var (firstKeyPropertyName, firstKeyPropertyType) = await GetPrimaryKeyInfoForManyToMany(dto);
            var (secondKeyPropertyName, secondKeyPropertyType) = await GetPrimaryKeyInfoForManyToMany(reversedDto);
            var properties = new List<MemberDeclarationSyntax>();

            // Add foreign key properties with annotations
            var firstEntityForeignKey = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName(firstKeyPropertyType),
                SyntaxFactory.Identifier($"{dto.FirstEntity}{firstKeyPropertyName}"))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

            var secondEntityForeignKey = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.ParseTypeName(secondKeyPropertyType),
                SyntaxFactory.Identifier($"{dto.SecondEntity}{secondKeyPropertyName}"))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

            // Add navigation properties with ForeignKey attributes
            var firstEntityNavigation = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.IdentifierName(dto.FirstEntity),
                SyntaxFactory.Identifier(dto.FirstEntity))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                .AddAttributeLists(
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(
                                SyntaxFactory.IdentifierName("ForeignKey"))
                            .WithArgumentList(
                                SyntaxFactory.AttributeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.AttributeArgument(
                                            SyntaxFactory.LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                SyntaxFactory.Literal($"{dto.FirstEntity}{firstKeyPropertyName}")))))))));

            var secondEntityNavigation = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.IdentifierName(dto.SecondEntity),
                SyntaxFactory.Identifier(dto.SecondEntity))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))
                .AddAttributeLists(
                    SyntaxFactory.AttributeList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Attribute(
                                SyntaxFactory.IdentifierName("ForeignKey"))
                            .WithArgumentList(
                                SyntaxFactory.AttributeArgumentList(
                                    SyntaxFactory.SingletonSeparatedList(
                                        SyntaxFactory.AttributeArgument(
                                            SyntaxFactory.LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                SyntaxFactory.Literal($"{dto.SecondEntity}{secondKeyPropertyName}")))))))));

            properties.AddRange(new[] { firstEntityForeignKey, secondEntityForeignKey, firstEntityNavigation, secondEntityNavigation });
            await dataCreatorService.CreateModels(new ModelDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName, ModelName = joinEntityName, ModelType = ModelType.Entity });
            syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(modelPath));
            root = await syntaxTree.GetRootAsync();
            var classDeclaration = root.DescendantNodes()
                                    .OfType<ClassDeclarationSyntax>().FirstOrDefault(cd => cd.Identifier.Text == joinEntityName);
            var idProperty = new DeletePropertyDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName, ModelName = joinEntityName, PropertyNames = ["Id"] };

            var updatedClass = classDeclaration.AddMembers(properties.ToArray());

            var updatedRoot = root.ReplaceNode(classDeclaration, updatedClass);

            await File.WriteAllTextAsync(modelPath, updatedRoot.NormalizeWhitespace().ToFullString());
            await dataCreatorService.DeletePropertiesFromEntity(idProperty);
        }
        public async Task AddManyToManyProperties(ManyToManyRelationshipDto dto)
        {
            var joinEntityName = $"{dto.FirstEntity}{dto.SecondEntity}";

            // Add join entity collection to first entity
            await AddJoinEntityCollectionProperty(
                dto.ProjectName,
                dto.ServiceName,
                dto.FirstEntity,
                joinEntityName);

            // Add join entity collection to second entity
            await AddJoinEntityCollectionProperty(
                dto.ProjectName,
                dto.ServiceName,
                dto.SecondEntity,
                joinEntityName);
        }
        public async Task<(string KeyPropertyName, string KeyPropertyType)> GetPrimaryKeyInfoForManyToMany(ManyToManyRelationshipDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };
            var dbContextPath = configurationDBService.GetDbContextPath(serviceDto);

            var customKeyProperty = await GetCustomKeyPropertyForManyToMany(dbContextPath, dto.FirstEntity);

            // Get source model properties
            var modelProperties = await dataCreatorService.GetPropertiesFromEntity(new ModelDto
            {
                ProjectName = dto.ProjectName,
                ServiceName = dto.ServiceName,
                ModelName = dto.FirstEntity
            });

            // If custom key exists, get its type
            if (!string.IsNullOrEmpty(customKeyProperty))
            {
                var keyProperty = modelProperties.Data.First(p => p.Name == customKeyProperty);
                return (customKeyProperty, keyProperty.Type);
            }

            // If no custom key, look for Id property
            var idProperty = modelProperties.Data.FirstOrDefault(p => p.Name == "Id");
            if (idProperty != null)
            {
                return ("Id", idProperty.Type);
            }

            throw new ArgumentException($"No primary key found for entity {dto.FirstEntity}");
        }
        public void RemoveJoinEntity(ManyToManyRelationshipDto dto)
        {

            var firstEntity = HelperMethods.IsValidName(dto.FirstEntity) ? dto.FirstEntity : HelperMethods.SanitizeName(dto.FirstEntity);
            var secondEntity = HelperMethods.IsValidName(dto.SecondEntity) ? dto.SecondEntity : HelperMethods.SanitizeName(dto.SecondEntity);
            var joinEntityName = firstEntity + secondEntity;
            var modeltobeDeletedDto = new ModelDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName, ModelName = joinEntityName, ModelType = ModelType.Entity };
            dataCreatorService.DeleteEntity(modeltobeDeletedDto);
        }
        public async Task RemoveManyToManyProperties(ManyToManyRelationshipDto dto)
        {
            var joinEntityName = $"{dto.FirstEntity}{dto.SecondEntity}";
            await RemoveCollectionProperty(dto.ProjectName, dto.ServiceName, dto.FirstEntity, joinEntityName);
            await RemoveCollectionProperty(dto.ProjectName, dto.ServiceName, dto.SecondEntity, joinEntityName);
        }
        private async Task RemoveCollectionProperty(string projectName, string serviceName, string entityName, string joinEntityName)
        {
            var modelPath = Path.Combine(projectService.GetServicePath(new ServiceDto { ProjectName = projectName, ServiceName = serviceName }), "Models", "Entities", $"{entityName}.cs");

            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(modelPath));
            var root = await syntaxTree.GetRootAsync();

            var classDeclaration = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First(c => c.Identifier.Text == entityName);

            var collectionProperty = classDeclaration.Members
                .OfType<PropertyDeclarationSyntax>()
                .FirstOrDefault(p => p.Identifier.Text == $"{joinEntityName}s");

            if (collectionProperty != null)
            {
                var newClass = classDeclaration.RemoveNode(collectionProperty, SyntaxRemoveOptions.KeepNoTrivia);
                var updatedRoot = root.ReplaceNode(classDeclaration, newClass);

                await File.WriteAllTextAsync(modelPath, updatedRoot.NormalizeWhitespace().ToFullString());
            }
        }
        private async Task<string> GetCustomKeyPropertyForManyToMany(string dbContextPath, string entityName)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(dbContextPath));
            var root = await syntaxTree.GetRootAsync();

            var onModelCreatingMethod = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "OnModelCreating");

            if (onModelCreatingMethod == null) return null;

            // Look for HasKey configuration
            var keyConfig = onModelCreatingMethod
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(invocation =>
                {
                    var methodChain = invocation.ToString();
                    return methodChain.Contains($"Entity<{entityName}>().HasKey");
                });

            if (keyConfig == null) return null;

            // Extract property name from the HasKey lambda
            var lambda = keyConfig.DescendantNodes()
                .OfType<SimpleLambdaExpressionSyntax>()
                .FirstOrDefault();

            if (lambda == null) return null;

            var memberAccess = lambda.DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .FirstOrDefault();

            return memberAccess?.Name.Identifier.Text;
        }
        private async Task AddJoinEntityCollectionProperty(string projectName, string serviceName, string entityName, string joinEntityName)
        {
            var serviceDto = new ServiceDto { ProjectName = projectName, ServiceName = serviceName };
            var modelPath = Path.Combine(projectService.GetServicePath(serviceDto), "Models", "Entities", $"{entityName}.cs");

            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(modelPath));
            var root = await syntaxTree.GetRootAsync();

            var classDeclaration = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First(c => c.Identifier.Text == entityName);

            // Create collection property for join entity only
            var joinEntityCollectionProperty = SyntaxFactory.PropertyDeclaration(
                SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier("ICollection"))
                    .WithTypeArgumentList(
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                SyntaxFactory.IdentifierName(joinEntityName)))),
                SyntaxFactory.Identifier($"{joinEntityName}s"))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword), SyntaxFactory.Token(SyntaxKind.VirtualKeyword))
                .AddAccessorListAccessors(
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));

            var newClass = classDeclaration.AddMembers(joinEntityCollectionProperty);

            var updatedRoot = root.ReplaceNode(classDeclaration, newClass);

            await File.WriteAllTextAsync(modelPath, updatedRoot.NormalizeWhitespace().ToFullString());
        }
        #endregion
    }
}
