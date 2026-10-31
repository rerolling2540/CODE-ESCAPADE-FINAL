Shader "CodeEscapade/UI/Torch Background"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _MenuTime ("Animation Time", Float) = 0
        _Flame0 ("Left Brazier", Vector) = (.1756,.3996,.042,.074)
        _Flame1 ("Right Brazier", Vector) = (.5737,.3679,.039,.069)
        _Flame2 ("Hanging Lantern", Vector) = (.9796,.6364,.0144,.036)
        _BannerRipple ("Banner Ripple (Pixels)", Range(0,4)) = 2.6
        _FoliageSway ("Foliage Sway (Pixels)", Range(0,6)) = 3.4
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 localPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            fixed4 _Color, _TextureSampleAdd;
            float4 _ClipRect, _Flame0, _Flame1, _Flame2;
            float4 _MainTex_TexelSize;
            float _MenuTime, _BannerRipple, _FoliageSway;
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.localPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }
            float Flame(float2 uv, float4 region, float phase, float movement, inout float2 sampleUV, inout float light)
            {
                float2 p = (uv - region.xy) / region.zw;
                float mask = (1 - smoothstep(.35,.55,abs(p.x))) * smoothstep(0,.12,p.y) * (1 - smoothstep(.9,1.15,p.y));
                float sway = sin(_MenuTime * 9.3 + p.y * 8 + phase) * .075 + sin(_MenuTime * 15.7 + p.y * 19 + phase) * .03;
                sampleUV.x -= sway * region.z * mask * saturate(p.y) * movement;
                sampleUV.y -= sin(_MenuTime * 11.1 + phase) * region.w * .025 * mask * saturate(p.y) * movement;
                float flicker = .5 + .27 * sin(_MenuTime * 7.7 + phase) + .23 * sin(_MenuTime * 13.9 + phase);
                float2 glow = (p - float2(0,.3)) / float2(1.6,1.2);
                light += exp(-dot(glow,glow) * 3) * flicker * .035;
                return mask * (flicker - .5) * .18;
            }
            float SoftRegion(float2 uv, float4 region, float feather)
            {
                float2 low = smoothstep(region.xy,region.xy+feather,uv);
                float2 high = 1-smoothstep(region.zw-feather,region.zw,uv);
                return low.x*low.y*high.x*high.y;
            }
            float WindGust()
            {
                float phase = frac((_MenuTime*.7-.65)/12)*12;
                float progress = saturate((phase-2)/4);
                float pulse = sin(progress*UNITY_PI);
                return pulse*pulse;
            }
            float LeafColour(fixed3 colour)
            {
                float total = max(.025,colour.r+colour.g+colour.b);
                float green = smoothstep(.015,.1,(colour.g-colour.r)/total);
                float notSky = 1-smoothstep(.12,.3,(colour.b-colour.g)/total);
                return green*notSky;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float warmLight = 0;
                float brightness = Flame(i.uv,_Flame0,.4,1,uv,warmLight);
                brightness += Flame(i.uv,_Flame1,2.7,1,uv,warmLight);
                brightness += Flame(i.uv,_Flame2,5.2,.2,uv,warmLight);
                float wind = .35 + .65*WindGust();
                float cloth = SoftRegion(i.uv,float4(.048,.465,.113,.697),.003);
                // The hanging edge moves most; the attachment to the pole stays still.
                float freeEdge = saturate((.692-i.uv.y)/.222);
                freeEdge *= freeEdge;
                float ripple = sin(_MenuTime*1.65+freeEdge*5.5) + .25*sin(_MenuTime*3.1+freeEdge*10);
                uv.x -= cloth*freeEdge*ripple*_MainTex_TexelSize.x*_BannerRipple*wind;
                uv.y += cloth*freeEdge*sin(_MenuTime*1.2+freeEdge*8)*_MainTex_TexelSize.y*.65*wind;
                brightness += cloth*freeEdge*ripple*.018*wind;
                // Include dark and blue-green leaves, including the low foreground plants.
                float green = LeafColour(tex2D(_MainTex,i.uv).rgb);
                float2 padding = _MainTex_TexelSize.xy*float2(_FoliageSway+1,2);
                green = max(green,LeafColour(tex2D(_MainTex,i.uv+float2(padding.x,0)).rgb)*.8);
                green = max(green,LeafColour(tex2D(_MainTex,i.uv-float2(padding.x,0)).rgb)*.8);
                green = max(green,LeafColour(tex2D(_MainTex,i.uv+float2(0,padding.y)).rgb)*.8);
                green = max(green,LeafColour(tex2D(_MainTex,i.uv-float2(0,padding.y)).rgb)*.8);
                float sides = max(1-smoothstep(.1,.25,i.uv.x),smoothstep(.8,.96,i.uv.x));
                float foreground = 1-smoothstep(.12,.29,i.uv.y);
                float rooted = smoothstep(.003,.035,i.uv.y);
                float foliage = max(sides,foreground)*green*rooted;
                float leafWind = .65+.35*WindGust();
                float sway = .7*sin(_MenuTime*.95+i.uv.y*23+i.uv.x*17) +
                    .3*sin(_MenuTime*2.4+i.uv.y*71+i.uv.x*43);
                uv.x += foliage*sway*_MainTex_TexelSize.x*_FoliageSway*leafWind;
                uv.y += foliage*cos(_MenuTime*1.2+i.uv.x*37)*_MainTex_TexelSize.y*_FoliageSway*.4*leafWind;
                fixed4 col = tex2D(_MainTex,uv) + _TextureSampleAdd;
                col.rgb = col.rgb * (1 + brightness) + float3(1,.36,.055) * warmLight;
                col *= i.color;
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.localPosition.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - .001);
                #endif
                return col;
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
