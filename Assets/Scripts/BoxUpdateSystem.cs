using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

[BurstCompile(CompileSynchronously = true)]
public partial struct BoxUpdateSystem : ISystem
{
    private Vector4 _newColor; // Used to store the new color
    private float _waveAmplitude; // Initial amplitude
    private float _moveSpeed; // Movement speed

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _newColor = (Vector4)Color.white; // Initialize to white
        _waveAmplitude = 1.0f; // Initialize amplitude
        _moveSpeed = 10.0f; // Initialize movement speed
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<Voxelizer>()) return;

        var writer = 
          SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
          .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        // Detect left mouse click
        if (Input.GetMouseButtonDown(0))
        {
             // Generate a relatively nice random color (light color)
            _newColor = (Vector4)UnityEngine.Random.ColorHSV(0.0f, 1.0f, 0.5f, 1.0f, 0.8f, 1.0f);
            // Randomly change amplitude
            _waveAmplitude = UnityEngine.Random.Range(1.0f, 100.0f); 
            // Output the new amplitude
            Debug.Log($"New wave amplitude: {_waveAmplitude}"); 
        }

        // Keyboard control movement
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
            NewColor = _newColor, // Pass the color to the Job
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
    public Vector4 NewColor; // New field
    public float MoveX; // Movement in X direction
    public float MoveZ; // Movement in Z direction

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

        // Rotation
        var rotationSpeed = 90.0f; // Rotation angle per second
        xform.Rotation = math.mul(xform.Rotation, quaternion.RotateY(rotationSpeed * DeltaTime));
        // Spiral
        var spiralSpeed = 0.8f; // Horizontal movement speed
        var spiralFrequency = 3.0f; // Spiral motion frequency
        xform.Position.x += spiralSpeed * math.sin(Time * spiralFrequency) * DeltaTime;
        xform.Position.z += spiralSpeed * math.cos(Time * spiralFrequency) * DeltaTime;

        // Scaling animation
        var p01 = box.Time / Voxelizer.VoxelLife;
        var p01_2 = p01 * p01;
        xform.Scale = Voxelizer.VoxelSize * (1 - p01_2 * p01_2);

        // Dynamic adjustment of HSV
        float hue, saturation, value;
        Color.RGBToHSV((Color)NewColor, out hue, out saturation, out value);

        // Dynamically adjust saturation and brightness
        saturation = 0.7f + 0.3f * math.sin(Time * 3.0f); // Saturation fluctuates between 0.3 and 3
        value = 0.6f + 0.4f * math.cos(Time * 2.5f);      // Brightness fluctuates between 0.4 and 2.5
        // Enhance dynamic brightness on collision
        if (xform.Position.y < 0.1f && math.abs(box.Velocity) > 0.1f)
        {
            value += 0.2f; // Enhance brightness change
            xform.Scale *= 1.2f; // Enhance scaling effect
        }
        
        // When two particles collide, darken and blacken the color and reduce the size
        if (xform.Position.y < 0.1f && math.abs(box.Velocity) > 0.1f)
        {
            saturation *= 0.5f; // Significantly reduce saturation
            value *= 0.3f; // Significantly reduce brightness
            xform.Scale *= 0.8f; // Reduce size
        }

        // Convert HSV back to RGB
        var dynamicColor = (Vector4)Color.HSVToRGB(hue, saturation, value);

        // Set color
        color.Value = dynamicColor;

        // New Visual Feedback: Add a brief scale-up effect when the box hits the ground
        if (xform.Position.y < 0.1f && math.abs(box.Velocity) > 0.1f)
        {
            xform.Scale *= 1f; // Temporarily scale up to emphasize the bounce
        }
    }
}
