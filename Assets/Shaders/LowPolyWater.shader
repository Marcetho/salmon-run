Shader "Custom/LowPolyWater"
{
    Properties
    {
        _Color ("Deep Color", Color) = (0, 0.5, 1, 0.5)
        _ShallowColor ("Wave Color", Color) = (0, 0.7, 1, 0.5)
        _WaveSpeed ("Wave Speed", Float) = 1
        _WaveHeight ("Wave Height", Float) = 0.1
        _WaveLength ("Wave Length", Float) = 10
        _FresnelPower ("Edge Power", Range(0.1, 5)) = 1.5
        _UnderwaterVisibility ("Underwater Visibility", Range(0.1, 1)) = 0.5
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 0.8)
        _FoamWidth ("Foam Width", Range(0, 2)) = 0.4
        _RippleScale ("Ripple Scale", Range(0.1, 5)) = 1
        _RippleSpeed ("Ripple Speed", Range(0.1, 5)) = 1
        _RippleStrength ("Ripple Strength", Range(0, 1)) = 0.5
        _WindDirection ("Wind Direction", Vector) = (1,0,0,0)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        LOD 100
        
        ZWrite Off  // Changed from ZWrite On
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldNormal : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                float waveHeight : TEXCOORD2;
                float4 screenPos : TEXCOORD3;
                float3 worldPos : TEXCOORD4;
            };

            fixed4 _Color;
            fixed4 _ShallowColor;
            float _WaveSpeed;
            float _WaveHeight;
            float _WaveLength;
            float _FresnelPower;
            float _UnderwaterVisibility;
            fixed4 _FoamColor;
            float _FoamWidth;
            float _RippleScale;
            float _RippleSpeed;
            float _RippleStrength;
            float4 _WindDirection;

            v2f vert (appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldPos = worldPos;
                
                // Calculate multiple waves for more natural ripples
                float2 windDir = normalize(_WindDirection.xz);
                
                // Main ripples
                float ripple1 = sin(dot(worldPos.xz * _RippleScale, windDir) + _Time.y * _RippleSpeed);
                float ripple2 = sin(dot(worldPos.xz * _RippleScale * 1.4, windDir * 0.8) + _Time.y * _RippleSpeed * 0.8);
                
                // Cross ripples for more variation
                float2 crossDir = float2(-windDir.y, windDir.x);
                float ripple3 = sin(dot(worldPos.xz * _RippleScale * 0.8, crossDir) + _Time.y * _RippleSpeed * 1.2);
                
                // Combine ripples with different weights
                float wave = (ripple1 * 0.5 + ripple2 * 0.3 + ripple3 * 0.2) * _RippleStrength * _WaveHeight;
                
                // Add small noise variation
                wave += sin(worldPos.x * 8.0 + worldPos.z * 6.0 + _Time.y * 0.5) * _WaveHeight * 0.1;
                
                // Apply height modification
                v.vertex.y += wave;
                
                // Modify normal based on ripples
                float3 modifiedNormal = v.normal;
                modifiedNormal.xz += wave * 0.05; // Reduced from 0.1 for subtler normal modification
                modifiedNormal = normalize(modifiedNormal);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                o.worldNormal = UnityObjectToWorldNormal(modifiedNormal);
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                o.waveHeight = wave / _WaveHeight;
                
                o.screenPos = ComputeScreenPos(o.vertex);
                COMPUTE_EYEDEPTH(o.screenPos.z);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                bool isUnderwater = _WorldSpaceCameraPos.y < mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).y;
                float facing = dot(i.worldNormal, i.viewDir) > 0 ? 1 : -1;
                bool isBackFace = facing < 0;
                
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV));
                float surfaceDepth = i.screenPos.z;
                float depthDifference = sceneDepth - surfaceDepth;
                
                float foam = saturate(1 - (depthDifference / _FoamWidth));
                float ndot1 = max(0, dot(i.worldNormal, float3(0, 1, 0)));
                
                float fresnel = pow(1.0 - saturate(dot(normalize(i.worldNormal), i.viewDir)), _FresnelPower);
                
                fixed4 finalColor = lerp(_Color, _ShallowColor, i.waveHeight * 0.5 + 0.5);
                finalColor = lerp(finalColor, _FoamColor, foam);
                
                if (isUnderwater)
                {
                    // Underwater behavior - maintain transparency for ghost fish
                    finalColor = lerp(_Color, _ShallowColor, i.waveHeight * 0.5 + 0.5);
                    finalColor.rgb *= 0.9;
                    finalColor.rgb *= (ndot1 * 0.3 + 0.7);
                    finalColor.rgb += fresnel * _ShallowColor.rgb * 0.1;
                    
                    // Preserve original alpha for ghost fish visibility
                    float viewAngleAlpha = saturate(1 - abs(dot(i.worldNormal, i.viewDir)) + 0.2);
                    finalColor.a = viewAngleAlpha * finalColor.a;
                }
                else
                {
                    finalColor.rgb *= (ndot1 * 0.3 + 0.7);
                    finalColor.rgb += fresnel * _ShallowColor.rgb * 0.2;
                }
                
                return finalColor;
            }
            ENDCG
        }
    }
}
