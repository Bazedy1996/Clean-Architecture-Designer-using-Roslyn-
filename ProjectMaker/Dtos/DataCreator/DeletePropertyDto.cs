namespace ProjectMaker.Dtos.DataCreator
{
    public class DeletePropertyDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;

        public List<string> PropertyNames { get; set; } = new List<string>();
    }
}
