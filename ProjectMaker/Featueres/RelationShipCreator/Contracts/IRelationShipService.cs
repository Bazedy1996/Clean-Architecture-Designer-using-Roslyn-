using ProjectMaker.Base;
using ProjectMaker.Dtos.RelationShipCreator;

namespace ProjectMaker.Featueres.RelationShipCreator.Contracts
{
    public interface IRelationShipService
    {
        public Task<Response<string>> AddOneToOneRelationship(OneToOneRelationshipDto dto);
        public Task<Response<string>> RemoveOneToOneRelationship(OneToOneRelationshipDto dto);
        public Task<Response<string>> AddOneToManyRelationship(OneToManyRelationshipDto dto);
        public Task<Response<string>> RemoveOneToManyRelationship(OneToManyRelationshipDto dto);
        public Task<Response<string>> AddManyToManyRelationship(ManyToManyRelationshipDto dto);
        public Task<Response<string>> RemoveManyToManyRelationship(ManyToManyRelationshipDto dto);

    }
}
