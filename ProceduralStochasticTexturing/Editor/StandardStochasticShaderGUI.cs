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

using System;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor
{
	internal class StandardStochasticShaderGUI : ShaderGUI
	{
		private enum WorkflowMode
		{
			Specular,
			Metallic,
			Dielectric
		}

		public enum BlendMode
		{
			Opaque,
			Cutout,
			Fade,   // Old school alpha-blending mode, fresnel does not affect amount of transparency
			Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
		}

		public enum SmoothnessMapChannel
		{
			SpecularMetallicAlpha,
			AlbedoAlpha,
		}

		private static class Styles
		{
			public static GUIContent uvSetLabel = EditorGUIUtility.TrTextContent("UV Set");

			public static GUIContent albedoText = EditorGUIUtility.TrTextContent("Albedo", "Albedo (RGB) and Transparency (A)");
			public static GUIContent alphaCutoffText = EditorGUIUtility.TrTextContent("Alpha Cutoff", "Threshold for alpha cutoff");
			public static GUIContent specularMapText = EditorGUIUtility.TrTextContent("Specular", "Specular (RGB) and Smoothness (A)");
			public static GUIContent metallicMapText = EditorGUIUtility.TrTextContent("Metallic", "Metallic (R) and Smoothness (A)");
			public static GUIContent smoothnessText = EditorGUIUtility.TrTextContent("Smoothness", "Smoothness value");
			public static GUIContent smoothnessScaleText = EditorGUIUtility.TrTextContent("Smoothness", "Smoothness scale factor");
			public static GUIContent smoothnessMapChannelText = EditorGUIUtility.TrTextContent("Source", "Smoothness texture and channel");
			public static GUIContent highlightsText = EditorGUIUtility.TrTextContent("Specular Highlights", "Specular Highlights");
			public static GUIContent reflectionsText = EditorGUIUtility.TrTextContent("Reflections", "Glossy Reflections");
			public static GUIContent normalMapText = EditorGUIUtility.TrTextContent("Normal Map", "Normal Map");
			public static GUIContent heightMapText = EditorGUIUtility.TrTextContent("Height Map", "Height Map (G)");
			public static GUIContent occlusionText = EditorGUIUtility.TrTextContent("Occlusion", "Occlusion (G)");
			public static GUIContent emissionText = EditorGUIUtility.TrTextContent("Color", "Emission (RGB)");
			public static GUIContent detailMaskText = EditorGUIUtility.TrTextContent("Detail Mask", "Mask for Secondary Maps (A)");
			public static GUIContent detailAlbedoText = EditorGUIUtility.TrTextContent("Detail Albedo x2", "Albedo (RGB) multiplied by 2");
			public static GUIContent detailNormalMapText = EditorGUIUtility.TrTextContent("Normal Map", "Normal Map");

			public static string primaryMapsText = "Main Maps";
			public static string secondaryMapsText = "Secondary Maps";
			public static string forwardText = "Forward Rendering Options";
			public static string renderingMode = "Rendering Mode";
			public static string advancedText = "Advanced Options";
			public static readonly string[] blendNames = Enum.GetNames(typeof(BlendMode));
		}

		MaterialProperty blendMode = null;
		MaterialProperty albedoMap = null;
		MaterialProperty albedoColor = null;
		MaterialProperty alphaCutoff = null;
		MaterialProperty specularMap = null;
		MaterialProperty specularColor = null;
		MaterialProperty metallicMap = null;
		MaterialProperty metallic = null;
		MaterialProperty smoothness = null;
		MaterialProperty smoothnessScale = null;
		MaterialProperty smoothnessMapChannel = null;
		MaterialProperty highlights = null;
		MaterialProperty reflections = null;
		MaterialProperty bumpScale = null;
		MaterialProperty bumpMap = null;
		MaterialProperty occlusionStrength = null;
		MaterialProperty occlusionMap = null;
		MaterialProperty heigtMapScale = null;
		MaterialProperty heightMap = null;
		MaterialProperty emissionColorForRendering = null;
		MaterialProperty emissionMap = null;
		MaterialProperty detailMask = null;
		MaterialProperty detailAlbedoMap = null;
		MaterialProperty detailNormalMapScale = null;
		MaterialProperty detailNormalMap = null;
		MaterialProperty uvSetSecondary = null;

		bool switchedThisFrame = false;
		Vector4 oldMainTexScaleOffset = Vector4.zero;
		Vector4 oldDetailTexScaleOffset = Vector4.zero;

		MaterialEditor m_MaterialEditor;
		WorkflowMode m_WorkflowMode = WorkflowMode.Specular;

		bool m_FirstTimeApply = true;

		public void FindProperties(MaterialProperty[] props)
		{
			blendMode = FindProperty("_Mode", props);
			albedoMap = FindProperty("_MainTex", props);
			albedoColor = FindProperty("_Color", props);
			alphaCutoff = FindProperty("_Cutoff", props);
			specularMap = FindProperty("_SpecGlossMap", props, false);
			specularColor = FindProperty("_SpecColor", props, false);
			metallicMap = FindProperty("_MetallicGlossMap", props, false);
			metallic = FindProperty("_Metallic", props, false);
			if (specularMap != null && specularColor != null)
				m_WorkflowMode = WorkflowMode.Specular;
			else if (metallicMap != null && metallic != null)
				m_WorkflowMode = WorkflowMode.Metallic;
			else
				m_WorkflowMode = WorkflowMode.Dielectric;
			smoothness = FindProperty("_Glossiness", props);
			smoothnessScale = FindProperty("_GlossMapScale", props, false);
			smoothnessMapChannel = FindProperty("_SmoothnessTextureChannel", props, false);
			highlights = FindProperty("_SpecularHighlights", props, false);
			reflections = FindProperty("_GlossyReflections", props, false);
			bumpScale = FindProperty("_BumpScale", props);
			bumpMap = FindProperty("_BumpMap", props);
			heigtMapScale = FindProperty("_Parallax", props);
			heightMap = FindProperty("_ParallaxMap", props);
			occlusionStrength = FindProperty("_OcclusionStrength", props);
			occlusionMap = FindProperty("_OcclusionMap", props);
			emissionColorForRendering = FindProperty("_EmissionColor", props);
			emissionMap = FindProperty("_EmissionMap", props);
			detailMask = FindProperty("_DetailMask", props);
			detailAlbedoMap = FindProperty("_DetailAlbedoMap", props);
			detailNormalMapScale = FindProperty("_DetailNormalMapScale", props);
			detailNormalMap = FindProperty("_DetailNormalMap", props);
			uvSetSecondary = FindProperty("_UVSec", props);

			// -------------Procedural Stochastic Texturing Properties-----------------
			albedoMapT = FindProperty("_MainTexT", props);
			specularMapT = FindProperty("_SpecGlossMapT", props, false);
			metallicMapT = FindProperty("_MetallicGlossMapT", props, false);
			bumpMapT = FindProperty("_BumpMapT", props);
			occlusionMapT = FindProperty("_OcclusionMapT", props);
			heightMapT = FindProperty("_ParallaxMapT", props);
			emissionMapT = FindProperty("_EmissionMapT", props);
			detailAlbedoMapT = FindProperty("_DetailAlbedoMapT", props);
			detailMaskT = FindProperty("_DetailMaskT", props);
			detailNormalMapT = FindProperty("_DetailNormalMapT", props);

			albedoMapInvT = FindProperty("_MainTexInvT", props);
			specularMapInvT = FindProperty("_SpecGlossMapInvT", props, false);
			metallicMapInvT = FindProperty("_MetallicGlossMapInvT", props, false);
			bumpMapInvT = FindProperty("_BumpMapInvT", props);
			occlusionMapInvT = FindProperty("_OcclusionMapInvT", props);
			heightMapInvT = FindProperty("_ParallaxMapInvT", props);
			emissionMapInvT = FindProperty("_EmissionMapInvT", props);
			detailAlbedoMapInvT = FindProperty("_DetailAlbedoMapInvT", props);
			detailMaskInvT = FindProperty("_DetailMaskInvT", props);
			detailNormalMapInvT = FindProperty("_DetailNormalMapInvT", props);

			mainTexDXTScalers = FindProperty("_MainTexDXTScalers", props);
			detailAlbedoMapDXTScalers = FindProperty("_DetailAlbedoMapDXTScalers", props);
			bumpMapDXTScalers = FindProperty("_BumpMapDXTScalers", props);
			detailNormalMapDXTScalers = FindProperty("_DetailNormalMapDXTScalers", props);
			emissionMapDXTScalers = FindProperty("_EmissionMapDXTScalers", props);
			
			mainTexColorSpaceOrigin = FindProperty("_MainTexColorSpaceOrigin", props);
			mainTexColorSpaceVector1 = FindProperty("_MainTexColorSpaceVector1", props);
			mainTexColorSpaceVector2 = FindProperty("_MainTexColorSpaceVector2", props);
			mainTexColorSpaceVector3 = FindProperty("_MainTexColorSpaceVector3", props);
			detailAlbedoColorSpaceOrigin = FindProperty("_DetailAlbedoColorSpaceOrigin", props);
			detailAlbedoColorSpaceVector1 = FindProperty("_DetailAlbedoColorSpaceVector1", props);
			detailAlbedoColorSpaceVector2 = FindProperty("_DetailAlbedoColorSpaceVector2", props);
			detailAlbedoColorSpaceVector3 = FindProperty("_DetailAlbedoColorSpaceVector3", props);
			bumpMapColorSpaceOrigin = FindProperty("_BumpMapColorSpaceOrigin", props);
			bumpMapColorSpaceVector1 = FindProperty("_BumpMapColorSpaceVector1", props);
			bumpMapColorSpaceVector2 = FindProperty("_BumpMapColorSpaceVector2", props);
			bumpMapColorSpaceVector3 = FindProperty("_BumpMapColorSpaceVector3", props);
			detailNormalColorSpaceOrigin = FindProperty("_DetailNormalColorSpaceOrigin", props);
			detailNormalColorSpaceVector1 = FindProperty("_DetailNormalColorSpaceVector1", props);
			detailNormalColorSpaceVector2 = FindProperty("_DetailNormalColorSpaceVector2", props);
			detailNormalColorSpaceVector3 = FindProperty("_DetailNormalColorSpaceVector3", props);
			emissionColorSpaceOrigin = FindProperty("_EmissionColorSpaceOrigin", props);
			emissionColorSpaceVector1 = FindProperty("_EmissionColorSpaceVector1", props);
			emissionColorSpaceVector2 = FindProperty("_EmissionColorSpaceVector2", props);
			emissionColorSpaceVector3 = FindProperty("_EmissionColorSpaceVector3", props);

			layerMask = FindProperty("_StochasticInputSelected", props);
			// ------------------------------------------------------------------------
		}

		public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
		{
			FindProperties(props); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
			m_MaterialEditor = materialEditor;
			Material material = materialEditor.target as Material;

			// Make sure that needed setup (ie keywords/renderqueue) are set up if we're switching some existing
			// material to a standard shader.
			// Do this before any GUI code has been issued to prevent layout issues in subsequent GUILayout statements (case 780071)
			if (m_FirstTimeApply)
			{
				MaterialChanged(material, m_WorkflowMode);
				m_FirstTimeApply = false;
			}

			// ----------Procedural Stochastic Texturing Interface------------------
			EditorGUI.BeginChangeCheck();
			layerMask.floatValue = (int)EditorGUILayout.MaskField("Stochastic Inputs", (int)(layerMask.floatValue), layers);

			// Display pre-process time warning if large texture present
			if (CheckIfPreprocessWillBeLong() == true)
				EditorGUILayout.HelpBox("Input textures larger than 1024² may take a while to pre-process.", MessageType.Info);

			if (GUILayout.Button("Apply"))
				ApplyUserStochasticInputChoice(material);

			GUILayout.Space(10);

			if (switchedThisFrame == true)
			{
				albedoMapT.textureScaleAndOffset = oldMainTexScaleOffset;
				detailAlbedoMapT.textureScaleAndOffset = oldDetailTexScaleOffset;
				if (layerMask.floatValue == 0)
					ApplyUserStochasticInputChoice(material);
				switchedThisFrame = false;
			}
			// ---------------------------------------------------------------------

			ShaderPropertiesGUI(material);
		}

		public void ShaderPropertiesGUI(Material material)
		{
			// Use default labelWidth
			EditorGUIUtility.labelWidth = 0f;

			// Detect any changes to the material
			EditorGUI.BeginChangeCheck();
			{
				BlendModePopup();

				// Primary properties
				GUILayout.Label(Styles.primaryMapsText, EditorStyles.boldLabel);
				DoAlbedoArea(material);
				DoSpecularMetallicArea();
				DoNormalArea();
				m_MaterialEditor.TexturePropertySingleLine(Styles.heightMapText, heightMap, heightMap.textureValue != null ? heigtMapScale : null);
				m_MaterialEditor.TexturePropertySingleLine(Styles.occlusionText, occlusionMap, occlusionMap.textureValue != null ? occlusionStrength : null);
				m_MaterialEditor.TexturePropertySingleLine(Styles.detailMaskText, detailMask);
				DoEmissionArea(material);
				EditorGUI.BeginChangeCheck();
				m_MaterialEditor.TextureScaleOffsetProperty(albedoMapT);
				albedoMap.textureScaleAndOffset = albedoMapT.textureScaleAndOffset;
				if (EditorGUI.EndChangeCheck())
					emissionMap.textureScaleAndOffset = albedoMapT.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake

				EditorGUILayout.Space();

				// Secondary properties
				GUILayout.Label(Styles.secondaryMapsText, EditorStyles.boldLabel);
				m_MaterialEditor.TexturePropertySingleLine(Styles.detailAlbedoText, detailAlbedoMap);
				m_MaterialEditor.TexturePropertySingleLine(Styles.detailNormalMapText, detailNormalMap, detailNormalMapScale);
				m_MaterialEditor.TextureScaleOffsetProperty(detailAlbedoMapT);
				detailAlbedoMap.textureScaleAndOffset = detailAlbedoMapT.textureScaleAndOffset;
				m_MaterialEditor.ShaderProperty(uvSetSecondary, Styles.uvSetLabel.text);

				// Third properties
				GUILayout.Label(Styles.forwardText, EditorStyles.boldLabel);
				if (highlights != null)
					m_MaterialEditor.ShaderProperty(highlights, Styles.highlightsText);
				if (reflections != null)
					m_MaterialEditor.ShaderProperty(reflections, Styles.reflectionsText);
			}
			if (EditorGUI.EndChangeCheck())
			{
				foreach (var obj in blendMode.targets)
					MaterialChanged((Material)obj, m_WorkflowMode);
			}

			EditorGUILayout.Space();

			// NB renderqueue editor is not shown on purpose: we want to override it based on blend mode
			GUILayout.Label(Styles.advancedText, EditorStyles.boldLabel);
			m_MaterialEditor.EnableInstancingField();
			m_MaterialEditor.DoubleSidedGIField();
		}

		internal void DetermineWorkflow(MaterialProperty[] props)
		{
			if (FindProperty("_SpecGlossMap", props, false) != null && FindProperty("_SpecColor", props, false) != null)
				m_WorkflowMode = WorkflowMode.Specular;
			else if (FindProperty("_MetallicGlossMap", props, false) != null && FindProperty("_Metallic", props, false) != null)
				m_WorkflowMode = WorkflowMode.Metallic;
			else
				m_WorkflowMode = WorkflowMode.Dielectric;
		}

		public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
		{
			Vector2 scale = material.GetTextureScale("_MainTex");
			Vector2 offset = material.GetTextureOffset("_MainTex");
			oldMainTexScaleOffset = new Vector4(scale.x, scale.y, offset.x, offset.y);

			scale = material.GetTextureScale("_DetailAlbedoMap");
			offset = material.GetTextureOffset("_DetailAlbedoMap");
			oldDetailTexScaleOffset = new Vector4(scale.x, scale.y, offset.x, offset.y);
			switchedThisFrame = true;

			// _Emission property is lost after assigning Standard shader to the material
			// thus transfer it before assigning the new shader
			if (material.HasProperty("_Emission"))
			{
				material.SetColor("_EmissionColor", material.GetColor("_Emission"));
			}

			base.AssignNewShaderToMaterial(material, oldShader, newShader);

			if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
			{
				SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));
				return;
			}

			BlendMode blendMode = BlendMode.Opaque;
			if (oldShader.name.Contains("/Transparent/Cutout/"))
			{
				blendMode = BlendMode.Cutout;
			}
			else if (oldShader.name.Contains("/Transparent/"))
			{
				// NOTE: legacy shaders did not provide physically based transparency
				// therefore Fade mode
				blendMode = BlendMode.Fade;
			}
			material.SetFloat("_Mode", (float)blendMode);

			DetermineWorkflow(MaterialEditor.GetMaterialProperties(new Material[] { material }));
			MaterialChanged(material, m_WorkflowMode);
		}

		void BlendModePopup()
		{
			EditorGUI.showMixedValue = blendMode.hasMixedValue;
			var mode = (BlendMode)blendMode.floatValue;

			EditorGUI.BeginChangeCheck();
			mode = (BlendMode)EditorGUILayout.Popup(Styles.renderingMode, (int)mode, Styles.blendNames);
			if (EditorGUI.EndChangeCheck())
			{
				m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
				blendMode.floatValue = (float)mode;
			}

			EditorGUI.showMixedValue = false;
		}

		void DoNormalArea()
		{
			m_MaterialEditor.TexturePropertySingleLine(Styles.normalMapText, bumpMap, bumpMap.textureValue != null ? bumpScale : null);
			if (bumpScale.floatValue != 1 && UnityEditorInternal.InternalEditorUtility.IsMobilePlatform(EditorUserBuildSettings.activeBuildTarget))
				if (m_MaterialEditor.HelpBoxWithButton(
					EditorGUIUtility.TrTextContent("Bump scale is not supported on mobile platforms"),
					EditorGUIUtility.TrTextContent("Fix Now")))
				{
					bumpScale.floatValue = 1;
				}
		}

		void DoAlbedoArea(Material material)
		{
			m_MaterialEditor.TexturePropertySingleLine(Styles.albedoText, albedoMap, albedoColor);
			if (((BlendMode)material.GetFloat("_Mode") == BlendMode.Cutout))
			{
				m_MaterialEditor.ShaderProperty(alphaCutoff, Styles.alphaCutoffText.text, MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1);
			}
		}

		void DoEmissionArea(Material material)
		{
			// Emission for GI?
			if (m_MaterialEditor.EmissionEnabledProperty())
			{
				bool hadEmissionTexture = emissionMap.textureValue != null;

				// Texture and HDR color controls
				m_MaterialEditor.TexturePropertyWithHDRColor(Styles.emissionText, emissionMap, emissionColorForRendering, false);

				// If texture was assigned and color was black set color to white
				float brightness = emissionColorForRendering.colorValue.maxColorComponent;
				if (emissionMap.textureValue != null && !hadEmissionTexture && brightness <= 0f)
					emissionColorForRendering.colorValue = Color.white;

				// change the GI flag and fix it up with emissive as black if necessary
				m_MaterialEditor.LightmapEmissionFlagsProperty(MaterialEditor.kMiniTextureFieldLabelIndentLevel, true);
			}
		}

		void DoSpecularMetallicArea()
		{
			bool hasGlossMap = false;
			if (m_WorkflowMode == WorkflowMode.Specular)
			{
				hasGlossMap = specularMap.textureValue != null;
				m_MaterialEditor.TexturePropertySingleLine(Styles.specularMapText, specularMap, hasGlossMap ? null : specularColor);
			}
			else if (m_WorkflowMode == WorkflowMode.Metallic)
			{
				hasGlossMap = metallicMap.textureValue != null;
				m_MaterialEditor.TexturePropertySingleLine(Styles.metallicMapText, metallicMap, hasGlossMap ? null : metallic);
			}

			bool showSmoothnessScale = hasGlossMap;
			if (smoothnessMapChannel != null)
			{
				int smoothnessChannel = (int)smoothnessMapChannel.floatValue;
				if (smoothnessChannel == (int)SmoothnessMapChannel.AlbedoAlpha)
					showSmoothnessScale = true;
			}

			int indentation = 2; // align with labels of texture properties
			m_MaterialEditor.ShaderProperty(showSmoothnessScale ? smoothnessScale : smoothness, showSmoothnessScale ? Styles.smoothnessScaleText : Styles.smoothnessText, indentation);

			++indentation;
			if (smoothnessMapChannel != null)
				m_MaterialEditor.ShaderProperty(smoothnessMapChannel, Styles.smoothnessMapChannelText, indentation);
		}

		public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
		{
			switch (blendMode)
			{
				case BlendMode.Opaque:
					material.SetOverrideTag("RenderType", "");
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					material.SetInt("_ZWrite", 1);
					material.DisableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = -1;
					break;
				case BlendMode.Cutout:
					material.SetOverrideTag("RenderType", "TransparentCutout");
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
					material.SetInt("_ZWrite", 1);
					material.EnableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
					break;
				case BlendMode.Fade:
					material.SetOverrideTag("RenderType", "Transparent");
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					material.SetInt("_ZWrite", 0);
					material.DisableKeyword("_ALPHATEST_ON");
					material.EnableKeyword("_ALPHABLEND_ON");
					material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
					break;
				case BlendMode.Transparent:
					material.SetOverrideTag("RenderType", "Transparent");
					material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
					material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
					material.SetInt("_ZWrite", 0);
					material.DisableKeyword("_ALPHATEST_ON");
					material.DisableKeyword("_ALPHABLEND_ON");
					material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
					material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
					break;
			}
		}

		static SmoothnessMapChannel GetSmoothnessMapChannel(Material material)
		{
			int ch = (int)material.GetFloat("_SmoothnessTextureChannel");
			if (ch == (int)SmoothnessMapChannel.AlbedoAlpha)
				return SmoothnessMapChannel.AlbedoAlpha;
			else
				return SmoothnessMapChannel.SpecularMetallicAlpha;
		}

		static void SetMaterialKeywords(Material material, WorkflowMode workflowMode)
		{
			// Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
			// (MaterialProperty value might come from renderer material property block)
			SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap") || material.GetTexture("_DetailNormalMap"));
			if (workflowMode == WorkflowMode.Specular)
				SetKeyword(material, "_SPECGLOSSMAP", material.GetTexture("_SpecGlossMap"));
			else if (workflowMode == WorkflowMode.Metallic)
				SetKeyword(material, "_METALLICGLOSSMAP", material.GetTexture("_MetallicGlossMap"));
			SetKeyword(material, "_PARALLAXMAP", material.GetTexture("_ParallaxMap"));
			SetKeyword(material, "_DETAIL_MULX2", material.GetTexture("_DetailAlbedoMap") || material.GetTexture("_DetailNormalMap"));

			// A material's GI flag internally keeps track of whether emission is enabled at all, it's enabled but has no effect
			// or is enabled and may be modified at runtime. This state depends on the values of the current flag and emissive color.
			// The fixup routine makes sure that the material is in the correct state if/when changes are made to the mode or color.
			MaterialEditor.FixupEmissiveFlag(material);
			bool shouldEmissionBeEnabled = (material.globalIlluminationFlags & MaterialGlobalIlluminationFlags.EmissiveIsBlack) == 0;
			SetKeyword(material, "_EMISSION", shouldEmissionBeEnabled);

			if (material.HasProperty("_SmoothnessTextureChannel"))
			{
				SetKeyword(material, "_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A", GetSmoothnessMapChannel(material) == SmoothnessMapChannel.AlbedoAlpha);
			}
		}

		static void MaterialChanged(Material material, WorkflowMode workflowMode)
		{
			SetupMaterialWithBlendMode(material, (BlendMode)material.GetFloat("_Mode"));

			SetMaterialKeywords(material, workflowMode);
		}

		static void SetKeyword(Material m, string keyword, bool state)
		{
			if (state)
				m.EnableKeyword(keyword);
			else
				m.DisableKeyword(keyword);
		}

		private bool CheckIfPreprocessWillBeLong()
		{
			if (InputIsSelected(0) && albedoMap != null && albedoMap.textureValue != null && albedoMap.textureValue.width * albedoMap.textureValue.height > 1048576
			|| InputIsSelected(1) && metallicMap != null && metallicMap.textureValue != null && metallicMap.textureValue.width * metallicMap.textureValue.height > 1048576
			|| InputIsSelected(1) && specularMap != null && specularMap.textureValue != null && specularMap.textureValue.width * specularMap.textureValue.height > 1048576
			|| InputIsSelected(2) && bumpMap != null && bumpMap.textureValue != null && bumpMap.textureValue.width * bumpMap.textureValue.height > 1048576
			|| InputIsSelected(3) && heightMap != null && heightMap.textureValue != null && heightMap.textureValue.width * heightMap.textureValue.height > 1048576
			|| InputIsSelected(4) && occlusionMap != null && occlusionMap.textureValue != null && occlusionMap.textureValue.width * occlusionMap.textureValue.height > 1048576
			|| InputIsSelected(5) && emissionMap != null && emissionMap.textureValue != null && emissionMap.textureValue.width * emissionMap.textureValue.height > 1048576
			|| InputIsSelected(6) && detailMask != null && detailMask.textureValue != null && detailMask.textureValue.width * detailMask.textureValue.height > 1048576
			|| InputIsSelected(7) && detailAlbedoMap != null && detailAlbedoMap.textureValue != null && detailAlbedoMap.textureValue.width * detailAlbedoMap.textureValue.height > 1048576
			|| InputIsSelected(8) && detailNormalMap != null && detailNormalMap.textureValue != null && detailNormalMap.textureValue.width * detailNormalMap.textureValue.height > 1048576)
				return true;
			else
				return false;
		}



		/*********************************************************************/
		/*********************************************************************/
		/*************Procedural Stochastic Texturing Pre-process*************/
		/*********************************************************************/
		/*********************************************************************/
		const float GAUSSIAN_AVERAGE = 0.5f;	// Expectation of the Gaussian distribution
		const float GAUSSIAN_STD = 0.16666f;	// Std of the Gaussian distribution
		const int LUT_WIDTH = 128;				// Size of the look-up table

		struct TextureData
		{
			public Color[] data;
			public int width;
			public int height;

			public TextureData(int w, int h)
			{
				width = w;
				height = h;
				data = new Color[w * h];
			}
			public TextureData(TextureData td)
			{
				width = td.width;
				height = td.height;
				data = new Color[width * height];
				for (int y = 0; y < height; y++)
					for (int x = 0; x < width; x++)
						data[y * width + x] = td.data[y * width + x];
			}

			public Color GetColor(int w, int h)
			{
				return data[h * width + w];
			}
			public void SetColor(int w, int h, Color value)
			{
				data[h * width + w] = value;
			}
			public void SetColor(int w, int h, int channel, float value)
			{
				data[h * width + w][channel] = value;
			}
		};

		private void ApplyUserStochasticInputChoice(Material material)
		{
			Vector3 colorSpaceVector1 = new Vector3();
			Vector3 colorSpaceVector2 = new Vector3();
			Vector3 colorSpaceVector3 = new Vector3();
			Vector3 colorSpaceOrigin = new Vector3();
			Vector3 dxtScalers = new Vector3();
			Texture2D texT = new Texture2D(1, 1);
			Texture2D texInvT = new Texture2D(1, 1);
			TextureFormat inputFormat = TextureFormat.RGB24;

			#region ALBEDO MAP
			if (InputIsSelected(0) && albedoMap.textureValue != null)
			{
				int stepCounter = 0;
				int totalSteps = 14;
				string inputName = "Albedo Map";
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

				// Section 1.4 Improvement: using a decorrelated color space for Albedo RGB
				TextureData albedoData = TextureToTextureData((Texture2D)albedoMap.textureValue, ref inputFormat);
				TextureData decorrelated = new TextureData(albedoData);
				DecorrelateColorSpace(ref albedoData, ref decorrelated, ref colorSpaceVector1, ref colorSpaceVector2, ref colorSpaceVector3, ref colorSpaceOrigin);
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
				ComputeDXTCompressionScalers((Texture2D)albedoMap.textureValue, ref dxtScalers, colorSpaceVector1, colorSpaceVector2, colorSpaceVector3);

				// Perform precomputations if precomputed textures don't already exist
				if (LoadPrecomputedTexturesIfExist((Texture2D)albedoMap.textureValue, ref texT, ref texInvT) == false)
				{
					TextureData Tinput = new TextureData(decorrelated.width, decorrelated.height);
					TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
					Precomputations(ref decorrelated, new List<int> { 0, 1, 2, 3 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);
					EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
					RescaleForDXTCompression(ref Tinput, ref dxtScalers);
					EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);

					// Serialize precomputed data and setup material
					SerializePrecomputedTextures((Texture2D)albedoMap.textureValue, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
				}
				EditorUtility.ClearProgressBar();

				// Apply to shader properties
				albedoMapT.textureValue = texT;
				albedoMapInvT.textureValue = texInvT;
				mainTexColorSpaceOrigin.vectorValue = colorSpaceOrigin;
				mainTexColorSpaceVector1.vectorValue = colorSpaceVector1;
				mainTexColorSpaceVector2.vectorValue = colorSpaceVector2;
				mainTexColorSpaceVector3.vectorValue = colorSpaceVector3;
				mainTexDXTScalers.vectorValue = dxtScalers;
				material.EnableKeyword("_STOCHASTIC_ALBEDO");
			}
			else
			{
				albedoMapT.textureValue = albedoMap.textureValue;
				material.DisableKeyword("_STOCHASTIC_ALBEDO");
			}
			#endregion

			#region METALLIC MAP
			if (m_WorkflowMode == WorkflowMode.Metallic)
			{
				if (InputIsSelected(1) && metallicMap.textureValue != null)
				{
					int stepCounter = 0;
					int totalSteps = 6;
					string inputName = "Metallic + Smoothness Map";
					EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

					// Perform precomputations if precomputed textures don't already exist
					if (LoadPrecomputedTexturesIfExist((Texture2D)metallicMap.textureValue, ref texT, ref texInvT) == false)
					{
						TextureData metallicData = TextureToTextureData((Texture2D)metallicMap.textureValue, ref inputFormat);

						TextureData Tinput = new TextureData(metallicData.width, metallicData.height);
						TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
						Precomputations(ref metallicData, new List<int> { 0, 3 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);

						// Serialize precomputed data and setup material
						SerializePrecomputedTextures((Texture2D)metallicMap.textureValue, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
					}
					EditorUtility.ClearProgressBar();

					// Apply to shader properties
					metallicMapT.textureValue = texT;
					metallicMapInvT.textureValue = texInvT;
					material.EnableKeyword("_STOCHASTIC_SPECMETAL");
				}
				else
				{
					metallicMapT.textureValue = metallicMap.textureValue;
					material.DisableKeyword("_STOCHASTIC_SPECMETAL");
				}
			}

			#endregion

			#region SPECULAR MAP
			if (m_WorkflowMode == WorkflowMode.Specular)
			{
				if (InputIsSelected(1) && specularMap.textureValue != null)
				{
					int stepCounter = 0;
					int totalSteps = 12;
					string inputName = "Specular + Smoothness Map";
					EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

					// Perform precomputations if precomputed textures don't already exist
					if (LoadPrecomputedTexturesIfExist((Texture2D)specularMap.textureValue, ref texT, ref texInvT) == false)
					{
						TextureData specularData = TextureToTextureData((Texture2D)specularMap.textureValue, ref inputFormat);

						TextureData Tinput = new TextureData(specularData.width, specularData.height);
						TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
						Precomputations(ref specularData, new List<int> { 0, 1, 2, 3 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);

						// Serialize precomputed data and setup material
						SerializePrecomputedTextures((Texture2D)specularMap.textureValue, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
					}
					EditorUtility.ClearProgressBar();

					// Apply to shader properties
					specularMapT.textureValue = texT;
					specularMapInvT.textureValue = texInvT;
					material.EnableKeyword("_STOCHASTIC_SPECMETAL");
				}
				else
				{
					specularMapT.textureValue = specularMap.textureValue;
					material.DisableKeyword("_STOCHASTIC_SPECMETAL");
				}
			}
			#endregion

			#region NORMAL MAP
			if (InputIsSelected(2) && bumpMap.textureValue != null)
			{
				int stepCounter = 0;
				int totalSteps = 11;
				string inputName = "Normal Map";
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

				// Section 1.4 Improvement: using a decorrelated color space for Albedo RGB
				TextureData normalData = TextureToTextureData((Texture2D)bumpMap.textureValue, ref inputFormat);
				TextureData decorrelated = new TextureData(normalData);
				DecorrelateColorSpace(ref normalData, ref decorrelated, ref colorSpaceVector1, ref colorSpaceVector2, ref colorSpaceVector3, ref colorSpaceOrigin);
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
				ComputeDXTCompressionScalers((Texture2D)bumpMap.textureValue, ref dxtScalers, colorSpaceVector1, colorSpaceVector2, colorSpaceVector3);

				// Perform precomputations if precomputed textures don't already exist
				if (LoadPrecomputedTexturesIfExist((Texture2D)bumpMap.textureValue, ref texT, ref texInvT) == false)
				{
					TextureData Tinput = new TextureData(decorrelated.width, decorrelated.height);
					TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
					Precomputations(ref decorrelated, new List<int> { 0, 1, 2 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);
					EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
					RescaleForDXTCompression(ref Tinput, ref dxtScalers);
					EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);

					// Serialize precomputed data and setup material
					SerializePrecomputedTextures((Texture2D)bumpMap.textureValue, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
				}
				EditorUtility.ClearProgressBar();

				// Apply to shader properties
				bumpMapT.textureValue = texT;
				bumpMapInvT.textureValue = texInvT;
				bumpMapColorSpaceOrigin.vectorValue = colorSpaceOrigin;
				bumpMapColorSpaceVector1.vectorValue = colorSpaceVector1;
				bumpMapColorSpaceVector2.vectorValue = colorSpaceVector2;
				bumpMapColorSpaceVector3.vectorValue = colorSpaceVector3;
				bumpMapDXTScalers.vectorValue = dxtScalers;
				material.EnableKeyword("_STOCHASTIC_NORMAL");
			}
			else
			{
				bumpMapT.textureValue = bumpMap.textureValue;
				material.DisableKeyword("_STOCHASTIC_NORMAL");
			}
			#endregion

			#region HEIGHT MAP
			if (InputIsSelected(3) && heightMap.textureValue != null)
			{
				int stepCounter = 0;
				int totalSteps = 3;
				string inputName = "Height Map";
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

				// Perform precomputations if precomputed textures don't already exist
				if (LoadPrecomputedTexturesIfExist((Texture2D)heightMap.textureValue, ref texT, ref texInvT) == false)
				{
					TextureData heightData = TextureToTextureData((Texture2D)heightMap.textureValue, ref inputFormat);

					TextureData Tinput = new TextureData(heightData.width, heightData.height);
					TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
					Precomputations(ref heightData, new List<int> { 1 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);

					// Serialize precomputed data and setup material
					SerializePrecomputedTextures((Texture2D)heightMap.textureValue, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
				}
				EditorUtility.ClearProgressBar();

				// Apply to shader properties
				heightMapT.textureValue = texT;
				heightMapInvT.textureValue = texInvT;
				material.EnableKeyword("_STOCHASTIC_HEIGHT");
			}
			else
			{
				heightMapT.textureValue = heightMap.textureValue;
				material.DisableKeyword("_STOCHASTIC_HEIGHT");
			}
			#endregion

			#region OCCLUSION MAP
			if (InputIsSelected(4) && occlusionMap.textureValue != null)
			{
				int stepCounter = 0;
				int totalSteps = 3;
				string inputName = "Occlusion Map";
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

				// Perform precomputations if precomputed textures don't already exist
				if (LoadPrecomputedTexturesIfExist((Texture2D)occlusionMap.textureValue, ref texT, ref texInvT) == false)
				{
					TextureData occlusionData = TextureToTextureData((Texture2D)occlusionMap.textureValue, ref inputFormat);

					TextureData Tinput = new TextureData(occlusionData.width, occlusionData.height);
					TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
					Precomputations(ref occlusionData, new List<int> { 1 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);

					// Serialize precomputed data and setup material
					SerializePrecomputedTextures((Texture2D)occlusionMap.textureValue, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
				}
				EditorUtility.ClearProgressBar();

				// Apply to shader properties
				occlusionMapT.textureValue = texT;
				occlusionMapInvT.textureValue = texInvT;
				material.EnableKeyword("_STOCHASTIC_OCCLUSION");
			}
			else
			{
				occlusionMapT.textureValue = occlusionMap.textureValue;
				material.DisableKeyword("_STOCHASTIC_OCCLUSION");
			}
			#endregion

			#region EMISSION MAP
			if (InputIsSelected(5) && emissionMap.textureValue != null)
			{
				int stepCounter = 0;
				int totalSteps = 11;
				string inputName = "Emission Map";
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

				// Section 1.4 Improvement: using a decorrelated color space for Albedo RGB
				TextureData emissionData = TextureToTextureData((Texture2D)emissionMap.textureValue, ref inputFormat);
				TextureData decorrelated = new TextureData(emissionData);
				DecorrelateColorSpace(ref emissionData, ref decorrelated, ref colorSpaceVector1, ref colorSpaceVector2, ref colorSpaceVector3, ref colorSpaceOrigin);
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
				ComputeDXTCompressionScalers((Texture2D)emissionMap.textureValue, ref dxtScalers, colorSpaceVector1, colorSpaceVector2, colorSpaceVector3);

				// Perform precomputations if precomputed textures don't already exist
				if (LoadPrecomputedTexturesIfExist((Texture2D)emissionMap.textureValue, ref texT, ref texInvT) == false)
				{
					TextureData Tinput = new TextureData(decorrelated.width, decorrelated.height);
					TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
					Precomputations(ref decorrelated, new List<int> { 0, 1, 2 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);
					EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
					RescaleForDXTCompression(ref Tinput, ref dxtScalers);
					EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);

					// Serialize precomputed data and setup material
					SerializePrecomputedTextures((Texture2D)emissionMap.textureValue, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
				}
				EditorUtility.ClearProgressBar();

				// Apply to shader properties
				emissionMapT.textureValue = texT;
				emissionMapInvT.textureValue = texInvT;
				emissionColorSpaceOrigin.vectorValue = colorSpaceOrigin;
				emissionColorSpaceVector1.vectorValue = colorSpaceVector1;
				emissionColorSpaceVector2.vectorValue = colorSpaceVector2;
				emissionColorSpaceVector3.vectorValue = colorSpaceVector3;
				emissionMapDXTScalers.vectorValue = dxtScalers;
				material.EnableKeyword("_STOCHASTIC_EMISSION");
			}
			else
			{
				emissionMapT.textureValue = emissionMap.textureValue;
				material.DisableKeyword("_STOCHASTIC_EMISSION");
			}
			#endregion

			#region DETAIL MASK MAP
			if (InputIsSelected(6) && detailMask.textureValue != null)
			{
				int stepCounter = 0;
				int totalSteps = 3;
				string inputName = "Detail Mask Map";
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

				// Perform precomputations if precomputed textures don't already exist
				if (LoadPrecomputedTexturesIfExist((Texture2D)detailMask.textureValue, ref texT, ref texInvT) == false)
				{
					TextureData maskData = TextureToTextureData((Texture2D)detailMask.textureValue, ref inputFormat);

					TextureData Tinput = new TextureData(maskData.width, maskData.height);
					TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
					Precomputations(ref maskData, new List<int> { 3 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);

					// Serialize precomputed data and setup material
					SerializePrecomputedTextures((Texture2D)detailMask.textureValue, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
				}
				EditorUtility.ClearProgressBar();

				// Apply to shader properties
				detailMaskT.textureValue = texT;
				detailMaskInvT.textureValue = texInvT;
				material.EnableKeyword("_STOCHASTIC_DETAILMASK");
			}
			else
			{
				detailMaskT.textureValue = detailMask.textureValue;
				material.DisableKeyword("_STOCHASTIC_DETAILMASK");
			}
			#endregion

			#region DETAIL ALBEDO MAP
			if (InputIsSelected(7) && detailAlbedoMap.textureValue != null)
			{
				int stepCounter = 0;
				int totalSteps = 11;
				string inputName = "Detail Albedo Map";
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

				// Section 1.4 Improvement: using a decorrelated color space for Albedo RGB
				TextureData albedoData = TextureToTextureData((Texture2D)detailAlbedoMap.textureValue, ref inputFormat);
				TextureData decorrelated = new TextureData(albedoData);
				DecorrelateColorSpace(ref albedoData, ref decorrelated, ref colorSpaceVector1, ref colorSpaceVector2, ref colorSpaceVector3, ref colorSpaceOrigin);
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
				ComputeDXTCompressionScalers((Texture2D)detailAlbedoMap.textureValue, ref dxtScalers, colorSpaceVector1, colorSpaceVector2, colorSpaceVector3);

				// Perform precomputations if precomputed textures don't already exist
				if (LoadPrecomputedTexturesIfExist((Texture2D)detailAlbedoMap.textureValue, ref texT, ref texInvT) == false)
				{
					TextureData Tinput = new TextureData(decorrelated.width, decorrelated.height);
					TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
					Precomputations(ref decorrelated, new List<int> { 0, 1, 2 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);
					EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
					RescaleForDXTCompression(ref Tinput, ref dxtScalers);
					EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);

					// Serialize precomputed data and setup material
					SerializePrecomputedTextures((Texture2D)detailAlbedoMap.textureValue, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
				}
				EditorUtility.ClearProgressBar();

				// Apply to shader properties
				detailAlbedoMapT.textureValue = texT;
				detailAlbedoMapInvT.textureValue = texInvT;
				detailAlbedoColorSpaceOrigin.vectorValue = colorSpaceOrigin;
				detailAlbedoColorSpaceVector1.vectorValue = colorSpaceVector1;
				detailAlbedoColorSpaceVector2.vectorValue = colorSpaceVector2;
				detailAlbedoColorSpaceVector3.vectorValue = colorSpaceVector3;
				detailAlbedoMapDXTScalers.vectorValue = dxtScalers;
				material.EnableKeyword("_STOCHASTIC_DETAILALBEDO");
			}
			else
			{
				detailAlbedoMapT.textureValue = detailAlbedoMap.textureValue;
				material.DisableKeyword("_STOCHASTIC_DETAILALBEDO");
			}
			#endregion

			#region DETAIL NORMAL MAP
			if (InputIsSelected(8) && detailNormalMap.textureValue != null)
			{
				int stepCounter = 0;
				int totalSteps = 11;
				string inputName = "Detail Normal Map";
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter / totalSteps);

				// Section 1.4 Improvement: using a decorrelated color space for Albedo RGB
				TextureData normalData = TextureToTextureData((Texture2D)detailNormalMap.textureValue, ref inputFormat);
				TextureData decorrelated = new TextureData(normalData);
				DecorrelateColorSpace(ref normalData, ref decorrelated, ref colorSpaceVector1, ref colorSpaceVector2, ref colorSpaceVector3, ref colorSpaceOrigin);
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
				ComputeDXTCompressionScalers((Texture2D)detailNormalMap.textureValue, ref dxtScalers, colorSpaceVector1, colorSpaceVector2, colorSpaceVector3);

				// Perform precomputations if precomputed textures don't already exist
				if (LoadPrecomputedTexturesIfExist((Texture2D)detailNormalMap.textureValue, ref texT, ref texInvT) == false)
				{
					TextureData Tinput = new TextureData(decorrelated.width, decorrelated.height);
					TextureData invT = new TextureData(LUT_WIDTH, (int)(Mathf.Log((float)Tinput.width) / Mathf.Log(2.0f))); // Height = Number of prefiltered LUT levels
					Precomputations(ref decorrelated, new List<int> { 0, 1, 2 }, ref Tinput, ref invT, inputName, ref stepCounter, totalSteps);
					EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
					RescaleForDXTCompression(ref Tinput, ref dxtScalers);
					EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);

					// Serialize precomputed data and setup material
					SerializePrecomputedTextures((Texture2D)detailNormalMap.textureValue, ref inputFormat, ref Tinput, ref invT, ref texT, ref texInvT);
				}
				EditorUtility.ClearProgressBar();

				// Apply to shader properties
				detailNormalMapT.textureValue = texT;
				detailNormalMapInvT.textureValue = texInvT;
				detailNormalColorSpaceOrigin.vectorValue = colorSpaceOrigin;
				detailNormalColorSpaceVector1.vectorValue = colorSpaceVector1;
				detailNormalColorSpaceVector2.vectorValue = colorSpaceVector2;
				detailNormalColorSpaceVector3.vectorValue = colorSpaceVector3;
				detailNormalMapDXTScalers.vectorValue = dxtScalers;
				material.EnableKeyword("_STOCHASTIC_DETAILNORMAL");
			}
			else
			{
				detailNormalMapT.textureValue = detailNormalMap.textureValue;
				material.DisableKeyword("_STOCHASTIC_DETAILNORMAL");
			}
			#endregion
		}

		bool LoadPrecomputedTexturesIfExist(Texture2D input, ref Texture2D Tinput, ref Texture2D invT)
		{
			Tinput = null;
			invT = null;

			string localInputPath = AssetDatabase.GetAssetPath(input);
			int fileExtPos = localInputPath.LastIndexOf(".");
			if (fileExtPos >= 0)
				localInputPath = localInputPath.Substring(0, fileExtPos);

			Tinput = (Texture2D)AssetDatabase.LoadAssetAtPath(localInputPath + "_T.png", typeof(Texture2D));
			invT = (Texture2D)AssetDatabase.LoadAssetAtPath(localInputPath + "_invT.png", typeof(Texture2D));

			if (Tinput != null && invT != null)
				return true;
			else
				return false;
		}

		TextureData TextureToTextureData(Texture2D input, ref TextureFormat inputFormat)
		{
			// Modify input texture import settings temporarily
			string texpath = AssetDatabase.GetAssetPath(input);
			TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(texpath);
			TextureImporterCompression prev = importer.textureCompression;
			TextureImporterType prevType = importer.textureType;
			bool linearInput = importer.sRGBTexture == false || importer.textureType == TextureImporterType.NormalMap;
			bool prevReadable = importer.isReadable;
			if (importer != null)
			{
				importer.textureType = TextureImporterType.Default;
				importer.isReadable = true;
				importer.textureCompression = TextureImporterCompression.Uncompressed;
				AssetDatabase.ImportAsset(texpath, ImportAssetOptions.ForceUpdate);
				inputFormat = input.format;
			}

			// Copy input texture pixel data
			Color[] colors = input.GetPixels();
			TextureData res = new TextureData(input.width, input.height);
			for(int x = 0; x < res.width; x++)
			{
				for (int y = 0; y < res.height; y++)
				{
					res.SetColor(x, y, linearInput || PlayerSettings.colorSpace == ColorSpace.Gamma ?
						colors[y * res.width + x] : colors[y * res.width + x].linear);
				}
			}

			// Revert input texture settings
			if (importer != null)
			{
				importer.textureType = prevType;
				importer.isReadable = prevReadable;
				importer.textureCompression = prev;
				AssetDatabase.ImportAsset(texpath, ImportAssetOptions.ForceUpdate);
			}
			return res;
		}

		void SerializePrecomputedTextures(Texture2D input, ref TextureFormat inputFormat, ref TextureData Tinput, ref TextureData invT, ref Texture2D output, ref Texture2D outputLUT)
		{
			string path = AssetDatabase.GetAssetPath(input);
			TextureImporter inputImporter = (TextureImporter)TextureImporter.GetAtPath(path);

			// Copy generated data into new textures
			output = new Texture2D(Tinput.width, Tinput.height, inputFormat, true, true);
			output.SetPixels(Tinput.data);
			output.Apply();

			outputLUT = new Texture2D(invT.width, invT.height, inputFormat, false, true);
			outputLUT.SetPixels(invT.data);
			outputLUT.Apply();

			// Create output path at input texture location
			string assetsPath = Application.dataPath;
			assetsPath = assetsPath.Substring(0, assetsPath.Length - "Assets".Length);

			string localInputPath = AssetDatabase.GetAssetPath(input);
			int fileExtPos = localInputPath.LastIndexOf(".");
			if (fileExtPos >= 0)
				localInputPath = localInputPath.Substring(0, fileExtPos);

			// Write output textures
			System.IO.File.WriteAllBytes(assetsPath + localInputPath + "_T.png", output.EncodeToPNG());
			System.IO.File.WriteAllBytes(assetsPath + localInputPath + "_invT.png", outputLUT.EncodeToPNG());
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			output = (Texture2D)AssetDatabase.LoadAssetAtPath(localInputPath + "_T.png", typeof(Texture2D));
			outputLUT = (Texture2D)AssetDatabase.LoadAssetAtPath(localInputPath + "_invT.png", typeof(Texture2D));

			// Change import settings
			path = AssetDatabase.GetAssetPath(output);
			TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
			importer.wrapMode = TextureWrapMode.Repeat;
			importer.filterMode = input.filterMode;
			importer.anisoLevel = input.anisoLevel;
			importer.mipmapEnabled = inputImporter.mipmapEnabled;
			importer.sRGBTexture = false;
			importer.textureCompression = inputImporter.textureCompression;
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

			path = AssetDatabase.GetAssetPath(outputLUT);
			importer = (TextureImporter)TextureImporter.GetAtPath(path);
			importer.wrapMode = TextureWrapMode.Clamp;
			importer.filterMode = FilterMode.Bilinear;
			importer.anisoLevel = 1;
			importer.mipmapEnabled = false;
			importer.sRGBTexture = false;
			importer.textureCompression = TextureImporterCompression.Uncompressed;
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
		}

		private void Precomputations(
			ref TextureData input,		// input: example image
				List<int> channels,		// input: channels to process
			ref TextureData Tinput,		// output: T(input) image
			ref TextureData invT,		// output: T^{-1} look-up table
			string inputName,
			ref int stepCounter,
			int totalSteps)
		{
			// Section 1.3.2 Applying the histogram transformation T on the input
			foreach (int channel in channels)
			{
				ComputeTinput(ref input, ref Tinput, channel);
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
			}

			// Section 1.3.3 Precomputing the inverse histogram transformation T^{-1}
			foreach (int channel in channels)
			{
				ComputeinvT(ref input, ref invT, channel);
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
			}

			// Section 1.5 Improvement: prefiltering the look-up table
			foreach (int channel in channels)
			{
				PrefilterLUT(ref Tinput, ref invT, channel);
				EditorUtility.DisplayProgressBar("Pre-processing textures for stochastic sampling", inputName, (float)stepCounter++ / totalSteps);
			}
		}

		private void ComputeDXTCompressionScalers(Texture2D input, ref Vector3 DXTScalers, Vector3 colorSpaceVector1, Vector3 colorSpaceVector2, Vector3 colorSpaceVector3)
		{
			string path = AssetDatabase.GetAssetPath(input);
			TextureImporter inputImporter = (TextureImporter)TextureImporter.GetAtPath(path);
			if (inputImporter.textureCompression != TextureImporterCompression.Uncompressed)
			{

				DXTScalers.x = 1.0f / Mathf.Sqrt(colorSpaceVector1.x * colorSpaceVector1.x + colorSpaceVector1.y * colorSpaceVector1.y + colorSpaceVector1.z * colorSpaceVector1.z);
				DXTScalers.y = 1.0f / Mathf.Sqrt(colorSpaceVector2.x * colorSpaceVector2.x + colorSpaceVector2.y * colorSpaceVector2.y + colorSpaceVector2.z * colorSpaceVector2.z);
				DXTScalers.z = 1.0f / Mathf.Sqrt(colorSpaceVector3.x * colorSpaceVector3.x + colorSpaceVector3.y * colorSpaceVector3.y + colorSpaceVector3.z * colorSpaceVector3.z);
			}
			else
			{
				DXTScalers.x = -1.0f;
				DXTScalers.y = -1.0f;
				DXTScalers.z = -1.0f;
			}
		}

		private void RescaleForDXTCompression(ref TextureData Tinput, ref Vector3 DXTScalers)
		{
			// If we use DXT compression
			// we need to rescale the Gaussian channels (see Section 1.6)
			if (DXTScalers.x >= 0.0f)
			{
				for (int y = 0; y < Tinput.height; y++)
					for (int x = 0; x < Tinput.width; x++)
						for (int i = 0; i < 3; i++)
						{
							float v = Tinput.GetColor(x, y)[i];
							v = (v - 0.5f) / DXTScalers[i] + 0.5f;
							Tinput.SetColor(x, y, i, v);
						}
			}
		}
		
		/*****************************************************************************/
		/**************** Section 1.3.1 Target Gaussian distribution *****************/
		/*****************************************************************************/

		private float Erf(float x)
		{
			// Save the sign of x
			int sign = 1;
			if (x < 0)
				sign = -1;
			x = Mathf.Abs(x);

			// A&S formula 7.1.26
			float t = 1.0f / (1.0f + 0.3275911f * x);
			float y = 1.0f - (((((1.061405429f * t + -1.453152027f) * t) + 1.421413741f)
				* t + -0.284496736f) * t + 0.254829592f) * t * Mathf.Exp(-x * x);

			return sign * y;
		}

		private float ErfInv(float x)
		{
			float w, p;
			w = -Mathf.Log((1.0f - x) * (1.0f + x));
			if (w < 5.000000f)
			{
				w = w - 2.500000f;
				p = 2.81022636e-08f;
				p = 3.43273939e-07f + p * w;
				p = -3.5233877e-06f + p * w;
				p = -4.39150654e-06f + p * w;
				p = 0.00021858087f + p * w;
				p = -0.00125372503f + p * w;
				p = -0.00417768164f + p * w;
				p = 0.246640727f + p * w;
				p = 1.50140941f + p * w;
			}
			else
			{
				w = Mathf.Sqrt(w) - 3.000000f;
				p = -0.000200214257f;
				p = 0.000100950558f + p * w;
				p = 0.00134934322f + p * w;
				p = -0.00367342844f + p * w;
				p = 0.00573950773f + p * w;
				p = -0.0076224613f + p * w;
				p = 0.00943887047f + p * w;
				p = 1.00167406f + p * w;
				p = 2.83297682f + p * w;
			}
			return p * x;
		}

		private float CDF(float x, float mu, float sigma)
		{
			float U = 0.5f * (1 + Erf((x - mu) / (sigma * Mathf.Sqrt(2.0f))));
			return U;
		}

		private float invCDF(float U, float mu, float sigma)
		{
			float x = sigma * Mathf.Sqrt(2.0f) * ErfInv(2.0f * U - 1.0f) + mu;
			return x;
		}
		
		/*****************************************************************************/
		/**** Section 1.3.2 Applying the histogram transformation T on the input *****/
		/*****************************************************************************/
		private struct PixelSortStruct
		{
			public int x;
			public int y;
			public float value;
		};

		private void ComputeTinput(ref TextureData input, ref TextureData T_input, int channel)
		{
			// Sort pixels of example image
			PixelSortStruct[] sortedInputValues = new PixelSortStruct[input.width * input.height];
			for (int y = 0; y < input.height; y++)
				for (int x = 0; x < input.width; x++)
				{
					sortedInputValues[y * input.width + x].x = x;
					sortedInputValues[y * input.width + x].y = y;
					sortedInputValues[y * input.width + x].value = input.GetColor(x, y)[channel];
				}
			Array.Sort(sortedInputValues, (x, y) => x.value.CompareTo(y.value));

			// Assign Gaussian value to each pixel
			for (uint i = 0; i < sortedInputValues.Length; i++)
			{
				// Pixel coordinates
				int x = sortedInputValues[i].x;
				int y = sortedInputValues[i].y;
				// Input quantile (given by its order in the sorting)
				float U = (i + 0.5f) / (sortedInputValues.Length);
				// Gaussian quantile
				float G = invCDF(U, GAUSSIAN_AVERAGE, GAUSSIAN_STD);
				// Store
				T_input.SetColor(x, y, channel, G);
			}
		}
		
		/*****************************************************************************/
		/** Section 1.3.3 Precomputing the inverse histogram transformation T^{-1} ***/
		/*****************************************************************************/

		private void ComputeinvT(ref TextureData input, ref TextureData Tinv, int channel)
		{
			// Sort pixels of example image
			float[] sortedInputValues = new float[input.width * input.height];
			for (int y = 0; y < input.height; y++)
				for (int x = 0; x < input.width; x++)
				{
					sortedInputValues[y * input.width + x] = input.GetColor(x, y)[channel];
				}
			Array.Sort(sortedInputValues);

			// Generate Tinv look-up table 
			for (int i = 0; i < Tinv.width; i++)
			{
				// Gaussian value in [0, 1]
				float G = (i + 0.5f) / (Tinv.width);
				// Quantile value 
				float U = CDF(G, GAUSSIAN_AVERAGE, GAUSSIAN_STD);
				// Find quantile in sorted pixel values
				int index = (int)Mathf.Floor(U * sortedInputValues.Length);
				// Get input value 
				float I = sortedInputValues[index];
				// Store in LUT
				Tinv.SetColor(i, 0, channel, I);
			}
		}
		
		/*****************************************************************************/
		/******** Section 1.4 Improvement: using a decorrelated color space **********/
		/*****************************************************************************/

		// Compute the eigen vectors of the histogram of the input
		private void ComputeEigenVectors(ref TextureData input, Vector3[] eigenVectors)
		{
			// First and second order moments
			float R = 0, G = 0, B = 0, RR = 0, GG = 0, BB = 0, RG = 0, RB = 0, GB = 0;
			for (int y = 0; y < input.height; y++)
			{
				for (int x = 0; x < input.width; x++)
				{
					Color col = input.GetColor(x, y);
					R += col.r;
					G += col.g;
					B += col.b;
					RR += col.r * col.r;
					GG += col.g * col.g;
					BB += col.b * col.b;
					RG += col.r * col.g;
					RB += col.r * col.b;
					GB += col.g * col.b;
				}
			}
				
			R /= (float)(input.width * input.height);
			G /= (float)(input.width * input.height);
			B /= (float)(input.width * input.height);
			RR /= (float)(input.width * input.height);
			GG /= (float)(input.width * input.height);
			BB /= (float)(input.width * input.height);
			RG /= (float)(input.width * input.height);
			RB /= (float)(input.width * input.height);
			GB /= (float)(input.width * input.height);

			// Covariance matrix
			double[][] covarMat = new double[3][];
			for (int i = 0; i < 3; i++)
				covarMat[i] = new double[3];
			covarMat[0][0] = RR - R* R;
			covarMat[0][1] = RG - R* G;
			covarMat[0][2] = RB - R* B;
			covarMat[1][0] = RG - R* G;
			covarMat[1][1] = GG - G* G;
			covarMat[1][2] = GB - G* B;
			covarMat[2][0] = RB - R* B;
			covarMat[2][1] = GB - G* B;
			covarMat[2][2] = BB - B* B;

			// Find eigen values and vectors using Jacobi algorithm
			double[][] eigenVectorsTemp = new double[3][];
			for (int i = 0; i < 3; i++)
				eigenVectorsTemp[i] = new double[3];
			double[] eigenValuesTemp = new double[3];
			ComputeEigenValuesAndVectors(covarMat, eigenVectorsTemp, eigenValuesTemp);

			// Set return values
			eigenVectors[0] = new Vector3((float) eigenVectorsTemp[0][0], (float) eigenVectorsTemp[1][0], (float) eigenVectorsTemp[2][0]);
			eigenVectors[1] = new Vector3((float) eigenVectorsTemp[0][1], (float) eigenVectorsTemp[1][1], (float) eigenVectorsTemp[2][1]);
			eigenVectors[2] = new Vector3((float) eigenVectorsTemp[0][2], (float) eigenVectorsTemp[1][2], (float) eigenVectorsTemp[2][2]);
		}

		// ----------------------------------------------------------------------------
		// Numerical diagonalization of 3x3 matrcies
		// Copyright (C) 2006  Joachim Kopp
		// ----------------------------------------------------------------------------
		// This library is free software; you can redistribute it and/or
		// modify it under the terms of the GNU Lesser General Public
		// License as published by the Free Software Foundation; either
		// version 2.1 of the License, or (at your option) any later version.
		//
		// This library is distributed in the hope that it will be useful,
		// but WITHOUT ANY WARRANTY; without even the implied warranty of
		// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
		// Lesser General Public License for more details.
		//
		// You should have received a copy of the GNU Lesser General Public
		// License along with this library; if not, write to the Free Software
		// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
		// ----------------------------------------------------------------------------
		// Calculates the eigenvalues and normalized eigenvectors of a symmetric 3x3
		// matrix A using the Jacobi algorithm.
		// The upper triangular part of A is destroyed during the calculation,
		// the diagonal elements are read but not destroyed, and the lower
		// triangular elements are not referenced at all.
		// ----------------------------------------------------------------------------
		// Parameters:
		//		A: The symmetric input matrix
		//		Q: Storage buffer for eigenvectors
		//		w: Storage buffer for eigenvalues
		// ----------------------------------------------------------------------------
		// Return value:
		//		0: Success
		//		-1: Error (no convergence)
		private int ComputeEigenValuesAndVectors(double[][] A, double[][] Q, double[] w)
		{
			const int n = 3;
			double sd, so;                  // Sums of diagonal resp. off-diagonal elements
			double s, c, t;                 // sin(phi), cos(phi), tan(phi) and temporary storage
			double g, h, z, theta;          // More temporary storage
			double thresh;

			// Initialize Q to the identitity matrix
			for (int i = 0; i < n; i++)
			{
				Q[i][i] = 1.0;
				for (int j = 0; j < i; j++)
					Q[i][j] = Q[j][i] = 0.0;
			}

			// Initialize w to diag(A)
			for (int i = 0; i < n; i++)
				w[i] = A[i][i];

			// Calculate SQR(tr(A))  
			sd = 0.0;
			for (int i = 0; i < n; i++)
				sd += System.Math.Abs(w[i]);
			sd = sd * sd;

			// Main iteration loop
			for (int nIter = 0; nIter < 50; nIter++)
			{
				// Test for convergence 
				so = 0.0;
				for (int p = 0; p < n; p++)
					for (int q = p + 1; q < n; q++)
						so += System.Math.Abs(A[p][q]);
				if (so == 0.0)
					return 0;

				if (nIter < 4)
					thresh = 0.2 * so / (n * n);
				else
					thresh = 0.0;

				// Do sweep
				for (int p = 0; p < n; p++)
				{
					for (int q = p + 1; q < n; q++)
					{
						g = 100.0 * System.Math.Abs(A[p][q]);
						if (nIter > 4 && System.Math.Abs(w[p]) + g == System.Math.Abs(w[p])
							&& System.Math.Abs(w[q]) + g == System.Math.Abs(w[q]))
						{
							A[p][q] = 0.0;
						}
						else if (System.Math.Abs(A[p][q]) > thresh)
						{
							// Calculate Jacobi transformation
							h = w[q] - w[p];
							if (System.Math.Abs(h) + g == System.Math.Abs(h))
							{
								t = A[p][q] / h;
							}
							else
							{
								theta = 0.5 * h / A[p][q];
								if (theta < 0.0)
									t = -1.0 / (System.Math.Sqrt(1.0 + (theta * theta)) - theta);
								else
									t = 1.0 / (System.Math.Sqrt(1.0 + (theta * theta)) + theta);
							}
							c = 1.0 / System.Math.Sqrt(1.0 + (t * t));
							s = t * c;
							z = t * A[p][q];

							// Apply Jacobi transformation
							A[p][q] = 0.0;
							w[p] -= z;
							w[q] += z;
							for (int r = 0; r < p; r++)
							{
								t = A[r][p];
								A[r][p] = c * t - s * A[r][q];
								A[r][q] = s * t + c * A[r][q];
							}
							for (int r = p + 1; r < q; r++)
							{
								t = A[p][r];
								A[p][r] = c * t - s * A[r][q];
								A[r][q] = s * t + c * A[r][q];
							}
							for (int r = q + 1; r < n; r++)
							{
								t = A[p][r];
								A[p][r] = c * t - s * A[q][r];
								A[q][r] = s * t + c * A[q][r];
							}

							// Update eigenvectors
							for (int r = 0; r < n; r++)
							{
								t = Q[r][p];
								Q[r][p] = c * t - s * Q[r][q];
								Q[r][q] = s * t + c * Q[r][q];
							}
						}
					}
				}
			}

			return -1;
		}

		// Main function of Section 1.4
		private void DecorrelateColorSpace(
			ref TextureData input,					// input: example image
			ref TextureData input_decorrelated,		// output: decorrelated input 
			ref Vector3 colorSpaceVector1,			// output: color space vector1 
			ref Vector3 colorSpaceVector2,			// output: color space vector2
			ref Vector3 colorSpaceVector3,			// output: color space vector3
			ref Vector3 colorSpaceOrigin)			// output: color space origin
		{
			// Compute the eigenvectors of the histogram
			Vector3[] eigenvectors = new Vector3[3];
			ComputeEigenVectors(ref input, eigenvectors);

			// Rotate to eigenvector space
			for (int y = 0; y < input.height; y++)
				for (int x = 0; x < input.width; x++)
					for (int channel = 0; channel < 3; ++channel)
					{
						// Get current color
						Color color = input.GetColor(x, y);
						Vector3 vec = new Vector3(color.r, color.g, color.b);
						// Project on eigenvector 
						float new_channel_value = Vector3.Dot(vec, eigenvectors[channel]);
						// Store
						input_decorrelated.SetColor(x, y, channel, new_channel_value);
					}

			// Compute ranges of the new color space
			Vector2[] colorSpaceRanges = new Vector2[3]{
				new Vector2(float.MaxValue, float.MinValue),
				new Vector2(float.MaxValue, float.MinValue),
				new Vector2(float.MaxValue, float.MinValue) };
			for (int y = 0; y < input.height; y++)
				for (int x = 0; x < input.width; x++)
					for (int channel = 0; channel < 3; ++channel)
					{
						colorSpaceRanges[channel].x = Mathf.Min(colorSpaceRanges[channel].x, input_decorrelated.GetColor(x, y)[channel]);
						colorSpaceRanges[channel].y = Mathf.Max(colorSpaceRanges[channel].y, input_decorrelated.GetColor(x, y)[channel]);
					}

			// Remap range to [0, 1]
			for (int y = 0; y < input.height; y++)
				for (int x = 0; x < input.width; x++)
					for (int channel = 0; channel < 3; ++channel)
					{
						// Get current value
						float value = input_decorrelated.GetColor(x, y)[channel];
						// Remap in [0, 1]
						float remapped_value = (value - colorSpaceRanges[channel].x) / (colorSpaceRanges[channel].y - colorSpaceRanges[channel].x);
						// Store
						input_decorrelated.SetColor(x, y, channel, remapped_value);
					}

			// Compute color space origin and vectors scaled for the normalized range
			colorSpaceOrigin.x = colorSpaceRanges[0].x * eigenvectors[0].x + colorSpaceRanges[1].x * eigenvectors[1].x + colorSpaceRanges[2].x * eigenvectors[2].x;
			colorSpaceOrigin.y = colorSpaceRanges[0].x * eigenvectors[0].y + colorSpaceRanges[1].x * eigenvectors[1].y + colorSpaceRanges[2].x * eigenvectors[2].y;
			colorSpaceOrigin.z = colorSpaceRanges[0].x * eigenvectors[0].z + colorSpaceRanges[1].x * eigenvectors[1].z + colorSpaceRanges[2].x * eigenvectors[2].z;
			colorSpaceVector1.x = eigenvectors[0].x * (colorSpaceRanges[0].y - colorSpaceRanges[0].x);
			colorSpaceVector1.y = eigenvectors[0].y * (colorSpaceRanges[0].y - colorSpaceRanges[0].x);
			colorSpaceVector1.z = eigenvectors[0].z * (colorSpaceRanges[0].y - colorSpaceRanges[0].x);
			colorSpaceVector2.x = eigenvectors[1].x * (colorSpaceRanges[1].y - colorSpaceRanges[1].x);
			colorSpaceVector2.y = eigenvectors[1].y * (colorSpaceRanges[1].y - colorSpaceRanges[1].x);
			colorSpaceVector2.z = eigenvectors[1].z * (colorSpaceRanges[1].y - colorSpaceRanges[1].x);
			colorSpaceVector3.x = eigenvectors[2].x * (colorSpaceRanges[2].y - colorSpaceRanges[2].x);
			colorSpaceVector3.y = eigenvectors[2].y * (colorSpaceRanges[2].y - colorSpaceRanges[2].x);
			colorSpaceVector3.z = eigenvectors[2].z * (colorSpaceRanges[2].y - colorSpaceRanges[2].x);
		}

		/*****************************************************************************/
		/* ===== Section 1.5 Improvement: prefiltering the look-up table =========== */
		/*****************************************************************************/
		// Compute average subpixel variance at a given LOD
		private float ComputeLODAverageSubpixelVariance(ref TextureData image, int LOD, int channel)
		{
			// Window width associated with
			int windowWidth = 1 << LOD;

			// Compute average variance in all the windows
			float average_window_variance = 0.0f;

			// Loop over al the windows
			for (int window_y = 0; window_y < image.height; window_y += windowWidth)
				for (int window_x = 0; window_x < image.width; window_x += windowWidth)
				{
					// Estimate variance of current window
					float v = 0.0f;
					float v2 = 0.0f;
					for (int y = 0; y < windowWidth; y++)
						for (int x = 0; x < windowWidth; x++)
						{
							float value = image.GetColor(window_x + x, window_y + y)[channel];
							v += value;
							v2 += value * value;
						}
					v /= (float)(windowWidth * windowWidth);
					v2 /= (float)(windowWidth * windowWidth);
					float window_variance = Mathf.Max(0.0f, v2 - v * v);

					// Update average
					average_window_variance += window_variance / (image.width * image.height / windowWidth / windowWidth);
				}

			return average_window_variance;
		}

		// Filter LUT by sampling a Gaussian N(mu, std²)
		private float FilterLUTValueAtx(ref TextureData LUT, float x, float std, int channel)
		{
			// Number of samples for filtering (heuristic: twice the LUT resolution)
			const int numberOfSamples = 2 * LUT_WIDTH;

			// Filter
			float filtered_value = 0.0f;
			for (int sample = 0; sample < numberOfSamples; sample++)
			{
				// Quantile used to sample the Gaussian
				float U = (sample + 0.5f) / numberOfSamples;
				// Sample the Gaussian 
				float sample_x = invCDF(U, x, std);
				// Find sample texel in LUT (the LUT covers the domain [0, 1])
				int sample_texel = Mathf.Max(0, Mathf.Min(LUT_WIDTH - 1, (int)Mathf.Floor(sample_x * LUT_WIDTH)));
				// Fetch LUT at level 0
				float sample_value = LUT.GetColor(sample_texel, 0)[channel];
				// Accumulate
				filtered_value += sample_value;
			}
			// Normalize and return
			filtered_value /= (float)numberOfSamples;
			return filtered_value;
		}

		// Main function of section 1.5
		private void PrefilterLUT(ref TextureData image_T_Input, ref TextureData LUT_Tinv, int channel)
		{
			// Prefilter 
			for (int LOD = 1; LOD < LUT_Tinv.height; LOD++)
			{
				// Compute subpixel variance at LOD 
				float window_variance = ComputeLODAverageSubpixelVariance(ref image_T_Input, LOD, channel);
				float window_std = Mathf.Sqrt(window_variance);

				// Prefilter LUT with Gaussian kernel of this variance
				for (int i = 0; i < LUT_Tinv.width; i++)
				{
					// Texel position in [0, 1]
					float x_texel = (i + 0.5f) / LUT_Tinv.width;
					// Filter look-up table around this position with Gaussian kernel
					float filteredValue = FilterLUTValueAtx(ref LUT_Tinv, x_texel, window_std, channel);
					// Store filtered value
					LUT_Tinv.SetColor(i, LOD, channel, filteredValue);
				}
			}
		}


		/*********************************************************************/
		/********** Procedural Stochastic Texturing Properties ***************/
		/*********************************************************************/
		// Individual selection of inputs to sample stochasticly
		MaterialProperty layerMask = null;
		string[] layers = new string[] { "Albedo", "Metallic-Specular", "Normal", "Height", "Occlusion", "Emission", "Detail Mask", "Detail Albedo", "Detail Normal" };
		private bool InputIsSelected(int layerToCheck)
		{
			return (int)layerMask.floatValue == ((int)layerMask.floatValue | (1 << layerToCheck));
		}

		MaterialProperty albedoMapT = null;
		MaterialProperty specularMapT = null;
		MaterialProperty metallicMapT = null;
		MaterialProperty bumpMapT = null;
		MaterialProperty occlusionMapT = null;
		MaterialProperty heightMapT = null;
		MaterialProperty emissionMapT = null;
		MaterialProperty detailAlbedoMapT = null;
		MaterialProperty detailMaskT = null;
		MaterialProperty detailNormalMapT = null;

		MaterialProperty albedoMapInvT = null;
		MaterialProperty specularMapInvT = null;
		MaterialProperty metallicMapInvT = null;
		MaterialProperty bumpMapInvT = null;
		MaterialProperty occlusionMapInvT = null;
		MaterialProperty heightMapInvT = null;
		MaterialProperty emissionMapInvT = null;
		MaterialProperty detailAlbedoMapInvT = null;
		MaterialProperty detailMaskInvT = null;
		MaterialProperty detailNormalMapInvT = null;

		// Only with DXT compression (Section 1.6)
		MaterialProperty mainTexDXTScalers = null;
		MaterialProperty detailAlbedoMapDXTScalers = null;
		MaterialProperty bumpMapDXTScalers = null;
		MaterialProperty detailNormalMapDXTScalers = null;
		MaterialProperty emissionMapDXTScalers = null;

		// Decorrelated color space vectors and origins, used on albedo and normal maps
		MaterialProperty mainTexColorSpaceOrigin = null;
		MaterialProperty mainTexColorSpaceVector1 = null;
		MaterialProperty mainTexColorSpaceVector2 = null;
		MaterialProperty mainTexColorSpaceVector3 = null;
		MaterialProperty detailAlbedoColorSpaceOrigin = null;
		MaterialProperty detailAlbedoColorSpaceVector1 = null;
		MaterialProperty detailAlbedoColorSpaceVector2 = null;
		MaterialProperty detailAlbedoColorSpaceVector3 = null;
		MaterialProperty bumpMapColorSpaceOrigin = null;
		MaterialProperty bumpMapColorSpaceVector1 = null;
		MaterialProperty bumpMapColorSpaceVector2 = null;
		MaterialProperty bumpMapColorSpaceVector3 = null;
		MaterialProperty detailNormalColorSpaceOrigin = null;
		MaterialProperty detailNormalColorSpaceVector1 = null;
		MaterialProperty detailNormalColorSpaceVector2 = null;
		MaterialProperty detailNormalColorSpaceVector3 = null;
		MaterialProperty emissionColorSpaceOrigin = null;
		MaterialProperty emissionColorSpaceVector1 = null;
		MaterialProperty emissionColorSpaceVector2 = null;
		MaterialProperty emissionColorSpaceVector3 = null;

	}
} // namespace UnityEditor
