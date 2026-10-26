namespace LooseLink
{
	public interface IServiceSourceCondition
	{
		bool CanResolve();
		string GetConditionMessage();
	}

	public interface IServiceSourceConditionChangeNotifier : IServiceSourceCondition
	{
		event System.Action ConditionChanged;
	}
}
