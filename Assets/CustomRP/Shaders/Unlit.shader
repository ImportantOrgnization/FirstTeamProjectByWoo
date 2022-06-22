Shader "CustomRP/Unlit"
{
    Properties
    {
        _BaseColor("Color",Color) = (1,1,1,1)
        _BaseMap("Texture",2D) = "white" {}
        [Toggle(_CLIPPING)] _Clipping ("AlphaClipping", float) = 0
        _Cutoff("Alpha Cutoff",Range(0.0,1.0)) = 0.5 
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend",FLoat) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend",FLoat) = 0
        [Enum(Off,0,On,1)] _ZWrite("Z Write" , Float) = 1
    }
    SubShader
    {
       
        Pass
        {
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            
            HLSLPROGRAM
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            
            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing
            
            #include "UnlitPass.hlsl"
            ENDHLSL
        }
    }
    CustomEditor "CustomShaderGUI"
}
