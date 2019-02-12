// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)
// ----------------------------------------------------------------------------
// Modified Unity Standard Shader for Procedural Stochastic Textures
// 2019 Unity Labs
// Paper:				https://eheitzresearch.wordpress.com/722-2/
// Technical chapter:	https://eheitzresearch.wordpress.com/738-2/
// Authors: 
// Thomas Deliot		<thomasdeliot@unity3d.com>
// Eric Heitz			<eric@unity3d.com>
// This software is a research prototype adapted for Unity in the hopes that it
// will be useful, but without any warranty of usability or maintenance. The
// comments in the code refer to specific sections of the Technical chapter.
// ----------------------------------------------------------------------------

#ifndef UNITY_STANDARD_INPUT_INCLUDED
#define UNITY_STANDARD_INPUT_INCLUDED

#include "UnityCG.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityPBSLighting.cginc" // TBD: remove
#include "UnityStandardUtils.cginc"

//---------------------------------------
// Directional lightmaps & Parallax require tangent space too
#if (_NORMALMAP || DIRLIGHTMAP_COMBINED || _PARALLAXMAP)
    #define _TANGENT_TO_WORLD 1
#endif

#if (_DETAIL_MULX2 || _DETAIL_MUL || _DETAIL_ADD || _DETAIL_LERP)
    #define _DETAIL 1
#endif

//---------------------------------------
half4       _Color;
half        _Cutoff;

Texture2D   _MainTexT;
SamplerState sampler_MainTexT;
float4      _MainTexT_ST;

Texture2D   _DetailAlbedoMapT;
SamplerState sampler_DetailAlbedoMapT;
float4      _DetailAlbedoMapT_ST;

Texture2D   _BumpMapT;
SamplerState sampler_BumpMapT;
half        _BumpScale;

Texture2D   _DetailMaskT;
SamplerState sampler_DetailMaskT;
Texture2D   _DetailNormalMapT;
SamplerState sampler_DetailNormalMapT;
half        _DetailNormalMapScale;

Texture2D   _SpecGlossMapT;
SamplerState sampler_SpecGlossMapT;
Texture2D   _MetallicGlossMapT;
SamplerState sampler_MetallicGlossMapT;
half        _Metallic;
float       _Glossiness;
float       _GlossMapScale;

Texture2D   _OcclusionMapT;
SamplerState sampler_OcclusionMapT;
half        _OcclusionStrength;

Texture2D   _ParallaxMapT;
SamplerState sampler_ParallaxMapT;
half        _Parallax;
half        _UVSec;

half4       _EmissionColor;
Texture2D   _EmissionMapT;
SamplerState sampler_EmissionMapT;

//-------------------------------------------------------------------------------------
// Input functions

struct VertexInput
{
    float4 vertex   : POSITION;
    half3 normal    : NORMAL;
    float2 uv0      : TEXCOORD0;
    float2 uv1      : TEXCOORD1;
#if defined(DYNAMICLIGHTMAP_ON) || defined(UNITY_PASS_META)
    float2 uv2      : TEXCOORD2;
#endif
#ifdef _TANGENT_TO_WORLD
    half4 tangent   : TANGENT;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};



// --------------Procedural Stochastic Texturing Uniforms----------------------
// Inverse histogram transformations T^{-1}
Texture2D   _MainTexInvT;
SamplerState sampler_MainTexInvT;
Texture2D   _DetailAlbedoMapInvT;
SamplerState sampler_DetailAlbedoMapInvT;
Texture2D   _BumpMapInvT;
SamplerState sampler_BumpMapInvT;
Texture2D   _DetailMaskInvT;
SamplerState sampler_DetailMaskInvT;
Texture2D   _DetailNormalMapInvT;
SamplerState sampler_DetailNormalMapInvT;
Texture2D   _SpecGlossMapInvT;
SamplerState sampler_SpecGlossMapInvT;
Texture2D   _MetallicGlossMapInvT;
SamplerState sampler_MetallicGlossMapInvT;
Texture2D   _OcclusionMapInvT;
SamplerState sampler_OcclusionMapInvT;
Texture2D   _ParallaxMapInvT;
SamplerState sampler_ParallaxMapInvT;
Texture2D   _EmissionMapInvT;
SamplerState sampler_EmissionMapInvT;

// Only with DXT compression (Section 1.6)
uniform float4 _MainTexDXTScalers;
uniform float4 _DetailAlbedoMapDXTScalers;
uniform float4 _BumpMapDXTScalers;
uniform float4 _DetailNormalMapDXTScalers;
uniform float4 _EmissionMapDXTScalers;

// Decorrelated color space vectors and origins, used on albedo and normal maps
uniform float3 _MainTexColorSpaceOrigin;
uniform float3 _MainTexColorSpaceVector1;
uniform float3 _MainTexColorSpaceVector2;
uniform float3 _MainTexColorSpaceVector3;
uniform float3 _DetailAlbedoColorSpaceOrigin;
uniform float3 _DetailAlbedoColorSpaceVector1;
uniform float3 _DetailAlbedoColorSpaceVector2;
uniform float3 _DetailAlbedoColorSpaceVector3;
uniform float3 _BumpMapColorSpaceOrigin;
uniform float3 _BumpMapColorSpaceVector1;
uniform float3 _BumpMapColorSpaceVector2;
uniform float3 _BumpMapColorSpaceVector3;
uniform float3 _DetailNormalColorSpaceOrigin;
uniform float3 _DetailNormalColorSpaceVector1;
uniform float3 _DetailNormalColorSpaceVector2;
uniform float3 _DetailNormalColorSpaceVector3;
uniform float3 _EmissionColorSpaceOrigin;
uniform float3 _EmissionColorSpaceVector1;
uniform float3 _EmissionColorSpaceVector2;
uniform float3 _EmissionColorSpaceVector3;
// ----------------------------------------------------------------------------


// --------------Procedural Stochastic Texturing Functions---------------------
float3 ReturnToOriginalColorSpace(float3 color, float3 colorSpaceOrigin, float3 colorSpaceVector1, float3 colorSpaceVector2, float3 colorSpaceVector3)
{
	float3 result =
		colorSpaceOrigin +
		colorSpaceVector1 * color.r +
		colorSpaceVector2 * color.g +
		colorSpaceVector3 * color.b;
	return result;
}

// Compute local triangle barycentric coordinates and vertex IDs
void TriangleGrid(float2 uv,
	out float w1, out float w2, out float w3,
	out int2 vertex1, out int2 vertex2, out int2 vertex3)
{
	// Scaling of the input
	uv *= 3.464; // 2 * sqrt(3)

	// Skew input space into simplex triangle grid
	const float2x2 gridToSkewedGrid = float2x2(1.0, 0.0, -0.57735027, 1.15470054);
	float2 skewedCoord = mul(gridToSkewedGrid, uv);

	// Compute local triangle vertex IDs and local barycentric coordinates
	int2 baseId = int2(floor(skewedCoord));
	float3 temp = float3(frac(skewedCoord), 0);
	temp.z = 1.0 - temp.x - temp.y;
	if (temp.z > 0.0)
	{
		w1 = temp.z;
		w2 = temp.y;
		w3 = temp.x;
		vertex1 = baseId;
		vertex2 = baseId + int2(0, 1);
		vertex3 = baseId + int2(1, 0);
	}
	else
	{
		w1 = -temp.z;
		w2 = 1.0 - temp.y;
		w3 = 1.0 - temp.x;
		vertex1 = baseId + int2(1, 1);
		vertex2 = baseId + int2(1, 0);
		vertex3 = baseId + int2(0, 1);
	}
}

// Fast random hash function
float2 SimpleHash2(float2 p)
{
	return frac(sin(mul(float2x2(127.1, 311.7, 269.5, 183.3), p)) * 43758.5453);
}

// Sample by-example procedural noise at uv on decorrelated input
float3 DecorrelatedStochasticSample(float2 uv, Texture2D Tinput, SamplerState samplerTinput, Texture2D invT, SamplerState samplerInvT,
	float4 dxtScalers, float3 colorSpaceOrigin, float3 colorSpaceVector1, float3 colorSpaceVector2, float3 colorSpaceVector3)
{
	// Get triangle info
	float w1, w2, w3;
	int2 vertex1, vertex2, vertex3;
	TriangleGrid(uv, w1, w2, w3, vertex1, vertex2, vertex3);

	// Assign random offset to each triangle vertex
	float2 uv1 = uv + SimpleHash2(vertex1);
	float2 uv2 = uv + SimpleHash2(vertex2);
	float2 uv3 = uv + SimpleHash2(vertex3);

	// Precompute UV derivatives 
	float2 duvdx = ddx(uv);
	float2 duvdy = ddy(uv);

	// Fetch Gaussian input
	float3 G1 = Tinput.SampleGrad(samplerTinput, uv1, duvdx, duvdy).rgb;
	float3 G2 = Tinput.SampleGrad(samplerTinput, uv2, duvdx, duvdy).rgb;
	float3 G3 = Tinput.SampleGrad(samplerTinput, uv3, duvdx, duvdy).rgb;

	// Variance-preserving blending
	float3 G = w1 * G1 + w2 * G2 + w3 * G3;
	G = G - 0.5;
	G = G * rsqrt(w1 * w1 + w2 * w2 + w3 * w3);
	if (dxtScalers.x >= 0.0) G = G * dxtScalers; // Only with DXT compression (Section 1.6)
	G = G + 0.5;

	// Compute used LOD level to fetch the prefiltered look-up table invT
	int dummy, lodLevels, widthT, heightT;
	invT.GetDimensions(0, dummy, lodLevels, dummy);
	Tinput.GetDimensions(0, widthT, heightT, dummy);
	duvdx *= float2(widthT, heightT);
	duvdy *= float2(widthT, heightT);
	float delta_max_sqr = max(dot(duvdx, duvdx), dot(duvdy, duvdy));
	float mml = 0.5 * log2(delta_max_sqr);
	float LOD = max(0, mml) / float(lodLevels);

	// Fetch prefiltered LUT (T^{-1})
	float3 color;
	color.r = invT.SampleLevel(samplerInvT, float2(G.r, LOD), 0).r;
	color.g = invT.SampleLevel(samplerInvT, float2(G.g, LOD), 0).g;
	color.b = invT.SampleLevel(samplerInvT, float2(G.b, LOD), 0).b;

	// Original color space for albedo RGB and normal XYZ
	color.rgb = ReturnToOriginalColorSpace(color.rgb, colorSpaceOrigin, colorSpaceVector1, colorSpaceVector2, colorSpaceVector3);

	return color;
}

// Sample by-example procedural noise at uv
float4 StochasticSample(float2 uv, Texture2D Tinput, SamplerState samplerTinput, Texture2D invT, SamplerState samplerInvT)
{
	// Get triangle info
	float w1, w2, w3;
	int2 vertex1, vertex2, vertex3;
	TriangleGrid(uv, w1, w2, w3, vertex1, vertex2, vertex3);

	// Assign random offset to each triangle vertex
	float2 uv1 = uv + SimpleHash2(vertex1);
	float2 uv2 = uv + SimpleHash2(vertex2);
	float2 uv3 = uv + SimpleHash2(vertex3);

	// Precompute UV derivatives 
	float2 duvdx = ddx(uv);
	float2 duvdy = ddy(uv);

	// Fetch Gaussian input
	float4 G1 = Tinput.SampleGrad(samplerTinput, uv1, duvdx, duvdy).rgba;
	float4 G2 = Tinput.SampleGrad(samplerTinput, uv2, duvdx, duvdy).rgba;
	float4 G3 = Tinput.SampleGrad(samplerTinput, uv3, duvdx, duvdy).rgba;

	// Variance-preserving blending
	float4 G = w1 * G1 + w2 * G2 + w3 * G3;
	G = G - 0.5;
	G = G * rsqrt(w1 * w1 + w2 * w2 + w3 * w3);
	G = G + 0.5;

	// Compute used LOD level to fetch the prefiltered look-up table invT
	int dummy, lodLevels, widthT, heightT;
	invT.GetDimensions(0, dummy, lodLevels, dummy);
	Tinput.GetDimensions(0, widthT, heightT, dummy);
	duvdx *= float2(widthT, heightT);
	duvdy *= float2(widthT, heightT);
	float delta_max_sqr = max(dot(duvdx, duvdx), dot(duvdy, duvdy));
	float mml = 0.5 * log2(delta_max_sqr);
	float LOD = max(0, mml) / float(lodLevels);

	// Fetch prefiltered LUT (T^{-1})
	float4 color;
	color.r = invT.SampleLevel(samplerInvT, float2(G.r, LOD), 0).r;
	color.g = invT.SampleLevel(samplerInvT, float2(G.g, LOD), 0).g;
	color.b = invT.SampleLevel(samplerInvT, float2(G.b, LOD), 0).b;
	color.a = invT.SampleLevel(samplerInvT, float2(G.a, LOD), 0).a;

	return color;
}
// ----------------------------------------------------------------------------



float4 TexCoords(VertexInput v)
{
    float4 texcoord;
    texcoord.xy = TRANSFORM_TEX(v.uv0, _MainTexT); // Always source from uv0
    texcoord.zw = TRANSFORM_TEX(((_UVSec == 0) ? v.uv0 : v.uv1), _DetailAlbedoMapT);
    return texcoord;
}

half DetailMask(float2 uv)
{
#if _STOCHASTIC_DETAILMASK
	return StochasticSample(uv, _DetailMaskT, sampler_DetailMaskT, _DetailMaskInvT, sampler_DetailMaskInvT).a;
#else
	return _DetailMaskT.Sample(sampler_DetailMaskT, uv).a;
#endif
}

half3 Albedo(float4 texcoords)
{
#if _STOCHASTIC_ALBEDO
	half3 albedo = _Color.rgb * DecorrelatedStochasticSample(texcoords.xy, _MainTexT, sampler_MainTexT, _MainTexInvT, sampler_MainTexInvT, _MainTexDXTScalers,
		_MainTexColorSpaceOrigin, _MainTexColorSpaceVector1, _MainTexColorSpaceVector2, _MainTexColorSpaceVector3).rgb;
#else
	half3 albedo = _Color.rgb * _MainTexT.Sample(sampler_MainTexT, texcoords.xy).rgb;
#endif
#if _DETAIL
    half mask = DetailMask(texcoords.xy);

	#if _STOCHASTIC_DETAILALBEDO
		half3 detailAlbedo = DecorrelatedStochasticSample(texcoords.zw, _DetailAlbedoMapT, sampler_DetailAlbedoMapT, _DetailAlbedoMapInvT, sampler_DetailAlbedoMapInvT, _DetailAlbedoMapDXTScalers,
			_DetailAlbedoColorSpaceOrigin, _DetailAlbedoColorSpaceVector1, _DetailAlbedoColorSpaceVector2, _DetailAlbedoColorSpaceVector3).rgb;
	#else
		half3 detailAlbedo = _DetailAlbedoMapT.Sample(sampler_DetailAlbedoMapT, texcoords.zw).rgb;
	#endif

    #if _DETAIL_MULX2
        albedo *= LerpWhiteTo (detailAlbedo * unity_ColorSpaceDouble.rgb, mask);
    #elif _DETAIL_MUL
        albedo *= LerpWhiteTo (detailAlbedo, mask);
    #elif _DETAIL_ADD
        albedo += detailAlbedo * mask;
    #elif _DETAIL_LERP
        albedo = lerp (albedo, detailAlbedo, mask);
    #endif
#endif
    return albedo.rgb;
}

half Alpha(float2 uv)
{
#if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
    return _Color.a;
#else
	#if _STOCHASTIC_ALBEDO
		return StochasticSample(uv, _MainTexT, sampler_MainTexT, _MainTexInvT, sampler_MainTexInvT).a * _Color.a;
	#else
		return _MainTexT.Sample(sampler_MainTexT, uv).a * _Color.a;
	#endif
#endif
}

half Occlusion(float2 uv)
{
#ifdef _STOCHASTIC_OCCLUSION
    half occ = StochasticSample(uv, _OcclusionMapT, sampler_OcclusionMapT, _OcclusionMapInvT, sampler_OcclusionMapInvT).g;
#else
	half occ = _OcclusionMapT.Sample(sampler_OcclusionMapT, uv).g;
#endif
    return LerpOneTo (occ, _OcclusionStrength);
}

half4 SpecularGloss(float2 uv)
{
    half4 sg;
#ifdef _SPECGLOSSMAP
	#if _STOCHASTIC_SPECMETAL
		half4 temp = StochasticSample(uv, _SpecGlossMapT, sampler_SpecGlossMapT, _SpecGlossMapInvT, sampler_SpecGlossMapInvT);
	#else
		half4 temp = _SpecGlossMapT.Sample(sampler_SpecGlossMapT, uv);
	#endif

    #if defined(_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A)
        sg.rgb = temp.rgb;
		#if _STOCHASTIC_ALBEDO
			sg.a = StochasticSample(uv, _MainTexT, sampler_MainTexT, _MainTexInvT, sampler_MainTexInvT).a;
		#else
			sg.a = _MainTexT.Sample(sampler_MainTexT, uv).a;
		#endif
    #else
        sg = temp;
    #endif
    sg.a *= _GlossMapScale;
#else
    sg.rgb = _SpecColor.rgb;
    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
		#if _STOCHASTIC_ALBEDO
			sg.a = StochasticSample(uv, _MainTexT, sampler_MainTexT, _MainTexInvT, sampler_MainTexInvT).a;
		#else
			sg.a = _MainTexT.Sample(sampler_MainTexT, uv).a;
		#endif
    #else
        sg.a = _Glossiness;
    #endif
#endif
    return sg;
}

half2 MetallicGloss(float2 uv)
{
    half2 mg;
#ifdef _METALLICGLOSSMAP
	#if _STOCHASTIC_SPECMETAL
		half4 temp = StochasticSample(uv, _MetallicGlossMapT, sampler_MetallicGlossMapT, _MetallicGlossMapInvT, sampler_MetallicGlossMapInvT);
	#else
		half4 temp = _MetallicGlossMapT.Sample(sampler_MetallicGlossMapT, uv);
	#endif

    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
        mg.r = temp.r;
		#if _STOCHASTIC_ALBEDO
			mg.g = StochasticSample(uv, _MainTexT, sampler_MainTexT, _MainTexInvT, sampler_MainTexInvT).a;
		#else
			mg.g = _MainTexT.Sample(sampler_MainTexT, uv).a;
		#endif
    #else
        mg = temp.ra;
    #endif
    mg.g *= _GlossMapScale;
#else
    mg.r = _Metallic;
    #ifdef _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
	#if _STOCHASTIC_ALBEDO
		mg.g = StochasticSample(uv, _MainTexT, sampler_MainTexT, _MainTexInvT, sampler_MainTexInvT).a * _GlossMapScale;
	#else
		mg.g = _MainTexT.Sample(sampler_MainTexT, uv).a * _GlossMapScale;
	#endif
    #else
        mg.g = _Glossiness;
    #endif
#endif
	return mg;
}

half2 MetallicRough(float2 uv)
{
    half2 mg;
#ifdef _METALLICGLOSSMAP
	#if _STOCHASTIC_SPECMETAL
		mg.r = StochasticSample(uv, _MetallicGlossMapT, sampler_MetallicGlossMapT, _MetallicGlossMapInvT, sampler_MetallicGlossMapInvT).r;
	#else
		mg.r = _MetallicGlossMapT.Sample(sampler_MetallicGlossMapT, uv).r;
	#endif
#else
    mg.r = _Metallic;
#endif

#ifdef _SPECGLOSSMAP
	#if _STOCHASTIC_SPECMETAL
		mg.g = 1.0f - StochasticSample(uv, _SpecGlossMapT, sampler_SpecGlossMapT, _SpecGlossMapInvT, sampler_SpecGlossMapInvT).r;
	#else
		mg.g = 1.0f - _SpecGlossMapT.Sample(sampler_SpecGlossMapT, uv).r;
	#endif
#else
    mg.g = 1.0f - _Glossiness;
#endif
    return mg;
}

half3 Emission(float2 uv)
{
#ifndef _EMISSION
    return 0;
#else
	#if _STOCHASTIC_EMISSION
		return DecorrelatedStochasticSample(uv, _EmissionMapT, sampler_EmissionMapT, _EmissionMapInvT, sampler_EmissionMapInvT, _EmissionMapDXTScalers,
			_EmissionColorSpaceOrigin, _EmissionColorSpaceVector1, _EmissionColorSpaceVector2, _EmissionColorSpaceVector3).rgb * _EmissionColor.rgb;
	#else
		return _EmissionMapT.Sample(sampler_EmissionMapT, uv).rgb * _EmissionColor.rgb;
	#endif
#endif
}

#ifdef _NORMALMAP
half3 NormalInTangentSpace(float4 texcoords)
{
#if _STOCHASTIC_NORMAL
	float3 noiseSample = DecorrelatedStochasticSample(texcoords.xy, _BumpMapT, sampler_BumpMapT, _BumpMapInvT, sampler_BumpMapInvT, _BumpMapDXTScalers,
		_BumpMapColorSpaceOrigin, _BumpMapColorSpaceVector1, _BumpMapColorSpaceVector2, _BumpMapColorSpaceVector3).rgb;
    half3 normalTangent = UnpackScaleNormal(float4(noiseSample, 1), _BumpScale);
#else
	half3 normalTangent = UnpackScaleNormal(_BumpMapT.Sample(sampler_BumpMapT, texcoords.xy), _BumpScale);
#endif

#if _DETAIL && defined(UNITY_ENABLE_DETAIL_NORMALMAP)
    half mask = DetailMask(texcoords.xy);

	#if _STOCHASTIC_DETAILNORMAL
		float3 detailNoiseSample = DecorrelatedStochasticSample(texcoords.zw, _DetailNormalMapT, sampler_DetailNormalMapT, _DetailNormalMapInvT, sampler_DetailNormalMapInvT, _DetailNormalMapDXTScalers,
			_DetailNormalColorSpaceOrigin, _DetailNormalColorSpaceVector1, _DetailNormalColorSpaceVector2, _DetailNormalColorSpaceVector3).rgb;
		half3 detailNormalTangent = UnpackScaleNormal(float4(detailNoiseSample, 1), _DetailNormalMapScale);
	#else
		half3 detailNormalTangent = UnpackScaleNormal(_DetailNormalMapT.Sample(sampler_DetailNormalMapT, texcoords.zw), _DetailNormalMapScale);
	#endif

    #if _DETAIL_LERP
        normalTangent = lerp(
            normalTangent,
            detailNormalTangent,
            mask);
    #else
        normalTangent = lerp(
            normalTangent,
            BlendNormals(normalTangent, detailNormalTangent),
            mask);
    #endif
#endif

    return normalTangent;
}
#endif

float4 Parallax (float4 texcoords, half3 viewDir)
{
#if !defined(_PARALLAXMAP)
	return texcoords;
#else
	#ifdef _STOCHASTIC_HEIGHT
		half h = StochasticSample(texcoords.xy, _ParallaxMapT, sampler_ParallaxMapT, _ParallaxMapInvT, sampler_ParallaxMapInvT).g;
	#else
		half h = _ParallaxMapT.Sample(sampler_ParallaxMapT, texcoords.xy).g;
	#endif

		float2 offset = ParallaxOffset1Step (h, _Parallax, viewDir);
		return float4(texcoords.xy + offset, texcoords.zw + offset);
#endif
}

#endif // UNITY_STANDARD_INPUT_INCLUDED
