﻿Shader "CustomRP/Lit"
{
    Properties
    {
	    _BaseMap("Texture", 2D) = "white" {}
	    _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1.0)
	    //透明度测试的阈值
	    _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
	    [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
        //透明通道预乘
	    [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha("Premultiply Alpha", Float) = 0
	    //遮罩纹理
	    [NoScaleOffset] _MaskMap("Mask (MODS)",2D) = "white" {}
	    _Occlusion ("Occlusion", Range(0,1)) = 1
        //金属度和光滑度
	    _Metallic("Metallic", Range(0, 1)) = 0
	    _Smoothness("Smoothness", Range(0, 1)) = 0.5
	    [NoScaleOffset] _EmissionMap("Emission",2D) = "white" {}
	    [HDR] _EmissionColor("Emission",Color) = (0.0,0.0,0.0,0.0)
	    //细节纹理
	    _DetailMap("Detials",2D) = "linearGrey" {}
	    _DetailAlbedo("Detail Albedo",Range(0,1)) = 1
	    //设置混合模式
	    [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
	    [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
	    //默认写入深度缓冲区
	    [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
	    //投影模式
	    [KeywordEnum(On,Clip,Dither,Off)] _Shadows ("Shadows" ,Float) = 0
	    [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows ("Receive Shadows",Float) = 1
	    //菲涅尔反射
	    _Fresnel ("Fresnel",Range(0,1)) = 1
	    [HideInInspector] _MainTex ("Texture for Lightmap" , 2D) = "white" {}
	    [HideInInspector] _Color ("Color for Lightmap" , Color) = (0.5,0.5,0.5,1.0)
    }
    SubShader
    {     
        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include "LitInput.hlsl"
        ENDHLSL

        Pass
        {
		    Tags {
				"LightMode" = "CustomLit"
			}

		    //定义混合模式
		    Blend[_SrcBlend][_DstBlend],One OneMinusSrcAlpha
		    //是否写入深度
		    ZWrite[_ZWrite]
            HLSLPROGRAM
		    #pragma target 3.5
		    #pragma shader_feature _CLIPPING
		    //是否透明通道预乘
		    #pragma shader_feature _PREMULTIPLY_ALPHA
		    #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _OTHER_PCF3 _OTHER_PCF5 _OTHER_PCF7
		    #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE
		    #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_instancing
            #pragma shader_feature _RECEIVE_SHADOWS
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            //是否使用逐对象光源
            #pragma multi_compile _ _LIGHTS_PER_OBJECT
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
		    //插入相关hlsl代码
            #include"LitPass.hlsl"
            ENDHLSL
        }
        
        Pass{
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
            //添加ColorMask 0 不写任何颜色数据，但会进行深度测试，并将深度值写到深度缓冲区中
            ColorMask 0 
            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #pragma multi_compile_instancing
            #pragma vertex ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment
            #include "ShadowCasterPass.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Tags
            {
                "LightMode" = "Meta"
            }
            Cull Off
            
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment
            #include "MetaPass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "CustomShaderGUI"
}
