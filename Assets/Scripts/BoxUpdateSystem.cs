using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[BurstCompile(CompileSynchronously = true)]
public partial struct BoxUpdateSystem : ISystem
{
    private Vector4 _newColor;
    private float _waveAmplitude;
    private float _moveSpeed;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _newColor = (Vector4)Color.white;
        _waveAmplitude = 1.0f;
        _moveSpeed = 10.0f;
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<Voxelizer>()) return;

        var writer =
          SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
          .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        // Mouse click for color change
        if (Input.GetMouseButtonDown(0))
        {
            _newColor = (Vector4)UnityEngine.Random.ColorHSV(0.0f, 1.0f, 0.5f, 1.0f, 0.8f, 1.0f);
            _waveAmplitude = UnityEngine.Random.Range(1.0f, 100.0f);
            Debug.Log($"New wave amplitude: {_waveAmplitude}");
        }

        // Keyboard movement
        float moveX = 0f;
        float moveZ = 0f;
        float dt = SystemAPI.Time.DeltaTime;

        if (Input.GetKey(KeyCode.W)) moveZ += _moveSpeed * dt;
        if (Input.GetKey(KeyCode.S)) moveZ -= _moveSpeed * dt;
        if (Input.GetKey(KeyCode.A)) moveX -= _moveSpeed * dt;
        if (Input.GetKey(KeyCode.D)) moveX += _moveSpeed * dt;

        // Wind force
        float3 windDirection = new float3(0, 1, 1);
        float windStrength = 10.0f;
        if (SystemAPI.HasSingleton<WindForce>())
        {
            var wind = SystemAPI.GetSingleton<WindForce>();
            windDirection = wind.Direction;
            windStrength = wind.Strength;
        }

        // show in console log what's the wind direction and strength
        Debug.Log($"Wind direction: {windDirection}, Wind strength: {windStrength}");

        var job = new BoxUpdateJob()
        {
            Commands = writer,
            Voxelizer = SystemAPI.GetSingleton<Voxelizer>(),
            Time = (float)SystemAPI.Time.ElapsedTime,
            DeltaTime = dt,
            NewColor = _newColor,
            MoveX = moveX,
            MoveZ = moveZ,
            WindDirection = windDirection,
            WindStrength = windStrength
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
    public Vector4 NewColor;
    public float MoveX;
    public float MoveZ;
    public float3 WindDirection;
    public float WindStrength;

    void Execute([ChunkIndexInQuery] int index,
                 Entity entity,
                 ref LocalTransform xform,
                 ref Box box,
                 ref URPMaterialPropertyBaseColor color)
    {
        box.Time += DeltaTime;

        // Expiration check
        if (box.Time > Voxelizer.VoxelLife)
        {
            Commands.DestroyEntity(index, entity);
            return;
        }

        // Gravity as a vector
        float3 gravityVector = new float3(0, -Voxelizer.Gravity, 0);

        // Update velocity with gravity
        box.Velocity += gravityVector * DeltaTime;

        // Add wind force
        box.Velocity += WindDirection * WindStrength * DeltaTime;

        // Update position
        xform.Position += box.Velocity * DeltaTime;

        // Apply user movement (direct position shift)
        xform.Position.x += MoveX;
        xform.Position.z += MoveZ;

        // Ground collision
        if (xform.Position.y < 0)
        {
            // Reflect vertical velocity
            box.Velocity.y = -box.Velocity.y * Voxelizer.Elasticity;

            // Fix position above ground
            xform.Position.y = -xform.Position.y;

            // Apply friction to horizontal velocity
            box.Velocity.x *= Voxelizer.Friction;
            box.Velocity.z *= Voxelizer.Friction;
        }

        // Rotation around Y axis
        var rotationSpeed = 90.0f;
        xform.Rotation = math.mul(xform.Rotation, quaternion.RotateY(rotationSpeed * DeltaTime));

        // Spiral motion
        var spiralSpeed = 0.8f;
        var spiralFrequency = 3.0f;
        xform.Position.x += spiralSpeed * math.sin(Time * spiralFrequency) * DeltaTime;
        xform.Position.z += spiralSpeed * math.cos(Time * spiralFrequency) * DeltaTime;

        // Scale over lifetime
        var p01 = box.Time / Voxelizer.VoxelLife;
        var p01_2 = p01 * p01;
        xform.Scale = Voxelizer.VoxelSize * (1 - p01_2 * p01_2);

        // Dynamic HSV color changes
        float hue, saturation, value;
        Color.RGBToHSV((Color)NewColor, out hue, out saturation, out value);

        saturation = 0.7f + 0.3f * math.sin(Time * 3.0f);
        value = 0.6f + 0.4f * math.cos(Time * 2.5f);

        // Velocity magnitude for collision effects
        float speedMagnitude = math.length(box.Velocity);
        if (xform.Position.y < 0.1f && speedMagnitude > 0.1f)
        {
            value += 0.2f;
            xform.Scale *= 1.2f;
        }

        // Further darken on collision
        if (xform.Position.y < 0.1f && speedMagnitude > 0.1f)
        {
            saturation *= 0.5f;
            value *= 0.3f;
            xform.Scale *= 0.8f;
        }

        var dynamicColor = (Vector4)Color.HSVToRGB(hue, saturation, value);
        color.Value = dynamicColor;

        if (xform.Position.y < 0.1f && speedMagnitude > 0.1f)
        {
            xform.Scale *= 1f;
        }
    }
}
