using System;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace GPUSpriteInstancing.Sample
{
    public class GPUSpriteInstanceTest : MonoBehaviour
    {
        [Header("Instance Settings")] [SerializeField]
        private Texture2D spriteAtlas;

        [SerializeField] private int instanceCount = 1000000;
        [SerializeField] private float spawnRange = 10f;

        [Header("Animation Settings")] [SerializeField]
        private Vector2Int spriteGridSize = new(4, 4);

        [SerializeField] private float animationSpeed = 0.1f;

        [Header("GPUSpriteInstancingRef")] [SerializeField]
        private GPUSpriteManager spriteManager;

        private NativeArray<SpriteRendererData> spriteDataList;
        private NativeArray<float> animationTimes;
        private float spriteUVWidth;
        private float spriteUVHeight;

        void Start()
        {
            InitializeManager();
            InitializeSpriteData();
        }

        private void InitializeManager()
        {
            spriteUVWidth = 1f / spriteGridSize.x;
            spriteUVHeight = 1f / spriteGridSize.y;
        }

        [BurstCompile]
        private struct InitializeSpriteDataJob : IJobParallelFor
        {
            public NativeArray<SpriteRendererData> spriteData;
            public NativeArray<float> animTimes;
            public float2 uvSize;
            public float spawnRange;
            public Random random;

            public void Execute(int i)
            {
                float angle = random.NextFloat(0, math.PI * 2);
                float radius = random.NextFloat(0, spawnRange);
                float2 position = new float2(
                    math.cos(angle) * radius,
                    math.sin(angle) * radius
                );

                spriteData[i] = new SpriteRendererData
                {
                    position = position,
                    rotation = 0,
                    scale = new float2(1, 1),
                    uvRect = new float4(0, 0, uvSize.x, uvSize.y),
                    sortingOrder = 0,
                    sortingLayerID = 0
                };

                animTimes[i] = random.NextFloat();
            }
        }

        [BurstCompile]
        private struct UpdateAnimationJob : IJobParallelFor
        {
            public NativeArray<SpriteRendererData> spriteData;
            public NativeArray<float> animTimes;
            public float deltaTime;
            public float animSpeed;
            public int2 gridSize;
            public float2 uvSize;

            public void Execute(int i)
            {
                animTimes[i] += deltaTime * animSpeed;
                if (animTimes[i] >= 1f) animTimes[i] -= 1f;

                int totalFrames = gridSize.x * gridSize.y;
                int currentFrame = (int)(animTimes[i] * totalFrames);
                int x = currentFrame % gridSize.x;
                int y = currentFrame / gridSize.x;

                var data = spriteData[i];
                data.uvRect = new float4(
                    x * uvSize.x,
                    y * uvSize.y,
                    uvSize.x,
                    uvSize.y
                );
                spriteData[i] = data;
            }
        }

        private void InitializeSpriteData()
        {
            // Change from NativeList to NativeArray since we know the fixed size
            spriteDataList = new NativeArray<SpriteRendererData>(instanceCount, Allocator.Persistent);
            animationTimes = new NativeArray<float>(instanceCount, Allocator.Persistent);

            var random = new Random((uint)DateTime.Now.Ticks);
            var initJob = new InitializeSpriteDataJob
            {
                spriteData = spriteDataList, // Now using NativeArray
                animTimes = animationTimes,
                uvSize = new float2(spriteUVWidth, spriteUVHeight),
                spawnRange = spawnRange,
                random = random
            };

            initJob.Schedule(instanceCount, 64).Complete();
        }

        void Update()
        {
            var updateJob = new UpdateAnimationJob
            {
                spriteData = spriteDataList,
                animTimes = animationTimes,
                deltaTime = Time.deltaTime,
                animSpeed = animationSpeed,
                gridSize = new int2(spriteGridSize.x, spriteGridSize.y),
                uvSize = new float2(spriteUVWidth, spriteUVHeight)
            };

            updateJob.Schedule(instanceCount, 64).Complete();

            // Now we can directly pass the NativeArray without conversion
            spriteManager.UpdateSprites(spriteDataList, spriteAtlas);
        }

        void OnDestroy()
        {
            if (spriteDataList.IsCreated) spriteDataList.Dispose();
            if (animationTimes.IsCreated) animationTimes.Dispose();
        }

        void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 24;
            style.normal.textColor = Color.white;

            // Draw outline/shadow effect
            GUIStyle outlineStyle = new GUIStyle(style);
            outlineStyle.normal.textColor = Color.black;

            string fps = $"FPS: {1.0f / Time.smoothDeltaTime:F1}";
            string count = $"Instance Count: {instanceCount:N0}";

            // Draw outline/shadow
            GUI.Label(new Rect(12, 12, 300, 30), fps, outlineStyle);
            GUI.Label(new Rect(12, 42, 300, 30), count, outlineStyle);

            // Draw main text
            GUI.Label(new Rect(10, 10, 300, 30), fps, style);
            GUI.Label(new Rect(10, 40, 300, 30), count, style);
        }
    }
}