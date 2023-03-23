Shader "Unlit/Visible Color"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color0 ("Color 0", Color) = (1, 1, 1, 1)
        _Color1 ("Color 1", Color) = (1, 1, 1, 1)
        _Color2 ("Color 2", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+5"}
        LOD 100

        //ZWrite Off
        //Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Stencil
            {
                ref 0
                Comp Equal
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color0;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color0;
            }
            ENDCG
        }

        Pass
        {
            Stencil
            {
                ref 1
                Comp Equal
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color1;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color1;
            }
            ENDCG
        }

        Pass
        {
            Stencil
            {
                ref 2
                Comp Equal
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color2;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _Color2;
            }
            ENDCG
        }
    }
}
