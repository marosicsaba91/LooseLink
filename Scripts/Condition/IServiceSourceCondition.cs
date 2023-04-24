namespace LooseLink
{
	public interface IServiceSourceCondition
	{
		bool CanResolve();
		string GetConditionMessage();
	}
}