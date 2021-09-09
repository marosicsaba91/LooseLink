using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityServiceLocator;
using Object = System.Object;

namespace Tests
{
    public class ServiceLocatorScripFilesTests
    { 
        const string scriptFilesScriptableObjectName = "UnityServiceLocatorTestScriptableObject2"; 
        SceneServiceInstaller _installer;  
 

        [OneTimeSetUp]
        public static void SetUp() =>ServiceLocatorSceneGameObjectTests.CleanupInstallers();
        
        [UnityTest, Order(order: 1)]
        public IEnumerator Test1_CreateSceneInstaller()
        {
            var installerGameObject = new GameObject("TestInstaller");
            _installer = installerGameObject.AddComponent<SceneServiceInstaller>();
            
            yield return null;
        }
        
        [UnityTest, Order(order: 2)]
        public IEnumerator Test2_AddPrefabSourceToInstaller()
        {
            var container = (UnityServiceLocatorTestScriptableObject2) Resources.Load(scriptFilesScriptableObjectName); 
            _installer.AddServiceSource(container.testMonoBehaviourScriptFile);
            _installer.AddServiceSource(container.testScriptableObjectScriptFile);
            
            yield return null;
        } 

        [UnityTest, Order(order: 3)]
        public IEnumerator Test3_GetScriptableObjectSourceFromLocator()
        {       
            var service1 = ServiceLocator.Get<IUnityServiceLocatorTestInterface>();
            bool foundService1 = service1 != null;
            Assert.IsTrue(foundService1);

            var service2 = ServiceLocator.Get<UnityServiceLocatorTestScriptableObject1>();
            bool foundService2= service2 != null;
            Assert.IsTrue(foundService2);
            yield return null;
        }
    }
}
