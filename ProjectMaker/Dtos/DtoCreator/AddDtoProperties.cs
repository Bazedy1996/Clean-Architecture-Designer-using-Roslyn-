using ProjectMaker.Dtos.DataCreator;

namespace ProjectMaker.Dtos.DtoCreator
{
    public class AddDtoProperties
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public DtoType DtoType { get; set; }
        public List<PropertyDto> properties { get; set; } = new List<PropertyDto>();
    }
}
