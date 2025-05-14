# Time Traveler's Museum

## Team Information
**Team Name**: Time Travelers  
**Date of Submission**: May 13, 2025  

### Team Members
- **Cristina Huang** – `hh3101`  
- **Yifan Shen** – `ys3869`  
- **Tom Xu** – `jx2518`

---

## Project Overview

**Project Title**: *Time Traveler's Museum*  
**Development Platforms**: Unity 6000.0.45f1., Windows 11, Meta Quest 3  
**Mobile/Server Platforms**: N/A  

---

## Directory Overview

Time Traveler's Museum - Directory Overview
Root Structure
.git/ - Git version control directory
Assets/ - Main Unity project assets
Library/ - Unity cache files
Logs/ - Unity log files
Packages/ - Unity package dependencies
ProjectSettings/ - Unity project configuration
UserSettings/ - User-specific Unity settings
.vs/ - Visual Studio configuration files
obj/ - Compiled code objects
Various .csproj files - C# project files for different components
TimeTravelersMuseum.sln - Visual Studio solution file
README.md - Project documentation
Assets Directory (Main Content)
Scenes/ - Unity scenes including:
DemoScene.unity - Main demonstration scene
SampleScene.unity - Sample development scene
BasicScene.unity - Basic testing scene
GSScene.unity and GSTestScene.unity - Gaussian Splatting test scenes
Scripts/ - C# scripts including:
SnapToFrame.cs - Frame alignment functionality
Various minimap scripts: MinimapControls.cs, MinimapTeleporter.cs, PlayerMiniMapMarker.cs
ShowGoldenPath.cs - Navigation guidance system
CrystalBallPortal.cs - Interactive portal functionality
SelfGuidingFire.cs - Guide fire behavior
GaussianAssets/ - 3D assets using Gaussian Splatting technique
Multiple scene data files (columbia, cherry noosso, car_undersea, etc.)
Each scene has position, color, and other data files
Prefab/ - Predefined Unity game objects
Furniture/ - Furniture models and setups
Decoration/ - Decorative elements including:
Crystal Ball objects
Guide elements like GuideFire and GuiderLantern
Museum artifacts and decorative items
VRMPAssets/ - VR Multiplayer specific assets
Scripts/ - VR interaction code
UI/ - User interface elements
Network/ - Networking functionality
Player/ - Player controls and interaction
Helpers/ - Utility scripts
Gameplay/ - Core gameplay mechanics
Prefabs/ - VR-specific prefabricated objects
Materials/ - Material definitions
Textures/ - Visual textures
Animation/ - Animation assets
Material/ - Material definitions for objects
RenderTexture/ - Assets for rendering to textures
RealWorldObjects/ - Real-world object models
XR/ and XRI/ - XR Interaction Toolkit assets
TextMesh Pro/ - Text rendering system
VFXGraph/ - Visual effects assets
Video/ - Video content assets
Resources/ - Unity resource assets

---

## Deployment Instructions

To access the Gaussian splatting package, place the UnityGaussianSplatting-main folder at D: UnityProjectUnityGaussianSplatting.

---

## Target Preparation

No special preparation required.

---

## Demo Video

> *(Please insert your demo video URL here once available.)*

---

## Known Bugs

- Occasionally, clicking **Play** may cause the Multiplayer Network to throw an error and pause the game.  
  Simply unpause the game in Unity to continue.  
  This does **not** affect the single-player experience.  
  The multiplayer feature was retained for future development iterations.

---

## Missing Features

None.

---

## Asset Sources

1. https://github.com/aras-p/UnityGaussianSplatting;
2. https://sketchfab.com/3d-models/jannotta-gallery-56dd703755e4404c93e3d13b8a0ecab5;
3. https://assetstore.unity.com/packages/3d/environments/apartment-kit-124055;
4. https://assetstore.unity.com/packages/3d/props/interior/picture-frames-with-photos-106907;;
5. https://assetstore.unity.com/packages/3d/props/furniture/rustic-small-cabinet-246869;
6. https://assetstore.unity.com/packages/3d/props/electronics/speakers-pbr-111606;
