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
        
		Pass    //7 
		{
			Name "Tone Mapping Reinhard"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ToneMappingReinhardPassFragment
			ENDHLSL
		}
		
		Pass {  //8
			Name "Tone Mapping Neutral"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ToneMappingNeutralPassFragment
			ENDHLSL
		}
		
		Pass {  //9
			Name "Tone Mapping ACES"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ToneMappingACESPassFragment
			ENDHLSL
		}
    }
   
}
