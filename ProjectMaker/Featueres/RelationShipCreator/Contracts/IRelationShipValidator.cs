using ProjectMaker.Dtos.RelationShipCreator;

namespace ProjectMaker.Featueres.RelationShipCreator.Contracts
{
    public interface IRelationShipValidator
    {
        public Task ValidateOneToOneModels(OneToOneRelationshipDto dto);
        public Task ValidateModelsForOneToMany(OneToManyRelationshipDto dto);
        public Task ValidateModelsForManyToMany(ManyToManyRelationshipDto dto);
    }
}
