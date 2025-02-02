using ProjectMaker.Base;
using ProjectMaker.Dtos.RelationShipCreator;
using ProjectMaker.Featueres.ProjectCreator.Contracts;
using ProjectMaker.Featueres.RelationShipCreator.Contracts;

namespace ProjectMaker.Featueres.RelationShipCreator.Services
{
    public class RelationShipService(ResponseHandler responseHandler, IProjectService projectService, IRelationShipValidator relationShipValidator, IRelationShipForiegnKey relationShipForiegnKey, IRelationShipConfiguration relationShipConfiguration) : IRelationShipService
    {
        #region One To One
        public async Task<Response<string>> AddOneToOneRelationship(OneToOneRelationshipDto dto)
        {
            try
            {
                // 1. Sanitize names
                var sanitizedDto = new OneToOneRelationshipDto
                {
                    ProjectName = HelperMethods.SanitizeName(dto.ProjectName),
                    ServiceName = HelperMethods.SanitizeName(dto.ServiceName),
                    SourceEntity = HelperMethods.SanitizeName(dto.SourceEntity),
                    TargetEntity = HelperMethods.SanitizeName(dto.TargetEntity),
                    IsMandatory = dto.IsMandatory,
                    DeleteRule = dto.DeleteRule
                };

                await relationShipValidator.ValidateOneToOneModels(sanitizedDto);
                await relationShipForiegnKey.AddForeignKeyProperty(sanitizedDto);
                await relationShipConfiguration.ConfigureRelationship(sanitizedDto);

                return responseHandler.Created("Successfully added one-to-one relationship");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create one-to-one relationship: {ex.Message}", ex);
            }
        }
        public async Task<Response<string>> RemoveOneToOneRelationship(OneToOneRelationshipDto dto)
        {
            try
            {
                // 1. Sanitize names
                var sanitizedDto = new OneToOneRelationshipDto
                {
                    ProjectName = !HelperMethods.IsValidName(dto.ProjectName) ? HelperMethods.SanitizeName(dto.ProjectName) : dto.ProjectName,
                    ServiceName = !HelperMethods.IsValidName(dto.ServiceName) ? HelperMethods.SanitizeName(dto.ServiceName) : dto.ServiceName,
                    SourceEntity = !HelperMethods.IsValidName(dto.SourceEntity) ? HelperMethods.SanitizeName(dto.SourceEntity) : dto.SourceEntity,
                    TargetEntity = !HelperMethods.IsValidName(dto.TargetEntity) ? HelperMethods.SanitizeName(dto.TargetEntity) : dto.TargetEntity
                };
                var reversedsanitizedDto = new OneToOneRelationshipDto
                {
                    ProjectName = !HelperMethods.IsValidName(dto.ProjectName) ? HelperMethods.SanitizeName(dto.ProjectName) : dto.ProjectName,
                    ServiceName = !HelperMethods.IsValidName(dto.ServiceName) ? HelperMethods.SanitizeName(dto.ServiceName) : dto.ServiceName,
                    SourceEntity = !HelperMethods.IsValidName(dto.TargetEntity) ? HelperMethods.SanitizeName(dto.TargetEntity) : dto.TargetEntity,
                    TargetEntity = !HelperMethods.IsValidName(dto.SourceEntity) ? HelperMethods.SanitizeName(dto.SourceEntity) : dto.SourceEntity
                };

                // 2. Remove properties from dependent entity (Model2)
                await relationShipForiegnKey.RemoveRelationshipForiegnKey(sanitizedDto);
                await relationShipForiegnKey.RemoveRelationshipForiegnKey(reversedsanitizedDto);
                // 3. Remove configuration from DbContext
                await relationShipConfiguration.RemoveRelationshipConfiguration(sanitizedDto);
                await relationShipConfiguration.RemoveRelationshipConfiguration(reversedsanitizedDto);

                return responseHandler.Deleted("Successfully removed one-to-one relationship between");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to remove one-to-one relationship: {ex.Message}", ex);
            }
        }
        #endregion
        #region One To Many
        public async Task<Response<string>> AddOneToManyRelationship(OneToManyRelationshipDto dto)
        {
            try
            {
                // 1. Sanitize names
                var sanitizedDto = new OneToManyRelationshipDto
                {
                    ProjectName = HelperMethods.SanitizeName(dto.ProjectName),
                    ServiceName = HelperMethods.SanitizeName(dto.ServiceName),
                    OneEntity = HelperMethods.SanitizeName(dto.OneEntity),
                    ManyEntity = HelperMethods.SanitizeName(dto.ManyEntity),
                    IsMandatory = dto.IsMandatory,
                    DeleteRule = dto.DeleteRule
                };
                await relationShipValidator.ValidateModelsForOneToMany(sanitizedDto);
                await relationShipForiegnKey.AddOneToManyProperties(sanitizedDto);
                await relationShipConfiguration.ConfigureOneToManyRelationship(sanitizedDto);

                return responseHandler.Created($"Successfully added one-to-many relationship between '{sanitizedDto.OneEntity}' and '{sanitizedDto.ManyEntity}'.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create one-to-many relationship: {ex.Message}", ex);
            }
        }
        public async Task<Response<string>> RemoveOneToManyRelationship(OneToManyRelationshipDto dto)
        {
            try
            {
                // 1. Sanitize names
                var sanitizedDto = new OneToManyRelationshipDto
                {
                    ProjectName = HelperMethods.SanitizeName(dto.ProjectName),

                    ServiceName = HelperMethods.SanitizeName(dto.ServiceName),
                    OneEntity = HelperMethods.SanitizeName(dto.OneEntity),
                    ManyEntity = HelperMethods.SanitizeName(dto.ManyEntity)
                };
                await relationShipForiegnKey.RemoveOneToManyProperties(sanitizedDto);
                await relationShipConfiguration.RemoveOneToManyConfiguration(sanitizedDto);

                return responseHandler.Created($"Successfully removed one-to-many relationship between '{sanitizedDto.OneEntity}' and '{sanitizedDto.ManyEntity}'.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to remove one-to-many relationship: {ex.Message}", ex);
            }
        }
        #endregion
        #region Many To Many
        public async Task<Response<string>> AddManyToManyRelationship(ManyToManyRelationshipDto dto)
        {
            try
            {
                // 1. Sanitize names
                var sanitizedDto = new ManyToManyRelationshipDto
                {
                    ProjectName = HelperMethods.SanitizeName(dto.ProjectName),

                    ServiceName = HelperMethods.SanitizeName(dto.ServiceName),
                    FirstEntity = HelperMethods.SanitizeName(dto.FirstEntity),
                    SecondEntity = HelperMethods.SanitizeName(dto.SecondEntity),
                    DeleteRule = dto.DeleteRule
                };


                // 3. Validate models
                await relationShipValidator.ValidateModelsForManyToMany(sanitizedDto);

                // 4. Create join entity
                await relationShipForiegnKey.CreateJoinEntity(sanitizedDto);

                // 5. Add navigation properties to both entities
                await relationShipForiegnKey.AddManyToManyProperties(sanitizedDto);

                // 6. Configure relationship in DbContext
                await relationShipConfiguration.ConfigureManyToManyRelationship(sanitizedDto);

                return responseHandler.Created($"Successfully added many-to-many relationship between '{sanitizedDto.FirstEntity}' and '{sanitizedDto.SecondEntity}'.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to create many-to-many relationship: {ex.Message}", ex);
            }
        }
        public async Task<Response<string>> RemoveManyToManyRelationship(ManyToManyRelationshipDto dto)
        {
            try
            {
                // 1. Sanitize names
                var sanitizedDto = new ManyToManyRelationshipDto
                {
                    ProjectName = HelperMethods.SanitizeName(dto.ProjectName),
                    ServiceName = HelperMethods.SanitizeName(dto.ServiceName),
                    FirstEntity = HelperMethods.SanitizeName(dto.FirstEntity),
                    SecondEntity = HelperMethods.SanitizeName(dto.SecondEntity)
                };
                // 2. Remove join entity class
                relationShipForiegnKey.RemoveJoinEntity(sanitizedDto);

                // 3. Remove collection properties from both entities
                await relationShipForiegnKey.RemoveManyToManyProperties(sanitizedDto);

                // 4. Remove configuration from DbContext
                await relationShipConfiguration.RemoveManyToManyConfiguration(sanitizedDto);

                return responseHandler.Deleted($"Successfully removed many-to-many relationship ");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to remove many-to-many relationship: {ex.Message}", ex);
            }
        }
        #endregion
    }
}
