Shader "Kiag/ClayPostFx"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BloomTex ("Bloom", 2D) = "black" {}
        _Threshold ("Threshold", Float) = 0.65
        _Intensity ("Intensity", Float) = 0.45
        _FocusDistance ("FocusDistance", Float) = 7.5
        _FocusRange ("FocusRange", Float) = 4.5
        _BlurSize ("BlurSize", Float) = 1.4
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0: extract bright
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragExtract
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _Threshold;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }
            fixed4 fragExtract(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float br = max(c.r, max(c.g, c.b));
                float k = saturate((br - _Threshold) / max(1e-3, 1.0 - _Threshold));
                return c * k;
            }
            ENDCG
        }

        // 1: blur H
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBlurH
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }
            fixed4 fragBlurH(v2f i) : SV_Target
            {
                float2 o = float2(_MainTex_TexelSize.x * _BlurSize, 0);
                fixed4 c = tex2D(_MainTex, i.uv) * 0.227027;
                c += tex2D(_MainTex, i.uv + o) * 0.1945946;
                c += tex2D(_MainTex, i.uv - o) * 0.1945946;
                c += tex2D(_MainTex, i.uv + o * 2) * 0.1216216;
                c += tex2D(_MainTex, i.uv - o * 2) * 0.1216216;
                c += tex2D(_MainTex, i.uv + o * 3) * 0.054054;
                c += tex2D(_MainTex, i.uv - o * 3) * 0.054054;
                return c;
            }
            ENDCG
        }

        // 2: blur V
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragBlurV
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            float _BlurSize;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }
            fixed4 fragBlurV(v2f i) : SV_Target
            {
                float2 o = float2(0, _MainTex_TexelSize.y * _BlurSize);
                fixed4 c = tex2D(_MainTex, i.uv) * 0.227027;
                c += tex2D(_MainTex, i.uv + o) * 0.1945946;
                c += tex2D(_MainTex, i.uv - o) * 0.1945946;
                c += tex2D(_MainTex, i.uv + o * 2) * 0.1216216;
                c += tex2D(_MainTex, i.uv - o * 2) * 0.1216216;
                c += tex2D(_MainTex, i.uv + o * 3) * 0.054054;
                c += tex2D(_MainTex, i.uv - o * 3) * 0.054054;
                return c;
            }
            ENDCG
        }

        // 3: composite bloom + mild DOF (far blur mix)
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment fragComp
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            sampler2D _BloomTex;
            sampler2D _BlurredTex;
            sampler2D_float _CameraDepthTexture;
            float _Intensity;
            float _FocusDistance;
            float _FocusRange;
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            v2f vert(appdata_img v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.texcoord; return o; }
            fixed4 fragComp(v2f i) : SV_Target
            {
                fixed4 sharp = tex2D(_MainTex, i.uv);
                fixed4 blur = tex2D(_BlurredTex, i.uv);
                float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv));
                float coc = saturate(abs(depth - _FocusDistance) / max(0.01, _FocusRange));
                coc = smoothstep(0.15, 1.0, coc);
                fixed4 color = lerp(sharp, blur, coc * 0.85);
                color.rgb += tex2D(_BloomTex, i.uv).rgb * _Intensity;
                // soft warm lift (figurine plate)
                color.rgb = color.rgb * 1.02 + float3(0.01, 0.008, 0.015);
                return color;
            }
            ENDCG
        }
    }
}
