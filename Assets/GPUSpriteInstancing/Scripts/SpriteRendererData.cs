using Unity.Mathematics;

namespace GPUSpriteInstancing
{
    public struct SpriteRendererData
    {
        public float2 position;      // Using float2 for 2D position
        public float rotation;       // Single float for 2D rotation
        public float2 scale;        // Using float2 for 2D scale
        public float4 uvRect;       // x,y = position, z,w = width,height
        public int sortingOrder;
        public int sortingLayerID;
    }
}