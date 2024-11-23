using Unity.Entities;
using UnityEngine;

public class VoxelizerAuthoring : MonoBehaviour
{
    [SerializeField] float _voxelSize = 0.05f;
    [SerializeField] float _voxelLife = 0.3f;
    [SerializeField] float _colorFrequency = 0.5f;
    [SerializeField] float _colorSpeed = 0.5f;
    [SerializeField] float _gravity = 0.2f;

    // add a elasticity parameter
    [SerializeField] float _elasticity = 0.1f;

    // add a friction parameter
    [SerializeField] float _friction = 0.1f;

    class Baker : Baker<VoxelizerAuthoring>
    {
        public override void Bake(VoxelizerAuthoring src)
        {
            var data = new Voxelizer()
            {
                VoxelSize = src._voxelSize,
                VoxelLife = src._voxelLife,
                ColorFrequency = src._colorFrequency,
                ColorSpeed = src._colorSpeed,
                Gravity = src._gravity,
                Elasticity = src._elasticity,
                Friction = src._friction
            };
            AddComponent(GetEntity(TransformUsageFlags.None), data);
        }
    }
}

public struct Voxelizer : IComponentData
{
    public float VoxelSize;
    public float VoxelLife;
    public float ColorFrequency;
    public float ColorSpeed;
    public float Gravity;
    public float Elasticity;
    public float Friction;
}
