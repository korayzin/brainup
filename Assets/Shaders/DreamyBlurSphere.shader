Shader "Custom/DreamSphereURP_Dreamy"
{
    Properties
    {
        [Header(Dream Image)]
        [MainTexture] _DreamTex ("Dream Image", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)

        [Header(Dream Fog Noise)]
        _NoiseTex ("Noise Texture", 2D) = "gray" {}
        _FogColor ("Fog Color", Color) = (1, 1, 1, 0.35)
        _NoiseScale ("Noise Tiling", Range(0.2, 6)) = 1.5
        _NoiseSpeed ("Noise Speed", Vector) = (0.05, 0.03, 0, 0)
        _Distort ("UV Distortion", Range(0, 0.05)) = 0.015

        [Header(Blur Control)]
        _Blur ("Blur Amount", Range(0, 1)) = 0.0
        _BlurRadius ("Max Blur Radius", Range(0.0, 0.40)) = 0.16
        _Samples ("Blur Samples", Range(6, 24)) = 16

        [Header(Soft Glow Rim)]
        _RimColor ("Rim Color", Color) = (0.75, 0.9, 1.0, 1.0)
        _RimPower ("Rim Power", Range(0.8, 8.0)) = 2.2
        _RimIntensity ("Rim Intensity", Range(0, 2)) = 0.8

        [Header(Subtle Waves)]
        _WaveAmp ("Wave Amplitude", Range(0, 0.15)) = 0.03
        _WaveFreq ("Wave Frequency", Range(0, 8)) = 1.7
        _WaveSpeed ("Wave Speed", Range(0, 4)) = 1.0

        [Header(Glitch Controls)]
        _Glitch ("Glitch Amount", Range(0, 1)) = 0.0
        _GlitchSpeed ("Glitch Speed", Range(0, 30)) = 12
        _GlitchBlockSize ("Glitch Block Size", Range(4, 120)) = 40
        _RGBShift ("RGB Shift", Range(0, 0.02)) = 0.006
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back
        ZWrite Off

        Pass
        {
            Name "DreamSpherePass"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float3 posWS      : TEXCOORD3;
            };

            TEXTURE2D(_DreamTex);
            SAMPLER(sampler_DreamTex);

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _DreamTex_ST;
                float4 _Tint;

                float4 _FogColor;
                float _NoiseScale;
                float2 _NoiseSpeed;
                float _Distort;

                float _Blur;
                float _BlurRadius;
                int _Samples;

                float4 _RimColor;
                float _RimPower;
                float _RimIntensity;

                float _WaveAmp;
                float _WaveFreq;
                float _WaveSpeed;

                float _Glitch;
                float _GlitchSpeed;
                float _GlitchBlockSize;
                float _RGBShift;
            CBUFFER_END

            // ---------- helpers ----------
            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float2 hash22(float2 p)
            {
                float n = hash12(p);
                return float2(n, hash12(p + n + 19.19));
            }

            float EaseBlur(float x)
            {
                // 0 -> 0, 1 -> 1 (ama 1'e yaklaşınca daha agresif)
                return pow(saturate(x), 3.0);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 pos = IN.positionOS.xyz;

                // Dreamy nefes dalgası (hafif)
                float t = _Time.y * _WaveSpeed;
                float f = max(0.0001, _WaveFreq);

                float wave =
                    sin(pos.x * f + t) * 0.55 +
                    cos(pos.z * f * 0.7 + t * 1.1) * 0.35 +
                    sin((pos.x + pos.z) * f * 0.35 + t * 0.85) * 0.25;

                pos += IN.normalOS * wave * _WaveAmp;

                VertexPositionInputs vp = GetVertexPositionInputs(pos);
                OUT.positionCS = vp.positionCS;
                OUT.posWS = vp.positionWS;

                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS = GetWorldSpaceViewDir(vp.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _DreamTex);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // ---------- GLITCH (titreme + cızırtı blokları) ----------
                float g = saturate(_Glitch);
                if (g > 0.0001)
                {
                    // blok koordinatı
                    float2 blockUV = floor(uv * _GlitchBlockSize);
                    float timeStep = floor(_Time.y * _GlitchSpeed);

                    float rnd = hash12(blockUV + timeStep);
                    float2 jitter = (hash22(blockUV + timeStep * 1.37) * 2.0 - 1.0);

                    // yatay “scanline” kaydırma hissi (cızırtı)
                    float scan = sin((uv.y + rnd) * 900.0 + _Time.y * 40.0) * 0.5 + 0.5;
                    float scanMask = smoothstep(0.85, 1.0, scan) * g;

                    // blok bazlı UV kaydırma + küçük titreme
                    float2 blockShift = float2((rnd - 0.5) * 0.03, 0.0) * g;
                    float2 microJitter = jitter * 0.0035 * g;

                    uv += blockShift + microJitter * (0.35 + 0.65 * scanMask);
                }

                // ---------- NOISE (fog + distortion) ----------
                float2 noiseUV = uv * _NoiseScale + _Time.y * _NoiseSpeed;
                half4 noise = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV);

                float noiseLuma = dot(noise.rgb, half3(0.299h, 0.587h, 0.114h));

                // Distortion blur ile beraber artar (rüya kayması)
                float b = EaseBlur(_Blur);
                float2 distortVec = (noise.rg * 2.0 - 1.0) * _Distort * (0.35 + 0.65 * noiseLuma);
                uv += distortVec * (0.25 + 0.85 * b);

                // ---------- EXTREME BLUR (siyahlamadan maksimum blur) ----------
                int samples = max(6, _Samples);
                float inv = 1.0 / (float)samples;

                // blurRadius: 0->0, 1-> çok büyük (okunmaz)
                // ekstra çarpan: max blur'da gerçekten dağılsın
                float blurRadius = _BlurRadius * b * (1.0 + 5.0 * b);

                half4 accum = 0;
                float wsum = 0;

                // blur pattern’i her frame çok az değişsin (dream shimmer)
                float seed = hash11(floor(_Time.y * 12.0)) * 6.2831853;

                // Spiral + gaussian ağırlık
                for (int i = 0; i < 64; i++)
                {
                    if (i >= samples) break;

                    float u = (i + 0.5) * inv;  // 0..1
                    float r = u * u;            // merkez daha yoğun
                    float ang = (i * 2.39996323) + seed; // golden-angle spiral

                    float2 dir = float2(cos(ang), sin(ang));

                    // r dağılımı + hafif jitter (max blur’da “okunmazlık” artar)
                    float jitterAmt = (0.15 + 0.85 * b) * 0.12;
                    float2 j2 = (hash22(float2(i, seed)) * 2.0 - 1.0) * jitterAmt;

                    float2 offs = (dir + j2) * blurRadius * (r * 2.9);

                    // ağırlık: merkez ağırlıklı ama max blur’da dış örnekler de daha etkili
                    float w = exp(-r * 3.0);
                    w = lerp(w, 1.0, b * 0.35); // max blur’da daha “flat” -> daha fazla smear

                    accum += SAMPLE_TEXTURE2D(_DreamTex, sampler_DreamTex, uv + offs) * (half)w;
                    wsum += w;
                }

                half4 dream = accum / max(1e-5, wsum);
                dream *= _Tint;

                // ---------- RGB SHIFT (glitch için renk kayması) ----------
                if (g > 0.0001)
                {
                    float shift = _RGBShift * g;
                    half rC = SAMPLE_TEXTURE2D(_DreamTex, sampler_DreamTex, uv + float2( shift, 0)).r;
                    half gC = SAMPLE_TEXTURE2D(_DreamTex, sampler_DreamTex, uv + float2(-shift, 0)).g;
                    half bC = SAMPLE_TEXTURE2D(_DreamTex, sampler_DreamTex, uv + float2(0, shift)).b;

                    // Çok hafif uygula (cozy kalsın)
                    dream.rgb = lerp(dream.rgb, half3(rC, gC, bC), 0.35h * (half)g);
                }

                // ---------- FOG MIX (rüya sisi) ----------
                float fogMask = saturate(_FogColor.a * (0.55 + 0.75 * noiseLuma));
                fogMask *= (0.20 + 1.10 * b);      // blur arttıkça sis artsın
                fogMask *= (1.0 + 0.35 * g);       // glitch varsa biraz “toz” gibi artsın

                half3 fogAdd = noise.rgb * _FogColor.rgb * (0.35h + (half)noiseLuma * 0.75h);
                half3 rgb = dream.rgb + fogAdd * (half)fogMask;

                // cozy: highlight compress (siyah basmadan yumuşatır)
                rgb = rgb / (1.0h + rgb * 0.45h);

                // ---------- RIM GLOW ----------
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);

                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower);
                rim = smoothstep(0.05, 1.0, rim);

                float rimBoost = lerp(1.0, 1.35, b) * lerp(1.0, 1.20, g);
                rgb += _RimColor.rgb * rim * _RimIntensity * rimBoost * _RimColor.a;

                // Alpha: siyahlamasın diye alpha’yı sabit tutuyoruz (istersen blur’a bağlı düşürürüz)
                return half4(rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
