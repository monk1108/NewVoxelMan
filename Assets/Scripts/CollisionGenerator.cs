using UnityEngine;
using UnityEngine.Profiling;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using MeshCollider = Unity.Physics.MeshCollider;
using UnityRandom = UnityEngine.Random;
using Material = UnityEngine.Material;

[BurstCompile(CompileSynchronously = true)]
public class CollisionGenerator : MonoBehaviour
{
    #region Editable attributes

    [SerializeField] private SkinnedMeshRenderer _source = null; // 地面网格源
    [SerializeField] private float waveAmplitude = 2f; // 波浪高度
    [SerializeField] private float waveFrequency = 0.5f; // 波浪频率
    [SerializeField] private bool isControllable = false; // 该小人是否可被控制
    [SerializeField] private Material controllableMaterial; // 可控小人材质
    [SerializeField] private Material defaultMaterial; // 默认小人材质

    #endregion

    #region Private fields

    private Mesh _mesh;
    private Entity _entity;
    private SkinnedMeshRenderer _renderer;

    #endregion

    #region DOTS interop

    [BurstCompile]
    static void CreateCollider
      (in EntityManager manager,
       in Entity entity,
       in NativeArray<float3> vtx,
       in NativeArray<int3> idx,
       int layer)
    {
        var filter = CollisionFilter.Default;
        filter.CollidesWith = (uint)layer;

        var collider = MeshCollider.Create(vtx, idx, filter);
        manager.SetComponentData(entity, new PhysicsCollider { Value = collider });
    }

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _mesh = new Mesh();
        _renderer = _source;

        // Entity allocation
        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var componentTypes = new ComponentType[]
        {
            typeof(LocalTransform),
            typeof(PhysicsCollider),
            typeof(PhysicsWorldIndex)
        };
        _entity = manager.CreateEntity(componentTypes);

        // Physics world index initialization
        var world = new PhysicsWorldIndex { Value = 0 };
        manager.AddSharedComponentManaged(_entity, world);

        // 设置材质
        if (isControllable)
        {
            _renderer.material = controllableMaterial;
        }
        else
        {
            _renderer.material = defaultMaterial;
        }
    }

    void Update()
    {
        // 更新地面波浪效果
        UpdateGroundWave();

        // 绘制参照线
        Debug.DrawLine(new Vector3(-50, 0, 0), new Vector3(50, 0, 0), Color.green);
        Debug.DrawLine(new Vector3(0, 0, -50), new Vector3(0, 0, 50), Color.green);
    }

    void UpdateGroundWave()
    {
        // Skinned mesh baking
        Profiler.BeginSample("BakeMesh");
        _source.BakeMesh(_mesh);
        Profiler.EndSample();

        // Simulate ground cracking
        var vertices = _mesh.vertices;
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i].y += Mathf.Sin(Time.time * waveFrequency + vertices[i].x * 0.5f) * waveAmplitude;
        }
        _mesh.vertices = vertices;
        _mesh.RecalculateNormals();

        // Vertex/index array retrieval
        using var vtx = new NativeArray<Vector3>(_mesh.vertices, Allocator.Temp);
        using var idx = new NativeArray<int>(_mesh.triangles, Allocator.Temp);
        var vtx_re = vtx.Reinterpret<float3>();
        var idx_re = idx.Reinterpret<int3>(sizeof(int));

        // Transform update
        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var xform = new LocalTransform
        {
            Position = _source.transform.position,
            Rotation = _source.transform.rotation,
            Scale = _source.transform.localScale.x
        };
        manager.SetComponentData(_entity, xform);

        Profiler.BeginSample("MeshCollider Update");
        CreateCollider(manager, _entity, vtx_re, idx_re, gameObject.layer);
        Profiler.EndSample();
    }

    #endregion
}
