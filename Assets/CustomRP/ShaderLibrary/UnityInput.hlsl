﻿//unity标准输入库
#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED
CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade;   //x存储的是过渡因子，y存储了相同的因子，只不过它被量化为16步，我们不使用它
//相机位置
float3 _WorldSpaceCameraPos;
//这个矩阵包含一些在这里我们不需要的转换信息
real4 unity_WorldTransformParams;

float4 unity_ProbesOcclusion;

float4 unity_LightmapST;
float4 unity_DynamicLightmapST;

float4 unity_SHAr;
float4 unity_SHAg;
float4 unity_SHAb;
float4 unity_SHBr;
float4 unity_SHBg;
float4 unity_SHBb;
float4 unity_SHC;

float4 unity_ProbeVolumeParams;
float4x4 unity_ProbeVolumeWorldToObject;
float4 unity_ProbeVolumeSizeInv;
float4 unity_ProbeVolumeMin;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;
#endif
