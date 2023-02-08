// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "RJ/Nebula3DDraw"
{
    Properties
    {
        
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest+250" "RenderType"="Transparent" } //Inner rendering
        LOD 100

        GrabPass {}

        Pass
        {
            Cull Front
            Zwrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _GrabTexture;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenSpace : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };

            sampler2D _QuarterResNebula;

            #include "./RaymarchFunctions.cginc"

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenSpace = ComputeScreenPos(o.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                
                float2 screenSpaceUV = i.screenSpace.xy / i.screenSpace.w;
                float3 viewDirection = normalize(i.worldPos - _WorldSpaceCameraPos);
                float4 backGroundColor = tex2D(_GrabTexture, screenSpaceUV);
                float4 raymarchedColor = tex2D(_QuarterResNebula, screenSpaceUV);

                float3 col = lerp(backGroundColor.rgb,raymarchedColor.rgb,raymarchedColor.a);

                return float4(col,1);
            }
            ENDCG
        }
    }
}
