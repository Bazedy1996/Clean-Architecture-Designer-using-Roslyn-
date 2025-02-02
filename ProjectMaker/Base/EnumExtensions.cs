using ProjectMaker.Base.CustomAttributes;

namespace ProjectMaker.Base
{
    public static class EnumExtensions
    {
        public static string GetContent(this Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var attributes = (ContentAttribute[])fieldInfo!.GetCustomAttributes(typeof(ContentAttribute), false);
            return attributes.Length > 0 ? attributes[0].Content : string.Empty;
        }
    }
}
