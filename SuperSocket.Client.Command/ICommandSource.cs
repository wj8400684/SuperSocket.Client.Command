namespace SuperSocket.Client.Command;

public interface ICommandSource
{
    IEnumerable<Type> GetCommandTypes(Predicate<Type> criteria);
}
