Shader "Custom/CartoonRiverWater"
{
    Properties
    {
        [Header(Surface Colors)]
        _ShallowColor ("Shallow Water Color", Color) = (0.4, 0.8, 1.0, 0.8)
        _DeepColor ("Deep Water Color", Color) = (0.1, 0.5, 0.8, 1.0)
        _FoamColor ("Shore Foam Color", Color) = (1, 1, 1, 1)
        _Transparency ("Transparency", Range(0, 1)) = 0.6
        
        [Header(Cell Shading)]
        _ShadingSteps ("Shading Steps", Range(2, 8)) = 4
        _ShadingSmooth ("Shading Smoothness", Range(0, 0.2)) = 0.05
        _RimPower ("Rim Light Power", Range(0.1, 8)) = 2.0
        _RimColor ("Rim Light Color", Color) = (0.8, 0.9, 1.0, 1)
        
        [Header(Animation)]
        _WaveSpeed ("Wave Speed", Range(0, 3)) = 1.0
        _WaveFrequency ("Wave Frequency", Range(0.1, 5)) = 2.0
        _WaveAmplitude ("Wave Amplitude", Range(0, 0.1)) = 0.02
        
        [Header(Shore Effects)]
        _ShoreWidth ("Shore Foam Width", Range(0, 5)) = 2.0
        _ShoreIntensity ("Shore Foam Intensity", Range(0, 2)) = 1.5
        _FoamNoiseScale ("Foam Noise Scale", Range(1, 20)) = 8.0
        _FoamSpeed ("Foam Animation Speed", Range(0, 2)) = 0.8
        
        [Header(Flow)]
        _FlowDirection ("Flow Direction", Vector) = (0, 0, 1, 0)
        _FlowSpeed ("Flow Speed", Range(0, 2)) = 0.5
        _FlowDistortion ("Flow Distortion", Range(0, 1)) = 0.3
        
        [Header(Simple Lighting)]
        _Brightness ("Overall Brightness", Range(0.5, 2)) = 1.2
        _ShadowTint ("Shadow Tint", Color) = (0.7, 0.8, 0.9, 1)
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Toon alpha:fade vertex:vert
        #pragma target 3.0
        
        sampler2D _CameraDepthTexture;
        
        fixed4 _ShallowColor;
        fixed4 _DeepColor;
        fixed4 _FoamColor;
        fixed4 _RimColor;
        fixed4 _ShadowTint;
        
        float _Transparency;
        float _ShadingSteps;
        float _ShadingSmooth;
        float _RimPower;
        float _WaveSpeed;
        float _WaveFrequency;
        float _WaveAmplitude;
        float _ShoreWidth;
        float _ShoreIntensity;
        float _FoamNoiseScale;
        float _FoamSpeed;
        float4 _FlowDirection;
        float _FlowSpeed;
        float _FlowDistortion;
        float _Brightness;
        
        struct Input
        {
            float2 uv_MainTex;
            float3 worldPos;
            float3 viewDir;
            float4 screenPos;
        };
        
        // Custom toon lighting model
        half4 LightingToon(SurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
        {
            half NdotL = dot(s.Normal, lightDir);
            
            // Stepped lighting for cell shading
            half lightIntensity = smoothstep(0, _ShadingSmooth, NdotL);
            lightIntensity = floor(lightIntensity * _ShadingSteps) / _ShadingSteps;
            
            // Rim lighting
            half rim = 1.0 - saturate(dot(viewDir, s.Normal));
            rim = pow(rim, _RimPower);
            
            half4 c;
            c.rgb = s.Albedo * _LightColor0.rgb * lightIntensity * atten * _Brightness;
            c.rgb = lerp(c.rgb, c.rgb * _ShadowTint.rgb, 1.0 - lightIntensity);
            c.rgb += _RimColor.rgb * rim;
            c.a = s.Alpha;
            
            return c;
        }
        
        // Simple noise function
        float noise(float2 pos)
        {
            return frac(sin(dot(pos, float2(12.9898, 78.233))) * 43758.5453);
        }
        
        // Smooth noise
        float smoothNoise(float2 pos)
        {
            float2 i = floor(pos);
            float2 f = frac(pos);
            f = f * f * (3.0 - 2.0 * f); // smooth interpolation
            
            float a = noise(i);
            float b = noise(i + float2(1, 0));
            float c = noise(i + float2(0, 1));
            float d = noise(i + float2(1, 1));
            
            return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
        }
        
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            // Cartoon-style wave animation
            float time = _Time.y * _WaveSpeed;
            float wave1 = sin((v.vertex.x + v.vertex.z) * _WaveFrequency + time) * _WaveAmplitude;
            float wave2 = sin((v.vertex.x * 1.3 - v.vertex.z * 0.7) * _WaveFrequency * 1.2 + time * 1.5) * _WaveAmplitude * 0.5;
            
            v.vertex.y += wave1 + wave2;
        }
        
        void surf(Input IN, inout SurfaceOutput o)
        {
            float time = _Time.y;
            
            // Calculate depth for shore effects
            float screenDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
            float surfaceDepth = IN.screenPos.z;
            float depthDifference = saturate((screenDepth - surfaceDepth) / _ShoreWidth);
            
            // Flow-based UV distortion
            float2 flowUV = IN.worldPos.xz * 0.1;
            flowUV += _FlowDirection.xz * time * _FlowSpeed;
            float flowNoise = smoothNoise(flowUV * 2) * 2 - 1;
            
            // Animated foam noise
            float2 foamUV = IN.worldPos.xz * _FoamNoiseScale * 0.1;
            foamUV += float2(flowNoise * _FlowDistortion, time * _FoamSpeed);
            
            float foamNoise1 = smoothNoise(foamUV);
            float foamNoise2 = smoothNoise(foamUV * 2.3 + float2(0.5, 0.5));
            float combinedFoam = (foamNoise1 + foamNoise2 * 0.5) / 1.5;
            
            // Shore foam calculation - stronger near shores
            float shoreDistance = 1.0 - depthDifference;
            float foamMask = smoothstep(0.3, 0.7, combinedFoam + shoreDistance * _ShoreIntensity);
            foamMask *= smoothstep(0.9, 0.3, depthDifference); // Fade foam in deeper water
            
            // Color mixing based on depth
            fixed4 waterColor = lerp(_DeepColor, _ShallowColor, 1.0 - depthDifference);
            
            // Apply foam
            o.Albedo = lerp(waterColor.rgb, _FoamColor.rgb, foamMask);
            
            // Simple cartoon normal - mostly flat with slight variation
            float2 normalOffset = float2(flowNoise, sin(time + IN.worldPos.x * 0.5)) * 0.1;
            o.Normal = normalize(float3(normalOffset.x, 1, normalOffset.y));
            
            // Stepped alpha for more cartoon-like transparency
            float alpha = waterColor.a * _Transparency;
            alpha += foamMask * (1.0 - alpha); // Foam is more opaque
            alpha *= smoothstep(0, 0.3, depthDifference); // Fade at shores
            
            // Quantize alpha slightly for more cartoon feel
            alpha = floor(alpha * 4) / 4;
            
            o.Alpha = alpha;
        }
        ENDCG
    }
    
    Fallback "Transparent/Diffuse"
}