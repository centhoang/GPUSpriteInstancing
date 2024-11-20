using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace GPUSpriteInstancing 
{
    public class GPUSpriteManager : MonoBehaviour
    {
        [SerializeField] private Material instanceMaterial;
        private Dictionary<Texture2D, GPUSpriteInstanceRenderer> renderers = new();

        // Modified to accept NativeArray instead of List
        public void UpdateSprites(NativeArray<SpriteRendererData> spriteData, Texture2D atlas)
        {
            if (!renderers.TryGetValue(atlas, out var renderer))
            {
                renderer = new GPUSpriteInstanceRenderer(instanceMaterial);
                renderers.Add(atlas, renderer);
            }

            renderer.UpdateAndRender(spriteData, atlas);
        }
    }
}