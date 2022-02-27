Shader "Unlit/PointCloudShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}
		SubShader
	{
		Tags
		{
			"RenderType" = "Transparent"
		}
		LOD 200

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			struct Appdata
			{
				float4 pos : POSITION;
				float4 col: COLOR;
			};

			struct v2g
			{
				float4 pos : SV_POSITION;
				float4 col : COLOR;
			};

			struct g2f
			{
				float4 pos : SV_POSITION;
				float4 col : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			v2g vert(Appdata v)
			{
				v2g o;

				o.pos = v.pos;
				o.col = v.col;

				return o;
			}

			[maxvertexcount(9)]
			void geom(triangle v2g input[3], inout TriangleStream<g2f> output)
			{
				float scaleMod = 0.1f;
				const float4 vc[3] = { float4(-scaleMod / 0.866, -scaleMod, 0.0f, 0.0f),
									float4(0.0f, scaleMod, 0.0f, 0.0f),
									float4(scaleMod / 0.866, -scaleMod, 0.0f, 0.0f) };

				for (int i = 0; i < 3; i++)
				{
					for (int j = 0; j < 3; j++)
					{
						g2f o;
						o.col = input[i].col;
						float4 mIn = float4(UnityObjectToViewPos(input[i].pos), 1.0f); //mul(UNITY_MATRIX_MV, input[i].pos);
						o.pos = mul(UNITY_MATRIX_P, mIn + vc[j]);

						output.Append(o);
					}

					output.RestartStrip();
				}
			}

			float4 frag(g2f i) : SV_Target
			{
				return i.col;
			}

			ENDCG
		}
	}
}