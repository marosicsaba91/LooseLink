namespace LooseServices
{
public interface IInitializable
{
	// Is called after Service was requested the first time. Could be before Awake.
	// Other Service locations should be called here.
	void Initialize();
}
}