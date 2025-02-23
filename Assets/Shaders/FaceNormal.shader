Shader "Custom/FaceNormalVisualization"
{
    Properties
    {
        _Color ("Base Color", Color) = (1, 1, 1, 1)
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

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Compute the face normal using finite differences
            float3 dx = ddx(IN.worldPos);
            float3 dy = ddy(IN.worldPos);
            float3 faceNormal = normalize(cross(dx, dy));

            // Ensure the normal remains in world space
            o.Normal = faceNormal;

            // Convert to 0-1 range for visualization (since normals are usually -1 to 1)
            o.Albedo = faceNormal * 0.5 + 0.5;
        }
        ENDCG
    }
}
