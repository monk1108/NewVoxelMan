using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[BurstCompile(CompileSynchronously = true)]
public partial struct BoxUpdateSystem : ISystem
{
    private Vector4 _newColor; // 用于存储新的颜色
    private float _waveAmplitude; // 初始振幅
    private float _moveSpeed; // 移动速度

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _newColor = (Vector4)Color.white; // 初始化为白色
        _waveAmplitude = 1.0f; // 初始化振幅
        _moveSpeed = 10.0f; // 初始化移动速度
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<Voxelizer>()) return;

        var writer = 
          SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
          .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        // 检测鼠标左键点击
        if (Input.GetMouseButtonDown(0))
        {
            _newColor = (Vector4)UnityEngine.Random.ColorHSV(0.0f, 1.0f, 0.5f, 1.0f, 0.8f, 1.0f); // 生成一个较为好看的随机颜色（浅色系）
            _waveAmplitude = UnityEngine.Random.Range(1.0f, 100.0f); // 随机改变振幅
            Debug.Log($"New wave amplitude: {_waveAmplitude}"); // 输出新的振幅
        }

        // 键盘控制移动
        float moveX = 0f;
        float moveZ = 0f;

        if (Input.GetKey(KeyCode.W))
        {
            moveZ += _moveSpeed * SystemAPI.Time.DeltaTime;
            Debug.Log("Moving forward");
        }
        if (Input.GetKey(KeyCode.S))
        {
            moveZ -= _moveSpeed * SystemAPI.Time.DeltaTime;
            Debug.Log("Moving backward");
        }
        if (Input.GetKey(KeyCode.A))
        {
            moveX -= _moveSpeed * SystemAPI.Time.DeltaTime;
            Debug.Log("Moving left");
        }
        if (Input.GetKey(KeyCode.D))
        {
            moveX += _moveSpeed * SystemAPI.Time.DeltaTime;
            Debug.Log("Moving right");
        }

        var job = new BoxUpdateJob()
        {
            Commands = writer,
            Voxelizer = SystemAPI.GetSingleton<Voxelizer>(),
            Time = (float)SystemAPI.Time.ElapsedTime,
            DeltaTime = SystemAPI.Time.DeltaTime,
            NewColor = _newColor, // 将颜色传递给Job
            MoveX = moveX,
            MoveZ = moveZ
        };

        job.ScheduleParallel();
    }
}

[BurstCompile(CompileSynchronously = true)]
partial struct BoxUpdateJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Commands;
    public Voxelizer Voxelizer;
    public float Time;
    public float DeltaTime;    
    public Vector4 NewColor; // 新增的字段
    public float MoveX; // X方向移动
    public float MoveZ; // Z方向移动

    void Execute([ChunkIndexInQuery] int index,
                 Entity entity,
                 ref LocalTransform xform,
                 ref Box box,
                 ref URPMaterialPropertyBaseColor color)
    {
        // Time step
        box.Time += DeltaTime;

        // Expiration
        if (box.Time > Voxelizer.VoxelLife)
        {
            Commands.DestroyEntity(index, entity);
            return;
        }

        // Dynamics
        box.Velocity -= Voxelizer.Gravity * DeltaTime;
        xform.Position.y += box.Velocity * DeltaTime;

        // Apply user movement
        xform.Position.x += MoveX;
        xform.Position.z += MoveZ;

        // Reflection on the ground
        if (xform.Position.y < 0)
        {
            box.Velocity *= -1 * Voxelizer.Elasticity;
            xform.Position.y = -xform.Position.y;

            // horizontal friction
            xform.Position.x *= Voxelizer.Friction;
            xform.Position.z *= Voxelizer.Friction;
        }

        // Scaling animation
        var p01 = box.Time / Voxelizer.VoxelLife;
        var p01_2 = p01 * p01;
        xform.Scale = Voxelizer.VoxelSize * (1 - p01_2 * p01_2);

        // 设置颜色为新的颜色
        color.Value = NewColor;

        // New Visual Feedback: Add a brief scale-up effect when the box hits the ground
        if (xform.Position.y < 0.1f && math.abs(box.Velocity) > 0.1f)
        {
            xform.Scale *= 5f; // Temporarily scale up to emphasize the bounce
        }
    }
}
