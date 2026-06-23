Shader "Sprites/Shine Sweep"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0,1)) = 0
        _StripeWidth ("Stripe Width", Range(0.001,1)) = 0.12
        _Angle ("Angle", Float) = -18
        _MaxAlpha ("Max Alpha", Range(0,1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float _Progress;
            float _StripeWidth;
            float _Angle;
            float _MaxAlpha;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 sprite = tex2D(_MainTex, input.texcoord);
                float2 centeredUv = input.texcoord - 0.5;
                float angleRadians = radians(_Angle);
                float2 normal = normalize(float2(cos(angleRadians), sin(angleRadians)));
                float sweepCenter = lerp(-0.85, 0.85, _Progress);
                float distanceToStripe = abs(dot(centeredUv, normal) - sweepCenter);
                float band = smoothstep(_StripeWidth, _StripeWidth * 0.25, distanceToStripe);
                return fixed4(1, 1, 1, sprite.a * band * _MaxAlpha) * input.color;
            }
            ENDCG
        }
    }
}
