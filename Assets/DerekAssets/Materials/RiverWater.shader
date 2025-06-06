Shader "Custom/RiverWater"
{
    Properties
    {
        [Header(Surface)]
        _Color ("Water Color", Color) = (0.1, 0.4, 0.7, 0.8)
        _DeepColor ("Deep Water Color", Color) = (0.05, 0.2, 0.4, 1.0)
        _Transparency ("Transparency", Range(0, 1)) = 0.7
        _FresnelPower ("Fresnel Power", Range(0.1, 5)) = 2.0
        
        [Header(Normal Mapping)]
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1.0
        _NormalSpeed ("Normal Speed", Vector) = (0.1, 0.1, 0, 0)
        
        [Header(Flow)]
        _FlowMap ("Flow Map (RG)", 2D) = "gray" {}
        _FlowSpeed ("Flow Speed", Range(0, 2)) = 1.0
        _FlowStrength ("Flow Strength", Range(0, 1)) = 0.5
        
        [Header(Foam)]
        _FoamTexture ("Foam Texture", 2D) = "white" {}
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _FoamCutoff ("Foam Cutoff", Range(0, 1)) = 0.3
        _FoamSpeed ("Foam Speed", Vector) = (0.2, 0.15, 0, 0)
        
        [Header(Depth)]
        _DepthFactor ("Depth Factor", Range(0, 10)) = 2.0
        _ShoreBlend ("Shore Blend", Range(0, 5)) = 1.0
        
        [Header(Reflection)]
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.3
        _Smoothness ("Smoothness", Range(0, 1)) = 0.8
        _Metallic ("Metallic", Range(0, 1)) = 0.0
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        
        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert
        #pragma target 3.0
        
        sampler2D _NormalMap;
        sampler2D _FlowMap;
        sampler2D _FoamTexture;
        sampler2D _CameraDepthTexture;
        
        fixed4 _Color;
        fixed4 _DeepColor;
        fixed4 _FoamColor;
        
        float _Transparency;
        float _FresnelPower;
        float _NormalStrength;
        float4 _NormalSpeed;
        float _FlowSpeed;
        float _FlowStrength;
        float _FoamCutoff;
        float4 _FoamSpeed;
        float _DepthFactor;
        float _ShoreBlend;
        float _ReflectionStrength;
        float _Smoothness;
        float _Metallic;
        
        struct Input
        {
            float2 uv_NormalMap;
            float2 uv_FlowMap;
            float2 uv_FoamTexture;
            float3 worldPos;
            float3 viewDir;
            float4 screenPos;
        };
        
        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            
            // Add subtle vertex animation for river flow
            float time = _Time.y * _FlowSpeed;
            float wave = sin(v.vertex.x * 0.5 + time) * 0.02;
            v.vertex.y += wave;
        }
        
        // Flow mapping function
        float3 FlowUVW(float2 uv, float2 flowVector, float time, bool flowB)
        {
            float phaseOffset = flowB ? 0.5 : 0.0;
            float progress = frac(time + phaseOffset);
            float3 uvw;
            uvw.xy = uv - flowVector * progress;
            uvw.z = 1.0 - abs(1.0 - 2.0 * progress);
            return uvw;
        }
        
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float time = _Time.y;
            
            // Sample flow map
            float2 flowVector = tex2D(_FlowMap, IN.uv_FlowMap).rg * 2.0 - 1.0;
            flowVector *= _FlowStrength;
            
            // Calculate flow UVs
            float3 flowUVWA = FlowUVW(IN.uv_NormalMap, flowVector, time * _FlowSpeed, false);
            float3 flowUVWB = FlowUVW(IN.uv_NormalMap, flowVector, time * _FlowSpeed, true);
            
            // Sample normals with flow
            float3 normalA = UnpackScaleNormal(tex2D(_NormalMap, flowUVWA.xy), _NormalStrength);
            float3 normalB = UnpackScaleNormal(tex2D(_NormalMap, flowUVWB.xy), _NormalStrength);
            
            // Blend normals based on flow phase
            float3 flowNormal = normalize(normalA * flowUVWA.z + normalB * flowUVWB.z);
            
            // Add secondary normal animation for more detail
            float2 normalUV2 = IN.uv_NormalMap + _NormalSpeed.xy * time;
            float3 detailNormal = UnpackScaleNormal(tex2D(_NormalMap, normalUV2 * 2.0), _NormalStrength * 0.5);
            
            o.Normal = normalize(flowNormal + detailNormal * 0.3);
            
            // Calculate depth for shore blending
            float screenDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
            float surfaceDepth = IN.screenPos.z;
            float depthDifference = saturate((screenDepth - surfaceDepth) / _DepthFactor);
            
            // Shore foam
            float2 foamUV = IN.uv_FoamTexture + _FoamSpeed.xy * time;
            float foamNoise = tex2D(_FoamTexture, foamUV).r;
            float foam = step(_FoamCutoff + depthDifference * 0.5, foamNoise) * (1.0 - depthDifference);
            
            // Water color based on depth
            fixed4 waterColor = lerp(_Color, _DeepColor, depthDifference);
            
            // Fresnel effect
            float fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), o.Normal)), _FresnelPower);
            
            // Combine colors
            o.Albedo = lerp(waterColor.rgb, _FoamColor.rgb, foam);
            o.Albedo = lerp(o.Albedo, _DeepColor.rgb, fresnel * 0.3);
            
            // Surface properties
            o.Metallic = _Metallic;
            o.Smoothness = lerp(_Smoothness, 0.1, foam);
            
            // Alpha with depth and shore blending
            float alpha = lerp(_Transparency, 1.0, fresnel * _ReflectionStrength);
            alpha = lerp(alpha, 1.0, foam);
            alpha *= saturate(depthDifference * _ShoreBlend);
            
            o.Alpha = alpha;
        }
        ENDCG
    }
    
    Fallback "Transparent/Diffuse"
}