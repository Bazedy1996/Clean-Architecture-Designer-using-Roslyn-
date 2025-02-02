using ProjectMaker.Base.CustomAttributes;

namespace ProjectMaker.Dtos.ProjectCreator
{
    public enum PackageInstaller
    {
        [Content("Microsoft.EntityFrameworkCore")]
        Package1 = 1,
        [Content("Microsoft.EntityFrameworkCore.SqlServer")]
        Package2,
        [Content("Microsoft.EntityFrameworkCore.Design")]
        Package3,
        [Content("Microsoft.EntityFrameworkCore.Tools")]
        Package4,
        [Content("Mapster")]
        Package5,
    }
}
