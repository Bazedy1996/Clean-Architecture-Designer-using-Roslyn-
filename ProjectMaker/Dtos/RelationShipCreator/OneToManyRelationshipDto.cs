namespace ProjectMaker.Dtos.RelationShipCreator
{
    public class OneToManyRelationshipDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string OneEntity { get; set; } = string.Empty;
        public string ManyEntity { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
        public DeleteBehavior DeleteRule { get; set; }
    }
}
