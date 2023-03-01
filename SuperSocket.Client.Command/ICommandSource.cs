namespace Work;

public interface ICommandSource
{
    IEnumerable<Type> GetCommandTypes(Predicate<Type> criteria);
}
