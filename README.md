# UnityGrassRenderingIndirectExample

This is a hobby project I created to learn how to use compute shaders and GPU APIs like **[Graphics.DrawMeshInstancedIndirect](https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstancedIndirect.html)** to efficiently render large amounts of grass on a terrain.


https://github.com/EricHu33/UnityGrassIndirectRenderingExample/assets/13420668/a5110d44-2f3a-474e-a7a6-467755b2f99b

## How To Use
1. add **Indirect Draw Controller** componnent to your terrain game object, other required component will automatically add.

![image](https://github.com/EricHu33/UnityGrassIndirectRenderingExample/assets/13420668/5b248e93-3421-4ead-9c65-7234c201279e)

2. on the terrain inspector, choose **magic grass detail painter**, this painter can paint the terrain detail into select channel.

![image](https://github.com/EricHu33/UnityGrassIndirectRenderingExample/assets/13420668/4e838025-4736-4b9f-abbb-61012be976a8)

3. in the **Indirect Draw Controller** Component inspector, Add Magic Grass Render Model scriptable object into the model list, each model is pair with 1 detail map's channel,
For example, the first model will pair with R channel of detail map, the second model will pair with G channel of the detail map.

![image](https://github.com/EricHu33/UnityGrassIndirectRenderingExample/assets/13420668/bad867ee-25e8-484b-bae3-d6a3d9164848)

4. you can create Magic Grass Render Model scriptable object by right click on the project window and select **Create/MagicGrass/MagicGrassRenderModel**

![image](https://github.com/EricHu33/UnityGrassIndirectRenderingExample/assets/13420668/f5b0ff4c-f289-44fa-9aee-ba489494febf)

### Right now, the tool and script provided in the project has below features :

- Runtime grass painting on terrain (store the painting result even in playmode)
- Multiple grass types on a single terrain
- Hi-Z culling (based on **[https://github.com/ellioman/Indirect-Rendering-With-Compute-Shaders](https://github.com/ellioman/Indirect-Rendering-With-Compute-Shaders)**)
- CPU-side culling (using **[https://docs.unity3d.com/Manual/CullingGroupAPI.html](https://docs.unity3d.com/Manual/CullingGroupAPI.html)**)
- Interactive grass (interaction status is stored in a render texture, processed by another compute shader)
- Grass direction aligns with the terrain's surface normals
- Procedually spawn grass via selectable terrain layer
- Procedually exclude grass via selectable terrain layer
![image](https://github.com/EricHu33/UnityGrassIndirectRenderingExample/assets/13420668/9539d0f3-9d1f-4335-b44d-53551567ce22)

## Here are the grass shader's features:

- All the grass motion and lighting can adjust along uv1(the second uv channel)'s y value. In short - the grass mesh's uv1 layout shoud be a straight line along Y direction.
- Custom code shader/ shader graph shader that support DrawInstancedIndirect.
- Distance-based scaling (grass grows when within a certain distance, the concept is similar to the implementation in The Legend of Zelda: Breath of the Wild)
- Wind, specular, translucent, and fake ambient occlusion effects
- Grass Color Tinting based on terrain surface color
- World-position-based color variation
- Random Height/Scale variation option
![Store Img (24)](https://github.com/EricHu33/UnityGrassIndirectRenderingExample/assets/13420668/01e824fe-a577-4a42-bc7b-f7048b1a2f8f)



## This is not a production-ready project:

- While it runs MUCH faster than regular instancing. It’s far from perfect. the culling logic and memory usage still has lots of space to improve. Also, LOD is not support yet . A good LOD impl  can save tons of perf as well. This project only serve as an example of using compute shader and how to working with unity terrain.
- For production-ready foliage renderer please I recommend use foliage renderer or advance terrain grass plugin.

## About the overall implementation :

The overall logic structure is similar as this video

[モバイル向け大量描画テクニック](https://www.youtube.com/watch?v=mmxpPDVskg0&t=187s)

### APIs I use
For generating a massive amount of foliage on terrain in Unity, **[Graphics.DrawMeshInstancedIndirect](https://docs.unity3d.com/ScriptReference/Graphics.DrawMeshInstancedIndirect.html)**(and RenderMeshIndirect) offers the fastest approach. While regular GPU instancing in Unity limitseach batch to 1023 objects (or fewer depending on the device), InstancedIndirect bypasses this restriction, allowing us to draw significantly more meshes with a single call.

### Logic

The key logic is describe as follow image. 

![Untitled](UnityGrassRenderingIndirectExample%20ae21e9fc54a44429a2f6ef889a8e6084/Untitled.png)

1. Split the terrain into 16 cells
2. **Cull-Unvisible Cells:** This is a crucial optimization step. Before performing GPU-based Hi-Z culling, we perform a preliminary cull on the CPU. This involves identifying and discarding cells that are not visible to the camera. Since feeding all grass data into a compute buffer, even for invisible cells, would consume a significant amount of VRAM. By efficiently culling cells on the CPU, we drastically reduce VRAM usage.
3. **Per-Cell Grass Sampling:** For each visible cell:
    - Utilize a compute shader to sample the terrain's detail map. The detail map defines the position and type of grass on the terrain.
    - Parameters within the compute shader allow us to further fine-tune the rotation and density of the grass within the cell.
    - The sampled grass data for the cell is then transferred into a compute buffer.
4. **Hi-Z Culling:**
    - The compute buffer containing the grass data for all visible cells is passed to a specialized Hi-Z culling compute shader.
    - This shader efficiently discards grass instances that are occluded by other geometry in the scene, along side frustum culling, further reducing the number of instances to be drawn.
    - An append buffer is used to collect the visible grass instances.
5. Draw the grass instances with **DrawMeshInstancedIndirect.**

Some side note :

- To ensure the grass blades always point upwards of the terrain's surface normal, I need to calculate the terrain's normal texture when the game starts. This normal texture is generated within a shader using the terrain's heightmap as input.
- grass interaction is done by render the intereactable object’s influence into a Render texture(interactable map), using interactable object’s world position as uv. This step is perform in another compute shader, but wheather to use compute shader is optional. Since its totally doable using regular fragment shader to render the interactable map. Then, the grass shader will sample the interactable map with it’s world positon therefore create a illusion that objects interact with the grass.
- The detailed map is store under unity’s terrain assets as a sub-asset. When entering the game it will become a texture 2d array via script.
- Alhpa-test has a major performance hit, a simple mesh grass without alpha clipping can nearly double the framerate.
- For grass shader, there's two version of it, one is pure custom code shader, the other one is shadergraph, There's some custom node trikery inside it to make shadergraph shader compatible with IndirectInstancing. But it works. 

## Summary :

Based on the commit history, I wrote the project around 1 and half year ago. At that time I learned many things when working on this small project. Such as how Unity terrain and how splat map works(and why unity’s terrain is kinda slow). 

I also found that I blew up my vram before I implement the cpu-side culling and split the world with cells. Rightnow it use CullingGroup API since it’s dead-simple to implement, but a quadtree or use burst compiler with frustum culling on cpu side is also worth trying in the future.

I put a lot of time implement all the shading logic/paramter of the grass shader (And the code is pretty dirty now….). Ghost of Tsushima GDC talk is my main inspiration.

## References :
[モバイル向け大量描画テクニック](https://www.youtube.com/watch?v=mmxpPDVskg0&t=187s)

[Procedural Grass in 'Ghost of Tsushima'](https://www.youtube.com/watch?v=Ibe1JBF5i5Y)


## Licenses Notice:
The assets under folder [TerrainSampleAssets] is from Unity's Terrain Sample project, which is govern by [Unity's UCL](https://unity.com/legal/licenses/unity-companion-license) 
