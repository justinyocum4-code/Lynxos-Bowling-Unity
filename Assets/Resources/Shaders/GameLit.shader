// Lynxos Bowling — own LIT shader, shipped with the game.
// v1.3.0: the v1.1.5 flat shader (GameUnlit) is why the whole game looks
// flat — no light, no shadow, no shine, so a rolling ball reads exactly
// like a still one. This shader does simple Lambert lighting from the
// main directional light, a Blinn-Phong shine highlight, and receives
// real-time shadows — so the ball and pins get shading and a shadow that
// anchors the ball to the lane.
//
// Same proven shipping trick as GameUnlit: a shader asset in a Resources
// folder is always included in the build, so BowlingConfig.FindGameShader
// loads it with Resources.Load<Shader>("Shaders/GameLit") first. If this
// shader ever fails to load, FindGameShader falls back to GameUnlit, so
// a missing GameLit can never go pink — it just renders flat again.
// NEVER assign a built-in (non-URP) shader in this URP project, and never
// rely on Shader.Find for a shader no material uses.
//
// Shadows degrade gracefully: when the URP renderer asset has shadows
// off, GetMainLight returns shadowAttenuation 1 and the scene simply
// renders lit without shadows. The directional light's shadow settings
// are set in LaneBuilder.BuildLightsAndCamera; the URP renderer asset
// itself lives in the GitHub repo (not in this local copy) and must have
// shadows enabled there or real shadows will silently not appear.
Shader "Lynxos/GameLit"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
        _Glossiness ("Glossiness", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float3 positionWS : TEXCOORD0;
                float4 shadowCoord : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _Smoothness;
                float _Glossiness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.shadowCoord = GetShadowCoord(posInputs);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 N = normalize(IN.normalWS);
                Light mainLight = GetMainLight(IN.shadowCoord);
                half3 L = normalize(mainLight.direction);
                half diff = max(dot(N, L), 0.0h);
                // Flat faces pointing away from the light still need to
                // read: a small ambient floor keeps shadowed faces from
                // going black, and the lit faces carry the colour.
                half3 ambient = _Color.rgb * 0.28h;
                half3 lit = _Color.rgb * mainLight.color * diff * mainLight.shadowAttenuation;
                // Blinn-Phong shine: a glossy ball gets a bright spot that
                // moves as it rolls, so the spin is visible. Smoothness
                // comes from either property — the C# material builders set
                // _Smoothness for URP shaders and _Glossiness otherwise.
                float smooth = max(_Smoothness, _Glossiness);
                half3 V = normalize(_WorldSpaceCameraPos - IN.positionWS);
                half3 H = normalize(L + V);
                half spec = pow(max(dot(N, H), 0.0h), 8.0h + smooth * 56.0h) * smooth * mainLight.shadowAttenuation;
                half3 col = ambient + lit + spec * mainLight.color * 0.6h;
                return half4(col, 1.0h);
            }
            ENDHLSL
        }
    }
}
