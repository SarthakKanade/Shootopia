Shader "Shootopia/ForcefieldBarrier"
{
    Properties
    {
        [Header(Color and Transparency)]
        _BaseColor("Base Color", Color) = (0.05, 0.45, 0.9, 0.35)
        _RimColor("Rim / Edge Glow", Color) = (0.3, 0.9, 1.0, 1.0)
        _RimPower("Rim Power", Range(0.5, 8.0)) = 2.5
        _Alpha("Overall Alpha", Range(0.0, 1.0)) = 0.65

        [Header(Holographic Grid)]
        _GridColor("Grid Line Color", Color) = (0.4, 0.85, 1.0, 0.9)
        _GridTiling("Grid Tiling (XY)", Vector) = (15, 8, 0, 0)
        _GridThickness("Grid Line Thickness", Range(0.01, 0.2)) = 0.05
        _ScrollSpeed("Grid Scroll Speed (XY)", Vector) = (0.0, 0.2, 0, 0)

        [Header(Energy Animation)]
        _ScanlineSpeed("Scanline Speed", Float) = 1.2
        _ScanlineDensity("Scanline Density", Float) = 25.0
        _PulseSpeed("Pulse Speed", Float) = 2.0
        _PulseIntensity("Pulse Intensity", Range(0.0, 0.5)) = 0.2
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _RimColor;
                float _RimPower;
                float _Alpha;

                float4 _GridColor;
                float4 _GridTiling;
                float _GridThickness;
                float4 _ScrollSpeed;

                float _ScanlineSpeed;
                float _ScanlineDensity;
                float _PulseSpeed;
                float _PulseIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normalize(normInputs.normalWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDirWS = normalize(GetCameraPositionWS() - input.positionWS);
                float3 normalWS = normalize(input.normalWS);
                // Make normal face camera if double-sided
                if (dot(normalWS, viewDirWS) < 0.0)
                {
                    normalWS = -normalWS;
                }

                // 1. Fresnel / Rim Glow
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fresnel = pow(1.0 - NdotV, _RimPower);

                // 2. Animated Grid
                float2 scrolledUV = input.uv * _GridTiling.xy + _ScrollSpeed.xy * _Time.y;
                float2 gridFract = frac(scrolledUV);
                float gridX = step(gridFract.x, _GridThickness) + step(1.0 - _GridThickness, gridFract.x);
                float gridY = step(gridFract.y, _GridThickness) + step(1.0 - _GridThickness, gridFract.y);
                float gridLine = saturate(gridX + gridY);

                // 3. Scanline Wave
                float scanline = sin((input.positionWS.y * _ScanlineDensity) - (_Time.y * _ScanlineSpeed * 5.0));
                scanline = smoothstep(0.7, 1.0, scanline);

                // 4. Energy Pulse (breathing pulse)
                float pulse = sin(_Time.y * _PulseSpeed) * _PulseIntensity;

                // 5. Color Composite
                float3 col = _BaseColor.rgb;
                col = lerp(col, _RimColor.rgb, fresnel);
                col += _GridColor.rgb * (gridLine * 0.85);
                col += _RimColor.rgb * (scanline * 0.4);

                // 6. Alpha Composite
                float finalAlpha = _BaseColor.a + (fresnel * _RimColor.a * 0.6) + (gridLine * 0.4) + (scanline * 0.25) + pulse;
                finalAlpha = saturate(finalAlpha * _Alpha);

                return half4(col, finalAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
