Shader "Custom/FaceNormal"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        struct Input
        {
            float3 worldPos; // World position of the fragment
        };

        fixed4 _Color;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Compute the face normal using finite differences
            float3 dx = ddx(IN.worldPos);
            float3 dy = ddy(IN.worldPos);
            float3 faceNormal = normalize(cross(dx, dy));

            // Apply normal to the shader output
            o.Normal = faceNormal;

            // Base color
            o.Albedo = _Color.rgb;
        }
        ENDCG
    }
}