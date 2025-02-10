Shader "Custom/LowPolyWater"
{
    Properties
    {
        _Color ("Deep Water Color", Color) = (0, 0.3, 0.5, 0.8)
        _ShallowColor ("Shallow Water Color", Color) = (0, 0.8, 1, 0.4)
        _WaveSpeed ("Wave Speed", Float) = 1
        _WaveHeight ("Wave Height", Float) = 0.1
        _WaveFrequency ("Wave Frequency", Float) = 1
        _SecondaryWaveFreq ("Secondary Wave Frequency", Float) = 2
        _Glossiness ("Smoothness", Range(0,1)) = 0.9
        _FresnelPower ("Fresnel Power", Range(0.1, 10)) = 5
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard alpha:fade
        #pragma target 3.0

        struct Input
        {
            float2 uv_NormalMap;
            float3 worldNormal;
            float3 viewDir;
            float4 screenPos;
        };

        fixed4 _Color;
        fixed4 _ShallowColor;
        float _WaveSpeed;
        float _WaveHeight;
        float _WaveFrequency;
        float _SecondaryWaveFreq;
        float _Glossiness;
        float _FresnelPower;
        sampler2D _NormalMap;
        float _NormalStrength;

        void vert (inout appdata_full v) 
        {
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            
            // Primary wave
            float wave1 = sin(worldPos.x * _WaveFrequency + unity_Time.y * _WaveSpeed) * 
                         cos(worldPos.z * _WaveFrequency + unity_Time.y * _WaveSpeed) * _WaveHeight;
            
            // Secondary wave
            float wave2 = sin(worldPos.x * _SecondaryWaveFreq - unity_Time.y * _WaveSpeed * 0.5) * 
                         cos(worldPos.z * _SecondaryWaveFreq + unity_Time.y * _WaveSpeed * 0.5) * (_WaveHeight * 0.5);
            
            v.vertex.y += wave1 + wave2;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Normal mapping
            float3 normal = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap + unity_Time.y * _WaveSpeed * 0.1));
            normal.xy *= _NormalStrength;
            o.Normal = normalize(normal);

            // Fresnel effect
            float fresnel = pow(1.0 - saturate(dot(o.Normal, IN.viewDir)), _FresnelPower);
            
            // Blend between shallow and deep water colors
            float waterDepth = saturate(IN.screenPos.w);
            fixed4 finalColor = lerp(_ShallowColor, _Color, waterDepth);
            
            o.Albedo = finalColor.rgb;
            o.Smoothness = _Glossiness;
            o.Alpha = lerp(finalColor.a, 1.0, fresnel);
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
