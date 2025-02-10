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
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
        LOD 100
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

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
            };

            fixed4 _Color;
            fixed4 _ShallowColor;
            float _WaveSpeed;
            float _WaveHeight;
            float _WaveLength;
            float _FresnelPower;
            float _UnderwaterVisibility;

            v2f vert (appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                // Create waves using sine waves
                float wave = sin(worldPos.x / _WaveLength + _Time.y * _WaveSpeed) * 
                           cos(worldPos.z / _WaveLength + _Time.y * _WaveSpeed) * 
                           _WaveHeight;
                
                // Add subtle normal distortion
                float3 modifiedNormal = v.normal;
                modifiedNormal.xz += wave * 0.1;
                modifiedNormal = normalize(modifiedNormal);
                
                v.vertex.y += wave;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                o.worldNormal = UnityObjectToWorldNormal(modifiedNormal);
                o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
                o.waveHeight = wave / _WaveHeight;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Check if camera is underwater
                bool isUnderwater = _WorldSpaceCameraPos.y < mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).y;
                
                float ndot1 = max(0, dot(i.worldNormal, float3(0, 1, 0)));
                
                // Reduce fresnel effect underwater
                float fresnelPower = isUnderwater ? _FresnelPower * 0.5 : _FresnelPower;
                float fresnel = pow(1.0 - saturate(dot(normalize(i.worldNormal), i.viewDir)), fresnelPower);
                
                fixed4 finalColor = lerp(_Color, _ShallowColor, i.waveHeight * 0.5 + 0.5);
                
                if (isUnderwater)
                {
                    // Enhance contrast underwater
                    finalColor.rgb *= (ndot1 * 0.7 + 0.3);
                    finalColor.a *= _UnderwaterVisibility;
                    finalColor.rgb *= _UnderwaterVisibility * 1.5; // Brighten underwater
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
