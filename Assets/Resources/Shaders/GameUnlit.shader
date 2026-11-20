// Lynxos Bowling — own unlit shader, shipped with the game.
// v1.1.5: Shader.Find cannot find ANY shader in the player build, because
// the template ships no materials, so Unity strips every URP/built-in
// shader out of the build (v1.1.4's whole lookup chain came back empty
// and the game showed "No 3D shader found"). A shader asset in a
// Resources folder is always included in the build, so this file is the
// one shader the game can count on. Flat colour, no lighting — the lane,
// pins and ball are simple shapes and read fine in flat gold/wood/white.
Shader "Lynxos/GameUnlit"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(_Color.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
