namespace ProjectMaker.Dtos.RelationShipCreator
{
    public class ManyToManyRelationshipDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string FirstEntity { get; set; } = string.Empty;
        public string SecondEntity { get; set; } = string.Empty;
        public DeleteBehavior DeleteRule { get; set; }
    }
}
