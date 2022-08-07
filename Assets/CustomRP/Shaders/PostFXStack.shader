Shader "Hidden/Custom RP/Post FX Stack"
{
    Properties
    {
	   
    }
    SubShader
    {     
        Cull Off
        ZTest Always
        ZWrite Off

        HLSLINCLUDE
        #include "../ShaderLibrary/Common.hlsl"
        #include"PostFXStackPasses.hlsl"
        ENDHLSL

        Pass    //0
        {
		   Name "Copy"
           HLSLPROGRAM
		   #pragma target 3.5
           #pragma vertex DefaultPassVertex
           #pragma fragment CopyPassFragment
           ENDHLSL
        }
        
        Pass    //1
        {
            Name "Bloom Horizontal"
            HLSLPROGRAM
		    #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomHorizontalPassFragment
            ENDHLSL
        }
        
        Pass    //2
        {
            Name "Bloom Vertical"
            HLSLPROGRAM
		    #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomVerticalPassFragment
            ENDHLSL
        }
        
        Pass    //3
        {
            Name "Bloom Add"
            HLSLPROGRAM
		    #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomAddPassFragment
            ENDHLSL
        }
        
         Pass   //4
        {
            Name "Bloom Scatter"
            HLSLPROGRAM
		    #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomScatterPassFragment
            ENDHLSL
        }
       
        Pass    //5
        {
            Name "Bloom Prefilter"
            HLSLPROGRAM
		    #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomPrefilterPassFragment
            ENDHLSL
        }
        
        Pass    //6
        {
            Name "Bloom Prefilter Fireflies"
            HLSLPROGRAM
		    #pragma target 3.5
            #pragma vertex DefaultPassVertex
            #pragma fragment BloomPrefilterFireFliesPassFragment
            ENDHLSL
        }
        
        Pass {  //7
			Name "Tone Mapping None"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ToneMappingNonePassFragment
			ENDHLSL
		}
        
		Pass    //8
		{
			Name "Tone Mapping Reinhard"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ToneMappingReinhardPassFragment
			ENDHLSL
		}
		
		Pass {  //9
			Name "Tone Mapping Neutral"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ToneMappingNeutralPassFragment
			ENDHLSL
		}
		
		Pass {  //10
			Name "Tone Mapping ACES"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ToneMappingACESPassFragment
			ENDHLSL
		}
		
		Pass {  //11
			Name "ApplyColorGrading"
			Blend [_FinalSrcBlend] [_FinalDstBlend]
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ApplyColorGradingPassFragment
			ENDHLSL
		}
		
		Pass {  //12
			Name "Final Rescale"
			Blend [_FinalSrcBlend] [_FinalDstBlend]
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment FinalPassFragmentRescale
			ENDHLSL
		}
		
		Pass {  //13
			Name "FXAA"

			Blend [_FinalSrcBlend] [_FinalDstBlend]
			
			HLSLPROGRAM
				#pragma target 3.5
                #include "FXAAPass.hlsl"
				#pragma vertex DefaultPassVertex
				#pragma fragment FXAAPassFragment
			ENDHLSL
		}
		
		Pass {  //14
			Name "Apply Color Grading With Luma"
			Blend [_FinalSrcBlend] [_FinalDstBlend]
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ApplyColorGradingWithLumaPassFragment
			ENDHLSL
		}
		
		Pass {  //15
			Name "FXAA With Luma"

			Blend [_FinalSrcBlend] [_FinalDstBlend]
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment FXAAPassFragment
				#define FXAA_ALPHA_CONTAINS_LUMA
				#include "FXAAPass.hlsl"
			ENDHLSL
		}
    }
   
}
