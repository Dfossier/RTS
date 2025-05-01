
Shader "Custom/Terrain"
{
	Properties
	{
		testTexture("Texture", 2D) = "white"{}
	testScale("Scale", Float) = 1
	}
		SubShader
	{
		Tags
	{
		"RenderType" = "Opaque"
	}

		LOD 200

		CGPROGRAM
#pragma surface surf Standard fullforwardshadows vertex:vert
#pragma target 3.0

#define MAX_LAYER_COUNT 8
#define EPSILON 1E-4

		int layerCount;
	float3 baseColours[MAX_LAYER_COUNT];
	float baseStartHeights[MAX_LAYER_COUNT];
	float baseStartHeats[MAX_LAYER_COUNT];
	float baseStartMoistures[MAX_LAYER_COUNT];
	float baseBlends[MAX_LAYER_COUNT];
	float baseColourStrength[MAX_LAYER_COUNT];
	float baseTextureScales[MAX_LAYER_COUNT];

	float minHeight;
	float maxHeight;

	float minHeat;
	float maxHeat;
	float minMoisture;
	float maxMoisture;

	float oneHeat;
	float twoHeat;
	float waterLevel;
	float mountainStart;

	sampler2D testTexture;
	float testScale;

	UNITY_DECLARE_TEX2DARRAY(baseTextures);

	//he added heatmap as a float2 in "Input"
	struct Input
	{
		float3 worldPos;
		float3 worldNormal;
		float2 biomeMap;
	};



	// 
	// NOTE(sietse): A shader exists of two programs,
	// a vertex program and a fragment program.
	// When using the Unity surface template,
	// some things get automatically generated.
	// In many cases this means you don't have to
	// write a vertex program.
	// The vertex program manipulates the data from
	// the vertices of the mesh and interpolates the
	// data based on the baricentric coordinate.
	// The interpolated data is now sent to the fragment
	// program, which determines what the color a given
	// pixel should be.
	// In this case I need a bit more data than Unity
	// automatically generates.
	// 
	// NOTE(sietse): I'm not sure why texcoord3 gives
	// the same result as texcoord2, but it has probably
	// to do with how Unity passes the data to the GPU.
	// The diffuse texture usually uses the first 
	// texcoord data, and lightmap the second texcoord.
	// When there is no second texcoord, Unity uses
	// the first one instead. So my guess is, when
	// a texcoord is not defined it uses the previous
	// value instead.
	void vert(inout appdata_full v, out Input o)
	{
		UNITY_INITIALIZE_OUTPUT(Input, o);
		o.biomeMap = v.texcoord2.xy;
	}


	//this is a function that clamps results from 0-1, we then use it to pass through textures in order, overwriting at the point they are >0.
	inline float
		inverseLerp(float a, float b, float value)
	{
		//unity says floating point error here divide by zero
		return saturate((value - a) / (b - a));
	}

	//this function projects the textures from all sides and blends them to avoid stretching
	inline float3
		triplanar(float3 worldPos, float scale, float3 blendAxes, int textureIndex)
	{
		float3 scaledWorldPos = worldPos / scale;
		float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
		float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
		float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
		return xProjection + yProjection + zProjection;
	}

	void surf(Input IN, inout SurfaceOutputStandard o)
	{
		// TODO(sietse): remove these lines
		// and make them modifiable in the CPU part
		
		
		minHeat = 0.000;
		maxHeat = 1;
		minMoisture = 0;
	    maxMoisture = 1;
		oneHeat = 1 / 2;
		twoHeat = 1 * 2 / 3;
		waterLevel = 1.5;


		float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
		float heatPercent = inverseLerp(minHeat, maxHeat, IN.biomeMap.x);
		float moisturePercent = inverseLerp(minMoisture, maxMoisture, IN.biomeMap.y);
		float halfHeat = inverseLerp(minHeat, oneHeat, IN.biomeMap.x);
		float isNotWater = saturate(sign(IN.worldPos.y - waterLevel));
		float isCold = saturate(sign(IN.biomeMap.x - twoHeat));



		float3 blendAxes = abs(IN.worldNormal);
		blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z; 


		for (int i = 0; i < layerCount; i++)
		{
			
			float heightStrength = inverseLerp(
				-baseBlends[i] / 2 - EPSILON,
				baseBlends[i] / 2,
				heightPercent - baseStartHeights[i]) / 2;

			float heatStrength = inverseLerp(
			   -baseBlends[i] / 2 - EPSILON,
				baseBlends[i] / 2,
				heatPercent - baseStartHeats[i])/2;

			float moistureStrength = inverseLerp(
			   -baseBlends[i] / 2 - EPSILON,
				baseBlends[i] / 2,
				moisturePercent - baseStartMoistures[i])/2;


			float3 baseColour = baseColours[i] * baseColourStrength[i];
			float3 textureColour = triplanar(IN.worldPos, baseTextureScales[i], blendAxes, i) * (1 - baseColourStrength[i]);

			//o.Albedo = (o.Albedo * (1 - heightStrength) + (baseColour + textureColour) * heightStrength);
			//the original, this writes lower height values first then overwrites them with higher height values
			//the higher the draw strength, the higher the overwrite starts
			//if we want water to be drawn first, then we need a low drawStrength


			//         This part only draws below the waterline
			//o.Albedo = o.Albedo * (1 - isWater)                 +            (baseColour + textureColour) * heightStrength
				
			o.Albedo = 
				//this one draws texture by moisture level for warm areas
				((o.Albedo * (1 - moistureStrength) + (baseColour + textureColour) * (moistureStrength))*(1-isCold)
				
				//this one draws the cold biome
				+(o.Albedo * (1 - heatStrength) + (baseColour + textureColour) * heatStrength)*(isCold))*isNotWater
				
				
				+(o.Albedo * (1 - heightStrength) + (baseColour + textureColour) * heightStrength)*(1-isNotWater)
				;
			
			

			//testing 2   this puts the water first by drawing textures based on inverse height strength
			//o.Albedo = o.Albedo * (1 - heightStrength)  
				
				//halfPercent draws everything else based on closeness to top (heightstrength), and everything on the inverse of heatstrength from dryest to wettest 
			    //+o.Albedo * heightStrength * (halfHeat) * (1-moistureStrength)
				//+(baseColour + textureColour) * (halfHeat) * (1-heightStrength)* (moistureStrength)
			    //this shows the cold areas
			    // +(baseColour + textureColour) * (1-halfHeat)* heightStrength 
				//;
		    
			//testing 1
			//o.Albedo = heatPercent;

			//o.Albedo =
				// o.Albedo * (1-halfHeat)
				//+(o.Albedo * (1 - moistureStrength) + (baseColour + textureColour) * moistureStrength)
				//+ (baseColour + textureColour)*heatStrength * halfHeat;
				// ;

		}

		//o.Albedo = float3(IN.heatmap, 0);
	}
	ENDCG
	}
		FallBack "Diffuse"
}
