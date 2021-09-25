using System.Collections;
using System.Diagnostics.CodeAnalysis;
using MUtility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityServiceLocator;

namespace Tests
{
public class ServiceLocatorSceneGameObjectTests
{
    SceneServiceInstaller _installer;
    UnityServiceLocatorTestComponent1 _testComponent1;
    UnityServiceLocatorTestComponent2 _testComponent2;
    WASD_MovementProvider _testComponent3;
    ServiceSource _testSource1;
    ServiceSource _testSource2;
    ServiceSource _testSource3;

    [OneTimeSetUp]
    public static void SetUp() => CleanupInstallers();

    internal static void CleanupInstallers()
    {
        foreach (SceneServiceInstaller installer in Object.FindObjectsOfType<SceneServiceInstaller>())
            Object.Destroy(installer.gameObject);
    }
    
    [UnityTest, Order(1)]
    public IEnumerator Test1_CreateSceneInstaller()
    {
        var installerGameObject = new GameObject("TestInstaller");
        _installer = installerGameObject.AddComponent<SceneServiceInstaller>();


        yield return null;
    }

    [UnityTest, Order(2)]
    public IEnumerator Test2_AddGameObjectSourcesToInstaller()
    {
        _testComponent1 = AddServiceGameObject<UnityServiceLocatorTestComponent1>("GameObject1", out _testSource1);
        _testComponent2 = AddServiceGameObject<UnityServiceLocatorTestComponent2>("GameObject2", out _testSource2);
        _testComponent3 = AddServiceGameObject<WASD_MovementProvider>("GameObject3", out _testSource3);

        T AddServiceGameObject<T>(string name, out ServiceSource source) where T : Component
        {
            var testGameObject = new GameObject(name);
            var result = testGameObject.AddComponent<T>();
            source = _installer.AddServiceSource(testGameObject);
            return result;
        }

        yield return null;
    }



    [UnityTest, Order(3)]
    public IEnumerator Test3_GetSourceFromLocator()
    {
        bool foundService1 = ServiceLocator.TryGet(out UnityServiceLocatorTestComponent1 _);
        Assert.IsFalse(foundService1);

        var service2 = ServiceLocator.Get<UnityServiceLocatorTestComponent2>();
        bool foundService2 = service2 == _testComponent2;
        Assert.IsTrue(foundService2);

        var service3 = ServiceLocator.Get<IMovementInputProvider>();
        bool foundService3 = service3.NameOfDirectionCommand(GeneralDirection2D.Up)  == KeyCode.W.ToString();
        Assert.IsTrue(foundService3);

        yield return null;
    }

    [UnityTest, Order(4)]
    [SuppressMessage("ReSharper", "RedundantAssignment")]
    [SuppressMessage("ReSharper", "JoinDeclarationAndInitializer")]
    public IEnumerator Test4_InitializationTest()
    {
        UnityServiceLocatorTestComponent2 service2;
        service2 = ServiceLocator.Get<UnityServiceLocatorTestComponent2>();
        service2 = ServiceLocator.Get<UnityServiceLocatorTestComponent2>();
        bool initializedOnce = service2.initializationCount == 1;
        Assert.IsTrue(initializedOnce);
        yield return null;
    }
    
    [UnityTest, Order(5)] 
    public IEnumerator Test5_AddComponentsLaterToSourceGameObject()
    {
        _testComponent1.gameObject.AddComponent<UnityServiceLocatorTestComponent2>();
        yield return null;
    }
    
    [UnityTest, Order(6)] 
    public IEnumerator Test6_FreshAndResearchSource()
    {
        bool findService = ServiceLocator.TryGet(out UnityServiceLocatorTestComponent2 service1); 
        Assert.IsTrue(findService);
        bool foundServiceIsObject2 = service1.gameObject == _testComponent2.gameObject; 
        Assert.IsTrue(foundServiceIsObject2);
        _testSource1.ClearDynamicData();
        
        findService = ServiceLocator.TryGet(out service1); 
        Assert.IsTrue(findService);
        bool foundServiceIsObject1 = service1.gameObject == _testComponent1.gameObject; 
        Assert.IsTrue(foundServiceIsObject1);
        
        yield return null;
    }
}
}
