// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/Nebula3D"
{
    Properties
    {
        _StepSize ("Marching step size", float) = 1
        _MaxSteps ("Max steps", int) = 200
        _DomainScale ("Domain Scale", vector) = (1,1,1,0)
        _DomainPosition ("Domain Position", vector) = (0,0,0,0)
        _Texture2D ("2D Texture", 2D) = "" {}
        _Texture2DSliceLength ("2D Texture slice with or height",int) = 4
        _Density ("Volume max density", float) = 1
        _Rotation ("Rotation", float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
        LOD 100

        GrabPass { }

        Pass
        {
            Cull Front
            //ZTest Always  ///TURN OF FOR KSP

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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenSpace = ComputeScreenPos(o.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float3 _DomainScale;

            float4 frag (v2f i) : SV_Target
            {
                float2 screenSpaceUV = i.screenSpace.xy / i.screenSpace.w;
                float4 backGroundColor = tex2D(_GrabTexture, screenSpaceUV);
                backGroundColor.a = length(i.worldPos - _WorldSpaceCameraPos);
                //backGroundColor.a /= max(_DomainScale.x,max(_DomainScale.y,_DomainScale.z)) * _ScaleFactor;
                backGroundColor.a = log2(backGroundColor.a);

                return backGroundColor;
            }
            ENDCG
        }
        
        GrabPass { }

        Pass
        {
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 screenSpace : TEXCOORD1;
                float3 normal : NORMAL;
                float3 worldPos : TEXCOORD2;
            };

            float4 _MainTex_ST;
            sampler2D _CameraDepthTexture;
            sampler2D _GrabTexture;


            float _StepSize;
            int _MaxSteps;

            sampler2D _Texture2D;
            float4 _Texture2D_TexelSize;
            int _Texture2DSliceLength;
            float3 _DomainScale;
            float3 _DomainPosition;
            float _Density;
            float _Rotation;

            /*
            int2 CoordFrom3D(int3 coord, uint sliceLen)
            {
                uint sqrtSliceLen = sqrt(sliceLen);
                int2 zw = int2(coord.z / sqrtSliceLen, coord.z % sqrtSliceLen);
                return coord.xy + zw * sliceLen;
            }

            float4 tex2DAs3D(sampler2D tex, float3 uv3D, float resolution)
            {
                float sliceLen = 4;
                float2 uv2D = CoordFrom3D(uv3D*sliceLen,sliceLen)/resolution;
                
                
                //float2 2Duv = float2()
                return tex2Dlod(tex,float4(uv2D,0,0));
            }
            */

            float3 rotateAroundY(float3 vec, float angle)
            {
                return float3(cos(angle)*vec.x-sin(angle)*vec.z,vec.y,sin(angle)*vec.x+cos(angle)*vec.z);
            }

            bool InBoundingBox(float3 position, float3 bounds, float3 boundsPosition)
            {
                position -= boundsPosition;
                return max(abs(position.x/bounds.x), max(abs(position.y/bounds.y), abs(position.z/bounds.z))) < 0.5;
            }

            int2 voxelPosToSamplePos(int3 voxelPos, int sliceLen, int3 voxelOffset)
            {
                //voxelPos = max(min(voxelPos + voxelOffset,sliceLen - 1),0);
                voxelPos += voxelOffset;
                voxelPos.x = voxelPos.x % sliceLen;
                voxelPos.y = voxelPos.y % sliceLen;
                voxelPos.z = voxelPos.z % sliceLen;

                //if (!InBoundingBox(voxelPos - (sliceLen*0.5),sliceLen,0))
                //    voxelPos -= sliceLen * voxelOffset;
                //voxelPos = (voxelPos + voxelOffset) % (sliceLen - 1);
                int2 zw = int2(sliceLen*floor(voxelPos.z/sqrt(sliceLen)),sliceLen*(voxelPos.z % sqrt(sliceLen)));
                return voxelPos.xy + zw;
            }

            float4 tex2DAs3D(sampler2D tex, float3 uv3D, float resolution, int sliceLen)
            {
                float2 uv2D = uv3D.xy;

                int3 voxelPos = floor(uv3D*sliceLen);
                //int2 zw = int2(sliceLen*floor(voxelPos.z/sqrt(sliceLen)),sliceLen*(voxelPos.z % sqrt(sliceLen)));
                //int2 samplePos = voxelPos.xy + zw;

                float4 floorSample = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(0,0,0))/resolution,0,0));
                float4 sampleX = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(1,0,0))/resolution,0,0));
                float4 sampleY = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(0,1,0))/resolution,0,0));
                float4 sampleZ = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(0,0,1))/resolution,0,0));
                float4 sampleXY = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(1,1,0))/resolution,0,0));
                float4 sampleXZ = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(1,0,1))/resolution,0,0));
                float4 sampleYZ = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(0,1,1))/resolution,0,0));
                float4 sampleXYZ = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(1,1,1))/resolution,0,0));

                float3 lerpingGrid = (uv3D*sliceLen)%1;


                float4 lerpZ1 = lerp(floorSample,sampleZ,lerpingGrid.z);
                float4 lerpZ2 = lerp(sampleX,sampleXZ,lerpingGrid.z);
                float4 lerpZ3 = lerp(sampleY,sampleYZ,lerpingGrid.z);
                float4 lerpZ4 = lerp(sampleXY,sampleXYZ,lerpingGrid.z);

                float4 lerpX1 = lerp(lerpZ1,lerpZ2,lerpingGrid.x);
                float4 lerpX2 = lerp(lerpZ3,lerpZ4,lerpingGrid.x);

                float4 finalCol = lerp(lerpX1,lerpX2,lerpingGrid.y);
                //finalCol /= (lerpingGrid.x + lerpingGrid.y + lerpingGrid.z);
                //float4 finalCol = float4(lerpingGrid,1);
                //finalCol /= 3;
                
                //float2 2Duv = float2()
                return finalCol;
                //return float4(voxelPos/sliceLen,1);
            }

            float GetHalfFOV()
            {
                float t = unity_CameraProjection._m11;
                return atan(1.0f / t ); //*2
            }

            float GetDistToClipPlane(float2 screenUV, float clipPlane)
            {
                float aspect = _ScreenParams.x/_ScreenParams.y; //mult x by this
                screenUV -= float2(0.5,0.5);
                screenUV *= float2(2,2);
                screenUV.x *= aspect;
                
                float s = length(screenUV);
                clipPlane /= cos(GetHalfFOV()*s);

                return clipPlane;
            }

            float GetSceneDepth(float2 screenUV)
            {
                float sceneDepth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,screenUV));
                float clipPlaneDistFactor = GetDistToClipPlane(screenUV,1);
                sceneDepth = lerp(0,_ProjectionParams.z*clipPlaneDistFactor,sceneDepth);
                return sceneDepth;
            }

            float4 SampleVolume(float3 position)
            {
                position = rotateAroundY(position - _DomainPosition,_Rotation);
                position += _DomainPosition;
                float3 texcoords = position - _DomainPosition;
                texcoords %= _DomainScale;
                texcoords /= _DomainScale;
                texcoords += float3(0.5,0.5,0.5);

                float outsideDomain = 1;
                if (!InBoundingBox(position,_DomainScale,_DomainPosition))
                    outsideDomain = 0;
                //^^^^^^^^^Bounding box shit

                float4 col = tex2DAs3D(_Texture2D, texcoords, _Texture2D_TexelSize.z, _Texture2DSliceLength);
                col.a *= _Density*outsideDomain;
                
                //col.rgb = max(min(0.1*length(position - _DomainPosition),1),0) * float3(0.5,0.5,0.5);

                return col;
            }

            float4 Raymarch(float3 rayDirection, float3 origin, float depth)
            {
                float3 averageColor = float3(0,0,0); //the average color of the volume after travelling through it (weighted with density)
                float averageColorDivider = 0;
                rayDirection = normalize(rayDirection); //ray direction normalized
                float accumulation = 0; //distance travelled in volume (weighted with density)
                float3 rayPosition = origin; //the raymarch's current position (default set at starting point)
                float stepSize = _StepSize; //raymarch step size
                float depthTravelled = 0; //depth travelled through the domain 
                int stepsTaken = 0; //steps taken

                while (stepsTaken < _MaxSteps)
                {
                    if ((depthTravelled + stepSize) > depth) //march forward until the distance is overshot with steps of _StepSize
                    {
                        break;
                    }
                    else
                    {
                        rayPosition += rayDirection * stepSize;
                        float4 volumeSample = SampleVolume(rayPosition);
                        //volumeSample.rgb *= ~lighting stuff~
                        accumulation += volumeSample.a * stepSize;
                        averageColorDivider += stepSize * volumeSample.a * exp(-accumulation);
                        averageColor += volumeSample.rgb * stepSize * volumeSample.a * exp(-accumulation);
                        depthTravelled += stepSize;
                        stepsTaken++;
                    }
                }
                //finish the remaining step with a fraction of _StepSize
                stepSize = depth - depthTravelled;
                rayPosition += rayDirection * stepSize;
                float4 lastVolumeSample = SampleVolume(rayPosition);
                accumulation += lastVolumeSample.a * stepSize;
                averageColorDivider += stepSize * lastVolumeSample.a * exp(-accumulation);
                averageColor += lastVolumeSample.rgb * stepSize * lastVolumeSample.a * exp(-accumulation);
                depthTravelled += stepSize;

                //averageColor = Σ(colorAtSamplePoint*densityAtSamplePoint)/distanceTravelledInVolume
                if (averageColorDivider != 0) //prevent /0 errors
                    averageColor /= averageColorDivider;
                else
                    averageColor = float3(0,0,0);
                //TODO: make black parts behind clouds on black backgrounds invisible

                //use the distance travelled to calculate the alpha value, just like how ordinary fog is calculated (1-e^(-distance))
                return float4(averageColor,1-exp(-accumulation));
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenSpace = ComputeScreenPos(o.vertex);
                o.normal = v.normal;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float2 screenSpaceUV = i.screenSpace.xy / i.screenSpace.w; //WORKS IN KSP
                float4 backGroundColor = tex2D(_GrabTexture, screenSpaceUV); //WORKS IN KSP
                //float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,screenSpaceUV)); //Depth in world units (not matching atm)
                //float sceneDepth = GetSceneDepth(screenSpaceUV); //WORKS IN KSP
                float3 viewDirection = normalize(i.worldPos - _WorldSpaceCameraPos); //WORKS IN KSP
                //backGroundColor.a *= max(_DomainScale.x,max(_DomainScale.y,_DomainScale.z)) * _ScaleFactor;
                backGroundColor.a = pow(2,backGroundColor.a);
                //backGroundColor.a = min(backGroundColor.a,sceneDepth); //WORKS IN KSP
                float thickness = backGroundColor.a-length(i.worldPos - _WorldSpaceCameraPos); //WORKS IN KSP
                //float4 raymarchedColor = RayMarchVolume(viewDirection,i.worldPos,thickness);
                
                
                //float3 col = float3(1,1,1)*GetHalfFOV()/(0.5*UNITY_PI);
                float3 col = float3(1,0,1);
                if (thickness < 0)
                    col = backGroundColor.rgb;
                else
                {
                    float4 raymarchedColor = Raymarch(viewDirection,i.worldPos,thickness);
                    col = lerp(backGroundColor.rgb,raymarchedColor.rgb,raymarchedColor.a);
                }
                //abs(backGroundColor.a-length(i.worldPos - _WorldSpaceCameraPos))/200
                return float4(col,1);
            }
            ENDCG
        }

        Pass
        {
            Cull Front

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

            sampler2D _CameraDepthTexture;

            float _StepSize;
            int _MaxSteps;

            sampler2D _Texture2D;
            float4 _Texture2D_TexelSize;
            int _Texture2DSliceLength;
            float3 _DomainScale;
            float3 _DomainPosition;
            int _TriplanarBlendMode;
            float _Density;
            float _Rotation;

            float3 rotateAroundY(float3 vec, float angle)
            {
                return float3(cos(angle)*vec.x-sin(angle)*vec.z,vec.y,sin(angle)*vec.x+cos(angle)*vec.z);
            }

            bool InBoundingBox(float3 position, float3 bounds, float3 boundsPosition)
            {
                position -= boundsPosition;
                return max(abs(position.x/bounds.x), max(abs(position.y/bounds.y), abs(position.z/bounds.z))) < 0.5;
            }

            int2 voxelPosToSamplePos(int3 voxelPos, int sliceLen, int3 voxelOffset)
            {
                //voxelPos = max(min(voxelPos + voxelOffset,sliceLen - 1),0);
                voxelPos += voxelOffset;
                voxelPos.x = voxelPos.x % sliceLen;
                voxelPos.y = voxelPos.y % sliceLen;
                voxelPos.z = voxelPos.z % sliceLen;

                //if (!InBoundingBox(voxelPos - (sliceLen*0.5),sliceLen,0))
                //    voxelPos -= sliceLen * voxelOffset;
                //voxelPos = (voxelPos + voxelOffset) % (sliceLen - 1);
                int2 zw = int2(sliceLen*floor(voxelPos.z/sqrt(sliceLen)),sliceLen*(voxelPos.z % sqrt(sliceLen)));
                return voxelPos.xy + zw;
            }

            float4 tex2DAs3D(sampler2D tex, float3 uv3D, float resolution, int sliceLen)
            {
                float2 uv2D = uv3D.xy;

                int3 voxelPos = floor(uv3D*sliceLen);
                //int2 zw = int2(sliceLen*floor(voxelPos.z/sqrt(sliceLen)),sliceLen*(voxelPos.z % sqrt(sliceLen)));
                //int2 samplePos = voxelPos.xy + zw;

                float4 floorSample = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(0,0,0))/resolution,0,0));
                float4 sampleX = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(1,0,0))/resolution,0,0));
                float4 sampleY = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(0,1,0))/resolution,0,0));
                float4 sampleZ = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(0,0,1))/resolution,0,0));
                float4 sampleXY = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(1,1,0))/resolution,0,0));
                float4 sampleXZ = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(1,0,1))/resolution,0,0));
                float4 sampleYZ = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(0,1,1))/resolution,0,0));
                float4 sampleXYZ = tex2Dlod(tex,float4(voxelPosToSamplePos(voxelPos,sliceLen,int3(1,1,1))/resolution,0,0));

                float3 lerpingGrid = (uv3D*sliceLen)%1;


                float4 lerpZ1 = lerp(floorSample,sampleZ,lerpingGrid.z);
                float4 lerpZ2 = lerp(sampleX,sampleXZ,lerpingGrid.z);
                float4 lerpZ3 = lerp(sampleY,sampleYZ,lerpingGrid.z);
                float4 lerpZ4 = lerp(sampleXY,sampleXYZ,lerpingGrid.z);

                float4 lerpX1 = lerp(lerpZ1,lerpZ2,lerpingGrid.x);
                float4 lerpX2 = lerp(lerpZ3,lerpZ4,lerpingGrid.x);

                float4 finalCol = lerp(lerpX1,lerpX2,lerpingGrid.y);
                //finalCol /= (lerpingGrid.x + lerpingGrid.y + lerpingGrid.z);
                //float4 finalCol = float4(lerpingGrid,1);
                //finalCol /= 3;
                
                //float2 2Duv = float2()
                return finalCol;
                //return float4(voxelPos/sliceLen,1);
            }

            float GetHalfFOV()
            {
                float t = unity_CameraProjection._m11;
                return atan(1.0f / t ); //*2
            }

            float GetDistToClipPlane(float2 screenUV, float clipPlane)
            {
                float aspect = _ScreenParams.x/_ScreenParams.y; //mult x by this
                screenUV -= float2(0.5,0.5);
                screenUV *= float2(2,2);
                screenUV.x *= aspect;
                
                float s = length(screenUV);
                clipPlane /= cos(GetHalfFOV()*s);

                return clipPlane;
            }

            float GetSceneDepth(float2 screenUV)
            {
                float sceneDepth = Linear01Depth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,screenUV));
                float clipPlaneDistFactor = GetDistToClipPlane(screenUV,1);
                sceneDepth = lerp(0,_ProjectionParams.z*clipPlaneDistFactor,sceneDepth);
                return sceneDepth;
            }

            float4 SampleVolume(float3 position)
            {
                position = rotateAroundY(position - _DomainPosition,_Rotation);
                position += _DomainPosition;
                float3 texcoords = position - _DomainPosition;
                texcoords %= _DomainScale;
                texcoords /= _DomainScale;
                texcoords += float3(0.5,0.5,0.5);

                float outsideDomain = 1;
                if (!InBoundingBox(position,_DomainScale,_DomainPosition))
                    outsideDomain = 0;
                //^^^^^^^^^Bounding box shit

                float4 col = tex2DAs3D(_Texture2D, texcoords, _Texture2D_TexelSize.z, _Texture2DSliceLength);
                col.a *= _Density*outsideDomain;
                
                //col.rgb = max(min(0.1*length(position - _DomainPosition),1),0) * float3(0.5,0.5,0.5);

                return col;
            }

            float4 Raymarch(float3 rayDirection, float3 origin, float depth)
            {
                float3 averageColor = float3(0,0,0); //the average color of the volume after travelling through it (weighted with density)
                float averageColorDivider = 0;
                rayDirection = normalize(rayDirection); //ray direction normalized
                float accumulation = 0; //distance travelled in volume (weighted with density)
                float3 rayPosition = origin; //the raymarch's current position (default set at starting point)
                float stepSize = _StepSize; //raymarch step size
                float depthTravelled = 0; //depth travelled through the domain 
                int stepsTaken = 0; //steps taken

                while (stepsTaken < _MaxSteps)
                {
                    if ((depthTravelled + stepSize) > depth) //march forward until the distance is overshot with steps of _StepSize
                    {
                        break;
                    }
                    else
                    {
                        rayPosition += rayDirection * stepSize;
                        float4 volumeSample = SampleVolume(rayPosition);
                        //volumeSample.rgb *= ~lighting stuff~
                        accumulation += volumeSample.a * stepSize;
                        averageColorDivider += stepSize * volumeSample.a * exp(-accumulation);
                        averageColor += volumeSample.rgb * stepSize * volumeSample.a * exp(-accumulation);
                        depthTravelled += stepSize;
                        stepsTaken++;
                    }
                }
                //finish the remaining step with a fraction of _StepSize
                stepSize = depth - depthTravelled;
                rayPosition += rayDirection * stepSize;
                float4 lastVolumeSample = SampleVolume(rayPosition);
                accumulation += lastVolumeSample.a * stepSize;
                averageColorDivider += stepSize * lastVolumeSample.a * exp(-accumulation);
                averageColor += lastVolumeSample.rgb * stepSize * lastVolumeSample.a * exp(-accumulation);
                depthTravelled += stepSize;

                //averageColor = Σ(colorAtSamplePoint*densityAtSamplePoint)/distanceTravelledInVolume
                if (averageColorDivider != 0) //prevent /0 errors
                    averageColor /= averageColorDivider;
                else
                    averageColor = float3(0,0,0);
                //TODO: make black parts behind clouds on black backgrounds invisible

                //use the distance travelled to calculate the alpha value, just like how ordinary fog is calculated (1-e^(-distance))
                return float4(averageColor,1-exp(-accumulation));
            }

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
                float depthVal = GetSceneDepth(screenSpaceUV); //Depth in world units
                float4 backGroundColor = tex2D(_GrabTexture, screenSpaceUV); //WORKS IN KSP
                float thickness = length(i.worldPos - _WorldSpaceCameraPos); //WORKS IN KSP
                //thickness = min(depthVal,thickness); //DOESNT WORKS IN KSP    ////PROBLEM LINE PLEASE FIX
                
                float3 col = float3(1,0,1);
                float4 raymarchedColor = Raymarch(viewDirection,_WorldSpaceCameraPos,thickness);
                col = lerp(backGroundColor.rgb,raymarchedColor.rgb,raymarchedColor.a);
                //col = lerp(backGroundColor.rgb,raymarchedColor.rgb,0);
                //l = lerp(backGroundColor.rgb,float3(0,thickness,1),0.5);
                //col = lerp(backGroundColor.rgb,raymarchedColor.rgb,raymarchedColor.a);
                //backGroundColor.rgb = float3(thickness,0,0);

                return float4(col,1);
            }
            ENDCG
        }
    }
}
