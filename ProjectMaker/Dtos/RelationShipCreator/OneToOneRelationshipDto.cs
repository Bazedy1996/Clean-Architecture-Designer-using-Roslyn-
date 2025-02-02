namespace ProjectMaker.Dtos.RelationShipCreator
{
    public class OneToOneRelationshipDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string SourceEntity { get; set; } = string.Empty;
        public string TargetEntity { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
        public DeleteBehavior DeleteRule { get; set; }
    }
}
