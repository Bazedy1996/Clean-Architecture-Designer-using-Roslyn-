namespace ProjectMaker.Dtos.DBHandler
{
    public class AttributeDto
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public string ModelName { get; set; } = string.Empty;
        public string PropertyName { get; set; } = string.Empty;
        public string AnnotationType { get; set; } = string.Empty;
        public object? Value { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
