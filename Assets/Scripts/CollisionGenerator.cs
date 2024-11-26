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

    [SerializeField] private SkinnedMeshRenderer _source = null; // Ground mesh source
    [SerializeField] private float waveAmplitude = 2f; // Wave height
    [SerializeField] private float waveFrequency = 0.5f; // Wave frequency
    [SerializeField] private bool isControllable = false; // Whether this character can be controlled
    [SerializeField] private Material controllableMaterial; // Material for controllable character
    [SerializeField] private Material defaultMaterial; // Default character material
    [SerializeField] private Material outlineMaterial; // Character outline material (new material property)
    [SerializeField] private GameObject glowCirclePrefab; // Glow circle prefab (new property)
    [SerializeField] private GameObject quadObject; // Initial Quad object (new property)

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

        // Set material
        if (isControllable)
        {
            _renderer.material = controllableMaterial;
        }
        else
        {
            _renderer.material = defaultMaterial;
        }

        // Hide the initial Quad object
        // if (quadObject != null)
        // {
        //     quadObject.SetActive(false);
        // }

        // Apply outline effect (new feature)
        ApplyOutlineEffect();
    }

    void Update()
    {
        // Update ground wave effect
        UpdateGroundWave();

        // Draw glow circle effect (new feature)
        DrawGlowCircle();

        // Draw reference lines
        Debug.DrawLine(new Vector3(-50, 0, 0), new Vector3(50, 0, 0), Color.green);
        Debug.DrawLine(new Vector3(0, 0, -50), new Vector3(0, 0, 50), Color.green);
    }

    void UpdateGroundWave()
    {
        // Skinned mesh baking
        Profiler.BeginSample("BakeMesh");
        _source.BakeMesh(_mesh);
        Profiler.EndSample();

        // Simulate ground cracks
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

    void DrawGlowCircle()
    {
        // Get character position and draw glow circle on the ground
        Vector3 characterPosition = _source.transform.position;
        if (glowCirclePrefab != null)
        {
            GameObject glowCircle = Instantiate(glowCirclePrefab, new Vector3(characterPosition.x, 0f, characterPosition.z), Quaternion.Euler(0, 0, 0));
            glowCircle.transform.localScale = new Vector3(1f, -0.01f, 1f); // Adjust glow circle size
            Destroy(glowCircle, 0.05f); // Ensure the glow circle disappears after a while
        }
    }

    void ApplyOutlineEffect()
    {
        // Create a new outline object by copying the SkinnedMeshRenderer (new feature)
        GameObject outlineObject = new GameObject("Outline");
        outlineObject.transform.SetParent(_source.transform, false);
        outlineObject.transform.localPosition = Vector3.zero;
        outlineObject.transform.localRotation = Quaternion.identity;
        outlineObject.transform.localScale = Vector3.one;

        SkinnedMeshRenderer outlineRenderer = outlineObject.AddComponent<SkinnedMeshRenderer>();
        outlineRenderer.sharedMesh = _source.sharedMesh;
        outlineRenderer.material = outlineMaterial; // Using outline material
        outlineRenderer.rootBone = _source.rootBone;
        outlineRenderer.bones = _source.bones;

        // Set the outline effect to render behind the original mesh by adjusting the render queue (new feature)
        outlineRenderer.material.SetFloat("_OutlineWidth", 0.03f); // Outline width
        outlineRenderer.material.renderQueue = 3000; // Ensure the outline renders after the main object
    }

    #endregion
}
