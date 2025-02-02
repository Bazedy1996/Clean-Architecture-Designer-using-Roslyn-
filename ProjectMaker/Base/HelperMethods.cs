using System.Text.RegularExpressions;

namespace ProjectMaker.Base
{
    public static class HelperMethods
    {
        public static bool IsValidName(string name)
        {
            if (!char.IsUpper(name[0]))
                return false;
            return Regex.IsMatch(name, @"^[a-zA-Z0-9_]+$");
        }
        public static string SanitizeName(string name)
        {
            string sanitized = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");
            string capitalizedprojectName = char.ToUpper(sanitized[0]) + sanitized.Substring(1);
            return capitalizedprojectName;
        }
    }
}
