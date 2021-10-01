namespace UnityServiceLocator
{
public interface IServiceSourceCondition 
{ 
	bool CanResolve();
	string GetConditionMessage();
}
}