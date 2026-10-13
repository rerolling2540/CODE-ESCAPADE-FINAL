Shader "CodeEscapade/PixelEnemy"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _PixelSize ("Source Pixels Per Block", Float) = 3
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            float _PixelSize;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 block = _MainTex_TexelSize.xy * max(1, _PixelSize);
                float2 uv = (floor(i.uv / block) + 0.5) * block;
                fixed4 c = tex2D(_MainTex, uv) * i.color;
                clip(c.a - 0.4);
                c.rgb = floor(c.rgb * 23 + 0.5) / 23;
                c.a = 1;
                return c;
            }
            ENDCG
        }
    }
    Fallback "Sprites/Default"
}
