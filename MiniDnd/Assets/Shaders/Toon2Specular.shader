Shader "Custom/Toon2Specular"
{
    Properties
    {
        _Color("Main Color", Color) = (1,1,1,1)
        _MainTex("Texture", 2D) = "white" {}
        _RampTex("Ramp", 2D) = "white" {}
        _SpecularRampTex("Specular Ramp", 2D) = "white" {}
        _SpecularA("_SpecularA", Float) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            //Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag            

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                half lightCoeff : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float3 normal: TEXCOORD2;
                float4 posWorld : TEXCOORD3;
            };

            sampler2D _MainTex;
            sampler2D _RampTex;
            sampler2D _SpecularRampTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _LightColor0;
            float _SpecularA;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                o.lightCoeff = dot(worldNormal, _WorldSpaceLightPos0.xyz) * 0.5 + 0.5;
                o.normal = worldNormal;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Specular
                float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
		        //float diffuseReflection = max(0.0, dot(i.normal, lightDirection));
                float3 lightReflectDirection = reflect(-lightDirection, i.normal);
                float3 viewDirection = float3(float4(_WorldSpaceCameraPos.xyz, 1.0) - i.posWorld.xyz);
                float lightSeeDirection = dot(lightReflectDirection, viewDirection) * 0.5 + 0.5; // * _SpecularW + _SpecularK;                

                // Difuse ramp
                fixed4 rampValue = tex2D(_RampTex, i.lightCoeff).r;
                fixed4 col = tex2D(_MainTex, i.uv) * _Color * _LightColor0;
                col.rgb *= rampValue;

                fixed4 rampValueSpec = tex2D(_SpecularRampTex, lightSeeDirection).r - 0.5;

                return col + rampValueSpec * _SpecularA;
            }
            ENDCG
        }
    }

        Fallback "Unlit/Texture"
}
