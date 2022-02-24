// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Unlit/PointCloudBufferShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "white" {}
	}

		SubShader
	{
		Tags { "RenderType" = "Transparent" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag

			#include "UnityCG.cginc"

			uniform StructuredBuffer<float3> posBuf : register(t1);
			uniform StructuredBuffer<int> colBuf : register(t2);
			uniform StructuredBuffer<float3> camBuf : register(t3);

			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;

				// This can be used by the compute buffer to index its
				// values by vertex index, if your buffer is storing some
				// values for each vertex.
				uint id : SV_VertexID;
			};

			struct v2g
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 lod : TEXCOORD0;
				uint id : VERTEXID;
			};

			struct g2f
			{
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float3 _CameraPos;

			float4 colorFromInt(int col)
			{
				float r = (float)((col & 0xFF000000) >> 24) / 255.0;
				float g = (float)((col & 0x00FF0000) >> 16) / 255.0;
				float b = (float)((col & 0x0000FF00) >> 8) / 255.0;
				float a = (float)((col & 0x000000FF) >> 0) / 255.0;

				return float4(r, g, b, a);
			}

			uint Permute(uint i)
			{
				uint pPrime = 249997;

				if (i <= pPrime / 2) return (i * i) % pPrime;
				else if (i < pPrime) return pPrime - (i * i) % pPrime;
				else return i;
			}

			v2g vert(appdata v)
			{
				v2g o;

				o.vertex = UnityObjectToClipPos(v.vertex);

				float lodLimit = 100.0f;
				o.lod = float4(0, 0, 0, 0);
				float d = distance(_WorldSpaceCameraPos, mul(unity_ObjectToWorld, v.vertex).xyz);
				o.lod.x = (int)(d / lodLimit);
				o.lod.y = clamp(d / lodLimit, 1.0f, d / lodLimit);

				o.color = v.color;
				o.id = v.id;

				return o;
			}

			[maxvertexcount(64)]
			void geom(point v2g input[1], inout PointStream<g2f> output)
			{
				const int maxLOD = 4;
				const int pointRatio = 64;
				uint id = input[0].id * (uint)pointRatio;

				int LOD = input[0].lod.x;
				float floatLOD = input[0].lod.y;

				if (LOD >= maxLOD)
				{
					g2f o;
					o.color = colorFromInt(colBuf[id]);
					o.vertex = input[0].vertex;

					output.Append(o);
					output.RestartStrip();
					return;
				}

				for (int i = 0; i < pointRatio / floatLOD; i++)
				{
					uint bufId = Permute(id + i);

					g2f o;
					o.color = colorFromInt(colBuf[bufId]);
					o.vertex = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_MV, float4(posBuf[bufId], 1.0f)));

					output.Append(o);
					output.RestartStrip();
				}
			}

			float4 frag(g2f i) : SV_Target
			{
				return i.color;
			}

			ENDCG
		}
	}
}