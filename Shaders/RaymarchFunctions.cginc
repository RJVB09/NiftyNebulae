float _StepSize;
int _MaxSteps;
int _FixedSteps;
int _LODLevel;

sampler3D _Texture3D;
sampler2D _VolumeDepth;
float3 _DomainScale;
float3 _DomainPosition;
float _Density;
float _Rotation;
float _BLIThreshold;
float _DetailFrequency;
float _DetailStrength;

float3 rotateAroundY(float3 vec, float angle)
{
    return float3(cos(angle)*vec.x-sin(angle)*vec.z,vec.y,sin(angle)*vec.x+cos(angle)*vec.z);
}

bool InBoundingBox(float3 position, float3 bounds, float3 boundsPosition)
{
    position -= boundsPosition;
    return max(abs(position.x/bounds.x), max(abs(position.y/bounds.y), abs(position.z/bounds.z))) < 0.5;
}

#include "./noiseLib.cginc"


float4 SampleVolume(float3 position)
{
    position = rotateAroundY(position - _DomainPosition,_Rotation);
    position += _DomainPosition;
    //position += _DetailStrength*float3(
    //    perlin(_DetailFrequency*position+float3(100,100,100)),
    //    perlin(_DetailFrequency*position+float3(100,100,100)+float3(43,98,23)),
    //    perlin(_DetailFrequency*position+float3(100,100,100)+float3(21,88,283))
    //);
    float3 texcoords = position - _DomainPosition;
    texcoords %= _DomainScale;
    texcoords /= _DomainScale;
    texcoords += float3(0.5,0.5,0.5);
    texcoords += _DetailStrength*float3(
        perlin(_DetailFrequency*texcoords+float3(100,100,100)),
        perlin(_DetailFrequency*texcoords+float3(100,100,100)+float3(43,98,23)),
        perlin(_DetailFrequency*texcoords+float3(100,100,100)+float3(21,88,283))
    );

    float outsideDomain = 1;
    if (!InBoundingBox(position,_DomainScale,_DomainPosition))
        outsideDomain = 0;
    //^^^^^^^^^Bounding box shit

    //float4 col = tex2DAs3D(_Texture2D, texcoords, _Texture2D_TexelSize.z, _Texture2DSliceLength);
    float4 col = tex3Dlod(_Texture3D,float4(texcoords,2.0));
    col.a *= _Density*outsideDomain;
                
    //col.rgb = max(min(0.1*length(position - _DomainPosition),1),0) * float3(0.5,0.5,0.5);

    return col;
}

//actual raymarch function
float4 Raymarch(float3 rayDirection, float3 origin, float depth)
{
    float3 averageColor = float3(0,0,0); //the average color of the volume after travelling through it (weighted with density)
    float averageColorDivider = 0;
    rayDirection = normalize(rayDirection); //ray direction normalized
    float accumulation = 0; //distance travelled in volume (weighted with density)
    float3 rayPosition = origin; //the raymarch's current position (default set at starting point)
    float stepSize = length(origin - _WorldSpaceCameraPos) % _StepSize; //raymarch step size
    float depthTravelled = 0; //depth travelled through the domain 
    int stepsTaken = 0; //steps taken
    rayPosition -= rayDirection*depth;

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
            stepSize = _StepSize;
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

    //averageColor = sum(colorAtSamplePoint*densityAtSamplePoint)/distanceTravelledInVolume
    //prevent /0 errors
    averageColor /= (averageColorDivider + 0.00001);
    //TODO: make black parts behind clouds on black backgrounds invisible

    //use the distance travelled to calculate the alpha value, just like how ordinary fog is calculated (1-e^(-distance))
    return float4(averageColor,1-exp(-accumulation));
}