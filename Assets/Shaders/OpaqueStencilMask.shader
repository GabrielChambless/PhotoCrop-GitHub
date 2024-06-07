Shader "Custom/OpaqueStencilMask"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" }
        LOD 100

        Stencil
        {
            Ref 1
            Comp always
            Pass replace
        }

        Pass
        {
            ColorMask 0
            Lighting Off
            ZWrite Off
        }
    }
}
