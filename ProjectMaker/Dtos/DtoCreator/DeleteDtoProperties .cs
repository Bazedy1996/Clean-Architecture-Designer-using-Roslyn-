namespace ProjectMaker.Dtos.DtoCreator
{
    public class DeleteDtoProperties
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public List<string> PropertyNames { get; set; } = new List<string>();
    }
}
