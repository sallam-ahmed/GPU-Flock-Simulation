Shader "Custom/InstancedBoid"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_PredatorAdditiveColor("Predator Color", Color) = (0,1,0,1)
		_PredatorHuntAdditiveColor("Predator Hunt Color", Color) = (0,0,1,1)
		_ChasedAdditiveColor("Chased Color", Color) = (1,0,0,1)
		_MainTex("Texture", 2D) = "white" {}
		_BoidScale ("Scale", Range(0.01, 10)) = 1.0
	}

		SubShader
	{

		CGPROGRAM

		fixed4 _Color;
		sampler2D _MainTex;
		float _BoidScale;
		fixed4 _PredatorAdditiveColor;
		fixed4 _ChasedAdditiveColor;
		fixed4 _PredatorHuntAdditiveColor;
		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};

		//addshadow 
		#pragma surface surf Standard vertex:vert addshadow nolightmap
		#pragma instancing_options procedural:setup

		#define WORLD_UP float3(0.0,1.0,0.0)

		float4x4 _LookAtMatrix;
		float3 _BoidPosition;
		
		
		 #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			struct Boid
			{
				float3 Position;
				float3 Direction;
				int IsPredator;
				int State;
				float2 Padding;
			};

			StructuredBuffer<Boid> boidBuffer;
		 #endif

		float4x4 look_at_matrix(float3 at, float3 eye, float3 up) {
			float3 zaxis = normalize(at - eye);
			float3 xaxis = normalize(cross(up, zaxis));
			float3 yaxis = cross(zaxis, xaxis);
			return float4x4(
				xaxis.x, yaxis.x, zaxis.x, 0,
				xaxis.y, yaxis.y, zaxis.y, 0,
				xaxis.z, yaxis.z, zaxis.z, 0,
			   0      , 0      , 0      , 1
			);

		}

		 void vert(inout appdata_full v, out Input data)
		{
			UNITY_INITIALIZE_OUTPUT(Input, data);

			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				v.vertex = mul(_LookAtMatrix, v.vertex * _BoidScale);
				v.vertex.xyz += _BoidPosition;
			#endif
		}

		void setup()
		{
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				_BoidPosition = boidBuffer[unity_InstanceID].Position;
				_LookAtMatrix = look_at_matrix(_BoidPosition, _BoidPosition + (boidBuffer[unity_InstanceID].Direction * -1), WORLD_UP);
			#endif
		}

		 void surf(Input IN, inout SurfaceOutputStandard o) {
			 fixed4 addColor = fixed4(0, 0, 0, 0);
			 fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
			if (boidBuffer[unity_InstanceID].IsPredator == 1)
			{
				if (boidBuffer[unity_InstanceID].State == 3)
				{
					addColor = _PredatorAdditiveColor;
				}
				else
				{
					addColor = _ChasedAdditiveColor;
				}
			}
			if (boidBuffer[unity_InstanceID].State == 2)
			{
				addColor = _PredatorHuntAdditiveColor;
				//c = fixed4(1, 1, 1, 1);
			}
#endif
			c += addColor;
			o.Albedo = c.rgb;
			o.Alpha = c.a;
		 }
		ENDCG
	}
}