Shader "MeditationVR/AmbientFlowSphere"
{
    Properties
    {
        _ColorA ("Color A", Color) = (0.08,0.24,0.70,1)
        _ColorB ("Color B", Color) = (0.12,0.63,0.85,1)
        _AccentColor ("Accent Color", Color) = (0.18,0.42,0.88,1)
        _FlowSpeed ("Flow Speed", Range(0.01, 2.0)) = 0.20
        _NoiseScale ("Noise Scale", Range(0.1, 8.0)) = 2.20
        _StreakScale ("Detail Scale", Range(1.0, 40.0)) = 10.0
        _StreakIntensity ("Detail Intensity", Range(0.0, 3.0)) = 0.35
        _EmissionIntensity ("Emission Intensity", Range(0.0, 3.0)) = 0.70
        _Cull ("Cull", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull [_Cull]
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ColorA;
                half4 _ColorB;
                half4 _AccentColor;
                half _FlowSpeed;
                half _NoiseScale;
                half _StreakScale;
                half _StreakIntensity;
                half _EmissionIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);

                float n000 = hash31(i + float3(0, 0, 0));
                float n100 = hash31(i + float3(1, 0, 0));
                float n010 = hash31(i + float3(0, 1, 0));
                float n110 = hash31(i + float3(1, 1, 0));
                float n001 = hash31(i + float3(0, 0, 1));
                float n101 = hash31(i + float3(1, 0, 1));
                float n011 = hash31(i + float3(0, 1, 1));
                float n111 = hash31(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, u.x);
                float nx10 = lerp(n010, n110, u.x);
                float nx01 = lerp(n001, n101, u.x);
                float nx11 = lerp(n011, n111, u.x);
                float nxy0 = lerp(nx00, nx10, u.y);
                float nxy1 = lerp(nx01, nx11, u.y);
                return lerp(nxy0, nxy1, u.z);
            }

            float fbm(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 5; i++)
                {
                    value += noise3D(p) * amplitude;
                    p = p * 2.03 + 17.17;
                    amplitude *= 0.5;
                }
                return saturate(value);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                float3 p = normalize(IN.positionOS);
                float t = _Time.y * _FlowSpeed;

                // Broad, smooth flow field (no hard banding/streak lines).
                float nBroadA = fbm(p * _NoiseScale + float3(t * 0.05, t * 0.03, -t * 0.02));
                float nBroadB = fbm(p * (_NoiseScale * 1.25) + float3(-t * 0.03, t * 0.04, t * 0.025));
                float blend = smoothstep(0.15, 0.85, 0.6 * nBroadA + 0.4 * nBroadB);
                float3 baseColor = lerp(_ColorA.rgb, _ColorB.rgb, blend);

                // Very soft detail to keep motion alive without visible striping.
                float nDetail = fbm(p * _StreakScale * 0.08 + float3(t * 0.08, -t * 0.05, t * 0.04));
                float detailMask = (nDetail - 0.5) * (_StreakIntensity * 0.35);

                float3 color = baseColor + _AccentColor.rgb * detailMask;
                color += baseColor * (_EmissionIntensity * 0.35);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
