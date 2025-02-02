namespace ProjectMaker.Dtos.DataCreator
{
    public class ModelDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public ModelType ModelType { get; set; }
    }
}
