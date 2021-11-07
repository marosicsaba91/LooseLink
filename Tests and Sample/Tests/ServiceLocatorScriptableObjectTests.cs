using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityServiceLocator; 

namespace Tests
{
    public class ServiceLocatorScriptableObjectTests
    {
        const string testScriptableObject1Name = "IJKL Buttons Input Provider"; 
        const string testScriptableObject2Name = "WASD Movement Provider"; 
        LocalServiceInstaller _installer;  
        ScriptableObject _testScriptableObject1;
        ScriptableObject _testScriptableObject2;
        ServiceSource _serviceSource1;
        ServiceSource _serviceSource2;

        const string testTag1 = "I hate snakes!"; 

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
        public IEnumerator Test2_AddScriptableObjectSourceToInstaller()
        {
            _testScriptableObject1 = (ScriptableObject) Resources.Load(testScriptableObject1Name);
            _testScriptableObject2 = (ScriptableObject) Resources.Load(testScriptableObject2Name); 
            _serviceSource1 = _installer.AddSource(_testScriptableObject1, ServiceSourceTypes.FromScriptableObjectPrototype);
            _serviceSource2 = _installer.AddSource(_testScriptableObject2, ServiceSourceTypes.FromScriptableObjectFile);
            
            yield return null;
        } 

        [UnityTest, Order(order: 3)]
        public IEnumerator Test3_GetScriptableObjectSourceFromLocator()
        {       
            var service1 = ServiceLocator.Resolve<KeyboardMovementProvider>();
            bool foundService1 = service1 != null && service1 != _testScriptableObject1;
            Assert.IsTrue(foundService1); 

            var service2 = ServiceLocator.Resolve<WasdScriptableObjectMovementProvider>();
            bool foundService2= service2 != null;
            Assert.IsTrue(foundService2);
            yield return null;
        }

        [UnityTest, Order(order: 5)] 
        public IEnumerator Test5_AddAndSearchForAdditionalTags()
        { 
            bool success1 = ServiceLocator.TryResolve(  
                new object[]{testTag1},
                out WasdMovementProvider _);
            Assert.IsFalse(success1); 
            
            _serviceSource1.AddTag(testTag1);
            yield return null; 
            
            bool success2 = ServiceLocator.TryResolve(new object[]{testTag1},
                out KeyboardMovementProvider _);
            Assert.IsTrue(success2);
        } 
    }
}
