Voxelman
========

<!-- add two local videos -->
<video width="800" controls>
    <source src="./video/video1.mp4" type="video/mp4">
</video>

<br>
<video width="800" controls>
    <source src="./video/video2.mp4" type="video/mp4">
</video>


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
7. 对两个小人添加描边效果
8. 当粒子碰撞时，缩小它们的体积，加深颜色
9. 对每个小人添加光圈，追踪小人跳舞轨迹
---------------------