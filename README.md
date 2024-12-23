Voxelman
========
Two demo videos are available on YouTube, one in fixed view and another in camera view. You could click on the following videos for more details.

[![Video 1](https://img.youtube.com/vi/PblCDjdS0j4/0.jpg)](https://www.youtube.com/watch?v=PblCDjdS0j4)


[![Video 2](https://img.youtube.com/vi/6m3dnMEybCE/0.jpg)](https://www.youtube.com/watch?v=6m3dnMEybCE)



**Voxelman** is an example that shows how to use the new **[Entity Component
System]** with Unity in an extreme way. Each voxel in the scene is instantiated
as an entity, and controlled by component systems. It also utilizes the **C#
Job System**, the **Burst Compiler** and the **asynchronous raycast** to hit
the maximum efficiency of multi-core processors.

[Entity Component System]: https://unity.com/ecs

System requirements
-------------------

- Unity 20222 LTS

## Refinements

1. Add two light green reference lines on the ground, one horizontal and one vertical.
2. Display two characters, one magenta and one black-gold mesh.
3. Left mouse button: change the box color and amplitude (not very noticeable).
4. Keyboard keys A, D, W, and S move the box left, right, forward, and backward, respectively.
5. Dynamically adjust saturation and brightness.
6. Modify the random distribution of voxels to form a ring shape (in SpawnerSystem.cs).
7. Add outline effects to both characters.
8. When particles collide, reduce their size and deepen their color.
9. Add a halo effect to each character to trace their dance trajectory.
10. Added tornado effect to the voxels.

---------------------

## How to Run

You can run the animation by simply downloading the whole project and open it up with the correct Unity version.