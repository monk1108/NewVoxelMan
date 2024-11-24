using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

[BurstCompile(CompileSynchronously = true)]
public partial struct SpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;

        foreach (var (spawner, xform) in
                 SystemAPI.Query<RefRW<Spawner>,
                                 RefRO<LocalTransform>>())
        {

            var world = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;

            // Timer update
            // 增加频率随时间动态变化的规则
            var dynamicFrequency = spawner.ValueRO.Frequency * (1 + 0.5f * math.sin((float)SystemAPI.Time.ElapsedTime));

            var nt = spawner.ValueRO.Timer + dynamicFrequency * dt;
            var count = (int)nt;
            spawner.ValueRW.Timer = nt - count;

            // Raycast base
            var p0 = xform.ValueRO.Position;
            var ext = spawner.ValueRO.Extent;

            // Collision filter
            var filter = CollisionFilter.Default;
            filter.CollidesWith = (uint)spawner.ValueRO.Mask;

            // Racast loop
            for (var i = 0; i < count; i++)
            {
                // 改变体素生成的随机分布，使其呈现环形分布
                var angle = spawner.ValueRW.Random.NextFloat(0, math.PI * 2); // 随机角度
                var radius = spawner.ValueRW.Random.NextFloat(0, math.length(ext.xy)); // 随机半径
                var disp = math.float2(math.cos(angle) * radius, math.sin(angle) * radius);

                var vox = SystemAPI.GetSingleton<Voxelizer>();
                disp = math.floor(disp / vox.VoxelSize) * vox.VoxelSize;

                var ray = new RaycastInput()
                  { Start = p0 + math.float3(disp, -ext.z),
                    End   = p0 + math.float3(disp, +ext.z),
                    Filter = filter };

                var hit = new RaycastHit();
                if (!world.CastRay(ray, out hit)) continue;

                // Prefab instantiation
                var spawned = state.EntityManager.Instantiate(spawner.ValueRO.Prefab);
                var cx = SystemAPI.GetComponentRW<LocalTransform>(spawned);
                cx.ValueRW.Position = hit.Position;
            }
        }
    }
}
