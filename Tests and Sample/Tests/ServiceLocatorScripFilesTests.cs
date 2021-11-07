using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityServiceLocator;

namespace Tests
{
    public class ServiceLocatorScripFilesTests
    { 
        const string scriptFilesScriptableObjectName = "UnityServiceLocatorTestInfo"; 
        LocalServiceInstaller _installer;  
 

        [OneTimeSetUp]
        public static void SetUp() => ServiceLocator.Environment.UninstallAllSourceSets();
        
        [UnityTest, Order(order: 1)]
        public IEnumerator Test1_CreateSceneInstaller()
        {
            var installerGameObject = new GameObject("TestInstaller");
            _installer = installerGameObject.AddComponent<LocalServiceInstaller>();
            
            yield return null;
        }
        
        [UnityTest, Order(order: 2)]
        public IEnumerator Test2_AddPrefabSourceToInstaller()
        {
            var container = (UnityServiceLocatorTestInfo) Resources.Load(scriptFilesScriptableObjectName); 
            _installer.AddSource(container.testMonoBehaviourScriptFile);
            _installer.AddSource(container.testScriptableObjectScriptFile);
            
            yield return null;
        } 

        [UnityTest, Order(order: 3)]
        public IEnumerator Test3_GetScriptableObjectSourceFromLocator()
        {       
            //Debug.Log(_installer.ServiceSourceCount);
            List<IServiceSourceProvider> installers = ServiceLocator.Environment.GetAllInstallers().ToList();
            foreach (var installer in installers)
            {
                Debug.Log($"{installer.Name}   {installer.SourceCount}");
                foreach (var source in installer.GetEnabledValidSourcesRecursive())
                    Debug.Log($"{source.IsServiceSource}   {source.ServiceSourceObject.name}");
            }

            //Debug.Log(ServiceLocator.Environment.GetAllInstallers().Count());
            var service1 = ServiceLocator.Resolve<WasdMovementProvider>();
            bool foundService1 = service1 != null;
            Assert.IsTrue(foundService1);

            var service2 = ServiceLocator.Resolve<KeyboardMovementProvider>();
            bool foundService2= service2 != null;
            Assert.IsTrue(foundService2);
            yield return null;
        }
    }
}
