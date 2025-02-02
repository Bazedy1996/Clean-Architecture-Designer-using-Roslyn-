namespace ProjectMaker.Base.CustomAttributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class ContentAttribute : Attribute
    {
        public string Content { get; }
        public ContentAttribute(string content)
        {
            Content = content;
        }

    }
}
