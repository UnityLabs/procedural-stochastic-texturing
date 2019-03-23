![alt text](https://i.imgur.com/fpAQs15.png)

## Procedural Stochastic Texturing

This plugin provides an alternative Standard shader that implements our procedural stochastic texturing method. For a certain class of textures called stochastic textures, it solves the issue of tiling repetition when tileable textures are repeated across a surface. This allows using smaller textures, achieving higher levels of detail and texturing larger surfaces without the need to hide the repetition patterns.

To use this prototype, either clone the repository into your Asset folder or download the .unitypackage in the Release tab.

More detailed description and instructions:
[Procedural Stochastic Texturing in Unity](https://blogs.unity3d.com/)

The version of the code should work on Unity versions 5.6.x

This work is the implementation of two recent research publications at Unity Labs, which are the best resource for understanding the technique in detail.

Paper: 					[High-Performance By-Example Noise using a Histogram-Preserving Blending Operator](https://eheitzresearch.wordpress.com/722-2/)

Technical chapter: 		[Procedural Stochastic Textures by Tiling and Blending](https://eheitzresearch.wordpress.com/738-2/)

The comments in the code refer to specific sections of the Technical chapter.

