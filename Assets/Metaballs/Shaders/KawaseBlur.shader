Shader "Hidden/KawaseBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float _Offset;
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            sampler2D _MetaballDepthRT;
            float _BlurDistance;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 applyBlur(const fixed4 color, const half2 uv, const half2 texelResolution, const half offset)
            {
                fixed4 result = color;
                
                result += tex2D(_MainTex, uv + half2( offset,  offset) * texelResolution);
                result += tex2D(_MainTex, uv + half2(-offset,  offset) * texelResolution);
                result += tex2D(_MainTex, uv + half2(-offset, -offset) * texelResolution);
                result += tex2D(_MainTex, uv + half2( offset, -offset) * texelResolution);
                result /= 5.0h;

                return result;
            }

            fixed applyAlphaBlur(const fixed4 color, const half2 uv, const half2 texelResolution, const half offset)
            {
                 fixed result = color.a;
                 
                 result += tex2D(_MainTex, uv + half2( offset,  offset) * texelResolution).a;
                 result += tex2D(_MainTex, uv + half2( offset, -offset) * texelResolution).a;
                 result += tex2D(_MainTex, uv + half2(-offset,  offset) * texelResolution).a;
                 result += tex2D(_MainTex, uv + half2(-offset, -offset) * texelResolution).a;
                 result /= 5.0h;
 
                 return result;               
            }

            fixed4 frag (const v2f input) : SV_Target
            {
                const half2 texelResolution = _MainTex_TexelSize.xy;
                const half2 uv = input.uv;
    
                fixed4 color = tex2D(_MainTex, uv);
                fixed4 depth = tex2D(_MetaballDepthRT, uv);
                if (depth.r < _BlurDistance)
                {
                    color = applyBlur(color, uv, texelResolution, _Offset);
                }
                return color;
            }
            ENDCG
        }
    }
}