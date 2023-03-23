Shader "Unlit/Stencil Increment"
{
    Properties
    {
        _Texture("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Geometry"}
        LOD 100

        ColorMask 0
        Zwrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Stencil
        {
            Ref 2
            Comp NotEqual
            Pass IncrSat
            Fail Zero
        }

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

            sampler2D _Texture;
            float4 _Texture_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _Texture);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                if (tex2D(_Texture, i.uv).a < 0.5) discard;
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
