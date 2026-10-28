using System.Collections; 
using LooseLink; 
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests
{
    [ServiceType]
    public sealed class LooseLinkTestService : ScriptableObject
    {
        public string Id;
    }

    public class LooseLinkTests
    {
        const string sampleScene = "LooseLink Sample Scene";

        readonly List<ServiceSourceSet> _installedSets = new();
        readonly List<Object> _objectsToDestroy = new();

        [SetUp]
        public void Setup()
        {
            SceneManager.LoadScene(sampleScene);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (ServiceSourceSet set in _installedSets)
                Services.Environment.TryUninstallServiceSourceSet(set);
            foreach (Object obj in _objectsToDestroy)
                if (obj != null)
                    Object.DestroyImmediate(obj);

            _installedSets.Clear();
            _objectsToDestroy.Clear();
        }

        [UnityTest, Order(1)]
        public IEnumerator Test1_ScriptableObject()
        {
            IShootingInputProvider shootingInput = Services.Get<IShootingInputProvider>();
            Assert.IsTrue(shootingInput != null); 
            yield break;
        }
        
        [UnityTest, Order(2)]
        public IEnumerator Test2_SceneObject()
        {
            Avatar movement = Services.Get<Avatar>();
            Assert.IsTrue(movement != null);
            yield break;
        }
        
        [UnityTest, Order(3)]
        public IEnumerator Test3_Prefab()
        {
            BallisticsManager ballisticsManager = Services.Get<BallisticsManager>();
            Assert.IsTrue(ballisticsManager != null);
            yield break;
        } 

        [UnityTest, Order(4)]
        public IEnumerator Test4_MonoBehaviourScript()
        {
            UpdateProvider update = Services.Get<UpdateProvider>();
            Assert.IsTrue(update != null);
            yield break;
        }
        
        [UnityTest, Order(5)]
        public IEnumerator Test5_ScriptableObjectScript()
        {
            IMovementInputProvider movement = Services.Get<IMovementInputProvider>();
            Assert.IsTrue(movement != null);
            yield break;
        }
        
        [UnityTest, Order(6)]
        public IEnumerator Test6_RuntimeTypeLookup()
        {
            object avatar = Services.Get(typeof(Avatar));
            Assert.IsInstanceOf<Avatar>(avatar);
            yield break;
        }

        [UnityTest, Order(7)]
        public IEnumerator Test7_PermanentProxyRefreshesLazily()
        {
            LooseLinkTestService defaultService = CreateTestService("Default");
            Install(defaultService, priority: 1);
            PermanentProxy<LooseLinkTestService> proxy = Services.GetProxy<LooseLinkTestService>();

            Assert.IsTrue(proxy.TryGetTarget(out LooseLinkTestService initial));
            Assert.AreSame(defaultService, initial);

            LooseLinkTestService replacementService = CreateTestService("Replacement");
            Install(replacementService, priority: 2);
            LooseLinkTestService previous = null;
            LooseLinkTestService current = null;
            int changeCount = 0;
            proxy.BindingChanged += OnBindingChanged;

            Assert.IsFalse(proxy.HasTarget);
            Assert.AreEqual(0, changeCount);
            Assert.IsTrue(proxy.TryGetTarget(out LooseLinkTestService replacement));
            Assert.AreSame(replacementService, replacement);
            Assert.AreSame(defaultService, previous);
            Assert.AreSame(replacementService, current);
            Assert.AreEqual(1, changeCount);

            proxy.BindingChanged -= OnBindingChanged;
            yield break;

            void OnBindingChanged(LooseLinkTestService oldService, LooseLinkTestService newService)
            {
                previous = oldService;
                current = newService;
                changeCount++;
            }
        }

        [UnityTest, Order(8)]
        public IEnumerator Test8_DestroyedCreatedServiceIsRecreated()
        {
            LooseLinkTestService prototype = CreateTestService("Prototype");
            ServiceSourceSet set = CreateSet(priority: 10);
            ServiceSource source = set.AddSource(prototype, ServiceSourceTypes.FromScriptableObjectPrototype);
            Assert.IsTrue(Services.Environment.TryInstallServiceSourceSet(set));
            _installedSets.Add(set);

            LooseLinkTestService first = Services.Get<LooseLinkTestService>();
            Assert.IsNotNull(first);
            Assert.AreNotSame(prototype, first);
            Object.DestroyImmediate(first);

            LooseLinkTestService second = Services.Get<LooseLinkTestService>();
            Assert.IsNotNull(second);
            Assert.AreNotSame(first, second);
            Assert.IsTrue(source.TryDestroyCreatedService());
            yield break;
        }

        [UnityTest, Order(9)]
        public IEnumerator Test9_NestedSourceSetCycleDoesNotRecurseForever()
        {
            ServiceSourceSet first = CreateSet(priority: 10);
            ServiceSourceSet second = CreateSet(priority: 10);
            first.AddSource(second);
            second.AddSource(first);
            Assert.IsTrue(Services.Environment.TryInstallServiceSourceSet(first));
            _installedSets.Add(first);

            Assert.IsFalse(Services.TryGet(out LooseLinkTestService _));
            yield break;
        }

        LooseLinkTestService CreateTestService(string id)
        {
            LooseLinkTestService service = ScriptableObject.CreateInstance<LooseLinkTestService>();
            service.Id = id;
            _objectsToDestroy.Add(service);
            return service;
        }

        ServiceSourceSet Install(LooseLinkTestService service, int priority)
        {
            ServiceSourceSet set = CreateSet(priority);
            set.AddSource(service);
            Assert.IsTrue(Services.Environment.TryInstallServiceSourceSet(set));
            _installedSets.Add(set);
            return set;
        }

        ServiceSourceSet CreateSet(int priority)
        {
            ServiceSourceSet set = ScriptableObject.CreateInstance<ServiceSourceSet>();
            set.PriorityValue = priority;
            _objectsToDestroy.Add(set);
            return set;
        }

    }
}
