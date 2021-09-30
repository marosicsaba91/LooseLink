namespace UnityServiceLocator
{
public interface IResolvingCondition 
{ 
	bool CanResolve(out string message);
}
}