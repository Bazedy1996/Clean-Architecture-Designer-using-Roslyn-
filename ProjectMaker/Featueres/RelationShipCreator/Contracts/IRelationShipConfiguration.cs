using ProjectMaker.Dtos.RelationShipCreator;

namespace ProjectMaker.Featueres.RelationShipCreator.Contracts
{
    public interface IRelationShipConfiguration
    {
        public Task ConfigureRelationship(OneToOneRelationshipDto dto);
        public Task RemoveRelationshipConfiguration(OneToOneRelationshipDto dto);
        public Task ConfigureOneToManyRelationship(OneToManyRelationshipDto dto);
        public Task RemoveOneToManyConfiguration(OneToManyRelationshipDto dto);
        public Task ConfigureManyToManyRelationship(ManyToManyRelationshipDto dto);
        public Task RemoveManyToManyConfiguration(ManyToManyRelationshipDto dto);
    }
}
