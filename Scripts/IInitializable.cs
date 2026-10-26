namespace LooseLink
{
	public interface IInitializable
	{
		// Called once when the service is first resolved, after its object is available and cached.
		// Resolve dependencies here when lazily created services may depend on one another.
		// Circular service lookups from Awake or OnEnable cannot be resolved safely.
		void Initialize();
	}
}
