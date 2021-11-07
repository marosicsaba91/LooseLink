using System.Collections;
using System.Linq;
using MUtility;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityServiceLocator;

namespace Tests
{
    public class ServiceLocatorPrefabTests
    {
        const string wasdPrefabName = "WASDMovementProviderPrefab";
        GameObject _wasdPrefab;
        ServiceSource _wasdPrefabServiceSource; // Installed As Prefab 
        
        const string cursorButtonsPrefabName = "CursorButtonsMovementProviderPrefab";
        GameObject _cursorButtonsPrefab;
        ServiceSource _cursorButtonsPrefabServiceSource; // Installed As Prefab File
 
        LocalServiceInstaller _installer;  
                
        [UnityTest, Order(1)]
        public IEnumerator Test1_TestIfThereAreNoInstaller()
        { 
            ServiceLocator.Environment.UninstallAllSourceSets();
            Assert.IsEmpty(ServiceLocator.Environment.GetAllInstallers());
            yield return null;
        }

        [UnityTest, Order(2)]
        public IEnumerator Test2_CreateLocalInstaller()
        {
            ServiceLocator.Environment.UninstallAllSourceSets();
            var installerGameObject = new GameObject("TestLocalInstaller");
            _installer = installerGameObject.AddComponent<LocalServiceInstaller>(); 
            Assert.AreEqual(1, ServiceLocator.Environment.GetAllInstallers().Count());
            
            yield return null;
        }
        
        [UnityTest, Order(3)]
        public IEnumerator Test3_AddPrefabSourceToInstaller()
        {
            _wasdPrefab = (GameObject) Resources.Load(wasdPrefabName);
            _wasdPrefabServiceSource = _installer.AddSource(_wasdPrefab, ServiceSourceTypes.FromPrefabPrototype);
            
            _cursorButtonsPrefab = (GameObject) Resources.Load(cursorButtonsPrefabName);
            _cursorButtonsPrefabServiceSource =
                _installer.AddSource(_cursorButtonsPrefab, ServiceSourceTypes.FromPrefabFile);

            yield return null;
        } 

        [UnityTest, Order(4)]
        public IEnumerator Test4_GetSourceFromLocator()
        { 
            ServiceLocator.TryResolve(out WasdMovementProvider wasdMovementProvider);
            bool wasdMovementProviderFound = wasdMovementProvider != null;
            Assert.IsTrue(wasdMovementProviderFound);
            bool isWasdMovementProviderSceneObject = wasdMovementProvider.gameObject.scene.IsValid();
            Assert.IsTrue(isWasdMovementProviderSceneObject); 
            Assert.IsTrue(IsWasd(wasdMovementProvider));
            
            ServiceLocator.TryResolve(out CursorButtonsMovementProvider cursorButtonsMovementProvider);
            bool service3Found = cursorButtonsMovementProvider != null;
            Assert.IsTrue(service3Found);
            bool service3IsPrefab = cursorButtonsMovementProvider.gameObject == _cursorButtonsPrefab;
            Assert.IsTrue(service3IsPrefab);  
            Assert.IsTrue(IsCursor(cursorButtonsMovementProvider));

            yield return null;
        }
        
        
        [UnityTest, Order(5)] 
        public IEnumerator Test5_AddAdditionalTypes()
        {
            bool successfulInterfaceTypeAdd = _wasdPrefabServiceSource.TryAddServiceType<Transform>();
            Assert.IsTrue(successfulInterfaceTypeAdd);
            bool successfulTransformTypeAdd = _cursorButtonsPrefabServiceSource.TryAddServiceType(typeof(IUnityComponent));
            Assert.IsTrue(successfulTransformTypeAdd);
            
            yield return null;
        }
        
        [UnityTest, Order(6)] 
        public IEnumerator Test6_SearchForAddAdditionalTypes()
        {   
            ServiceLocator.TryResolve(out Transform transform);
            bool transformServiceFound = transform!= null;
            Assert.IsTrue(transformServiceFound);
            bool rightTransformFound =  transform.gameObject.name == wasdPrefabName;
            Assert.IsTrue(rightTransformFound);
            yield return null;
            
            ServiceLocator.TryResolve(out IUnityComponent unityComponent);
            bool service1Found = unityComponent != null;
            Assert.IsTrue(service1Found); 
            Assert.IsTrue(unityComponent.GameObject == _cursorButtonsPrefab);
        }
        
        bool IsWasd(IMovementInputProvider movementProvider) => 
            movementProvider.NameOfDirectionCommand(GeneralDirection2D.Up) == KeyCode.W.ToString();
        
        bool IsCursor(IMovementInputProvider movementProvider) => 
            movementProvider.NameOfDirectionCommand(GeneralDirection2D.Up) == KeyCode.UpArrow.ToString();
    }
}
