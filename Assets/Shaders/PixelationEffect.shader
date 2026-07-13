Shader "Custom/PixelationEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _PixelSize ("Pixel Size", Range(1, 100)) = 10
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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

            sampler2D _MainTex;
            float _PixelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Calcul des coordonnées de pixel
                float2 pixelUV = i.uv;
                pixelUV = floor(pixelUV * _ScreenParams.xy / _PixelSize) * _PixelSize / _ScreenParams.xy;

                // Échantillonnage de la texture
                fixed4 col = tex2D(_MainTex, pixelUV);

                return col;
            }
            ENDCG
        }
    }
}