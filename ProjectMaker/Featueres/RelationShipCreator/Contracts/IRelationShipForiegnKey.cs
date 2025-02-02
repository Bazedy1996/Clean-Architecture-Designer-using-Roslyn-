using ProjectMaker.Dtos.RelationShipCreator;

namespace ProjectMaker.Featueres.RelationShipCreator.Contracts
{
    public interface IRelationShipForiegnKey
    {
        //one to one
        public Task AddForeignKeyProperty(OneToOneRelationshipDto dto);
        public Task<(string KeyPropertyName, string KeyPropertyType)> GetPrimaryKeyInfoSourceEntity(OneToOneRelationshipDto dto);
        public Task RemoveRelationshipForiegnKey(OneToOneRelationshipDto dto);
        //one to many
        public Task AddOneToManyProperties(OneToManyRelationshipDto dto);
        public Task<(string KeyPropertyName, string KeyPropertyType)> GetPrimaryKeyInfoForOneToMany(OneToManyRelationshipDto dto);
        public Task RemoveOneToManyProperties(OneToManyRelationshipDto dto);
        //many to many
        public Task CreateJoinEntity(ManyToManyRelationshipDto dto);
        public Task AddManyToManyProperties(ManyToManyRelationshipDto dto);
        public Task<(string KeyPropertyName, string KeyPropertyType)> GetPrimaryKeyInfoForManyToMany(ManyToManyRelationshipDto dto);
        public void RemoveJoinEntity(ManyToManyRelationshipDto dto);
        public Task RemoveManyToManyProperties(ManyToManyRelationshipDto dto);

    }
}
