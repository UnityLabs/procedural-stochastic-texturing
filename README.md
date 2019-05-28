![alt text](https://i.imgur.com/fpAQs15.png)

# Procedural Stochastic Texturing
This repository offers two implementation of our procedural stochastic texturing prototype for Unity. For a certain class of textures called stochastic textures, it solves the issue of tiling repetition when tileable textures are repeated across a surface. This allows using smaller textures, achieving higher levels of detail and texturing larger surfaces without the need to hide the repetition patterns.


## Shader Graph Implementation
This implementation adds the "Sample Procedural Texture 2D" alternative input node to Shader Graph, and a new ProceduralTexture2D asset type taking care of the required precomputations. This is the easiest and most flexible way to use this technique, but does require a bit more setup currently.

Since it isn't possible to add files to Unity packages in Library/PackageCache, this version is distributed here as a custom package containing ShaderGraph 6.7.1 entirely, with our added code. It requires using Unity 2019.2 or later, and a project that has either the LWRP 6.7.1 or HDRP 6.7.1 package installed. Make sure to update your render pipeline package to that version in the package manager.

Either clone the repository into your project's root folder or extract the archive from the Release tab there. The folder "procedural-stochastic-texturing" containing this repository's files should be in your project's root folder. Then open your project's Packages/manifest.json file and add the line:

```"com.unity.shadergraph": "file:../procedural-stochastic-texturing",```

At the top of the dependencies list. When you re-open your project, Unity will now use the custom ShaderGraph package at that path and you are good to go.

### How to use
1. Create a ProceduralTexture2D asset using right click/Create or Assets/Create. Assign the desired texture input, select its type (Color for strictly color information such as albedo or emission, Normal for normal maps, Other for other data such as occlusion, roughness, height, etc.) and hit Apply.

2. In a Shader Graph, create a new Sample Procedural Texture 2D node and use it just as you would a normal Sample Texture 2D node, except that you need to assign the ProceduralTexture2D asset as input.

3. Tweak the Blend parameter if needed. This value should be between 0 and 1. A value of 0 works well for most cases, but higher values can produce less messy results for textures featuring strong lines and shapes. A value too high will however start showing an hexadecimal pattern due to the blending scheme.

<img src="https://i.imgur.com/VLm0ROH.png" width="700" class="center">

### Notes
- Make sure a normal texture is imported as a Normal map in its import settings before using it with a ProceduralTexture2D asset.
- Large input textures might take a while to pre-process.
- If you modify a ProceduralTexture2D's parameters and apply them, each ShaderGraph using this ProceduralTexture2D needs to be re-opened and re-saved manually to use the updated parameters.



## Standard Shader Implementation
This plugin provides an alternative Standard shader that implements our procedural stochastic texturing method for projects using the legacy Unity renderer.

To use this prototype, download the .unitypackage in the Release tab and import it into your project. Two versions are available, for Unity 2018.3 and Unity 5.6.6.

More detailed description and instructions on how to use this Standard version:
[Procedural Stochastic Texturing in Unity](https://blogs.unity3d.com/)


## Read More
This work is the implementation of two recent research publications at Unity Labs, which are the best resource for understanding the technique in detail.

Paper: 					[High-Performance By-Example Noise using a Histogram-Preserving Blending Operator](https://eheitzresearch.wordpress.com/722-2/)

Technical chapter: 		[Procedural Stochastic Textures by Tiling and Blending](https://eheitzresearch.wordpress.com/738-2/)

The comments in the code refer to specific sections of the Technical chapter.

