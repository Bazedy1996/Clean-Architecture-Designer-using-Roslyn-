using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ProjectMaker.Base;
using ProjectMaker.Dtos.ProjectCreator;
using ProjectMaker.Dtos.RelationShipCreator;
using ProjectMaker.Featueres.DataCreator.Contracts;
using ProjectMaker.Featueres.RelationShipCreator.Contracts;

namespace ProjectMaker.Featueres.RelationShipCreator.Services
{
    public class RelationShipValidator(IDataCreator dataCreatorService, IConfigurationDB configurationDBService) : IRelationShipValidator
    {
        #region One To One
        public async Task ValidateOneToOneModels(OneToOneRelationshipDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };

            // Check if models exist
            var existingModels = dataCreatorService.GetEntitiesFromModels(serviceDto);

            if (!existingModels.Data!.Contains(dto.SourceEntity))
                throw new ArgumentException($"Source entity '{dto.SourceEntity}' does not exist");

            if (!existingModels.Data.Contains(dto.TargetEntity))
                throw new ArgumentException($"Target entity '{dto.TargetEntity}' does not exist");

            var dbContextPath = configurationDBService.GetDbContextPath(serviceDto);
            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(dbContextPath));
            var root = await syntaxTree.GetRootAsync();

            var onModelCreatingMethod = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "OnModelCreating");



            var entityConfigs = onModelCreatingMethod!
                .DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(invocation =>
                {
                    var fullconfig = invocation.ToString();
                    return fullconfig.Contains(dto.SourceEntity) && fullconfig.Contains(dto.TargetEntity);
                }).ToList();
            foreach (var config in entityConfigs)
            {
                var configText = config.ToString();
                bool hasDirectRelationship =
                    (configText.Contains($"Entity<{dto.SourceEntity}>()") && configText.Contains(dto.TargetEntity)) ||
                    (configText.Contains($"Entity<{dto.TargetEntity}>()") && configText.Contains(dto.SourceEntity));
                if (hasDirectRelationship)
                {
                    throw new ArgumentException(
                        $"Cannot create one-to-many relationship: A relationship already exists between '{dto.SourceEntity}' and '{dto.TargetEntity}'. " +
                        "Each pair of entities can only have one relationship.");
                }
            }


        }


        #endregion
        #region One To Many
        public async Task ValidateModelsForOneToMany(OneToManyRelationshipDto dto)
        {

            var serviceDto = new ServiceDto { ProjectName = HelperMethods.SanitizeName(dto.ProjectName), ServiceName = HelperMethods.SanitizeName(dto.ServiceName) };
            var existingModels = dataCreatorService.GetEntitiesFromModels(serviceDto);

            if (!existingModels.Data!.Contains(dto.OneEntity))
                throw new ArgumentException($"One side entity '{dto.OneEntity}' does not exist");

            if (!existingModels.Data.Contains(dto.ManyEntity))
                throw new ArgumentException($"Many side entity '{dto.ManyEntity}' does not exist");

            var dbContextPath = configurationDBService.GetDbContextPath(serviceDto);
            var dbContextContent = await File.ReadAllTextAsync(dbContextPath);
            var syntaxTree = CSharpSyntaxTree.ParseText(dbContextContent);
            var root = await syntaxTree.GetRootAsync();

            var onModelCreatingMethod = root.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.Text == "OnModelCreating");
            if (onModelCreatingMethod != null)
            {
                var methodBody = onModelCreatingMethod.ToString();
                var entityConfigs = onModelCreatingMethod
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Where(invocation =>
                    {
                        var fullConfig = invocation.ToString();
                        return fullConfig.Contains(dto.OneEntity) && fullConfig.Contains(dto.ManyEntity);
                    })
                    .ToList();

                foreach (var config in entityConfigs)
                {
                    var configText = config.ToString();
                    bool hasDirectRelationship =
                        (configText.Contains($"Entity<{dto.OneEntity}>()") && configText.Contains(dto.ManyEntity)) ||
                        (configText.Contains($"Entity<{dto.ManyEntity}>()") && configText.Contains(dto.OneEntity));

                    if (hasDirectRelationship)
                    {
                        throw new ArgumentException(
                            $"Cannot create one-to-many relationship: A relationship already exists between '{dto.OneEntity}' and '{dto.ManyEntity}'. " +
                            "Each pair of entities can only have one relationship.");
                    }
                }
            }
        }
        #endregion
        #region Many To Many
        public async Task ValidateModelsForManyToMany(ManyToManyRelationshipDto dto)
        {
            var serviceDto = new ServiceDto { ProjectName = dto.ProjectName, ServiceName = dto.ServiceName };
            var existingModels = dataCreatorService.GetEntitiesFromModels(serviceDto);

            if (!existingModels.Data!.Contains(dto.FirstEntity))
                throw new ArgumentException($"Entity '{dto.FirstEntity}' does not exist");

            if (!existingModels.Data.Contains(dto.SecondEntity))
                throw new ArgumentException($"Entity '{dto.SecondEntity}' does not exist");

            var dbContextPath = configurationDBService.GetDbContextPath(serviceDto);
            var syntaxTree = CSharpSyntaxTree.ParseText(await File.ReadAllTextAsync(dbContextPath));
            var root = await syntaxTree.GetRootAsync();

            var onModelCreatingMethod = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Identifier.Text == "OnModelCreating");

            if (onModelCreatingMethod != null)
            {
                var existingRelationship = onModelCreatingMethod
                    .DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .Any(invocation =>
                    {
                        var configText = invocation.ToString();
                        bool involvesEntities = configText.Contains(dto.FirstEntity) &&
                                              configText.Contains(dto.SecondEntity);

                        if (!involvesEntities) return false;

                        // Check for one-to-one relationship
                        bool hasOneToOne = configText.Contains("HasOne") && configText.Contains("WithOne");

                        // Check for one-to-many relationship
                        bool hasOneToMany = configText.Contains("HasOne") && configText.Contains("WithMany");
                        bool hasManyToOne = configText.Contains("HasMany") && configText.Contains("WithOne");

                        // Check for many-to-many relationship
                        bool hasManyToMany = configText.Contains("HasMany") && configText.Contains("WithMany");

                        return hasOneToOne || hasOneToMany || hasManyToOne || hasManyToMany;
                    });

                if (existingRelationship)
                {
                    throw new ArgumentException(
                        $"Cannot create many-to-many relationship: A relationship " +
                        $"already exists between '{dto.FirstEntity}' and '{dto.SecondEntity}'. Each pair of entities can only " +
                        "have one type of relationship.");
                }
            }
        }
        #endregion
    }
}
