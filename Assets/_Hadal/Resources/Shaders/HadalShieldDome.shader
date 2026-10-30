// The energy dome over a shielded base (docs/30 §3, ShieldDome.cs): a glowing rim where the dome turns away from the
// camera (fresnel), a faint hexagon lattice over the surface, and a bright band that rolls upward like the genre's
// force fields. Additive and unlit, so the base stays readable under it on both the light seabed and dark water.
// It lives in Resources so the build always carries it (ShieldDome finds it by name at runtime).
Shader "Hadal/ShieldDome"
{
    Properties
    {
        _Color ("Colour", Color) = (0.25, 0.9, 1, 0.2)
        _Rim ("Rim strength", Range(0, 4)) = 1.6
        _Grid ("Hexagon strength", Range(0, 2)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Blend SrcAlpha One
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Rim;
                half _Grid;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                float3 world = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(world);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.viewWS = GetWorldSpaceViewDir(world);
                o.positionOS = input.positionOS.xyz;
                return o;
            }

            // Distance to the nearest hexagon edge (0 on an edge), on a pointy-top lattice.
            float HexEdge(float2 p)
            {
                const float2 r = float2(1.0, 1.7320508);
                float2 a = fmod(abs(p), r) - r * 0.5;
                float2 b = fmod(abs(p - r * 0.5), r) - r * 0.5;
                float2 g = dot(a, a) < dot(b, b) ? a : b;
                g = abs(g);
                return 0.5 - max(dot(g, normalize(r)), g.x);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                float3 v = normalize(i.viewWS);
                float rim = pow(1.0 - saturate(abs(dot(n, v))), 2.2) * _Rim;

                // Unit sphere in object space: wrap the lattice around it and roll a band upward.
                float3 p = i.positionOS * 2.0;
                float2 uv = float2(atan2(p.z, p.x) * 3.2, p.y * 5.0);
                float edge = 1.0 - smoothstep(0.0, 0.07, HexEdge(uv));
                float band = pow(0.5 + 0.5 * sin(p.y * 7.0 - _Time.y * 2.2), 6.0);
                float grid = edge * _Grid * (0.35 + 0.65 * band);

                // Only the half above the seabed shows, fading in just above the ground line.
                float above = smoothstep(-0.02, 0.12, i.positionOS.y);
                float alpha = saturate(_Color.a * 0.6 + rim * 0.55 + grid * 0.5 + band * 0.08) * above;
                return half4(_Color.rgb * (1.0 + rim * 0.6), alpha);
            }
            ENDHLSL
        }
    }
}
