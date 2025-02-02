namespace ProjectMaker.Dtos.DataCreator
{
    public class AddPropertyDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public string Servicename { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public List<PropertyDto> Properties { get; set; } = new List<PropertyDto>();
    }
}
