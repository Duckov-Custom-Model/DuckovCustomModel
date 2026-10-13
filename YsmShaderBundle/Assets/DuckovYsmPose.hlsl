#ifndef DUCKOV_YSM_POSE_INCLUDED
#define DUCKOV_YSM_POSE_INCLUDED
#if defined(YSM_GPU_POSE)
struct YsmBone
{
    float4 x, y, z;
    float4 normalX, normalY, normalZ;
    float4 color, parameters;
};
StructuredBuffer<YsmBone> _YsmBones;
#endif
float3 YsmPosition(float3 position, float boneIndex)
{
#if defined(YSM_GPU_POSE)
    YsmBone bone = _YsmBones[(uint)boneIndex];
    float4 vertex = float4(position, 1);
    return float3(dot(bone.x, vertex), dot(bone.y, vertex), dot(bone.z, vertex));
#else
    return position;
#endif
}
float3 YsmNormal(float3 normal, float boneIndex)
{
#if defined(YSM_GPU_POSE)
    YsmBone bone = _YsmBones[(uint)boneIndex];
    float3 result = float3(dot(bone.normalX.xyz, normal), dot(bone.normalY.xyz, normal), dot(bone.normalZ.xyz, normal));
    float magnitude = max(max(abs(result.x), abs(result.y)), abs(result.z));
    return magnitude > 0 ? normalize(result / magnitude) : float3(0,1,0);
#else
    return normal;
#endif
}
float4 YsmColor(float4 color, float boneIndex)
{
#if defined(YSM_GPU_POSE)
    return _YsmBones[(uint)boneIndex].color;
#else
    return color;
#endif
}
float YsmGlow(float glow, float boneIndex)
{
#if defined(YSM_GPU_POSE)
    return _YsmBones[(uint)boneIndex].parameters.x;
#else
    return glow;
#endif
}
#endif
