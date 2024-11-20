# GPU Sprite Instancing :zap:

## I. Overview

* GPUSpriteInstancing is a Unity package that provides efficient GPU-based sprite rendering using compute buffers and instancing
* Optimized with Unity Burst compiler and Job System for high-performance sprite rendering

<br />

<p style="text-align:center;">
  <img src="/Assets/DocResources/Gifs/SampleDemo.gif" width="1000">
</p>

**<center>Sample demo running on Mac with 1,000,000 instances</center>**
<center>Free sprite resource <a href="https://cupnooble.itch.io/sprout-lands-asset-pack">here</center>
## II. Features

  <details>
  <summary>Core Features</summary>
  
  * GPU Instanced Sprite Rendering
  * Batched Drawing System
  * Sprite Atlas Support
  * Memory Management
  * Buffer Management
  * Job System Integration
  * Burst Compilation
  </details>

  <details>
  <summary>Components</summary>

  * GPU Sprite Manager
  * Instance Renderer
  * Custom Shader
  * Sprite Data Structure
  </details>

## III. Setup

### **>** Requirements

* Unity 6000.0.24 or higher
* Burst Package
* Mathematics Package

### **>** Installation

1. Open Package Manager from Window > Package Manager
2. Click on the "+" button > Add package from git URL
3. Enter the following URL:

```
https://github.com/centhoang/GPUSpriteInstancing.git?path=/Assets/GPUSpriteInstancing
```

Or open Packages/manifest.json and add the following to the dependencies block:

```json
{
    "dependencies": {
        "com.centhoang.gpu_sprite_instancing": "https://github.com/centhoang/GPUSpriteInstancing.git?path=/Assets/GPUSpriteInstancing"
    }
}
```

> [!IMPORTANT]
> Use `tag(#)` to avoid auto-fetching undesired version
>
> <sub>Example</sub>
> ```
> https://github.com/centhoang/GPUSpriteInstancing.git?path=/Assets/GPUSpriteInstancing#1.1.0
> ```

### **>** Intergration

1. Add a **GPUSpriteManager** component to a GameObject in your scene
2. Assign the **GPUSpriteInstancingMat** material (or your material which suits the system) to the manager
3. Create your sprite atlas texture
4. Use `GPUSpriteManager.UpdateSprites()` to render your sprites

###

## IV. Usage

### **>** Basic Implementation

```c#
// Create sprite data
var spriteData = new NativeArray<SpriteRendererData>(count, Allocator.Temp);
// Fill sprite data with positions, UVs, etc.

// Update and render sprites
spriteManager.UpdateSprites(spriteData, atlasTexture);

```

###

### **>** Best Practices

- Use shared texture atlases to minimize draw calls
- Keep sprite batches within the 1023 instance limit per draw call
- Dispose of NativeArrays when no longer needed
- Consider using object pooling for dynamic sprite systems

###

## V. Performance

- Optimized for rendering hundreds of thousands of sprites
- Minimal CPU overhead using Job System
- Efficient memory usage with compute buffers
- Batched rendering to reduce draw calls