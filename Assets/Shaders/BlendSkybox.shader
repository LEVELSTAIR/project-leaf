Shader "Custom/BlendSkybox"
{
    Properties
    {
        _Skybox1 ("Skybox 1", Cube) = "" {}
        _Skybox2 ("Skybox 2", Cube) = "" {}
        _Blend ("Blend", Range(0,1)) = 0.5
        _Rotation ("Rotation", Range(0,360)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Background" "Queue"="Background" }
        Pass
        {
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderVariablesFunctions.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 texcoord : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 texcoord : TEXCOORD0;
            };

            TEXTURECUBE(_Skybox1);
            TEXTURECUBE(_Skybox2);
            SAMPLER(sampler_Skybox1);
            SAMPLER(sampler_Skybox2);
            float _Blend;
            float _Rotation;

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.texcoord = input.texcoord;
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Apply rotation to direction
                float3 dir = input.texcoord;
                float rotRad = radians(_Rotation);
                float cosA = cos(rotRad);
                float sinA = sin(rotRad);
                // Rotate around Y axis (optional)
                float3 rotDir = float3(
                    dir.x * cosA - dir.z * sinA,
                    dir.y,
                    dir.x * sinA + dir.z * cosA
                );

                half4 col1 = SAMPLE_TEXTURECUBE(_Skybox1, sampler_Skybox1, rotDir);
                half4 col2 = SAMPLE_TEXTURECUBE(_Skybox2, sampler_Skybox2, rotDir);
                half4 col = lerp(col1, col2, _Blend);
                return col;
            }
            ENDHLSL
        }
    }
}