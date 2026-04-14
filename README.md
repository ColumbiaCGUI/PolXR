# VISER: XR scientific visualization tool for ice-penetrating radar data in Antarctica and Greenland

This application facilitates the 3D visualization of ice-penetrating radar data for polar geophysicists using Extended Reality (XR) to render data in an accurate 3D spatial context in order to improve data interpretation.

Project Website: https://pgg.ldeo.columbia.edu/projects/VISER

Unity Version: 2022.3.20f1

_Note: This project is still in development_.

***Last Updated 04/14/2026.***

# Team

Developers:
* [Ben Yang](https://github.com/benplus1), Columbia University
  
Additional Contributors:
* [Isabel Cordero](https://lamont.columbia.edu/directory/s-isabel-cordero), Lamont-Doherty Earth Observatory
* [Andrew Hoffman](https://lamont.columbia.edu/directory/andrew-o-hoffman), Lamont-Doherty Earth Observatory
* [Carmine Elvezio](https://carmineelvezio.com/), Columbia University
* [Bettina Schlager](https://www.cs.columbia.edu/~bschlager/), Columbia University

Advisors:
* Dr. [Robin Bell](https://pgg.ldeo.columbia.edu/people/robin-e-bell), Lamont-Doherty Earth Observatory
* Dr. [Alexandra Boghosian](https://alexandraboghosian.com/), Lamont-Doherty Earth Observatory
* Dr. [Kirsty Tinto](https://pgg.ldeo.columbia.edu/people/kirsty-j-tinto), Lamont-Doherty Earth Observatory
* Professor [Steven Feiner](http://www.cs.columbia.edu/~feiner/), Columbia University

Former Collaborators:
* [Qazi Ashikin](https://github.com/qaziashikin), Columbia University
* [Ashley Cho](https://github.com/hc8756), Barnard College
* [Eris Yunxin Gao](https://github.com/SamIAm2000), Barnard College
* [Shengyue Guo](https://github.com/guosy1998), Columbia University
* [Anusha Lavanuru](), Columbia University
* [Shanying Liu](), Columbia University
* [Leah Kim](https://github.com/LEAAHKIM), Barnard College
* [Roshan Krishnan](), Columbia University
* [Ellie Madsen](), Columbia University
* [Moises Mata](https://github.com/moisesmata), Columbia University
* [John Mitnik](), Columbia University
* [Linda Mukarakate](), Barnard College
* [Emily Mackevicius](https://emackev.github.io/), Columbia University
* [Greg Ou](https://github.com/21go), Columbia University
* [David Rios](https://github.com/MrDavidRios), Columbia University
* [Joel Salzman](https://github.com/joelsalzman), Columbia University
* [Sofia Sanchez Zarate](https://github.com/sofiasanchez985), Columbia University

<br />

# Background

Ice-penetrating radar is a powerful tool to study glaciological processes but the data are often difficult to interpret. These radar measurements are taken by flying a plane equipped with sensors along some path (known as the flightline) and plotting the radar returns. These plots (radargrams) must be interpreted, often manually, in order to distinguish features inside the ice. Glaciologists are intensely interested in identifying the ice surface, bedrock depth, and whatever features can be discerned in the subsurface. An essential step in this process is "picking lines," or identifying contiguous curves in radargrams that correspond to real features in the world (as opposed to noise). Current methods are suboptimal.

VISER aims to improve interpretability of radargrams using XR. First, we place the user inside a digital twin of the relevant ice shelf so that all the data are properly contextualized relative to each other in space. Second, we model the radargrams as Unity GameObjects so that the features visible on the plots (which appear as textures on the GameObjects) also appear in the proper 3D geospatial context. Third, we implement numerous interfaces for analysis and manipulation so that users can explore the data.

# How-To Deploy Application

The Pol-XR Application can be deployed in two ways: Standalone (headset alone), or Tethered (headset wired to PC). Both methods currently require Unity to be installed. A free personal use license is acceptable to deploy this application - account may be required for download. 

| Required Downloads | Description |
| :-----------: | ----------- |
| [Unity Version: 2022.3.20f1](https://unity.com/releases/editor/whats-new/2022.3.20) | Current version of Unity Editor Build for Pol-XR |
| [Meta Horizon Link Application](https://www.meta.com/help/quest/1517439565442928/) | Use for building application in tethered mode |
| Pol-XR Application | Either Git pull the most recent version of the application, or navigate above and download as ZIP |

1. Launch UnityHub and Open the Pol-XR Project using the Editor Version stated above 
2. Once the Editor has opened, navigate within the lower third toolbar, select Assets, then Scenes, and finally homescreen-no-mrtk. Double click to make sure it loads in the Unity player.
3. Connect the Meta Quest2 or MetaQuest3 via USB to the computer.
4. To build the application *Standalone*, navigate to the top toolbar and select File > Build Settings, select Android, and click Build and Run.
To build  the application *Tethered*, open the Meta Link Application and see if the device is registered and connected. Place the headset on your head and boot the headset as normal. Once able, navigate to the Settings within the Main Menu. If Quick Access is visible, select Quest Link or AirLink - AirLink is an alternative option if WiFi is available, but not recommended due to volume of data to stream (steps are the same). Select “Devices” from the left menu, if neither are available. Once connected, select the computer in question and select Launch. Once in the Link Menu Lobby, select the Desktop icon from the menu bar below. Once the entire computer desktop is visible to the user, move onto the next step.
5. Select Run (play button) located in the top-center toolbar of the Unity Editor window. Wait for application to load.
6. Begin Pol-XR use by loading into the HomeScene and selecting a scene from the DEM dropdown menu and radar flights to visualize from the dropdown menu. Select load scene when ready to load scene. *Please Note:* flight lines from Greenland and Antarctica are contained in one menu item. If flight does not load it may be because it is data from a different hemisphere. Try again, or identify a flight of interest ahead of time.

### How-To Generate Assets for Deployment
In order to generate additional assets for Pol-XR use, the user is required to select the region of interest ahead of time. This portion of the pipeline is targeted at Polar Researchers who are developing a regional case study. It is simple and accessible to non-academic/research users, despite not being the stakeholders of the project.

[How To Build the Pol-XR App - V2](https://docs.google.com/document/d/14tePFAZo64yKGk1ZaOYWqmi1WKJFhpcVagegzImrQYU/edit?usp=sharing) <- Awaiting transposition for GitHub

<br />

# Project Architecture

The application contains two scenes:
* homescreen-no-mrtk
* generic-no-mrtk

The scenes are named with "no-mrtk" in the title currently as a development indicator. Previous iterations of Pol-XR used MRTK for all scene interactions. Now the application uses XR Toolkit.

The Homescreen is a small scene with a cabin based on the Cape Evans and Discovery Point Huts built by early Antarctic explorers. The User is met by the Data Loader Menu, where they select the assets they would like to load into the Generic scene.

The Generic Scene is an "empty" scene in which the selected assets are loaded into the scene. The Assets contain real-world projected coordinates and so will not appear centered at 0,0. The camera/user is moved to the centroid of the assets. The centroid is calculated during asset creation.

<br />

### HomeScene

The HomeScene scene uses the following assets.
| Asset Name | Source | Description |
| :-----------: | ----------- | ----------- |
| HomeSceneAssets |  | Cozy cabin for user comfort during asset loading |
| Data Loader Menu |  | Menu populated by HomeMenu.cs with DEM Dir Names and FlightLine Directory Names |

<br />

### Generic Scene

The Generic scene uses the following assets.
| Asset Name | Source | Description |
| :-----------: | ----------- | ----------- |
| Surface DEM | BedMachine | Digital elevation model of glacier surface |
| Base DEM | BedMachine | Digital elevation model of bedrock |
| Flightlines | IceBridge (CReSIS) | Polylines corresponding to where the plane flying the radar system went above the surface |
| Radargram meshes | IceBridge (CReSIS) | Triangle meshes with radargrams mapped as texture |
| Surface Texture 1 | Sentinel Mosaic Imagery or Landsat Mosaic Imagery | Satellite imagery of ice surface |
| Surface Texture 2 | MEaSUREs Annual Velocity | Color-scale image of annual velocity of ice surface gridded data product |
| Base Texture 1 | BedMachine | Color height map of bedrock elevation |

<br />

In this scene, the flightlines and radargrams are generated at runtime as GameObjects rather than living permanently in the scene. The flightlines are rendered as polylines and are broken up into segments during preprocessing. Each radar object is modeled as a triangle mesh, textured with a radargram, and linked to the associated flightline portion. Using meshes enables the entire flightline to be displayed. The flightline coordinates are accurate within the projected coordinate system but the vertical coordinate is snapped to the surface DEM for ease of viewing.

<br />

### Asset Generation

The data pipeline involves generating mesh objects from data assets, semi-programmatically. For how the data pipline functions, see the ReadMe in the directory called /data_processing/.

| Script Name | File Location | Description |
| :-----------: | ----------- | ----------- |
| run.py | data_pipeline | Calls the necessary conversion script for the desired asset, and uses the states EPSG code (default 3413) |
| mat_to_mesh.py | data_pipeline | Converts MAT-files to Mesh OBJ format, and converts the coordinate system from geographic to the projected coordinate system stated in the the run.py command |
| dem_to_mesh.py | data_pipeline | Converts GeoTIFF to Mesh OBJ |
| fetch_cresis_flightlines.py | data_pipeline | Remotely accesses and downloads CReSIS flight data through URL in the run.py command. |


The radargram objects are generated in the following way. In preprocessing, the plots are generated from MAT-files and converted into the three separate files by the _mat-to-obj.m_ script that is called by run.py within /data_processing/. 

* .obj (contains the geometry of the mesh)
* .mtl (the material file for the mesh)
* .png (the radargram; this is mapped onto the mesh)

The .obj files are generated as 2-triangle quads for every chunk of data per radar frame. CReSIS radar commonly contains 30 seconds to 1 minute of overlap at the beginning and end of each frame/segment of flight data. This data conversion is a 1-to-1 conversion and does not compensate for the overlap, as such the edge overlap is visible in the application. 

These simplified .obj files, along with the .mtl and .png files, are automatically to the Assets folder within a directory named after the flight. Within that directory, each frame/segment of radar mesh trio is housed in a directory named after its segment number. Upon loading the scene, the _LoadFlightlines.cs_ script programmatically generates meshes from the file triples and associates each textured mesh with the corresponding flightline polyline. Users can select a flightline portion to load a mesh.

All DEM and Flightline assets are staged in /StreamingAssets/ and/or the user's PermanentPath (depending on whether the application is being deployed via standalone headset or tethered to a headset and running from the Unity Editor. To stage newly created files, the user needs to run _GenerateMenifest.cs_ from the Unity Editor toolbar. Once the Manifest is complete, _data_loader.cs_ will stage the Assets based on whether the user is building to Android or not.

### Code files

| Filename | Directory Name | Status | Description |
| :-----------: | ----------- | ----------- | ----------- |
| CSVReadPlot | Scripts | Deprecated | Reads flightlines from CSV files into the scene from deprecated workflow |
| CameraController | Scripts | Active | ... |
| ConsoleToGUI | Scripts | Active | ... |
| DataLoader | Scripts | Active | Stages and loads in Assets selected by the user via the HomeMenu |
| DrawGrid | Scripts | Deprecated | Generates a graticule grid in the scene from a deprecated workflow |
| DynamicLabel | Scripts | Active | Manages on-the-fly radar menu updates |
| Events | Scripts | TBD | ... |
| FlightlineInfo  | Scripts | Active | ... |
| HomeMenuController | Scripts | Active | Provides GUI for user to select Assets for DataLoader |
| HomeMenuEvents | Scripts | Active | Handles events in the Home Menu scene |
| HomeMenuToggle | Scripts | Active | ... |
| HoverLabel | Scripts | Deprecated | Manages on-the-fly radar image tick-marks |
| InputManager | Scripts | Deprecated | ... |
| InputHandler | Scripts | Deprecated | ... |
| LoadFlightLines | Scripts | Active | Generates in-game radargram meshes from obj/mtl files |
| LinePicking | Scripts | Active | ... |
| LinePicking | LinePicking | Active | ... |
| Measurement | Scripts | Active | Calculates distances used in Measurement Mode |
| MarkObj | Scripts | Deprecated | Handles events associated with the mark (cursor) object for 2D mesh planes associated with deprecated workflow |
| MarkObj3D | Scripts | Active | Handles events associated with the mark (cursor) object for curved meshes |
| MenuController | Scripts | Active | ... |
| MenuEvents | Scripts | Active | Handles generic events associated with menus |
| MinimapControl | Scripts | Active | Generates overhead camera projection of entire scene. Click teleportation interaction deprecated but marked for reintroduction |
| MinimapFollowUser | Scripts | Active | Minimap Camera follows user within the scene. Red dot marks user location within FOV of Minimap camera |
| MyRadarEvents | Scripts | Paused | ... |
| Mode | Scripts | Active | Allow user to define Mode within the scene. Trigger Measurement Mode (active), Layer Picking (active), and Study Scene (dev paused) |
| OptimizedOBJLoader | Scripts | Active | Pre-loads OBJ meshes and stages Assets for latency reduction with large datasets/scene assets |
| PalmUpDetection | Scripts | Paused | Hand tracking Radial Menu functionality (DEPRECATED) |
| PreserveRadargrams | Scripts | Paused | Study Scene snapping to orient selected radargrams with respect to one other by preserving their geometry |
| RadarEvents2D | Unused | Deprecated | Handles events specific to the (2D) radargram planes from deprecated workflow |
| RadarEvents3D | Scripts | Active | Handles events specific to the (3D) radargram meshes |
| RadarEvents | Scripts | Active | Handles events associated with radargrams that are the same in both scenes |
| RadialMenu | Scripts | Paused | ... |
| RadialSelection | Scripts | Paused | ... |
| ScaleConstants | Scripts | Active | ... |
| SceneManagerScript | Scripts | Active | ... |
| SnapRadargramManager | Scripts | Paused | ... |
| StudySceneManager | Scripts | Paused | ... |
| SubmenuDragHandle | Scripts | Active | ... |
| UpdateLine | Scripts | Active | Redraws the line between the Mark and Measure objects |


<br />

# Controls

VISER currently works with VR (Quest) headsets, with previous iterations functioning on AR (HoloLens) headsets. Work to reintroduce optimized gesture and voice controls required for AR deployment is slated for the next version. In this section we describe the VR controls available in this version of the application.

### Universal Controls 

Here is a list of interactions available everywhere using the Oculus Controllers:

| Interaction | Description |
| :-----------: | ----------- |
| Trigger Buttons | Used to interact with the menus and select radar images |
| Joysticks | Used to move around the entire scene freely |
| Bumper Button | Used to move vertically up and down in the scene |

Movement can be accomplished with the joysticks. Tilt forward to shoot a ray; if the ray is white, release the joystick to teleport there. You can also nudge yourself back by tilting back on the joystick.

The scene bounding box can be used to scale everything inside the scene along any of the three axes. Users can grab the bounding box from the corners or the center of any edge and use standard MRTK manipulation to adjust the box's size.


| Main Menu Interaction Title | Description |
| :-----------: | ----------- |
| Scene Vertical Scaling | Scales everything in the scene along the vertical (Y) axis |
| Radargrams | Checking this button turns all the radargram meshes on/off |
| Bounding Box | Toggles the bounding box around the scene |
| Flightlines | Checking this button turns the flight lines that are currently loaded on/off |
| Surface DEM | Checking this button turns the Surface DEM on/off |
| Base DEM | Checking this button turns the Base DEM on/off |
| Minimize Menu | Minimizes the Main Menu panel and stores it in a wast high docked panel |

Here is a list of interactions available with the radar menu. These are all specific to the 3D workflow.
| Radar Menu Interaction Title | Description |
| :-----------: | ----------- |
| Rotation | Rotates the radar mesh by the seleced amount of degrees from its initial orientation |
| Exaggeration Along Axes | Stretches or shrinks the radar image (the exaggeration is dynamic and can be repeated limitlessly); the sliders are in the order Y (vertical), X (horizontal) |
| Transparency | Makes the radar image transparent by the selected percent for qualitative comparison |
| Radargram Mesh | Checking this box turns the radargram mesh on/off |
| Surface DEM | Checking this button turns the Surface DEM on/off |
| Picking Mode | Enables radar horizon picking, which allows the user to digitally trace bright features visible in the radargram |
| Minimize Menu | Minimizes the Main Menu panel and stores it in a wast high docked panel |


<br />

### Radar Layer Picking
Scientists interpret ice-penetrating radar by tracking reflectors/horizons in visualized radar data. The reflectors are the interface between ice and something else (bedrock, ash, different ice, etc.) and tracing their structures throughout a region allows researchers to develop an understanding of ice thickness and structure. They can use these measurements along with other geospatial data (i.e. surface imagery, velocity grids, digital elevation models) to predict change and understand historical change. Layer picking is traditionally a 2-D process, looking at one frame at a time with little-to-no geospatial context. This layer picking feature intends to provide an alternative for these researchers.

When Layer Picking mode is enabled, the user can point the controller at the radargram and click with the trigger button on the controller. A seed point will be created at the point where the ray caster collides with the radargram mesh. The UV coordinates of the pixel are collected and then the tracing logic proceeds to generate a line along the radargram using the brightness of the next pixel - this brightness threshold is a user defined window, which can be adjusted at any time.

Recent updates allow the user to push and hold the trigger and trace the target reflector with motion (e.g. painting). Before letting the trigger button go, the user has an opportunity to “backspace/undo” a mistaken pick by moving the ray caster back (Left) to a position where the traced pick looks good and continuing forward (Right).

| Radar Picking Script Title | Description |
| :-----------: | ----------- |
| CoordinateUtils |  …  |
| GeometryUtils |  …  |
| LineGeneration |  …  |
| LinePickIndicatorPoint |  …  |
| LinePickUtils |  …  |
| LinePickingPointInfo |  …  |
| LineRendererUtils |  …  |
| PickLine |  …  |
| RadargramMeshUtils |  …  |
| TextureUtils |  …  |
| ToggleLinePickingMode |  …  |

<br />

### DEPRECATED Voice Commands

In the HoloLens compatible version of this application, voice commands could be used at any time and do not need to be toggled. Users simply said the word clearly and Pol-XR would process the command. It is our intention to re-integrate these accessibility features to enable HoloLens deployment again.

| Voice Command | Description |
| :-----------: | ----------- |
| "Menu" | Open/close the menu |
| "Model" | Turn on/off the DEM models |
| "Image" | Turn on/off the image for the selected radar line |
| "Line" | Turn on/off the CSV picks for the selected radar line |
| "Go" | Teleport to the image location for the selected radar line |
| "Mark" | Add a point to the image for the selected radar line |
| "Reset" | Reset the radar lines for the whole scene |
| "Measure" | Turn on/off measurement mode |
| "Box" | Turn on/off the bounding box for the entire scene |
