namespace Work;

public interface IPackageMapper<PackageFrom, PackageTo>
{
    PackageTo Map(PackageFrom package);
}
