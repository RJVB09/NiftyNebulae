// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "RJ/Nebula3DRaymarch"
{
    Properties
    {
        _StepSize ("Marching step size", float) = 1
        _MaxSteps ("Max steps", int) = 200
        _DomainScale ("Domain Scale", vector) = (1,1,1,0)
        _DomainPosition ("Domain Position", vector) = (0,0,0,0)
        _Texture3D ("3D Texture", 3D) = "" {}
        _DetailFrequency ("Detail frequency", float) = 2
        _DetailStrength ("Detail strength", float) = 0.3
        _UseDetail ("Use detail", int) = 0
        _LODLevel ("Level of detail for textures", int) = 0
        _Density ("Volume max density", float) = 1
        _Rotation ("Rotation", float) = 0
        _BLIThreshold ("Bilinear Interpolation Threshold", float) = 0.05 
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest+250" "RenderType"="Transparent" } //Inner rendering
        LOD 100

        Pass
        {
            Cull Front
            Zwrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            

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

            sampler2D _CameraDepthTexture;
            sampler2D _GrabTexture;

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
                float3 viewDirection = normalize(i.worldPos - _WorldSpaceCameraPos); //WORKS IN KSP
                float4 backGroundColor = tex2D(_VolumeDepth, screenSpaceUV); //WORKS IN KSP
                float depth = length(i.worldPos - _WorldSpaceCameraPos)-tex2D(_VolumeDepth, screenSpaceUV);
                
                float4 raymarchedColor = Raymarch(viewDirection,i.worldPos,depth);
                //l = lerp(backGroundColor.rgb,float3(0,thickness,1),0.5);
                //col = lerp(backGroundColor.rgb,raymarchedColor.rgb,raymarchedColor.a);
                //backGroundColor.rgb = float3(thickness,0,0);

                return raymarchedColor;
            }
            ENDCG
        }

        Pass
        {
            Cull Back
            Zwrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            

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
                //float3 viewDirection = normalize(i.worldPos - _WorldSpaceCameraPos); //WORKS IN KSP
                
                //float4 raymarchedColor = Raymarch(viewDirection,i.worldPos);
                //float4 col = raymarchedColor;
                //l = lerp(backGroundColor.rgb,float3(0,thickness,1),0.5);
                //col = lerp(backGroundColor.rgb,raymarchedColor.rgb,raymarchedColor.a);
                //backGroundColor.rgb = float3(thickness,0,0);

                return float4(float3(1,1,1)*length(i.worldPos - _WorldSpaceCameraPos),1);
            }
            ENDCG
        }
    }
}
