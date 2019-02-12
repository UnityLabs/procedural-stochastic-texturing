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

Shader "StandardStochastic"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
        _GlossMapScale("Smoothness Scale", Range(0.0, 1.0)) = 1.0
        [Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0

        [Gamma] _Metallic("Metallic", Range(0.0, 1.0)) = 0.0
        _MetallicGlossMap("Metallic", 2D) = "white" {}

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _GlossyReflections("Glossy Reflections", Float) = 1.0

        _BumpScale("Scale", Float) = 1.0
        _BumpMap("Normal Map", 2D) = "bump" {}

        _Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
        _ParallaxMap ("Height Map", 2D) = "black" {}

        _OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}

        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}

        _DetailMask("Detail Mask", 2D) = "white" {}

        _DetailAlbedoMap("Detail Albedo x2", 2D) = "grey" {}
        _DetailNormalMapScale("Scale", Float) = 1.0
        _DetailNormalMap("Normal Map", 2D) = "bump" {}

        [Enum(UV0,0,UV1,1)] _UVSec ("UV Set for secondary textures", Float) = 0


		// ------------High Performance By-Example Noise Sampling----------------------
		_MainTexT("Albedo", 2D) = "white" {}
		_MetallicGlossMapT("Metallic", 2D) = "white" {}
		_ParallaxMapT("Height Map", 2D) = "black" {}
		_BumpMapT("Normal Map", 2D) = "bump" {}
		_OcclusionMapT("Occlusion", 2D) = "white" {}
		_EmissionMapT("Emission", 2D) = "white" {}
		_DetailMaskT("Detail Mask", 2D) = "white" {}
		_DetailAlbedoMapT("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapT("Normal Map", 2D) = "bump" {}

		_MainTexInvT("Albedo", 2D) = "white" {}
		_MetallicGlossMapInvT("Metallic", 2D) = "white" {}
		_ParallaxMapInvT("Height Map", 2D) = "black" {}
		_BumpMapInvT("Normal Map", 2D) = "bump" {}
		_OcclusionMapInvT("Occlusion", 2D) = "white" {}
		_EmissionMapInvT("Emission", 2D) = "white" {}
		_DetailMaskInvT("Detail Mask", 2D) = "white" {}
		_DetailAlbedoMapInvT("Detail Albedo x2", 2D) = "grey" {}
		_DetailNormalMapInvT("Normal Map", 2D) = "bump" {}

		// Only with DXT compression (Section 1.6)
		_MainTexDXTScalers("_MainTexDXTScalers", Vector) = (0,0,0,0)
		_DetailAlbedoMapDXTScalers("_DetailAlbedoMapDXTScalers", Vector) = (0,0,0,0)
		_BumpMapDXTScalers("_BumpMapDXTScalers", Vector) = (0,0,0,0)
		_DetailNormalMapDXTScalers("_DetailNormalMapDXTScalers", Vector) = (0,0,0,0)
		_EmissionMapDXTScalers("_EmissionMapDXTScalers", Vector) = (0,0,0,0)

		//Decorrelated color space vectors and origins, used on albedo and normal maps
		_MainTexColorSpaceOrigin("_MainTexColorSpaceOrigin", Vector) = (0,0,0,0)
		_MainTexColorSpaceVector1("_MainTexColorSpaceVector1", Vector) = (0,0,0,0)
		_MainTexColorSpaceVector2("_MainTexColorSpaceVector2", Vector) = (0,0,0,0)
		_MainTexColorSpaceVector3("_MainTexColorSpaceVector3", Vector) = (0,0,0,0)
		_DetailAlbedoColorSpaceOrigin("_DetailAlbedoColorSpaceOrigin", Vector) = (0,0,0,0)
		_DetailAlbedoColorSpaceVector1("_DetailAlbedoColorSpaceVector1", Vector) = (0,0,0,0)
		_DetailAlbedoColorSpaceVector2("_DetailAlbedoColorSpaceVector2", Vector) = (0,0,0,0)
		_DetailAlbedoColorSpaceVector3("_DetailAlbedoColorSpaceVector3", Vector) = (0,0,0,0)
		_BumpMapColorSpaceOrigin("_BumpMapColorSpaceOrigin", Vector) = (0,0,0,0)
		_BumpMapColorSpaceVector1("_BumpMapColorSpaceVector1", Vector) = (0,0,0,0)
		_BumpMapColorSpaceVector2("_BumpMapColorSpaceVector2", Vector) = (0,0,0,0)
		_BumpMapColorSpaceVector3("_BumpMapColorSpaceVector3", Vector) = (0,0,0,0)
		_DetailNormalColorSpaceOrigin("_DetailNormalColorSpaceOrigin", Vector) = (0,0,0,0)
		_DetailNormalColorSpaceVector1("_DetailNormalColorSpaceVector1", Vector) = (0,0,0,0)
		_DetailNormalColorSpaceVector2("_DetailNormalColorSpaceVector2", Vector) = (0,0,0,0)
		_DetailNormalColorSpaceVector3("_DetailNormalColorSpaceVector3", Vector) = (0,0,0,0)
		_EmissionColorSpaceOrigin("_EmissionColorSpaceOrigin", Vector) = (0,0,0,0)
		_EmissionColorSpaceVector1("_EmissionColorSpaceVector1", Vector) = (0,0,0,0)
		_EmissionColorSpaceVector2("_EmissionColorSpaceVector2", Vector) = (0,0,0,0)
		_EmissionColorSpaceVector3("_EmissionColorSpaceVector3", Vector) = (0,0,0,0)

		[HideInInspector] _StochasticInputSelected("_StochasticInputSelected", Int) = 0
		 // ----------------------------------------------------------------------------

        // Blending state
        [HideInInspector] _Mode ("__mode", Float) = 0.0
        [HideInInspector] _SrcBlend ("__src", Float) = 1.0
        [HideInInspector] _DstBlend ("__dst", Float) = 0.0
        [HideInInspector] _ZWrite ("__zw", Float) = 1.0
    }

    CGINCLUDE
        #define UNITY_SETUP_BRDF_INPUT MetallicSetup
    ENDCG

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 300


        // ------------------------------------------------------------------
        //  Base forward pass (directional light, emission, lightmaps, ...)
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------

            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature ___ _DETAIL_MULX2
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature _ _GLOSSYREFLECTIONS_OFF
            #pragma shader_feature _PARALLAXMAP

			#pragma shader_feature _STOCHASTIC_ALBEDO
			#pragma shader_feature _STOCHASTIC_NORMAL
			#pragma shader_feature _STOCHASTIC_HEIGHT
			#pragma shader_feature _STOCHASTIC_OCCLUSION
			#pragma shader_feature _STOCHASTIC_SPECMETAL
			#pragma shader_feature _STOCHASTIC_EMISSION
			#pragma shader_feature _STOCHASTIC_DETAILMASK
			#pragma shader_feature _STOCHASTIC_DETAILALBEDO
			#pragma shader_feature _STOCHASTIC_DETAILNORMAL

            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertBase
            #pragma fragment fragBase
            #include "UnityStandardStochasticCoreForward.cginc"

            ENDCG
        }
        // ------------------------------------------------------------------
        //  Additive forward pass (one light per pass)
        Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode" = "ForwardAdd" }
            Blend [_SrcBlend] One
            Fog { Color (0,0,0,0) } // in additive pass fog should be black
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------


            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature ___ _DETAIL_MULX2
            #pragma shader_feature _PARALLAXMAP

			#pragma shader_feature _STOCHASTIC_ALBEDO
			#pragma shader_feature _STOCHASTIC_NORMAL
			#pragma shader_feature _STOCHASTIC_HEIGHT
			#pragma shader_feature _STOCHASTIC_OCCLUSION
			#pragma shader_feature _STOCHASTIC_SPECMETAL
			#pragma shader_feature _STOCHASTIC_EMISSION
			#pragma shader_feature _STOCHASTIC_DETAILMASK
			#pragma shader_feature _STOCHASTIC_DETAILALBEDO
			#pragma shader_feature _STOCHASTIC_DETAILNORMAL

            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertAdd
            #pragma fragment fragAdd
            #include "UnityStandardStochasticCoreForward.cginc"

            ENDCG
        }
        // ------------------------------------------------------------------
        //  Shadow rendering pass
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma target 3.0

            // -------------------------------------


            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature _PARALLAXMAP
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster

            #include "UnityStandardShadow.cginc"

            ENDCG
        }
        // ------------------------------------------------------------------
        //  Deferred pass
        Pass
        {
            Name "DEFERRED"
            Tags { "LightMode" = "Deferred" }

            CGPROGRAM
            #pragma target 3.0
            #pragma exclude_renderers nomrt


            // -------------------------------------

            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature ___ _DETAIL_MULX2
            #pragma shader_feature _PARALLAXMAP

			#pragma shader_feature _STOCHASTIC_ALBEDO
			#pragma shader_feature _STOCHASTIC_NORMAL
			#pragma shader_feature _STOCHASTIC_HEIGHT
			#pragma shader_feature _STOCHASTIC_OCCLUSION
			#pragma shader_feature _STOCHASTIC_SPECMETAL
			#pragma shader_feature _STOCHASTIC_EMISSION
			#pragma shader_feature _STOCHASTIC_DETAILMASK
			#pragma shader_feature _STOCHASTIC_DETAILALBEDO
			#pragma shader_feature _STOCHASTIC_DETAILNORMAL

            #pragma multi_compile_prepassfinal
            #pragma multi_compile_instancing
            // Uncomment the following line to enable dithering LOD crossfade. Note: there are more in the file to uncomment for other passes.
            //#pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma vertex vertDeferred
            #pragma fragment fragDeferred

            #include "UnityStandardStochasticCore.cginc"

            ENDCG
        }

        // ------------------------------------------------------------------
        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags { "LightMode"="Meta" }

            Cull Off

            CGPROGRAM
            #pragma vertex vert_meta
            #pragma fragment frag_meta
			#pragma target 3.0

            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature ___ _DETAIL_MULX2
            #pragma shader_feature EDITOR_VISUALIZATION

			#pragma shader_feature _STOCHASTIC_ALBEDO
			#pragma shader_feature _STOCHASTIC_NORMAL
			#pragma shader_feature _STOCHASTIC_HEIGHT
			#pragma shader_feature _STOCHASTIC_OCCLUSION
			#pragma shader_feature _STOCHASTIC_SPECMETAL
			#pragma shader_feature _STOCHASTIC_EMISSION
			#pragma shader_feature _STOCHASTIC_DETAILMASK
			#pragma shader_feature _STOCHASTIC_DETAILALBEDO
			#pragma shader_feature _STOCHASTIC_DETAILNORMAL

            #include "UnityStandardStochasticMeta.cginc"
            ENDCG
        }
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
        LOD 150

        // ------------------------------------------------------------------
        //  Base forward pass (directional light, emission, lightmaps, ...)
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            CGPROGRAM
            #pragma target 3.0

            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature _ _GLOSSYREFLECTIONS_OFF
            // SM2.0: NOT SUPPORTED shader_feature ___ _DETAIL_MULX2
            // SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP

			#pragma shader_feature _STOCHASTIC_ALBEDO
			#pragma shader_feature _STOCHASTIC_NORMAL
			#pragma shader_feature _STOCHASTIC_HEIGHT
			#pragma shader_feature _STOCHASTIC_OCCLUSION
			#pragma shader_feature _STOCHASTIC_SPECMETAL
			#pragma shader_feature _STOCHASTIC_EMISSION
			#pragma shader_feature _STOCHASTIC_DETAILMASK
			#pragma shader_feature _STOCHASTIC_DETAILALBEDO
			#pragma shader_feature _STOCHASTIC_DETAILNORMAL

            #pragma skip_variants SHADOWS_SOFT DIRLIGHTMAP_COMBINED

            #pragma multi_compile_fwdbase
            #pragma multi_compile_fog

            #pragma vertex vertBase
            #pragma fragment fragBase
            #include "UnityStandardStochasticCoreForward.cginc"

            ENDCG
        }
        // ------------------------------------------------------------------
        //  Additive forward pass (one light per pass)
        Pass
        {
            Name "FORWARD_DELTA"
            Tags { "LightMode" = "ForwardAdd" }
            Blend [_SrcBlend] One
            Fog { Color (0,0,0,0) } // in additive pass fog should be black
            ZWrite Off
            ZTest LEqual

            CGPROGRAM
            #pragma target 3.0

            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature _ _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature ___ _DETAIL_MULX2
            // SM2.0: NOT SUPPORTED shader_feature _PARALLAXMAP
            #pragma skip_variants SHADOWS_SOFT

			#pragma shader_feature _STOCHASTIC_ALBEDO
			#pragma shader_feature _STOCHASTIC_NORMAL
			#pragma shader_feature _STOCHASTIC_HEIGHT
			#pragma shader_feature _STOCHASTIC_OCCLUSION
			#pragma shader_feature _STOCHASTIC_SPECMETAL
			#pragma shader_feature _STOCHASTIC_EMISSION
			#pragma shader_feature _STOCHASTIC_DETAILMASK
			#pragma shader_feature _STOCHASTIC_DETAILALBEDO
			#pragma shader_feature _STOCHASTIC_DETAILNORMAL

            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_fog

            #pragma vertex vertAdd
            #pragma fragment fragAdd
            #include "UnityStandardStochasticCoreForward.cginc"

            ENDCG
        }
        // ------------------------------------------------------------------
        //  Shadow rendering pass
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On ZTest LEqual

            CGPROGRAM
            #pragma target 3.0

            #pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma skip_variants SHADOWS_SOFT
            #pragma multi_compile_shadowcaster

            #pragma vertex vertShadowCaster
            #pragma fragment fragShadowCaster

            #include "UnityStandardShadow.cginc"

            ENDCG
        }

        // ------------------------------------------------------------------
        // Extracts information for lightmapping, GI (emission, albedo, ...)
        // This pass it not used during regular rendering.
        Pass
        {
            Name "META"
            Tags { "LightMode"="Meta" }

            Cull Off

            CGPROGRAM
            #pragma vertex vert_meta
            #pragma fragment frag_meta
			#pragma target 3.0

            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature ___ _DETAIL_MULX2
            #pragma shader_feature EDITOR_VISUALIZATION

			#pragma shader_feature _STOCHASTIC_ALBEDO
			#pragma shader_feature _STOCHASTIC_NORMAL
			#pragma shader_feature _STOCHASTIC_HEIGHT
			#pragma shader_feature _STOCHASTIC_OCCLUSION
			#pragma shader_feature _STOCHASTIC_SPECMETAL
			#pragma shader_feature _STOCHASTIC_EMISSION
			#pragma shader_feature _STOCHASTIC_DETAILMASK
			#pragma shader_feature _STOCHASTIC_DETAILALBEDO
			#pragma shader_feature _STOCHASTIC_DETAILNORMAL

            #include "UnityStandardStochasticMeta.cginc"
            ENDCG
        }
    }


    FallBack "VertexLit"
    CustomEditor "StandardStochasticShaderGUI"
}
