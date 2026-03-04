
Shader "Custom/Terrain"
{
	Properties
	{
		testTexture("Texture", 2D) = "white"{}
		testScale("Scale", Float) = 1
		_DebugMode("Debug Mode", Float) = 0
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
	float _DebugMode;

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
		waterLevel = 1.5;


		float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
		float heatPercent = inverseLerp(minHeat, maxHeat, IN.biomeMap.x);
		float moisturePercent = inverseLerp(minMoisture, maxMoisture, IN.biomeMap.y);
		float isNotWater = saturate(sign(IN.worldPos.y - waterLevel));



		float3 blendAxes = abs(IN.worldNormal);
		float blendAxesSum = blendAxes.x + blendAxes.y + blendAxes.z;
		blendAxes /= max(blendAxesSum, 0.0001); 


		// WHITTAKER BIOME DIAGRAM - 2D LOOKUP WITH SOFT BLENDING
		// Heat and moisture are independent axes
		// Blend between adjacent biomes for smooth transitions

		float blendWidth = 0.15;  // Blend zone width

		// WATER CHECK
		if (isNotWater < 0.5) {
			// Water/beach - use layer 0 or 1 based on height
			int waterBiome = (heightPercent > baseStartHeights[1]) ? 1 : 0;
			float3 baseColour = baseColours[waterBiome] * baseColourStrength[waterBiome];
			float3 textureColour = triplanar(IN.worldPos, baseTextureScales[waterBiome], blendAxes, waterBiome) * (1 - baseColourStrength[waterBiome]);
			o.Albedo = baseColour + textureColour;
		}
		else {
			// LAND - Whittaker diagram with blending

			float3 finalColor = float3(0, 0, 0);

			// TUNDRA (heat > 0.6) - MUCH MORE tundra
			float tundraStrength = smoothstep(0.6 - blendWidth, 0.6 + blendWidth, heatPercent);

			// HOT ZONE (heat < 0.2) - Keep desert size
			float hotZone = 1.0 - smoothstep(0.2 - blendWidth, 0.2 + blendWidth, heatPercent);
			float desertStrength = hotZone * (1.0 - smoothstep(0.3 - blendWidth, 0.3 + blendWidth, moisturePercent));
			float jungleStrength = hotZone * smoothstep(0.3 - blendWidth, 0.3 + blendWidth, moisturePercent);

			// TEMPERATE ZONE (0.2 <= heat < 0.6) - Narrower temperate band
			float temperateZone = (1.0 - tundraStrength) * (1.0 - hotZone);
			float steppeStrength = temperateZone * (1.0 - smoothstep(0.4 - blendWidth, 0.4 + blendWidth, moisturePercent));
			float grasslandStrength = temperateZone * smoothstep(0.4 - blendWidth, 0.4 + blendWidth, moisturePercent);

			// Render each biome with its strength
			float3 tundraColor = baseColours[5] * baseColourStrength[5] + triplanar(IN.worldPos, baseTextureScales[5], blendAxes, 5) * (1 - baseColourStrength[5]);
			float3 desertColor = baseColours[2] * baseColourStrength[2] + triplanar(IN.worldPos, baseTextureScales[2], blendAxes, 2) * (1 - baseColourStrength[2]);
			float3 steppeColor = baseColours[3] * baseColourStrength[3] + triplanar(IN.worldPos, baseTextureScales[3], blendAxes, 3) * (1 - baseColourStrength[3]);
			float3 grassJungleColor = baseColours[4] * baseColourStrength[4] + triplanar(IN.worldPos, baseTextureScales[4], blendAxes, 4) * (1 - baseColourStrength[4]);

			// Blend all biomes
			finalColor = tundraColor * tundraStrength
					   + desertColor * desertStrength
					   + grassJungleColor * (jungleStrength + grasslandStrength)
					   + steppeColor * steppeStrength;

			o.Albedo = finalColor;
		}

		// DEBUG VISUALIZATIONS
		// Change _DebugMode in material inspector:
		// 0 = Normal biome rendering
		// 1 = Heat map (Red gradient: dark=cold/equator, bright=hot/poles)
		// 2 = Moisture map (Green gradient: dark=dry, bright=wet)
		// 3 = Combined (Red=heat, Green=moisture)
		// 4 = Heat zones (shows cold threshold at 0.667)
		// 5 = Moisture zones (shows biome boundaries)

		if (_DebugMode == 1) {
			// Heat only - Red gradient
			o.Albedo = float3(IN.biomeMap.x, 0, 0);
		}
		else if (_DebugMode == 2) {
			// Moisture only - Green gradient
			o.Albedo = float3(0, IN.biomeMap.y, 0);
		}
		else if (_DebugMode == 3) {
			// Combined: Red=heat, Green=moisture, Blue=height
			o.Albedo = float3(IN.biomeMap.x, IN.biomeMap.y, heightPercent);
		}
		else if (_DebugMode == 4) {
			// Heat zones: Blue=cold (>0.6), Red=hot (<0.2), Yellow=temperate
			if (heatPercent > 0.6)
				o.Albedo = float3(0.5, 0.7, 1.0);  // Light blue - Tundra zone (coldest 40%)
			else if (heatPercent < 0.2)
				o.Albedo = float3(1.0, 0.3, 0.2);  // Red - Hot zone (hottest 20%)
			else
				o.Albedo = float3(1.0, 0.9, 0.3);  // Yellow - Temperate zone (middle 40%)
		}
		else if (_DebugMode == 5) {
			// Whittaker Biome Diagram - matches the shader's actual biome selection
			if (heatPercent > 0.6) {
				// Tundra (EXPANDED - 40% of map)
				o.Albedo = float3(0.9, 0.9, 1.0);  // White/blue
			}
			else if (heatPercent < 0.2 && moisturePercent < 0.3) {
				// Desert (hot + dry)
				o.Albedo = float3(0.9, 0.6, 0.3);  // Reddish sandy
			}
			else if (heatPercent < 0.2 && moisturePercent >= 0.3) {
				// Jungle (hot + wet)
				o.Albedo = float3(0.1, 0.5, 0.1);  // Dark green
			}
			else if (moisturePercent < 0.4) {
				// Steppe (temperate + dry)
				o.Albedo = float3(0.8, 0.7, 0.4);  // Tan
			}
			else {
				// Grassland (temperate + wet)
				o.Albedo = float3(0.2, 0.7, 0.15);  // Bright green
			}
		}
		// else: _DebugMode == 0 or undefined, use normal biome rendering (already set above)
	}
	ENDCG
	}
		FallBack "Diffuse"
}
