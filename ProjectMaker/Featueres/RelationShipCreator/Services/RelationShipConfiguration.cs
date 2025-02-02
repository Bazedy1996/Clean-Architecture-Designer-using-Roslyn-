using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Dtos.RelationShipCreator;
using ProjectMaker.Featueres.DataCreator.Contracts;
using ProjectMaker.Featueres.RelationShipCreator.Contracts;

namespace ProjectMaker.Featueres.RelationShipCreator.Services
{
    public class RelationShipConfiguration(IRelationShipForiegnKey relationShipForiegnKeyService, IConfigurationDB configurationDBService) : IRelationShipConfiguration
    {
        #region One To One
        public async Task ConfigureRelationship(OneToOneRelationshipDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = HelperMethods.SanitizeName(dto.ProjectName), ServiceName = HelperMethods.SanitizeName(dto.ServiceName) };
            var dbContextPath = configurationDBService.GetDbContextPath(new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName });

            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(dbContextPath));
            var root = await syntaxTree.GetRootAsync();

            var dbContextClass = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First(c => c.Identifier.Text == "ApplicationDbContext");

            var onModelCreatingMethod = dbContextClass.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "OnModelCreating");

            var relationshipConfig = await CreateOneToOneRelationshipConfiguration(dto);
            var newMethodBody = onModelCreatingMethod!.Body!.AddStatements(relationshipConfig);
            var newMethod = onModelCreatingMethod.WithBody(newMethodBody);
            var newClass = dbContextClass.ReplaceNode(onModelCreatingMethod, newMethod);

            var newRoot = root.ReplaceNode(dbContextClass, newClass);
            await File.WriteAllTextAsync(dbContextPath, newRoot.NormalizeWhitespace().ToFullString());
        }
        public async Task RemoveRelationshipConfiguration(OneToOneRelationshipDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };
            var dbContextPath = configurationDBService.GetDbContextPath(serviceDto);
            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(dbContextPath));
            var root = await syntaxTree.GetRootAsync();

            var dbContextClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "ApplicationDbContext");

            var onModelCreatingMethod = dbContextClass.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == "OnModelCreating");

            if (onModelCreatingMethod != null)
            {
                // Find the relationship configuration
                var relationshipConfig = onModelCreatingMethod
                    .DescendantNodes()
                    .OfType<ExpressionStatementSyntax>()
                    .FirstOrDefault(e =>
                        (e.ToString().Contains($"Entity<{dto.TargetEntity}>()") && e.ToString().Contains($"{dto.SourceEntity}")) ||
                        (e.ToString().Contains($"Entity<{dto.SourceEntity}>()") && e.ToString().Contains($"{dto.TargetEntity}"))
                    );

                if (relationshipConfig != null)
                {
                    // Remove the configuration
                    var newMethod = onModelCreatingMethod.RemoveNode(
                        relationshipConfig,
                        SyntaxRemoveOptions.KeepNoTrivia);

                    var newClass = dbContextClass.ReplaceNode(onModelCreatingMethod, newMethod!);
                    var newRoot = root.ReplaceNode(dbContextClass, newClass);

                    await File.WriteAllTextAsync(dbContextPath, newRoot.NormalizeWhitespace().ToFullString());
                }
            }
        }
        private async Task<StatementSyntax> CreateOneToOneRelationshipConfiguration(OneToOneRelationshipDto dto)
        {
            var (keyPropertyName, keyPropertyType) = await relationShipForiegnKeyService.GetPrimaryKeyInfoSourceEntity(dto);
            // Start with Model2 (dependent entity)
            var entityType = SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("Entity"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.IdentifierName(dto.TargetEntity)))); // TargetEntity is Model2

            var modelBuilderAccess = SyntaxFactory.IdentifierName("modelBuilder");
            var entityMethodCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    modelBuilderAccess,
                    entityType));

            // HasOne with lambda for navigation property
            var hasOne = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    entityMethodCall,
                    SyntaxFactory.IdentifierName("HasOne")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier("m")),
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("m"),
                                        SyntaxFactory.IdentifierName(dto.SourceEntity)))))));

            // WithOne with no argument since principal has no navigation
            var withOne = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    hasOne,
                    SyntaxFactory.IdentifierName("WithOne")));

            // HasForeignKey with lambda for foreign key property
            var hasForeignKey = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    withOne,
                    SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("HasForeignKey"))
                        .WithTypeArgumentList(
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                                    SyntaxFactory.IdentifierName(dto.TargetEntity))))))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier("m")),
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("m"),
                                        SyntaxFactory.IdentifierName($"{dto.SourceEntity}{keyPropertyName}")))))));

            ExpressionSyntax configuration = hasForeignKey;

            // Add IsRequired if mandatory
            if (dto.IsMandatory)
            {
                configuration = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        configuration,
                        SyntaxFactory.IdentifierName("IsRequired")));
            }

            // Add delete behavior
            configuration = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    configuration,
                    SyntaxFactory.IdentifierName("OnDelete")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("DeleteBehavior"),
                                    SyntaxFactory.IdentifierName(dto.DeleteRule.ToString()))))));

            return SyntaxFactory.ExpressionStatement(configuration);
        }
        #endregion

        #region One To Many
        public async Task ConfigureOneToManyRelationship(OneToManyRelationshipDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };
            var dbContextPath = configurationDBService.GetDbContextPath(serviceDto);

            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(dbContextPath));
            var root = await syntaxTree.GetRootAsync();

            var dbContextClass = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First(c => c.Identifier.Text == "ApplicationDbContext");

            var onModelCreatingMethod = dbContextClass.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == "OnModelCreating");
            var relationshipConfig = await CreateOneToManyConfiguration(dto);
            var newMethodBody = onModelCreatingMethod.Body.AddStatements(relationshipConfig);
            var newMethod = onModelCreatingMethod.WithBody(newMethodBody);
            var newClass = dbContextClass.ReplaceNode(onModelCreatingMethod, newMethod);

            var newRoot = root.ReplaceNode(dbContextClass, newClass);
            await File.WriteAllTextAsync(dbContextPath, newRoot.NormalizeWhitespace().ToFullString());
        }
        private async Task<StatementSyntax> CreateOneToManyConfiguration(OneToManyRelationshipDto dto)
        {
            var (keyPropertyName, keyPropertyType) = await relationShipForiegnKeyService.GetPrimaryKeyInfoForOneToMany(dto);
            var entityType = SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("Entity"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.IdentifierName(dto.ManyEntity))));

            var modelBuilderAccess = SyntaxFactory.IdentifierName("modelBuilder");
            var entityMethodCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    modelBuilderAccess,
                    entityType));

            // HasOne configuration
            var hasOne = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    entityMethodCall,
                    SyntaxFactory.IdentifierName("HasOne")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier("m")),
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("m"),
                                        SyntaxFactory.IdentifierName(dto.OneEntity)))))));

            // WithMany configuration
            var withMany = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    hasOne,
                    SyntaxFactory.IdentifierName("WithMany")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier("m")),
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("m"),
                                        SyntaxFactory.IdentifierName($"{dto.ManyEntity}s")))))));

            // HasForeignKey configuration
            var hasForeignKey = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    withMany,
                    SyntaxFactory.IdentifierName("HasForeignKey")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier("m")),
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("m"),
                                        SyntaxFactory.IdentifierName($"{dto.OneEntity}{keyPropertyName}")))))));

            ExpressionSyntax configuration = hasForeignKey;

            // Add IsRequired if mandatory
            if (dto.IsMandatory)
            {
                configuration = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        configuration,
                        SyntaxFactory.IdentifierName("IsRequired")));
            }

            configuration = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    configuration,
                    SyntaxFactory.IdentifierName("OnDelete")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName("DeleteBehavior"),
                                    SyntaxFactory.IdentifierName(dto.DeleteRule.ToString()))))));

            return SyntaxFactory.ExpressionStatement(configuration);
        }
        public async Task RemoveOneToManyConfiguration(OneToManyRelationshipDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };
            var dbContextPath = configurationDBService.GetDbContextPath(serviceDto);

            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(dbContextPath));
            var root = await syntaxTree.GetRootAsync();

            var dbContextClass = root.DescendantNodes().OfType<ClassDeclarationSyntax>().First(c => c.Identifier.Text == "ApplicationDbContext");
            var onModelCreatingMethod = dbContextClass.Members.OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == "OnModelCreating");

            if (onModelCreatingMethod != null)
            {
                var relationshipConfig = onModelCreatingMethod
                    .DescendantNodes()
                    .OfType<ExpressionStatementSyntax>()
                    .FirstOrDefault(e =>
                        e.ToString().Contains($"Entity<{dto.ManyEntity}>()") &&
                        e.ToString().Contains($"{dto.OneEntity}"));

                if (relationshipConfig != null)
                {
                    var newMethod = onModelCreatingMethod.RemoveNode(
                        relationshipConfig,
                        SyntaxRemoveOptions.KeepNoTrivia);

                    var newClass = dbContextClass.ReplaceNode(onModelCreatingMethod, newMethod);
                    var newRoot = root.ReplaceNode(dbContextClass, newClass);

                    await File.WriteAllTextAsync(dbContextPath, newRoot.NormalizeWhitespace().ToFullString());
                }
            }
        }
        #endregion

        #region Many To Many
        public async Task ConfigureManyToManyRelationship(ManyToManyRelationshipDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };
            var dbContextPath = configurationDBService.GetDbContextPath(serviceDto);

            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(dbContextPath));
            var root = await syntaxTree.GetRootAsync();

            var dbContextClass = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>()
                .First(c => c.Identifier.Text == "ApplicationDbContext");

            var onModelCreatingMethod = dbContextClass.Members
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "OnModelCreating");

            var relationshipConfigs = await CreateManyToManyConfigurationAsync(dto);
            var newMethodBody = onModelCreatingMethod.Body.AddStatements(relationshipConfigs);
            var newMethod = onModelCreatingMethod.WithBody(newMethodBody);
            var newClass = dbContextClass.ReplaceNode(onModelCreatingMethod, newMethod);

            var newRoot = root.ReplaceNode(dbContextClass, newClass);
            await File.WriteAllTextAsync(dbContextPath, newRoot.NormalizeWhitespace().ToFullString());
        }
        private async Task<StatementSyntax[]> CreateManyToManyConfigurationAsync(ManyToManyRelationshipDto dto)
        {

            var reversedDto = new ManyToManyRelationshipDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName, FirstEntity = dto.SecondEntity, SecondEntity = dto.SecondEntity, DeleteRule = dto.DeleteRule };
            var (firstKeyPropertyName, firstKeyPropertyType) = await relationShipForiegnKeyService.GetPrimaryKeyInfoForManyToMany(dto);
            var (secondKeyPropertyName, secondKeyPropertyType) = await relationShipForiegnKeyService.GetPrimaryKeyInfoForManyToMany(reversedDto);
            var joinEntityName = $"{dto.FirstEntity}{dto.SecondEntity}";

            var entityType = SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("Entity"))
                .WithTypeArgumentList(
                    SyntaxFactory.TypeArgumentList(
                        SyntaxFactory.SingletonSeparatedList<TypeSyntax>(
                            SyntaxFactory.IdentifierName(joinEntityName))));

            var modelBuilderAccess = SyntaxFactory.IdentifierName("modelBuilder");
            var entityMethodCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    modelBuilderAccess,
                    entityType));

            // 1. Configure primary key
            // 1. Configure primary key
            var hasKey = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    entityMethodCall,
                    SyntaxFactory.IdentifierName("HasKey")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(
                                        SyntaxFactory.Identifier("x")),
                                    SyntaxFactory.AnonymousObjectCreationExpression(
                                        SyntaxFactory.SeparatedList<AnonymousObjectMemberDeclaratorSyntax>(
                                            new[] {
                                    SyntaxFactory.AnonymousObjectMemberDeclarator(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("x"),
                                            SyntaxFactory.IdentifierName($"{dto.FirstEntity}{firstKeyPropertyName}"))),
                                    SyntaxFactory.AnonymousObjectMemberDeclarator(
                                        SyntaxFactory.MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactory.IdentifierName("x"),
                                            SyntaxFactory.IdentifierName($"{dto.SecondEntity}{secondKeyPropertyName}")))
                                            })))))));


            // 2. Configure First Entity relationship
            var firstEntityRelationship = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    entityMethodCall,
                    SyntaxFactory.IdentifierName("HasOne")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("sc")),
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("sc"),
                                        SyntaxFactory.IdentifierName(dto.FirstEntity)))))));

            var withManyFirst = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    firstEntityRelationship,
                    SyntaxFactory.IdentifierName("WithMany")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("e")),
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("e"),
                                        SyntaxFactory.IdentifierName($"{joinEntityName}s")))))));

            var withForeignKeyFirst = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    withManyFirst,
                    SyntaxFactory.IdentifierName("HasForeignKey")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("sc")),
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("sc"),
                                        SyntaxFactory.IdentifierName($"{dto.FirstEntity}{firstKeyPropertyName}")))))));

            // 3. Configure Second Entity relationship
            var secondEntityRelationship = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    entityMethodCall,
                    SyntaxFactory.IdentifierName("HasOne")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("sc")),
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("sc"),
                                        SyntaxFactory.IdentifierName(dto.SecondEntity)))))));

            var withManySecond = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    secondEntityRelationship,
                    SyntaxFactory.IdentifierName("WithMany")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("e")),
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("e"),
                                        SyntaxFactory.IdentifierName($"{joinEntityName}s")))))));

            var withForeignKeySecond = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    withManySecond,
                    SyntaxFactory.IdentifierName("HasForeignKey")))
                .WithArgumentList(
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.Argument(
                                SyntaxFactory.SimpleLambdaExpression(
                                    SyntaxFactory.Parameter(SyntaxFactory.Identifier("sc")),
                                    SyntaxFactory.MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName("sc"),
                                        SyntaxFactory.IdentifierName($"{dto.SecondEntity}{secondKeyPropertyName}")))))));

            return new[]
            {
        SyntaxFactory.ExpressionStatement(hasKey),
        SyntaxFactory.ExpressionStatement(withForeignKeyFirst),
        SyntaxFactory.ExpressionStatement(withForeignKeySecond)
    };
        }
        public async Task RemoveManyToManyConfiguration(ManyToManyRelationshipDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };
            var dbContextPath = configurationDBService.GetDbContextPath(serviceDto);
            var lines = await File.ReadAllLinesAsync(dbContextPath);
            var joinEntityName = $"{dto.FirstEntity}{dto.SecondEntity}";
            var linesToRemove = new List<int>();

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.Contains($"Entity<{joinEntityName}>()"))
                {
                    linesToRemove.Add(i);
                }
            }

            if (!linesToRemove.Any())
            {
                throw new ArgumentException($"No many-to-many relationship configurations found between '{dto.FirstEntity}' and '{dto.SecondEntity}'.");
            }
            var newLines = lines.Where((line, index) => !linesToRemove.Contains(index)).ToList();
            await File.WriteAllLinesAsync(dbContextPath, newLines);
            var modifiedContent = await File.ReadAllTextAsync(dbContextPath);
            if (modifiedContent.Contains($"Entity<{joinEntityName}>()"))
            {
                throw new Exception("Failed to remove all relationship configurations.");
            }
        }
        #endregion
    }

}
