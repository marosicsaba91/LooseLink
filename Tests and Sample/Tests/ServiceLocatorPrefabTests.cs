using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityServiceLocator;

namespace Tests
{
    public class ServiceLocatorPrefabTests
    {
        const string testPrefab1Name = "UnityServiceLocatorTestPrefab1";
        const string testPrefab2Name = "UnityServiceLocatorTestPrefab2";
        const string testPrefab3Name = "UnityServiceLocatorTestPrefab3";
        SceneServiceInstaller _installer;  
        GameObject _testPrefab1;
        GameObject _testPrefab2;
        GameObject _testPrefab3;
        ServiceSource _serviceSource1; 
        ServiceSource _serviceSource2; 

        [OneTimeSetUp]
        public static void SetUp() =>ServiceLocatorSceneGameObjectTests.CleanupInstallers();
        
        [UnityTest, Order(1)]
        public IEnumerator Test1_CreateSceneInstaller()
        {
            ServiceLocator.Environment.UninstallAllSourceSets();
            var installerGameObject = new GameObject("TestInstaller");
            _installer = installerGameObject.AddComponent<SceneServiceInstaller>();
            
            yield return null;
        }
        
        [UnityTest, Order(2)]
        public IEnumerator Test2_AddPrefabSourceToInstaller()
        {
            _testPrefab1 = (GameObject) Resources.Load(testPrefab1Name);
            _testPrefab2 = (GameObject) Resources.Load(testPrefab2Name);
            _testPrefab3 = (GameObject) Resources.Load(testPrefab3Name);
            _serviceSource1 = _installer.AddServiceSource(_testPrefab1, ServiceSourceTypes.FromPrefabPrototype);
            _serviceSource2 = _installer.AddServiceSource(_testPrefab2, ServiceSourceTypes.FromPrefabFile);
            _installer.AddServiceSource(_testPrefab3, ServiceSourceTypes.FromPrefabPrototype);

            yield return null;
        } 

        [UnityTest, Order(3)]
        public IEnumerator Test3_GetSourceFromLocator()
        { 
            ServiceLocator.TryGet(out UnityServiceLocatorTestComponent2 service2);
            bool service2Found = service2 != null;
            Assert.IsTrue(service2Found);
            bool service2IsNotInstantiatedToScene = service2.gameObject ==_testPrefab2;
            Assert.IsTrue(service2IsNotInstantiatedToScene);
            
            ServiceLocator.TryGet(out IAvatarInputProvider service3);
            bool service3Found = service3 != null;
            Assert.IsTrue(service3Found);
            bool service3IsInstantiatedToScene = ((GameObject)service3.UnityObject).scene.IsValid();
            Assert.IsTrue(service3IsInstantiatedToScene);
            bool service3IsInstantiatedFromTestPrefab3 = service3.UnityObject.name == _testPrefab3.name;
            Assert.IsTrue(service3IsInstantiatedFromTestPrefab3);

            yield return null;
        }
        
        [UnityTest, Order(4)] 
        public IEnumerator Test4_AddAdditionalTypes()
        {
            bool successfulTypeAdd1 = _serviceSource1.TryAddType<UnityServiceLocatorTestComponent1>();
            Assert.IsTrue(successfulTypeAdd1);
            bool successfulTypeAdd2 = _serviceSource2.TryAddType(typeof(Transform));
            Assert.IsTrue(successfulTypeAdd2);
            
            yield return null;
        }
        
        [UnityTest, Order(5)] 
        public IEnumerator Test5_SearchForAddAdditionalTypes()
        {   
            ServiceLocator.TryGet(out UnityServiceLocatorTestComponent1 service1);
            bool service1Found = service1 != null;
            Assert.IsTrue(service1Found);
            bool service1IsInstantiatedToScene = service1.gameObject.scene.IsValid();
            Assert.IsTrue(service1IsInstantiatedToScene);
            
            ServiceLocator.TryGet(out Transform service2);
            bool service2Found = service2.gameObject == _testPrefab2;
            Assert.IsTrue(service2Found);
            bool service2IsNotInstantiatedToScene = service2.gameObject ==_testPrefab2;
            Assert.IsTrue(service2IsNotInstantiatedToScene);
            yield return null; 
        }
    }
}
