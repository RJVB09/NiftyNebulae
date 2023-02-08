float3 random3(float3 c) 
{
	float j = 4096.0*sin(dot(c,float3(17.0, 59.4, 15.0)));
	float3 r;
	r.z = frac(512.0*j);
	j *= .125;
	r.x = frac(512.0*j);
	j *= .125;
	r.y = frac(512.0*j);
	return r-0.5;
}

float3 smoothStep(float3 x)
{
    return 6.0 * pow(x,5.0) - 15.0 * pow(x,4.0) + 10.0 * pow(x,3.0);
}

float lerp3d(float o, float x, float y, float z, float xy, float xz, float yz, float xyz, float3 target)
{
    target = smoothStep(target);
    return lerp(lerp(lerp(o,z,target.z),lerp(x,xz,target.z),target.x),lerp(lerp(y,yz,target.z),lerp(xy,xyz,target.z),target.x),target.y);
}

float perlin(float3 pos)
{
    float3 oPos = pos % float3(1,1,1);
    float3 xPos = oPos - float3(1.0,0.0,0.0);
    float3 yPos = oPos - float3(0.0,1.0,0.0);
    float3 zPos = oPos - float3(0.0,0.0,1.0);
    
    float3 xyPos = oPos - float3(1.0,1.0,0.0);
    float3 xzPos = oPos - float3(1.0,0.0,1.0);
    float3 yzPos = oPos - float3(0.0,1.0,1.0);
    
    float3 xyzPos = oPos - float3(1.0,1.0,1.0);
    
    
    float3 oRandom = normalize(random3(floor(pos) + float3(0.0,0.0,0.0)));
    float3 xRandom = normalize(random3(floor(pos) + float3(1.0,0.0,0.0)));
    float3 yRandom = normalize(random3(floor(pos) + float3(0.0,1.0,0.0)));
    float3 zRandom = normalize(random3(floor(pos) + float3(0.0,0.0,1.0)));
    
    float3 xyRandom = normalize(random3(floor(pos) + float3(1.0,1.0,0.0)));
    float3 xzRandom = normalize(random3(floor(pos) + float3(1.0,0.0,1.0)));
    float3 yzRandom = normalize(random3(floor(pos) + float3(0.0,1.0,1.0)));
    
    float3 xyzRandom = normalize(random3(floor(pos) + float3(1.0,1.0,1.0)));
    
    
    float outputValue = lerp3d(dot(oPos,oRandom),dot(xPos,xRandom),dot(yPos,yRandom),dot(zPos,zRandom),dot(xyPos,xyRandom),dot(xzPos,xzRandom),dot(yzPos,yzRandom),dot(xyzPos,xyzRandom),oPos);
    
    return outputValue;
}