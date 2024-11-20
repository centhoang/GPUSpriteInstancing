Shader "Custom/GPUSpriteInstancing"
{
    Properties
    {
        _MainTex ("Sprite Sheet", 2D) = "white" {}
    }
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            StructuredBuffer<float2> _PositionBuffer;
            StructuredBuffer<float4> _SpriteDataBuffer;
            int _InstanceOffset;

            v2f vert (appdata v)
            {
                v2f o;
                uint id = v.instanceID + _InstanceOffset;
                
                float2 position = _PositionBuffer[id];
                float3 worldPosition = float3(position + v.vertex.xy, v.vertex.z);
                o.vertex = UnityWorldToClipPos(float4(worldPosition, 1));
                
                float4 spriteData = _SpriteDataBuffer[id];
                o.uv = v.uv * spriteData.zw + spriteData.xy;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}