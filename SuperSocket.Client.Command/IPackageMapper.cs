namespace SuperSocket.Client.Command;

public interface IPackageMapper<PackageFrom, PackageTo>
{
    PackageTo Map(PackageFrom package);
}
