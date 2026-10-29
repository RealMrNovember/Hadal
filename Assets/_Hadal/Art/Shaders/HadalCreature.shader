// Sea creatures on the world map (art/gpu/creatures.json → art/blender/props.py → Art/Creatures).
// TRELLIS meshes have no rig, so the motion that reads at map distance is done here in the vertex stage, in object
// space (metres, head along +Z, up +Y; CreatureProfiles sets the numbers per creature):
//   swim     the body sways sideways, more toward the tail, as a travelling wave (fish, anglers, goosefish)
//   tentacle everything below _TentacleTop waves, more the further down (jellies, kraken)
//   pulse    the bell / mantle squashes and stretches (jellies, kraken)
//   claws    the front corners lift and pinch in short snaps (crab)
// Bright saturated texels glow (bioluminescence) and _Flash (MaterialPropertyBlock) tints the body on a hit.
// Lighting is a cheap half-Lambert main light + SH ambient + fog: no shadows, one pass, SRP-batcher compatible.
Shader "Hadal/Creature"
{
    Properties
    {
        _BaseMap ("Albedo", 2D) = "white" {}
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        [HDR] _GlowColor ("Glow colour", Color) = (0.4, 1.0, 1.0, 1)
        _Glow ("Glow strength", Range(0, 6)) = 1.2
        _GlowThreshold ("Glow from brightness", Range(0, 1)) = 0.55

        _HalfLength ("Half body length (m)", Float) = 0.7
        _HalfWidth ("Half body width (m)", Float) = 0.4
        _SwimAmp ("Swim sway (share of half length)", Range(0, 0.5)) = 0.08
        _SwimFreq ("Swim speed", Float) = 4
        _SwimWaves ("Waves along the body", Float) = 0.8

        _TentacleTop ("Tentacles start below (m)", Float) = -10
        _TentacleLen ("Tentacle length (m)", Float) = 1
        _TentacleAmp ("Tentacle wave (m)", Float) = 0
        _TentacleFreq ("Tentacle speed", Float) = 2

        _PulseAmp ("Pulse", Range(0, 0.3)) = 0
        _PulseFreq ("Pulse speed", Float) = 2.2

        _ClawAmp ("Claw snap (m)", Float) = 0
        _ClawFront ("Claws from (share of half length)", Range(-1, 1)) = 0.3
        _ClawSide ("Claws beyond (share of half width)", Range(0, 1)) = 0.35
        _ClawFreq ("Claw speed", Float) = 1.3

        _Flash ("Hit flash", Range(0, 1)) = 0
        _FlashColor ("Hit colour", Color) = (1, 0.45, 0.35, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _GlowColor;
            half _Glow;
            half _GlowThreshold;
            float _HalfLength;
            float _HalfWidth;
            float _SwimAmp;
            float _SwimFreq;
            float _SwimWaves;
            float _TentacleTop;
            float _TentacleLen;
            float _TentacleAmp;
            float _TentacleFreq;
            float _PulseAmp;
            float _PulseFreq;
            float _ClawAmp;
            float _ClawFront;
            float _ClawSide;
            float _ClawFreq;
            half _Flash;
            half4 _FlashColor;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        // Each instance gets its own phase from where it stands, so a school never moves in lockstep.
        float Phase()
        {
            float3 o = float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);
            return frac(dot(o, float3(0.137, 0.071, 0.113))) * 6.2831;
        }

        float3 Deform(float3 p, float t)
        {
            float halfLength = max(_HalfLength, 0.01);

            // Tentacles: 0 at the top of the skirt, 1 at the tips.
            float hang = saturate((_TentacleTop - p.y) / max(_TentacleLen, 0.001));
            float body = 1.0 - hang;

            // Bell / mantle pulse: squash and stretch the body, the tentacles follow.
            float pulse = sin(t * _PulseFreq) * _PulseAmp;
            p.xz *= 1.0 - pulse * body;
            p.y *= 1.0 + pulse * body * 0.8;

            // Swim: 0 at the head, 1 at the tail; the sway grows toward the tail and travels along the body.
            float along = saturate((halfLength - p.z) / (2.0 * halfLength));
            float sway = sin(t * _SwimFreq - along * _SwimWaves * 6.2831);
            p.x += sway * _SwimAmp * halfLength * (0.12 + along * along * 1.3);

            // Tentacles wave around, more toward the tips; neighbours (by angle) out of phase.
            float around = atan2(p.x, p.z);
            float wave = t * _TentacleFreq + p.y * 2.5 + around * 2.0;
            float reach = _TentacleAmp * hang * hang;
            p.x += sin(wave) * reach;
            p.z += cos(wave * 0.83) * reach;

            // Claws: the front corners snap up and in, a short sharp beat with a long rest.
            float claw = smoothstep(_ClawFront, _ClawFront + 0.25, p.z / halfLength)
                       * smoothstep(_ClawSide, _ClawSide + 0.25, abs(p.x) / max(_HalfWidth, 0.01));
            float snap = pow(saturate(sin(t * _ClawFreq)), 8.0);
            p.y += claw * snap * _ClawAmp;
            p.x -= sign(p.x) * claw * snap * _ClawAmp * 0.6;
            return p;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half fog : TEXCOORD2;
                half pulse : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                float phase = Phase();
                float t = _Time.y + phase;
                float3 positionOS = Deform(input.positionOS.xyz, t);
                float3 positionWS = TransformObjectToWorld(positionOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fog = ComputeFogFactor(output.positionCS.z);
                output.pulse = 0.75 + 0.25 * sin(_Time.y * 1.7 + phase * 3.0);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half3 normal = normalize(input.normalWS);
                Light light = GetMainLight();
                half wrap = saturate(dot(normal, light.direction) * 0.5 + 0.5);
                half3 lit = albedo.rgb * (light.color * wrap * wrap + SampleSH(normal));

                // Bioluminescence: bright, saturated texels (cyan spots, violet seams, amber lures) light up.
                half high = max(albedo.r, max(albedo.g, albedo.b));
                half low = min(albedo.r, min(albedo.g, albedo.b));
                half mask = smoothstep(_GlowThreshold, _GlowThreshold + 0.2, high) * saturate((high - low) * 2.5);
                half3 color = lit + albedo.rgb * _GlowColor.rgb * mask * _Glow * input.pulse;

                color = lerp(color, _FlashColor.rgb, _Flash);
                color = MixFog(color, input.fog);
                return half4(color, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                output.positionCS = TransformObjectToHClip(Deform(input.positionOS.xyz, _Time.y + Phase()));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
