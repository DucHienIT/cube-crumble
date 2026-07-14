// Opaque unlit textured shader with EXPLICIT render state, replacing the
// legacy built-in "Unlit/Texture". The built-in one cross-compiles
// unreliably for GLES/WebGL under the URP 2D Renderer, so the camera-facing
// ("top") faces of the 3D cubes dropped out on WebGL while sides survived.
// Spelling out Cull/ZWrite/ZTest here makes back-face culling + depth behave
// identically across D3D11, GLES and WebGL2. No LightMode tag, so the URP 2D
// renderer draws it via SRPDefaultUnlit exactly like the old shader.
Shader "CubeBurst/UnlitCube"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "IgnoreProjector" = "True" }
        LOD 100

        Cull Back
        ZWrite On
        ZTest LEqual

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
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
    Fallback "Unlit/Texture"
}
