# LooseLink

LooseLink is a Unity-native, type-based service locator designed to resolve (locate or create) unique, high-level, singleton-like services. It is a powerful tool for keeping your codebase clean, testable, flexible, and easy to modify.

## The problem we want to solve

Unity classes often contain too much information about other classes and the overall architecture of the software.

A client class usually need to know most of this to be able to use a high-level system:

- What is its exact class?
- Is it a component in a scene or a ScriptableObject?
- Does it exist yet, or does it need to be created?
- What is its lifecycle model?
- How should I access it?

This is what it should actually know:

- I need this functionality to do my work.

LooseLink is designed to make this blissful ignorance called loose coupling possible and easy.

---

To resolve a service the cliant only needs to request it like this:

```csharp
IInputManager input = Services.Get<IInputManager>();
```

The resolved object may come from another scene, a prefab, a ScriptableObject, a reusable configuration asset, or an object that does not exist yet. The service locator's job is to determine those details from a service environment that is easy to configure and monitor in the Editor. The client only knows the bare minimum, that it neesd the service.

----

LooseLink is narrower than a general dependency-injection framework. It is only intended for manager-like, high-level objects where only one service is expected to be active for a given role at a time. This is why only type-based resolution is supported. It is not a general search mechanism for enemies, projectiles, UI elements, or collections of similar low-level objects. This limitation is intentional. LooseLink is built to complement a specific blind spot in Unity's toolset, not to replace its entire serialization system.


## Core model

LooseLink has five principal layers:

- **Service:** Object or only an interfaces of an object exposed to be accassable thrugh the service locator.
- **Server Object:** A Unity Object (GameObject, MonoBehaviour or ScriptableObject) that provides one or more services.
- **ServiceSource:** It describes how to access a Server Object and which Services (types) that object provides.
- **SourceProvider:** It contains one or more service sources.
- **ServiceEnvironment:** A global state of the software, aware of the currently installed providers.

One object can provide several service types. For example, one `GameManager` component may be exposed as `IGameManager`, `ISaveManager`, and `GameManager`.

## Service sources

A service source can access a service in several ways:

| Source | Behavior |
| --- | --- |
| Scene GameObject | Returns a compatible component from an existing scene object. |
| Prefab File | Returns a compatible component from the prefab asset itself. |
| Prefab Prototype | Instantiates the prefab lazily and returns a component from the new instance. |
| ScriptableObject File | Returns the existing ScriptableObject asset. |
| ScriptableObject Prototype | Clones the asset lazily and returns the clone. |
| MonoBehaviour Type | Creates a GameObject and adds the configured MonoBehaviour type lazily. |
| ScriptableObject Type | Creates an instance of the configured ScriptableObject type lazily. |
| ServiceSourceSet | Expands a reusable, nested collection of other sources. |

When a source creates an object, it caches the object and reuses it while it remains alive.

## Providers

LooseLink supports three provider forms.

### LocalServiceInstaller

A scene component containing multiple service sources. Its sources are eligible while the installer is installed, active, and enabled.

### ServerObject

A component that exposes services from its own GameObject. Enable **Install Automatically** when the object should register itself without appearing in another installer.

### ServiceSourceSet

A reusable ScriptableObject containing service configuration. A set can be nested inside an installer or another set.

Enable **Use as Global Installer** to load a set automatically. Global sets must be stored inside a `Resources` folder so they are discoverable in both the Editor and player builds.

## Priorities and overrides

Providers are resolved from highest priority to lowest priority. This allows a scene, game mode, test, or platform-specific provider to override a global default.

Sources inside one provider are resolved in their serialized list order. Their order can be changed in the Inspector.

Local installers support either:

- A fixed priority value.
- **Highest At Installation**, which assigns a value above the current maximum when installed.

Competing bindings at the same provider priority are reported as configuration warnings. Assign distinct priorities when precedence between providers matters.

## Exposing service types

Mark a class or interface with `[ServiceType]` to make it discoverable automatically:

```csharp
[ServiceType]
public interface IInputManager
{
	void ReadInput();
}
```

Other compatible classes and interfaces can be exposed explicitly through the Inspector.

## Requesting services

The generic API is normally the clearest option:

```csharp
IInputManager input = Services.Get<IInputManager>();
```

Runtime type lookup is also supported:

```csharp
object input = Services.Get(typeof(IInputManager));
```

Use `TryGet` when a missing service is an expected result:

```csharp
if (Services.TryGet(out IInputManager input))
{
	input.ReadInput();
}
```

Resolution searches eligible providers by priority and sources by list order. A blocked or invalid source is skipped so a lower-priority fallback can be used.

`Services.Get` applies the configured missing-service policy when resolution fails. The available policies are:

- Return `null`.
- Return `null` and log a warning.
- Throw an exception.

## Lazy creation and initialization

Created services are normally instantiated only when first requested. The source caches the resulting object for later requests.

Unity invokes `Awake()` and `OnEnable()` synchronously during `Instantiate`, `AddComponent`, and `CreateInstance`. LooseLink cannot cache the new object until that Unity construction call returns.

This means circular dependency lookup from `Awake()` or `OnEnable()` is not safe:

```text
A.Awake() → Get<B>()
B.Awake() → Get<A>()
```

LooseLink detects the second request for A, returns failure for that lookup, and logs the construction chain with advice to use `IInitializable`. It does not destroy or roll back the objects that Unity already created.

Use `IInitializable` when lazily created services need post-creation setup or may depend on one another:

```csharp
public sealed class GameplayManager : MonoBehaviour, IInitializable
{
	IInputManager _input;

	public void Initialize()
	{
		_input = Services.Get<IInputManager>();
	}
}
```

LooseLink marks the object as initialized before invoking callbacks, so circular resolution through `Initialize()` is safe. Each created service object is initialized once per cached instance.

## Conditions and fallbacks

A component or ScriptableObject associated with a source can implement `IServiceSourceCondition`:

```csharp
public sealed class PlatformCondition : MonoBehaviour, IServiceSourceCondition
{
	public bool CanResolve() =>
		Application.platform == RuntimePlatform.WindowsPlayer;

	public string GetConditionMessage() =>
		"Available only on Windows.";
}
```

When `CanResolve()` returns `false`, LooseLink skips that source and continues searching. This supports configurations such as:

1. Windows input manager.
2. Mobile input manager.
3. Default fallback input manager.

For a condition that can change at runtime, implement `IServiceSourceConditionChangeNotifier` and raise `ConditionChanged`:

```csharp
public interface IServiceSourceConditionChangeNotifier : IServiceSourceCondition
{
	event Action ConditionChanged;
}
```

The notification advances the environment revision. It does not eagerly resolve services or invoke client callbacks.

## Following service replacements

An ordinary lookup returns the service active at that moment:

```csharp
IInputManager input = Services.Get<IInputManager>();
```

Use a permanent proxy when a client needs to follow later replacements:

```csharp
PermanentProxy<IInputManager> input = Services.GetProxy<IInputManager>();

input.BindingChanged += (previous, current) =>
{
	// Detach from the previous service and configure the new service.
};

input.Target.ReadInput();
```

Permanent proxies are lazy. Environment mutations only advance a revision number. A proxy reevaluates its binding the next time `Target` or `TryGetTarget` is accessed, and `BindingChanged` is invoked during that access only when the newly resolved object differs from the cached object.

The proxy members have deliberately different side-effect guarantees:

| Member | Behavior |
| --- | --- |
| `HasTarget` | Checks only the current cached binding. It never resolves or creates a service. It returns `false` when the environment revision is stale. |
| `TryGetTarget(out T target)` | May resolve or create the current service. Returns `false` when no service is available. |
| `Target` | Resolves when necessary and applies the configured missing-service policy on failure. |
| `BindingChanged` | Fires lazily during `Target` or `TryGetTarget` access when the effective target changed. |

`Services.GetProxy<T>()` returns a shared proxy for that service type. Event subscriptions therefore strongly retain their subscribers. Unity clients should unsubscribe in `OnDisable()` or `OnDestroy()`.

## Runtime environment changes

The environment revision changes when, for example:

- A provider is installed or uninstalled.
- An installer is enabled or disabled.
- A source is enabled, disabled, reordered, or reconfigured.
- Provider priority changes.
- A source condition reports a change.
- A lazily created service is created or explicitly destroyed.

LooseLink does not publish an eager environment-wide change event. Clients that care about replacements use `PermanentProxy<T>` and observe changes on their next proxy access.

## Service lifetime and ownership

LooseLink does not own service instances.

Sources cache the objects they access or create, but disabling a source, replacing its configuration, uninstalling a provider, clearing the environment, or leaving Play Mode does not cause LooseLink to destroy the service.

If a cached Unity object has been destroyed, the source treats it as unavailable. A source configured to create services may create and cache a replacement on the next resolution. It does not delete any surviving previous container while doing so.

Cache clearing only forgets references. If a still-live created object is forgotten and the source resolves again, another instance may be created. Managing that possibility is the developer's responsibility.

Explicit deletion remains available for tools or developer-controlled cleanup:

```csharp
bool destroyed = serviceSource.TryDestroyCreatedService();
```

This succeeds only when the source created the cached object. LooseLink never calls it automatically. Existing scene objects, prefab assets, and ScriptableObject assets are not destroyed by this method.

This policy matches the usual provider lifetimes:

- A service parented under a scene installer normally disappears when its scene unloads.
- A `ServerObject` and its services share the same GameObject lifetime.
- A global service normally lives for the application lifetime.
- Unusual cross-scene or runtime-created ScriptableObject lifetimes remain application responsibilities.

## Setup asset

The settings button in the Loose Link Services window can create a `ServiceLocationSetupData` asset. Store exactly one setup asset in a `Resources` folder.

The setup asset controls:

- Whether LooseLink initializes at Play Mode start in the Editor.
- Whether LooseLink initializes before the first scene in a player build.
- The action taken when `Services.Get` cannot resolve a service.

Without a setup asset, LooseLink uses an in-memory default configuration.

## Loose Link Services window

Open the editor window from:

`Tools > Loose Link Services`

The window displays the active environment in four columns:

| Column | Description |
| --- | --- |
| Service Sources | Installed providers, nested source sets, and source leaves. |
| Source Type | How the source accesses or creates its object. |
| Services | The classes and interfaces exposed by the source. |
| Server Object | The currently cached object, with controls for explicit resolution or deletion where applicable. |

The window supports source configuration, priority inspection, runtime monitoring, explicit lazy instantiation, cache clearing, and explicit deletion of a created service.

## Recommended use

LooseLink is best suited to unique services selected by role rather than location:

- `ISaveManager`
- `IAudioManager`
- `IInputManager`
- `SurfaceNavigation`
- `GameplayBodyManager`

Use ordinary Inspector references when both objects already exist together in a known scene or asset. Use LooseLink when a dependency:

- Lives in another scene.
- Is represented by an interface.
- Is selected by platform, game mode, or runtime condition.
- Needs a local override over a global default.
- Should be created only when first requested.
- May be replaced during the consumer's lifetime.

In one sentence: **LooseLink is a Unity-native, type-based service environment that selects, creates, caches, overrides, and lazily tracks singleton-like manager objects without owning their lifetime.**
