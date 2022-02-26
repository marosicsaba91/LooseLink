using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class BallisticsManager : MonoBehaviour
{
    readonly Dictionary<Projectile, List<Projectile>> pools = new Dictionary<Projectile, List<Projectile>>();

    public void Shoot(Vector3 start, Vector3 destination, Projectile prototype)
    {
        Projectile projectile = GetProjectileFromPool(prototype);
        projectile.gameObject.SetActive(value: true);
        projectile.Shoot(start, destination);
    }

    Projectile GetProjectileFromPool(Projectile prototype)
    {
        List<Projectile> pool = GetPool( prototype);

        Projectile projectile;
        if (pool.Count > 0)
        {
            projectile = pool.Last();
            pool.RemoveAt(pool.Count - 1);
            return projectile;
        }
        
        GameObject newInstance = Instantiate(prototype.gameObject, transform);
        projectile = newInstance.GetComponent<Projectile>();
        projectile.Setup(prototype, this);
        return projectile;
    }

    public void PutPrototypeBackInPool(Projectile projectile)
    {
        Projectile prototype = projectile.Prototype;
        List<Projectile> pool = GetPool( prototype);
        projectile.gameObject.SetActive(value: false);
        pool.Add(projectile);
    }
    
    public  List<Projectile> GetPool(Projectile prototype)
    { 
        if (!pools.TryGetValue(prototype,out List<Projectile> pool))
        {
            pool = new List<Projectile>();
            pools.Add(prototype, pool); 
        }
        return pool;
    }
    
        
    public static Vector3 CalculateShootDirection(Vector3 start, Vector3 destination, float startVelocity)
    { 
        Vector3 distanceVec = destination - start; 
        var horizontalDirection = new Vector3(distanceVec.x, 0, distanceVec.z);
        float angel = CalculateAngle(distanceVec, startVelocity);
        Vector3 direction = Vector3.RotateTowards(
            horizontalDirection.normalized, 
            Vector3.up, 
            angel* Mathf.Deg2Rad,
            1); 
        return direction * startVelocity;
    }

    public static float CalculateAngle(Vector3 distanceVec, float startVelocity)
    {
        const float maxAngle = 89;
        const float minAngle = 1;
        const float angleStep = 1;
        
        float g = Physics.gravity.magnitude; 
        float dx = new Vector2(distanceVec.x, distanceVec.z).magnitude;
        float targetH = distanceVec.y;
        var difference = float.MaxValue;
        for (float alpha = maxAngle; alpha >= minAngle; alpha-= angleStep)
        {
            float vx = Mathf.Cos(alpha * Mathf.Deg2Rad) * startVelocity;
            float vy = Mathf.Sin(alpha * Mathf.Deg2Rad) * startVelocity;
            float tx = dx / vx;
            float deltaH = (vy * tx) - (tx * tx* g/2);
            float d = Mathf.Abs(deltaH - targetH);
            if (d > difference)
                return alpha + angleStep;
            difference = d;
        }

        return minAngle;
    }

    
}