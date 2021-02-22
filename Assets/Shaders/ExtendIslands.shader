Shader "TNTC/ExtendIslands"{
    Properties{
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader{
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata{
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f{
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

            v2f vert (appdata v){
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target{
                float2 offsets[8] = {float2(-1,0), float2(1,0), float2(0,1), float2(0,-1), float2(-1,1), float2(1,1), float2(1,-1), float2(-1,-1)};
				float2 uv = i.uv;
				float4 color = tex2D(_MainTex, uv);

				float4 extendedColor = color;
				for	(int i = 0; i < offsets.Length; i++){
					float2 currentUV = uv + offsets[i] * _MainTex_TexelSize.xy;
					float4 offsettedColor = tex2D(_MainTex, currentUV);
					extendedColor = max(offsettedColor, extendedColor);
				}

				color = extendedColor;

				return color;
            }
            ENDCG
        }
    }
}
