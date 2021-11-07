using System.Collections;
using MUtility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityServiceLocator;

namespace Tests
{
public class ServiceLocatorSceneGameObjectTests
{
    LocalServiceInstaller _installer; 
    WasdMonoBehaviourMovementProvider _testComponent3; 
    ServiceSource _testSource;

    [OneTimeSetUp]
    public static void SetUp() => ServiceLocator.Environment.UninstallAllSourceSets();
    
    [UnityTest, Order(1)]
    public IEnumerator Test1_CreateSceneInstaller()
    {
        var installerGameObject = new GameObject("TestInstaller");
        _installer = installerGameObject.AddComponent<LocalServiceInstaller>();


        yield return null;
    }

    [UnityTest, Order(2)]
    public IEnumerator Test2_AddGameObjectSourcesToInstaller()
    { 
        _testComponent3 = AddServiceGameObject<WasdMonoBehaviourMovementProvider>("GameObject3", out _testSource);

        T AddServiceGameObject<T>(string name, out ServiceSource source) where T : Component
        {
            var testGameObject = new GameObject(name);
            var result = testGameObject.AddComponent<T>();
            source = _installer.AddSource(testGameObject);
            return result;
        }

        yield return null;
    }

    [UnityTest, Order(3)]
    public IEnumerator Test3_GetSourceFromLocator()
    {
        var service3 = ServiceLocator.Resolve<IMovementInputProvider>();
        bool foundService3 = service3.NameOfDirectionCommand(GeneralDirection2D.Up)  == KeyCode.W.ToString();
        Assert.IsTrue(foundService3);

        yield return null;
    }
}
}
