using System;
using LooseLink;
using UnityEngine;

[ServiceType]
public class UpdateProvider : MonoBehaviour
{
    public event Action OnUpdate;
    public event Action OnFixedUpdate;
    public event Action OnLateUpdate; 

    public void Update()
    {
        OnUpdate?.Invoke();
    }

    void LateUpdate()
    {
        OnLateUpdate?.Invoke();
    }

    void FixedUpdate()
    {
        OnFixedUpdate?.Invoke();
    }
}