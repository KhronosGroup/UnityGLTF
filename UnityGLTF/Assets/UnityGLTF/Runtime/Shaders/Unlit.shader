Shader "GLTF/Unlit"
{
	Properties
	{
		_Color("Base Color Factor", Color) = (1,1,1,1)
		_MainTex("Base Color Texture", 2D) = "white" {}
	}
    SubShader
	{
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        
        Lighting Off

		Pass
		{
			Color [_Color]
			SetTexture[_MainTex] { combine texture * primary } 
		}
	}
	FallBack "Unlit/Color" 
}