using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace GPUSpriteInstancing
{
    public class GPUSpriteInstanceRenderer
    {
        public GPUSpriteInstanceRenderer(Material instanceMaterial)
        {
            this.instanceMaterial = instanceMaterial;
        }
        
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");
        private static readonly int PositionBuffer = Shader.PropertyToID("_PositionBuffer");
        private static readonly int SpriteDataBuffer = Shader.PropertyToID("_SpriteDataBuffer");
        private static readonly int InstanceOffset = Shader.PropertyToID("_InstanceOffset");

        private readonly Material instanceMaterial;
        
        private ComputeBuffer positionBuffer;
        private ComputeBuffer spriteDataBuffer;
        
        private NativeArray<float2> positions;
        private NativeArray<float4> spriteData;
        
        private Mesh quadMesh;
        private bool isInitialized;

        private MaterialPropertyBlock propertyBlock;
        private const int BATCH_SIZE = 1023;
        private Bounds renderBounds;

        private void SetupMeshAndMaterial(int count)
        {
            quadMesh = CreateQuadMesh();
            propertyBlock = new MaterialPropertyBlock();
            
            // Create buffers
            positionBuffer = new ComputeBuffer(count, sizeof(float) * 2);
            spriteDataBuffer = new ComputeBuffer(count, sizeof(float) * 4);
            
            // Setup material buffers
            propertyBlock.SetBuffer(PositionBuffer, positionBuffer);
            propertyBlock.SetBuffer(SpriteDataBuffer, spriteDataBuffer);
            
            // Create native arrays
            positions = new NativeArray<float2>(count, Allocator.Persistent);
            spriteData = new NativeArray<float4>(count, Allocator.Persistent);
            
            // Set initial bounds
            renderBounds = new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 100f));
            
            isInitialized = true;
        }

        private static Mesh CreateQuadMesh()
        {
            var mesh = new Mesh();
            var size = 0.5f;

            var vertices = new[]
            {
                new Vector3(-size, -size, 0),
                new Vector3(size, -size, 0),
                new Vector3(size, size, 0),
                new Vector3(-size, size, 0)
            };

            var uv = new[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(1, 1),
                new Vector2(0, 1)
            };

            var triangles = new[]
            {
                0, 1, 2,
                0, 2, 3
            };

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;
            return mesh;
        }

        [BurstCompile]
        private struct UpdateInstanceDataJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<SpriteRendererData> sourceData;
            [WriteOnly] public NativeArray<float2> positions;
            [WriteOnly] public NativeArray<float4> spriteData;

            public void Execute(int index)
            {
                var data = sourceData[index];
                positions[index] = data.position;
                spriteData[index] = data.uvRect;
            }
        }

        public void UpdateAndRender(NativeArray<SpriteRendererData> spriteData, Texture2D atlas)
        {
            var count = spriteData.Length;
            
            if (!isInitialized)
                SetupMeshAndMaterial(count);

            // Update instance data using job system
            new UpdateInstanceDataJob
            {
                sourceData = spriteData,
                positions = positions,
                spriteData = this.spriteData
            }.Schedule(count, 64).Complete();

            // Update buffers
            positionBuffer.SetData(positions);
            spriteDataBuffer.SetData(this.spriteData);

            // Set texture and render
            propertyBlock.SetTexture(MainTex, atlas);

            // Render in batches
            var remainingInstances = count;
            var offset = 0;

            while (remainingInstances > 0)
            {
                var batchCount = Mathf.Min(remainingInstances, BATCH_SIZE);
                propertyBlock.SetInt(InstanceOffset, offset);
                
                Graphics.DrawMeshInstancedProcedural(
                    quadMesh,
                    0,
                    instanceMaterial,
                    renderBounds,
                    batchCount,
                    propertyBlock
                );
                
                remainingInstances -= BATCH_SIZE;
                offset += BATCH_SIZE;
            }
        }

        public void Release()
        {
            if (positions.IsCreated) positions.Dispose();
            if (spriteData.IsCreated) spriteData.Dispose();
            
            positionBuffer?.Release();
            spriteDataBuffer?.Release();
        }
    }
}