using System.Collections; 
using LooseLink; 
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests
{
    public class LooseLinkTests
    {
        const string sampleScene = "LooseLink Sample Scene";

        [SetUp]
        public void Setup()
        {
            SceneManager.LoadScene(sampleScene);
        }

        [UnityTest, Order(1)]
        public IEnumerator Test1_ScriptableObject()
        {
            var shootingInput = Services.Get<IShootingInputProvider>();
            Assert.IsTrue(shootingInput != null); 
            yield break;
        }
        
        [UnityTest, Order(2)]
        public IEnumerator Test2_SceneObject()
        {
            var movement = Services.Get<Avatar>();
            Assert.IsTrue(movement != null);
            yield break;
        }
        
        [UnityTest, Order(3)]
        public IEnumerator Test3_Prefab()
        {
            var ballisticsManager = Services.Get<BallisticsManager>();
            Assert.IsTrue(ballisticsManager != null);
            yield break;
        } 

        [UnityTest, Order(4)]
        public IEnumerator Test4_MonoBehaviourScript()
        {
            var update = Services.Get<UpdateProvider>();
            Assert.IsTrue(update != null);
            yield break;
        }
        
        [UnityTest, Order(5)]
        public IEnumerator Test5_ScriptableObjectScript()
        {
            var movement = Services.Get<IMovementInputProvider>();
            Assert.IsTrue(movement != null);
            yield break;
        }
        
        [UnityTest, Order(6)]
        public IEnumerator Test6_Tag()
        {
            var avatar = Services.Get<Transform>("Avatar");
            Assert.IsTrue(avatar.GetComponent<Avatar>() != null);
            yield break;
        }

    }
}
