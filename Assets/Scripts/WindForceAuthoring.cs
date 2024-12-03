using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class WindForceAuthoring : MonoBehaviour
{
    [SerializeField] private float3 _windDirection = new float3(0, 0, 1);
    [SerializeField] private float  _windStrength = 1000.0f;

    class Baker : Baker<WindForceAuthoring>
    {
        public override void Bake(WindForceAuthoring src)
        {
            var data = new WindForce
            {
                Direction = math.normalize(src._windDirection),
                Strength = src._windStrength
            };
            AddComponent(GetEntity(TransformUsageFlags.None), data);
        }
    }
}

public struct WindForce : IComponentData
{
    public float3 Direction;
    public float Strength;
}
