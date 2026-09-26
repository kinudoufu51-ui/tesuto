// 体力の低下に応じて画面から色を抜き、暗くしていく。
// オーバーレイでは「上に重ねる」ことしかできず、既に描かれた映像の彩度は変えられないため、
// ここだけはイメージエフェクトとして持つ必要がある。
Shader "MereSouls/ScreenCondition"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Saturation ("Saturation", Range(0,1)) = 1
        _Darkness ("Darkness", Range(0,1)) = 0
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Saturation;
            float _Darkness;

            fixed4 frag(v2f_img i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // 人間の目の感度に合わせた輝度。単純平均だと赤が沈んで血の色が消える。
                float lum = dot(col.rgb, float3(0.299, 0.587, 0.114));

                col.rgb = lerp(float3(lum, lum, lum), col.rgb, _Saturation);
                col.rgb *= (1.0 - _Darkness);
                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
