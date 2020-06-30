Shader "Encap/LitSmokeAndFire2/CGNormalMappedMegaShader"
{
	Properties
	{
		[ShaderBackgroundDecorator(Assets..EditorResources..LitSmokeAndFire2..MaterialBackground.png,Assets..EditorResources..LitSmokeAndFire2..MaterialHeader.png)]
		[ShaderUserDocsLink()]
		[ShaderHelpDecorator(Plain textures do not work in lit smoke and fire you must use normal mapped textures formatted for the shader for more details check the user documentation, A different base texture format is used for packed emissive lighting make sure you are using the right texture for the shading mode you are using.,140)]
		_MainTex("Texture", 2D) = "white" {}
		_InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		
		[ShaderHelpDecorator(Blend frames blends the current texture frame and the next frame in the sprite sheet together to create a smoother animation using fewer frames.Blending frames samples each texture twice which adds an additional cost to the shader especially when used with non packed emissive lighting.,120)]
		[Toggle(BLEND_FRAMES)] _BlendFramesToggle("Sprite Sheet Frame Blending", float) = 0  //---------------------------------------- Frame Blend Control

		[ShaderHelpDecorator(None is the fastest but does not render any self lighting effects like flames.,Emissive Lighting uses the colours stored in a separate texture as emissive self light.It is the most expensive of the three modes especially when combined with frame blending but has the least lighting artifacts when used with mostly transparent smoke.,Packed Emissive Lighting packages an emissive lookup value into the main texture and uses a small 1D texture to turn that lookup value into an actual colour.This mode has better performance and uses less memory than standard emissive lighting but has more rendering artifacts.,290)]
		[ShaderKeyWordList( ,SEPERATE_EMISSIVE_LIGHTING,PACKED_EMISSIVE_LIGHTING,None, Emissive Lighting, Packed Emissive Lighting)] _EmissiveLighting("Emissive Lighting", float) = 0

		[ShaderHelpDecorator(The colours in this texture are added to the output colour of the particle.If you are using HDR you can multiply the brightness of these colours. Do not forget to scale the duller colours like the red part of the flame down so when they are scaled back up by the HDR multiplier the resulting brightness matches the target brightness.,160,_EmissiveLighting,1)]
		[ShaderVariableHider(_EmissiveLighting,1)] _EmissiveTex("Emissive texture", 2D) = "white" {}

		[ShaderHelpDecorator(The emissive lookup texture is used in conjunction with a lookup value stored in a packed emissive texture to calculate the colour of the emissive lighting.The left half of the emissive lookup texture is used to store the transparency of the particle while the right half of the texture stores the emissive lighting colour at different intensities.Different emissive lookup textures can be used with the same packed emissive texture to create different coloured emissive effects.,180,_EmissiveLighting,2)]
		[ShaderVariableHider(_EmissiveLighting,2)] _EmissiveLookupTex("Emissive colour Lookup", 2D) = "white" {}

		[ShaderHelpDecorator(When the particle alpha is low multiply the emissive colour with this colour to avoid odd flame colouring this will slow down the shader.,60)]
		[Toggle(LOW_ALPHA_EMISSIVE_COLOUR)]_Low_Alpha_Emissive_Colour("Change low alpha emissive", float) = 0

		[ShaderHelpDecorator(The emissive colour multiplier when particle alpha is low.,40)]
		[HDR]
		[ShaderVariableHider(_Low_Alpha_Emissive_Colour,1)] _LowAlphaEmissiveColour("Low alpha emissive colour", Color) = (1,0.1,0,1)

		[ShaderVariableHider(_Low_Alpha_Emissive_Colour,1)] _LowAlphaStart("Low alpha start offset", float) = 10

		[ShaderHelpDecorator(Increases or decreases the effect of ambient lighting on this particle effect.,40)]
		_AmbientLightMultiplier("Ambient Lighting Multiplier", Range(0,2)) = 1

		[ShaderHelpDecorator(Increases or decreases the effect of direct lighting on this particle effect.,40)]
		_DirectLightMultiplier("Direct Lighting Multiplier", Range(0,2)) = 1

		[ShaderHelpDecorator(When activated this adds extra lighting options at a small performance cost.,40)]
		[Toggle(COMPLEX_LIGHT_CONTROL)] _ComplexLightControlToggle("Advanced light control options", float) = 0  //---------------------------------------- Advanced Light Controls
		
		[ShaderHelpDecorator(Controls  how far around the object the light stretches.A value of one matches the real world while a value near half functions like a Half Lambert shader,80,_ComplexLightControlToggle,1)]
		[ShaderVariableHider(_ComplexLightControlToggle,1)]_LightWrapAround("Light Wrap Around" ,float) = 0.5

		[ShaderHelpDecorator(Controls how dark are the shadows are,30,_ComplexLightControlToggle,1)]
		[ShaderVariableHider(_ComplexLightControlToggle,1)]_ShadowEffectMultiplyer("Shadow Multiplyer", float) = 0.5

		[ShaderHelpDecorator(Controls how much brighter surfaces directly facing the light source are compared to surfaces only partially facing the light source. ,60,_ComplexLightControlToggle,1)]
		[ShaderVariableHider(_ComplexLightControlToggle,1)]_LightSharpness("LightSharpness", float) = 2

		[ShaderHelpDecorator(Multiplies the emissive value to make it brighter than what the screen can do.Use this for better bloom and to make the flame look thicker on more transparent particles. ,60)]
		[Toggle(HDR)] _HDRToggle("Multiply emissive for HDR", float) = 0  //---------------------------------------- HDR Controls
		[ShaderVariableHider(_HDRToggle,1)]_HDREmissiveMultiplyer("Emissive multiplyer", float) = 1

		[ShaderHelpDecorator(When light travels through smoke it scatters.When looking towards a light source this light scattering creates a halo.The light scattering option fakes this scattering using the texture alpha and light direction. ,80)]
		[Toggle(LIGHT_SCATTERING)] _LIGHT_SCATTERINGToggle("Fake Light Scatter", float) = 0 //---------------------------------- Light scatter controls
		[ShaderVariableHider(_LIGHT_SCATTERINGToggle,1)]_LightScatterSharpness("Light Scatter Sharpness", float) = 1
		[ShaderVariableHider(_LIGHT_SCATTERINGToggle,1)]_LightScatterMinAlphaMultiplyer("Low Alpha Scatter Multiplyer", float) = 1
		[ShaderVariableHider(_LIGHT_SCATTERINGToggle,1)]_LightScatterMaxAlphaMultiplyer("Hight Alpha Scatter Multiplyer", float) = 1
		[ShaderVariableHider(_LIGHT_SCATTERINGToggle,1)]_LightScatterAmbientLightMultiplyer("Ambient Lighting Scatter Multiplyer", Range(0,2)) = 1


		[ShaderHelpDecorator(Instead of multiplying the texture alpha by the colour alpha value you set in the editor disolve alpha subtracts the colour alpha from the texture alpha.  ,60)]
		[Toggle(DISSOLVE_ALPHA)] _DISSOLVE_ALPHAToggle("Disolve particle based on colour alpha", float) = 0

		[ShaderHelpDecorator(Enabling light probes allows pre baked lighting to light the particle.A SetLightProbe script must be attached to the particle system and a CameraInverseViewMatrix script must be attached to the camera for this to work correctly.Light probe values are sampled once at the center of the smoke this may cause problems for large particle systems.,140)]
		[Toggle(LIGHTPROBE)] _LIGHTPROBEToggle("Enable Light probes", float) = 0  //----------------------------------------Light Probe Fade Controls
		//[ShaderKeyWordList(,LIGHTPROBE,Fixed Ambient Light,Light Probes)] _LIGHTPROBEToggle("Enable Light probes", float) = 0
		
		//-------- Light Probe Sperical Harmonics -------
		[PerRendererData] Particle_Probe_SHAr("Particle_Probe_SHAr", Vector) = (0,0,0,0)
		[PerRendererData] Particle_Probe_SHAg("Particle_Probe_SHAg", Vector) = (0, 0, 0, 0)
		[PerRendererData] Particle_Probe_SHAb("Particle_Probe_SHAb", Vector) = (0, 0, 0, 0)
		[PerRendererData] Particle_Probe_SHBr("Particle_Probe_SHBr", Vector) = (0, 0, 0, 0)
		[PerRendererData] Particle_Probe_SHBg("Particle_Probe_SHBg", Vector) = (0, 0, 0, 0)
		[PerRendererData] Particle_Probe_SHBb("Particle_Probe_SHBb", Vector) = (0, 0, 0, 0)
		[PerRendererData] Particle_Probe_SHC("Particle_Probe_SHC", Vector) = (0, 0, 0, 0)

		//used for ambient testsing 

		//Particle_Ambient_SHAr( "Particle_Ambient_SHAr", Vector) = (0,0,0,0)
		//Particle_Ambient_SHAg ("Particle_Ambient_SHAg", Vector) = (0, 0, 0, 0)
		//Particle_Ambient_SHAb ("Particle_Ambient_SHAb", Vector) = (0, 0, 0, 0)
		//Particle_Ambient_SHBr ("Particle_Ambient_SHBr", Vector) = (0, 0, 0, 0)
		//Particle_Ambient_SHBg ("Particle_Ambient_SHBg", Vector) = (0, 0, 0, 0)
		//Particle_Ambient_SHBb ("Particle_Ambient_SHBb", Vector) = (0, 0, 0, 0)
		//Particle_Ambient_SHC ( "Particle_Ambient_SHC", Vector) = (0, 0, 0, 0)




		// ----------------------------------------- Un Comment Code For Distance Fade Options ---------------------------------------------------

		//[ShaderHelpDecorator(When activated particles will fade in and out based on their distance from the camera.,40)]
		//[Toggle(DISTANCEFADE)] _DISTANCEFADEToggle("Distance Fading", float) = 0 //---------------------------------------- Distance Fade Controls
		//
		//
		//[ShaderHelpDecorator(The distance at which particles start to become visible,40,_DISTANCEFADEToggle,1)]
		//[ShaderVariableHider(_DISTANCEFADEToggle,1)] _ParticleFadeInStart("Fade In Start", float) = 0
		//
		//[ShaderHelpDecorator(The percent increase in visibility per unit of distance from the fade start distance.,40,_DISTANCEFADEToggle,1)]
		//[ShaderVariableHider(_DISTANCEFADEToggle,1)] _ParticleFadeInRate("Fade In Rate", float) = 0
		//
		//[ShaderHelpDecorator(The distance at which particles fully fade out.,40,_DISTANCEFADEToggle,1)]
		//[ShaderVariableHider(_DISTANCEFADEToggle,1)] _ParticleFadeOutEnd("Fade Out End", float) = 0
		//
		//[ShaderHelpDecorator(The percent decrease in visibility per unit of distance closer to fade end distance.,40,_DISTANCEFADEToggle,1)]
		//[ShaderVariableHider(_DISTANCEFADEToggle,1)] _ParticleFadeOutRate("Fade Out Rate", float) = 0
		//
		//[ShaderHelpDecorator(Even if a particle is fully transparent it still consumes computer resources to render it.To increase performance once a vertex is fully faded out this shader moves it off screen so it no longer costs resources to render.With view aligned particles the vertex hiding system works perfectly because all vertexes are at the same distance from the camera.stretched and facing particles do not work as well with the vertex hiding system as the vertexes of the particle can be at different distances from the camera causing one vertex to become hidden while the rest of the vertexes have not yet faded out completely.The particle cull offset delays the vertex hiding until the entire particle has faded out.If you are seeing strange artifacts as particles fade out then you may need to increase the particle cull offset values.,300,_DISTANCEFADEToggle,1)]
		//[ShaderVariableHider(_DISTANCEFADEToggle,1)] _ParticleNearDistanceCullOffset("Particle Cull Near Offset", float) = 0
		//[ShaderVariableHider(_DISTANCEFADEToggle,1)] _ParticleFarDistanceCullOffset("Particle Cull Far Offset", float) = 0
		
	}
	SubShader
	{
		Tags{ "LIGHTMODE" = "Vertex" "QUEUE" = "Transparent" "IGNOREPROJECTOR" = "true" "RenderType" = "Transparent" "PreviewType" = "Plane" }
		Pass
		{
			Tags{ "LIGHTMODE" = "Vertex" "QUEUE" = "Transparent" "IGNOREPROJECTOR" = "true" "RenderType" = "Transparent" "PreviewType" = "Plane" }
			ZWrite Off
			Cull Off
			Blend One OneMinusSrcAlpha
			ColorMask RGB
			CGPROGRAM

#pragma vertex vert
#pragma fragment frag
//pragma target 2.0
//pragma target 3.0
#pragma multi_compile_fog
#pragma multi_compile_particles

#include "UnityCG.cginc"

		//Defines
#define USING_FOG (defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2))

		// ES2.0/WebGL/3DS can not do loops with non-constant-expression iteration counts :(
#if defined(SHADER_API_GLES)
#define LIGHT_LOOP_LIMIT 8
#elif defined(SHADER_API_N3DS)
#define LIGHT_LOOP_LIMIT 4
#else
#define LIGHT_LOOP_LIMIT unity_VertexLightParams.x
#endif

		//convert texture lookups on low end platforms to tex 2d instead of tex 1d
//if defined(SHADER_API_GLES)
#define TEX_2D_ONLY
//endif

		//HDR Lighting Define
#if defined(HDR )
#define HDR_EMISSIVE_MULTIPLYER * _HDREmissiveMultiplyer
#else 
#define HDR_EMISSIVE_MULTIPLYER
#endif



#ifdef SEPERATE_EMISSIVE_LIGHTING
#ifdef BLEND_FRAMES
#define BLEND_EMISSIVE_LIGHTING
#else
#define SINGLE_EMISSIVE_LIGHTING
#endif
#endif
//
///define emissive lighting when using packed emissive
#if defined(PACKED_EMISSIVE_LIGHTING) || defined(SEPERATE_EMISSIVE_LIGHTING)
#define EMISSIVE_LIGHTING
#endif

		// Compile specialized variants for when positional (point/spot) and spot lights are present

		//dont disable 
#pragma multi_compile __ POINT SPOT 
#pragma shader_feature __ COMPLEX_LIGHT_CONTROL 
#pragma multi_compile __ BLEND_FRAMES
#pragma shader_feature __ SEPERATE_EMISSIVE_LIGHTING PACKED_EMISSIVE_LIGHTING 
#pragma shader_feature __ LOW_ALPHA_EMISSIVE_COLOUR
#pragma shader_feature __ HDR
#pragma multi_compile __ LIGHTPROBE
//pragma multi_compile __ DISTANCEFADE //results in too many instructions on shader model 2.0
#pragma shader_feature __ DISSOLVE_ALPHA
#pragma multi_compile __ LIGHT_SCATTERING

//---------- For Testing Only 
//define COMPLEX_LIGHT_CONTROL
//define BLEND_FRAMES 
//define HDR
//define SPOT
//define DISSOLVE_ALPHA
//define LIGHT_SCATTERING
//define LIGHTPROBE
		//-------------- If you are running out of shader words duplicat this shader, disable the above values and activate these -----------------------
//define COMPLEX_LIGHT_CONTROL
//define BLEND_FRAMES 
//define SEPERATE_EMISSIVE_LIGHTING
//define PACKED_EMISSIVE_LIGHTING
//define HDR 
//define LIGHTPROBE
//define DISTANCEFADE 
//define DISSOLVE_ALPHA 
//define LIGHT_SCATTERING

			//---------------------- GLobal Variables ------------------------------------

		//static consts used in tri axis lighting 

			static const fixed3 RIGHT_TRI_AXIS = fixed3(0.81649658092, 0, 0.57735026999);
			static const fixed3 UP_LEFT_TRI_AXIS = fixed3(-0.40824829046, 0.70710678118, 0.57735026999);
			//static const fixed3 DOWN_LEFT_TRI_AXIS = fixed3(-0.40824829046, -0.70710678118, 0.57735026999);
			
			int4 unity_VertexLightParams; // x: light count, y: zero, z: one (y/z needed by d3d9 vs loop instruction)

			half _LightWrapAround;
			half _ShadowEffectMultiplyer;
			half _LightSharpness;

			//--------------- Low Alpha Emissive --------------
			half3 _LowAlphaEmissiveColour;
			half _LowAlphaStart;

			half _HDREmissiveMultiplyer;

			//-------- Particle Fading --------------------
			half _ParticleFadeInStart;
			half _ParticleFadeInRate;
			
			half _ParticleFadeOutEnd;
			half _ParticleFadeOutRate;

			half _ParticleNearDistanceCullOffset;
			half _ParticleFarDistanceCullOffset;

			//-------- Light Scatter ----------------------
			half _LightScatterSharpness;
			half _LightScatterMinAlphaMultiplyer;
			half _LightScatterMaxAlphaMultiplyer;
			half _LightScatterAmbientLightMultiplyer;

			//-------- Spherical harmonics values ---------

			half _AmbientLightMultiplier;
			half _DirectLightMultiplier;

			//-------- Light Probe Sperical Harmonics -------
			half4 Particle_Probe_SHAr;
			half4 Particle_Probe_SHAg;
			half4 Particle_Probe_SHAb;
			half4 Particle_Probe_SHBr;
			half4 Particle_Probe_SHBg;
			half4 Particle_Probe_SHBb;
			half4 Particle_Probe_SHC;

			//-------- Global Ambient Harmonics -------------

			half4 Particle_Ambient_SHAr;
			half4 Particle_Ambient_SHAg;
			half4 Particle_Ambient_SHAb;
			half4 Particle_Ambient_SHBr;
			half4 Particle_Ambient_SHBg;
			half4 Particle_Ambient_SHBb;
			half4 Particle_Ambient_SHC;

			//harmonics source defines
#if defined(LIGHTPROBE)

#define SH_A_R Particle_Probe_SHAr
#define SH_A_G Particle_Probe_SHAg
#define SH_A_B Particle_Probe_SHAb
#define SH_B_R Particle_Probe_SHBr
#define SH_B_G Particle_Probe_SHBg
#define SH_B_B Particle_Probe_SHBb
#define SH_C Particle_Probe_SHC

#else 

#define SH_A_R Particle_Ambient_SHAr
#define SH_A_G Particle_Ambient_SHAg
#define SH_A_B Particle_Ambient_SHAb
#define SH_B_R Particle_Ambient_SHBr
#define SH_B_G Particle_Ambient_SHBg
#define SH_B_B Particle_Ambient_SHBb
#define SH_C Particle_Ambient_SHC

#endif

			float4x4 _Camera2World;
			
			float _InvFade;

#if defined(TEX_2D_ONLY)
			sampler2D _EmissiveLookupTex;
#else

			sampler1D _EmissiveLookupTex;
#endif
			sampler2D _EmissiveTex;
	//		sampler2D _TestingTransTex;
			sampler2D _MainTex;
			float4 _MainTex_ST;

			sampler2D_float _CameraDepthTexture;


			//------------------------- Structs ----------------------------------------

			struct appdata
			{
				float3 vertex : POSITION;
				float3 normal : NORMAL;
				float3 tangent : TANGENT;
				half4 color : COLOR;
				float2 uv : TEXCOORD0;
#ifdef BLEND_FRAMES
				float3 uv2blendandframe : TEXCOORD2;
#endif
			};

			struct v2f
			{
				
				float4 vertex : SV_POSITION;
				half4 UVRG1 : TEXCOORD0;
				half4 B1RGB2 : TEXCOORD1;
				half4 RGB3A : TEXCOORD2;
#if defined(USING_FOG) || defined(BLEND_FRAMES)
				half4 UV2BLF : TEXCOORD3;
#endif

#ifdef SOFTPARTICLES_ON
				half4 projPos : TEXCOORD4;
#endif

#ifdef LIGHT_SCATTERING
				half3 LSCAT : TEXCOORD5;
#endif
				//float TestingValue1 : TEXCOORD4;
				//float4 TestingValue2 : TEXCOORD5;
			};

			//---------------------------- Functions ---------------------------

			// particle distance fading 
			float ApplyDistanceFadeIn( inout float4 VertPos, float Distance, float DistanceFadeStart, float DistanceFadeRate , float fCullDistanceOffset)
			{
				float StartDistanceOffset = (-Distance - DistanceFadeStart);
				

				//hide particle when range limit reached
				VertPos = lerp(float4(9999.0, 9999.0, 9999.0, 9999.0), VertPos, clamp((StartDistanceOffset - fCullDistanceOffset) * 999999.0f, 0.0, 1.0));

				return clamp(StartDistanceOffset * DistanceFadeRate, 0.0, 1.0);
			}

			float ApplyDistanceFadeOut( inout float4 VertPos, float Distance, float DistanceFadeEnd, float DistanceFadeRate, float fCullDistanceOffset)
			{
				float EndDistanceOffset = (-Distance - DistanceFadeEnd);


				//hide particle when range limit reached
				VertPos = lerp( VertPos , float4(9999.0, 9999.0, 9999.0, 9999.0), clamp((EndDistanceOffset + fCullDistanceOffset) * 999999.0f, 0.0, 1.0));

				return clamp(EndDistanceOffset * DistanceFadeRate, 0.0, 1.0);
			}

			//---------- Lighting --------------

			// normal should be normalized, w=1.0
			half3 ParticleSHEvalLinearL0L1(half4 normal)
			{
				half3 x;

				// Linear (L1) + constant (L0) polynomial terms
				x.r = dot(SH_A_R, normal);
				x.g = dot(SH_A_G, normal);
				x.b = dot(SH_A_B, normal);

				return x;
			}

			// normal should be normalized, w=1.0
			half3 ParticleSHEvalLinearL2(half4 normal)
			{
				half3 x1, x2;
				// 4 of the quadratic (L2) polynomials
				half4 vB = normal.xyzz * normal.yzzx;
				x1.r = dot(SH_B_R, vB);
				x1.g = dot(SH_B_G, vB);
				x1.b = dot(SH_B_B, vB);

				// Final (5th) quadratic (L2) polynomial
				half vC = normal.x*normal.x - normal.y*normal.y;
				x2 = SH_C.rgb * vC;

				return x1 + x2;
			}

			// normal should be normalized, w=1.0
			// output in active color space
			half3 ParticleShadeSH9(half4 normal)
			{

				// Linear + constant polynomial terms
				half3 res = ParticleSHEvalLinearL0L1(normal);

				// Quadratic polynomials
				res += ParticleSHEvalLinearL2(normal);

				#ifdef UNITY_COLORSPACE_GAMMA
					res = LinearToGammaSpace(res);
				#endif

				return res;
			}

			//compute light scatter contribution
			half3 ComputeLightScatter(int idx, half3 vertEyePos,float3 lightDir, half atten)
			{

				half3 eyeVirtDir = normalize(vertEyePos);
				//half3 eyeLightDir = normalize(lightDir);

				return unity_LightColor[idx].rgb * (saturate(pow(saturate(dot(eyeVirtDir, lightDir)), _LightScatterSharpness))  * atten);

			//	return unity_LightColor[idx].rgb * (saturate(dot(eyeVirtDir, eyeLightDir))  * atten);

				//return half3(0, 0, 0);
			}

			half3 ComputeLightScatter2(half3 vertEyePos, float3 lightDir, half3 Incominglight)
			{

				half3 eyeVirtDir = normalize(vertEyePos);
				//half3 eyeLightDir = normalize(lightDir);

				return  (saturate(pow(saturate(dot(eyeVirtDir, lightDir)), _LightScatterSharpness))  * Incominglight);

				//	return unity_LightColor[idx].rgb * (saturate(dot(eyeVirtDir, eyeLightDir))  * atten);

				//return half3(0, 0, 0);
			}

			// Compute illumination from one light, given attenuation
			half3 computeLighting(int idx, half3 dirToLight, half3 eyeNormal, half atten) 
			{
#ifdef COMPLEX_LIGHT_CONTROL
				half NdotL = max(dot(eyeNormal, dirToLight), 0.0);

				NdotL = pow((clamp((NdotL * _LightWrapAround) + 1.0 - _LightWrapAround, 0.0, 1.0) * _ShadowEffectMultiplyer) + (1.0 - _ShadowEffectMultiplyer), _LightSharpness);
#else
				half NdotL = max(dot(eyeNormal, dirToLight), 0.0);
#endif
				// diffuse
				half3 color = NdotL * unity_LightColor[idx].rgb;

				return color * atten;
			}

			// Compute illumination from one light, given attenuation
			half3 ComputeLightForDirection(half3 dirToLight, half3 eyeNormal, half3 attenuatedLight)
			{
#ifdef COMPLEX_LIGHT_CONTROL
				half NdotL = max(dot(eyeNormal, dirToLight), 0.0);

				NdotL = pow((clamp((NdotL * _LightWrapAround) + 1.0 - _LightWrapAround, 0.0, 1.0) * _ShadowEffectMultiplyer) + (1.0 - _ShadowEffectMultiplyer), _LightSharpness);
#else
				half NdotL = max(dot(eyeNormal, dirToLight), 0.0);
#endif
				// diffuse
				//half3 color = NdotL * unity_LightColor[idx].rgb;

				return attenuatedLight * NdotL;
			}

			void ComputeAttenuation(int idx, float3 eyePosition,out float3 dirToLight,out half attenuation)
			{
				dirToLight = unity_LightPosition[idx].xyz;
				attenuation = 1.0;
#if defined(POINT) || defined(SPOT)
				dirToLight -= eyePosition * unity_LightPosition[idx].w;
				// distance attenuation
				float distSqr = dot(dirToLight, dirToLight);
				attenuation /= (1.0 + unity_LightAtten[idx].z * distSqr);
				if (unity_LightPosition[idx].w != 0 && distSqr > unity_LightAtten[idx].w) attenuation = 0.0; // set to 0 if outside of range
				distSqr = max(distSqr, 0.000001); // don't produce NaNs if some vertex position overlaps with the light
				dirToLight *= rsqrt(distSqr);
#if defined(SPOT)
				// spot angle attenuation
				half rho = max(dot(dirToLight, unity_SpotDirection[idx].xyz), 0.0);
				half spotAtt = (rho - unity_LightAtten[idx].x) * unity_LightAtten[idx].y;
				attenuation *= saturate(spotAtt);
#endif
#endif
				attenuation *= 0.5; // passed in light colors are 2x brighter than what used to be in FFP
			}

			void ComputeAttenuatedLight(int idx, float3 eyePosition, out float3 dirToLight, out half3 light)
			{
				dirToLight = unity_LightPosition[idx].xyz;
				light = unity_LightColor[idx].rgb;
#if defined(POINT) || defined(SPOT)
				dirToLight -= eyePosition * unity_LightPosition[idx].w;
				// distance attenuation
				float distSqr = dot(dirToLight, dirToLight);
				light /= (1.0 + unity_LightAtten[idx].z * distSqr);
				if (unity_LightPosition[idx].w != 0 && distSqr > unity_LightAtten[idx].w) light = half3(0, 0, 0); // set to 0 if outside of range
				distSqr = max(distSqr, 0.000001); // don't produce NaNs if some vertex position overlaps with the light
				dirToLight *= rsqrt(distSqr);
#if defined(SPOT)
				// spot angle attenuation
				half rho = max(dot(dirToLight, unity_SpotDirection[idx].xyz), 0.0);
				half spotAtt = (rho - unity_LightAtten[idx].x) * unity_LightAtten[idx].y;
				light *= saturate(spotAtt);
#endif
#endif
				//moved to post intinsity colour multiply
				light *= _DirectLightMultiplier; // Scale light brightness
			}

			// Compute attenuation & illumination from one light
			half3 computeOneLight(int idx, float3 eyePosition, half3 eyeNormal) 
			{
				float3 dirToLight = unity_LightPosition[idx].xyz;
				half att = 1.0;
#if defined(POINT) || defined(SPOT)
				dirToLight -= eyePosition * unity_LightPosition[idx].w;
				// distance attenuation
				float distSqr = dot(dirToLight, dirToLight);
				att /= (1.0 + unity_LightAtten[idx].z * distSqr);
				if (unity_LightPosition[idx].w != 0 && distSqr > unity_LightAtten[idx].w) att = 0.0; // set to 0 if outside of range
				distSqr = max(distSqr, 0.000001); // don't produce NaNs if some vertex position overlaps with the light
				dirToLight *= rsqrt(distSqr);
#if defined(SPOT)
				// spot angle attenuation
				half rho = max(dot(dirToLight, unity_SpotDirection[idx].xyz), 0.0);
				half spotAtt = (rho - unity_LightAtten[idx].x) * unity_LightAtten[idx].y;
				att *= saturate(spotAtt);
#endif
#endif
				att *= 0.5; // passed in light colors are 2x brighter than what used to be in FFP

				return min(computeLighting(idx, dirToLight, eyeNormal, att), 1.0);
			}



			half3 calculateAmbientLight(half3 normalWorld)
			{
				//Flat ambient is just the sky color
				fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * 0.75;

#if defined(TRI_COLOR_AMBIENT)    

				//Magic constants used to tweak ambient to approximate pixel shader spherical harmonics 
				half3 worldUp = fixed3(0, 1, 0);
				half skyGroundDotMul = 2.5;
				half minEquatorMix = 0.5;
				half equatorColorBlur = 0.33;

				half upDot = dot(normalWorld, worldUp);

				//Fade between a flat lerp from sky to ground and a 3 way lerp based on how bright the equator light is.
				//This simulates how directional lights get blurred using spherical harmonics

				//Work out color from ground and sky, ignoring equator
				float adjustedDot = upDot * skyGroundDotMul;
				half3 skyGroundColor = lerp(unity_AmbientGround, unity_AmbientSky, saturate((adjustedDot + 1.0) * 0.5));

				//Work out equator lights brightness
				float equatorBright = saturate(dot(unity_AmbientEquator.rgb, unity_AmbientEquator.rgb));

				//Blur equator color with sky and ground colors based on how bright it is.
				half3 equatorBlurredColor = lerp(unity_AmbientEquator, saturate(unity_AmbientEquator + unity_AmbientGround + unity_AmbientSky), equatorBright * equatorColorBlur);

				//Work out 3 way lerp inc equator light
				float smoothDot = pow(abs(upDot), 1);
				half3 equatorColor = lerp(equatorBlurredColor, unity_AmbientGround, smoothDot) * step(upDot, 0) + lerp(equatorBlurredColor, unity_AmbientSky, smoothDot) * step(0, upDot);

				return lerp(skyGroundColor, equatorColor, saturate(equatorBright + minEquatorMix)) * 0.75;

#endif // TRI_COLOR_AMBIENT    

				return ambient;
			}

			void Calculate3WayFullAmbientLight(half3 WorldNormalRight, half3 WorldNormalUpLeft, half3 WorldNormalDownLeft, out half3 RightCol, out half3 UpLeftCol, out half3 DownLeftCol)
			{
				
				
				//magic constants
				half3 worldUp = fixed3(0, 1, 0);
				half skyGroundDotMul = 2.5;
				half minEquatorMix = 0.5;
				half equatorColorBlur = 0.33;

				//calculate shared values

				//Work out equator lights brightness
				float equatorBright = saturate(dot(unity_AmbientEquator.rgb, unity_AmbientEquator.rgb));

				//Blur equator color with sky and ground colors based on how bright it is.
				half3 equatorBlurredColor = lerp(unity_AmbientEquator, saturate(unity_AmbientEquator + unity_AmbientGround + unity_AmbientSky), equatorBright * equatorColorBlur);

				//dot product for 3 normals
				half3 skyGroundColorRight = lerp(unity_AmbientGround, unity_AmbientSky, saturate(((WorldNormalRight.y * skyGroundDotMul) + 1.0) * 0.5));
				half3 skyGroundColorUpLeft = lerp(unity_AmbientGround, unity_AmbientSky, saturate(((WorldNormalUpLeft.y * skyGroundDotMul) + 1.0) * 0.5));
				half3 skyGroundColorDownLeft = lerp(unity_AmbientGround, unity_AmbientSky, saturate(((WorldNormalDownLeft.y * skyGroundDotMul) + 1.0) * 0.5));

				//Work out 3 way lerp inc equator light
				float smoothDotRight = pow(abs(WorldNormalRight.y), 1);
				float smoothDotUpLeft = pow(abs(WorldNormalUpLeft.y), 1);
				float smoothDotDownLeft = pow(abs(WorldNormalDownLeft.y), 1);


				half3 equatorColorRight = lerp(equatorBlurredColor, unity_AmbientGround, smoothDotRight) * step(WorldNormalRight.y, 0) + lerp(equatorBlurredColor, unity_AmbientSky, smoothDotRight) * step(0, WorldNormalRight.y);
				half3 equatorColorUpLeft = lerp(equatorBlurredColor, unity_AmbientGround, smoothDotUpLeft) * step(WorldNormalUpLeft.y, 0) + lerp(equatorBlurredColor, unity_AmbientSky, smoothDotUpLeft) * step(0, WorldNormalUpLeft.y);
				half3 equatorColorDownLeft = lerp(equatorBlurredColor, unity_AmbientGround, smoothDotDownLeft) * step(WorldNormalDownLeft.y, 0) + lerp(equatorBlurredColor, unity_AmbientSky, smoothDotDownLeft) * step(0, WorldNormalDownLeft.y);

				RightCol = lerp(skyGroundColorRight, equatorColorRight, saturate(equatorBright + minEquatorMix)) * 0.75;
				UpLeftCol = lerp(skyGroundColorUpLeft, equatorColorUpLeft, saturate(equatorBright + minEquatorMix)) * 0.75;
				DownLeftCol = lerp(skyGroundColorDownLeft, equatorColorDownLeft, saturate(equatorBright + minEquatorMix)) * 0.75;

				
				//RightCol = 0;
				//UpLeftCol = 0;
				//DownLeftCol = 0;

				//RightCol = WorldNormalRight.yyy;
				//RightCol = WorldNormalUpLeft.yyy;
				//RightCol = WorldNormalDownLeft.yyy;
			}
			//---------------------------- Vertex --------------------------------
			v2f vert(appdata v)
			{
				v2f o;

				o.vertex = UnityObjectToClipPos(v.vertex);
				o.UVRG1.xy = TRANSFORM_TEX(v.uv, _MainTex);

				float3 eyePos = mul(UNITY_MATRIX_MV, float4(v.vertex, 1)).xyz;

				//soft particle calculation
#ifdef SOFTPARTICLES_ON
				o.projPos = ComputeScreenPos(o.vertex);
				COMPUTE_EYEDEPTH(o.projPos.z);
#endif

				//calc normal direction
				//half3 eyeNormal = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.normal).xyz);
				//half3 eyeTangent = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, v.tangent).xyz);

				half3 eyeNormal = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal).xyz);
				half3 eyeTangent = normalize(mul((float3x3)UNITY_MATRIX_MV, v.tangent).xyz);
				half3 eyeCrossTangent = cross(eyeNormal, eyeTangent);

				//Convert Normal to tri axis
				half3 TriAxisRight = (eyeTangent * RIGHT_TRI_AXIS.x) + (eyeCrossTangent * RIGHT_TRI_AXIS.y) + (eyeNormal * RIGHT_TRI_AXIS.z);
				half3 TriAxisUpLeft = (eyeTangent * UP_LEFT_TRI_AXIS.x) + (eyeCrossTangent * UP_LEFT_TRI_AXIS.y) + (eyeNormal * UP_LEFT_TRI_AXIS.z);
				half3 TriAxisDownLeft = cross(TriAxisRight, TriAxisUpLeft);

#if defined(LIGHT_SCATTERING)
				half3 ScatterDirection = normalize(eyePos);
#endif
				//get world spce lighting directions 
				half4 TriAxisRightWorld = float4(mul(_Camera2World, half4(TriAxisRight, 0)).xyz,1);
				half4 TriAxisUpLeftWorld = float4(mul(_Camera2World, half4(TriAxisUpLeft, 0)).xyz,1);
				half4 TriAxisDownLeftWorld = half4(cross(TriAxisUpLeftWorld.xyz ,TriAxisRightWorld.xyz),1);

				//do the sperical harmonic lighting for either probe or ambient lighting 
				half3 RightCol = ParticleShadeSH9(TriAxisRightWorld) * _AmbientLightMultiplier ;
				half3 UpLeftCol = ParticleShadeSH9(TriAxisUpLeftWorld) * _AmbientLightMultiplier ;
				half3 DownLeftCol = ParticleShadeSH9(TriAxisDownLeftWorld) * _AmbientLightMultiplier ;

#if defined(LIGHT_SCATTERING)
				half3 LightScatterCol = ParticleShadeSH9(half4(mul(_Camera2World, half4(ScatterDirection, 0)).xyz,1)) * _LightScatterAmbientLightMultiplyer ;
#endif
								
				float3 DirToLight;
				half3 light;

				//do all the lighting for each of the axis
				for (int i = 0; i < LIGHT_LOOP_LIMIT; i++)
				{
					//calculate light attenuation 
					//ComputeAttenuation(i, eyePos,  DirToLight, Attenuation);
					ComputeAttenuatedLight(i, eyePos, DirToLight, light);


					//RightCol += computeOneLight(i, eyePos, TriAxisRight);
					//UpLeftCol += computeOneLight(i, eyePos, TriAxisUpLeft);
					//DownLeftCol += computeOneLight(i, eyePos, TriAxisDownLeft);

					//RightCol += min(computeLighting(i, DirToLight, TriAxisRight, Attenuation), 1.0);
					//UpLeftCol += min(computeLighting(i, DirToLight, TriAxisUpLeft, Attenuation), 1.0);
					//DownLeftCol += min(computeLighting(i, DirToLight, TriAxisDownLeft, Attenuation), 1.0);

					//RightCol += min(ComputeLightForDirection(DirToLight, TriAxisRight, light), 1.0);
					//UpLeftCol += min(ComputeLightForDirection(DirToLight, TriAxisUpLeft, light), 1.0);
					//DownLeftCol += min(ComputeLightForDirection(DirToLight, TriAxisDownLeft, light), 1.0);

					RightCol +=  ComputeLightForDirection(DirToLight, TriAxisRight, light) ;
					UpLeftCol +=  ComputeLightForDirection(DirToLight, TriAxisUpLeft, light) ;
					DownLeftCol += ComputeLightForDirection(DirToLight, TriAxisDownLeft, light) ;

#if defined(LIGHT_SCATTERING)

					//LightScatterCol += ComputeLightScatter(i, eyePos, DirToLight, Attenuation);
					LightScatterCol += ComputeLightScatter2( eyePos, DirToLight, light);
#endif
				}



#if defined(LIGHT_SCATTERING)
				LightScatterCol *= v.color.rgb;
#endif

				//scale light value
				//v.color.rgb *= _DirectLightMultiplier;

				//apply particle colouring 
				RightCol *= v.color.rgb;
				UpLeftCol *= v.color.rgb;
				DownLeftCol *= v.color.rgb;

#ifdef PACKED_EMISSIVE_LIGHTING
				//apply emissive scaling to compensate for normal lenght
				RightCol *= half3(1.1547h, 1.1547h, 1.1547h);
				UpLeftCol *= half3(1.1547h, 1.1547h, 1.1547h);
				DownLeftCol *= half3(1.1547h, 1.1547h, 1.1547h);
#endif

				// pack values 
				o.UVRG1.zw = RightCol.rg;
				o.B1RGB2 = half4(RightCol.b, UpLeftCol.rgb);
				o.RGB3A = half4(DownLeftCol.rgb, v.color.a);

#if defined(LIGHT_SCATTERING)
				o.LSCAT = LightScatterCol;
#endif

#if defined(USING_FOG) || defined(BLEND_FRAMES)
				o.UV2BLF = half4(0, 0, 0, 0);
#endif

#ifdef USING_FOG
				//float fogCoord = length(eyePos.xyz); // radial fog distance
				//UNITY_CALC_FOG_FACTOR_RAW(fogCoord);
				UNITY_CALC_FOG_FACTOR_RAW(length(eyePos.xyz));
				
				o.UV2BLF.w = saturate(unityFogFactor); //store fog factor
#endif

#ifdef BLEND_FRAMES
				o.UV2BLF.xyz = v.uv2blendandframe;
#endif

#ifdef DISTANCEFADE

		//	//fade in
		//	half FadeInValue = ApplyDistanceFadeIn(o.vertex, eyePos.z, _ParticleFadeInStart, _ParticleFadeInRate, _ParticleNearDistanceCullOffset);
		//	
		//	//fade out
		//	half FadeoutValue = ApplyDistanceFadeOut(o.vertex, eyePos.z, _ParticleFadeOutEnd, _ParticleFadeOutRate, _ParticleFarDistanceCullOffset);
				
				///o.RGB3A.a = -Distance * 1;
				o.RGB3A.a *= min(ApplyDistanceFadeIn(o.vertex, eyePos.z, _ParticleFadeInStart, _ParticleFadeInRate, _ParticleNearDistanceCullOffset), ApplyDistanceFadeOut(o.vertex, eyePos.z, _ParticleFadeOutEnd, _ParticleFadeOutRate, _ParticleFarDistanceCullOffset));
#endif


				//for testing 
				//o.TestingValue1 = half4(TriAxisRight,1);
				//o.TestingValue1 = half4(eyeTangent, 1);
				return o;
			}

			//--------------------------------- Fragment ------------------------------

			fixed4 frag(v2f i) : SV_Target
			{
				//return half4(0.5f,0.5f, 0.5f,1);
				//return half4(i.RGB3A.aaa,1);
				//return half4(half3(i.UVRG1.zw, i.B1RGB2.x),1);
				//return half4(i.B1RGB2.yzw, 1);
				//return half4(i.RGB3A.rgb, 1);
				//return half4(i.TestingValue1 , i.TestingValue1, i.TestingValue1,1);


				//half4  tex;
				//fixed4 col;
				//---------------- sample the texture ---------------
				
#ifdef BLEND_FRAMES
				half4 tex = lerp(tex2D(_MainTex, i.UVRG1.xy), tex2D(_MainTex, i.UV2BLF.xy), i.UV2BLF.z);
#else
				half4 tex = tex2D(_MainTex, i.UVRG1.xy);
				//fixed4 tex = tex2D(_MainTex, i.UVRG1.xy);
#endif
				//---------------- Apply Soft Particle ---------------
#ifdef SOFTPARTICLES_ON
				//i.RGB3A.a *= saturate(_InvFade * tex.a * tex.a * (LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))) - i.projPos.z));

				i.RGB3A.a *= saturate(_InvFade * (LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))) - i.projPos.z));

#endif

#ifdef PACKED_EMISSIVE_LIGHTING
				//------------------- Emissive and Occlusion packed into normal ----------------

				//half NormalMagnitude = dot(tex.rgb, tex.rgb);

				//fixed4 EmissiveLookup = tex1D(_EmissiveOcclusionLookupTex, NormalMagnitude);

				//apply occlusion to tex
				//tex.rgb = tex.rgb * EmissiveLookup.a;

				//return fixed4(EmissiveLookup.a, EmissiveLookup.a, EmissiveLookup.a, tex.a);
				//return fixed4(EmissiveLookup.rgb, tex.a);


				//-------------------------- Emissive packed into alpha ---------------------
				//half EmissiveLookUp = tex.a;
				//

#if defined(TEX_2D_ONLY)
				half4 Emissive = tex2D(_EmissiveLookupTex, half2(tex.a,0));
#else
				half4 Emissive = tex1D(_EmissiveLookupTex, tex.a);
#endif
				//
				//tex.a = saturate(tex.a * 2); // EmissiveLookup.a;

				//------------------------- Transparency Packed into Normal -----------------
				
				//float4 TestingTex =  tex2D(_TestingTransTex, i.UVRG1.xy);

				//tex.a = 0.5f + (tex.a - saturate((dot(tex.rgb, tex.rgb) * -0.444444444f) + 1.33333333f));
				//tex.a = 0.5f + (TestingTex.a - saturate((dot(tex.rgb, tex.rgb) * -0.444444444f) + 1.33333333f));
				//tex.a = 0.5f + (TestingTex.a - tex.a);

				//return float4(tex.a, tex.a, tex.a, TestingTex.a);
				
				//tex.a = min(((dot(tex.rgb, tex.rgb) * -0.444444444h) + 1.33333333h) , Emissive.a);
				
				half EmissiveAlphaFix = ((dot(tex.rgb, tex.rgb) * -0.444444444h) + 1.33333333h);

				tex = half4(tex.rgb * (EmissiveAlphaFix ), min(EmissiveAlphaFix, Emissive.a));

				//return half4(EmissiveAlphaFix, EmissiveAlphaFix, EmissiveAlphaFix, min(EmissiveAlphaFix, Emissive.a));

				//*EmissiveAlphaFix * EmissiveAlphaFix
				//tex.a = 0.5f + (TestingTex.a - tex.a);
				//return float4(tex.a, tex.a, tex.a, TestingTex.a);

				//float fNormalAlpha = ((dot(tex.rgb, tex.rgb) * -0.444444444f) + 1.33333333f);

				//return float4(fNormalAlpha, fNormalAlpha, fNormalAlpha, 1);

				//return float4(dot(tex.rgb, tex.rgb), dot(tex.rgb, tex.rgb), dot(tex.rgb, tex.rgb), 1);

				//--------------- Emissive Packed Into Normal Occlusion Packed into Alpha ---------

				//get emissive lookup
				//half LookupUV = dot(tex.rgb, tex.rgb);

				//look up normalization and emissive
				//fixed4 EmissiveLookup = tex1D(_EmissiveOcclusionLookupTex, LookupUV);

				//normalize and prep alpha for unpack
				//tex *= half4(EmissiveLookup.aaa,2);

				//tex *= half4(1,1,1, 2);

				//get occlusion and factor in extra scaling for normalization
				//half Occlusion = saturate( -tex.a + 2) ;

				//return fixed4(Occlusion, Occlusion, Occlusion, tex.a);

				//get true alpha and apply occlusion
				//tex = half4(tex.rgb * Occlusion,saturate(tex.a));
#endif
#ifdef SINGLE_EMISSIVE_LIGHTING 
			half4 Emissive = tex2D(_EmissiveTex,  i.UVRG1.xy);
#endif
#ifdef BLEND_EMISSIVE_LIGHTING  
			half4 Emissive = lerp(tex2D(_EmissiveTex, i.UVRG1.xy), tex2D(_EmissiveTex, i.UV2BLF.xy), i.UV2BLF.z);
#endif

				//--------------- commpile all the lighting -----------
				tex.rgb = (half3(i.UVRG1.zw, i.B1RGB2.x) * tex.r) + (i.B1RGB2.yzw *  tex.g) + (i.RGB3A.rgb * tex.b);



				//--------------- appply alpha -----------------------
#ifdef DISSOLVE_ALPHA
				tex.a = saturate( tex.a - (1 - i.RGB3A.a));
#else
				tex.a *= i.RGB3A.a;
#endif
				//--------------- Apply low alpha emissive lerp -------
#if defined(EMISSIVE_LIGHTING) && defined(LOW_ALPHA_EMISSIVE_COLOUR)  
				Emissive.rgb *= lerp(_LowAlphaEmissiveColour, half3(1, 1, 1), saturate(tex.a * _LowAlphaStart));
#endif
				//--------------- add emissive lighting ---------------
#ifdef EMISSIVE_LIGHTING
				//add the emissive lighting;

				tex.rgb += Emissive.rgb HDR_EMISSIVE_MULTIPLYER;
#endif



#if defined(LIGHT_SCATTERING)
				tex.rgb += i.LSCAT * saturate(lerp(_LightScatterMinAlphaMultiplyer, _LightScatterMaxAlphaMultiplyer, tex.a * tex.a));
#endif

				//-------------------- Calculate fog ---------------
#if USING_FOG
				tex.rgb = lerp(unity_FogColor.rgb, tex.rgb, i.UV2BLF.w) * tex.a;
#else
				tex.rgb *= tex.a;
#endif
				return tex;
			}
			ENDCG
		}
	}
}
