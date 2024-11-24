Voxelman
========

![gif](https://github.com/keijiro/Voxelman/assets/343936/007c60af-dc88-4137-af3d-41820a681761)
![gif](https://github.com/keijiro/Voxelman/assets/343936/8f6e6a29-cf0e-4a8b-afc1-8a87804cb518)

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

1. 地面上加了横竖两条浅绿色的基准线
2. 显示两个小人，一个玫红色，一个黑金mesh
3. 鼠标左键，改变box颜色和振幅（不太明显）
4. 键盘adws分别使box向左右前后移动
5. 动态调整饱和度和亮度
6. 改变体素生成的随机分布，使其呈现环形分布(SpawnerSystem.cs)
---------------------