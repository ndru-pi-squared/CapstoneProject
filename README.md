# Armament

README

This includes libraries and examples for Armament, a 3D fps game built with Unity game engine, Photon Networking API, and PlayFab backend.
 
Armament has cross-platform build options that take into account what type of device is currently running the code in order to make optimizations for that device. 

NEW FEATURES 

Space arena 
    Smart A.I. 
    Mobile overlay
    Multiple types of weapons and items
    Leaderboards
    Friends
    Chat
    Authentication
    User accounts
    HUD & custom UI sprites
Arena select
Team preference select 
Team auto-balancing
First and third person view renderings
Animations for player avatars
Team wins round notification
Latency and ping notification
Host migration
Master Client UI indicator (“-MC” appended to player nickname field).



KNOWN BUGS
    
Round ends upon the death of one player on either team, regardless of team size.
    A.I. has been shown to fail to locate a target (gun) when toggled.
    Mobile chatbox has no send message button, but can receive messages.
    Guns can sometimes become invisible for Windows builds.
Building or pushing/pulling with git sometimes strips prefabs from the GameManager in Space_Arena scene or Simple Room. Click on GameManager within the scene, and check to make sure the SerializeFields and public fields are set to their proper GameObjects. The player prefabs require two My Kyle Robot FPS Controller prefabs (one blue, one red). The weapons prefabs in Simple Room’s GameManager require Gun 1, Gun 2, FragGrenade, Medkit. Space_Arena requires the various space guns as well as medkits and grenades. The Original Dividing Wall should be set in Simple Room, and Copy - Original Dividing Wall should be set in Space Arena. Another public field that gets stripped sometimes is the Medkit prefab on the My Kyle Robot FPS Controller, and this should be set with Medkit.prefab. 
Some git LFS files become un-stashable changes to particular files. If this happens, try git lfs uninstall -> git reset --hard -> git lfs install.

INSTALLATION AND BUILD SETTINGS

To build Armament, a copy of the Unity Game Editor must be installed onto the device. 
https://unity3d.com/get-unity/download

Once Unity is installed, it may prompt you to set up an account, which is required in order to maintain your Unity projects. Once you’ve created an account and logged in, clone the git repository onto your local machine. When that is complete, open up Unity and select the cloned project directory location using the browser, which will open up the Unity project in the Unity Editor. The Unity project is equipped with various APIs, libraries, and assets we’ve compiled and made into our own. If you are using Git, make sure you initialize git LFS to handle 100mb+ pushes/pulls.

In order to build executables for various platforms, different build settings must be taken into consideration based on the needs of the developers and for the particular build platform. There are also Unity-native player settings that can affect builds as well, which can be accessed through the build menu (file -> build settings -> player settings). They are worth exploring, but not worth mentioning in any further detail. For any build, regardless of platform, the scenes must be added into the build through the build settings. If a scene is not active in 

BUILDING

WINDOWS
I believe the default platform is either Windows or Mac standalone depending on what platform the developer is using. If need be, open up the build settings (file -> build settings) and swap to the Windows platform. Click the Build button, and select a directory. The Unity Engine will begin building an executable file of Armament with supporting Mono framework and Unity Engine files all in one directory. 

MAC OSX
To build for Mac OSX, simply open up the build settings and make sure the platform target build is set to Mac. Select the desired storage location and click Build-- Unity will being building a client that can be run on Mac OSX. 

ANDROID
With the project open in the Unity Editor, navigate to the Mobile Input menu at the top, and enable mobile input. This will prompt the cross-platform build settings to kick in and recognize the particular code enclosed in tags that mark the beginning and end of mobile platform behavior within the code. In addition to enabling mobile input, the target build platform must be swapped from the default to Android. Switching platforms, especially on a Windows PC, can take quite a long time (~30 minutes with 8GB RAM). 


IOS
(You will need an Apple account to build for iOS)

With the project open in the Unity Editor, navigate to the Mobile Input menu at the top, and enable mobile input. This will prompt the cross-platform build settings to kick in and recognize the particular code enclosed in tags that mark the beginning and end of mobile platform behavior within the code. In addition to enabling mobile input, the target build platform must be swapped from the default to iOS. Switching platforms, especially on a Windows PC, can take quite a long time (~30 minutes with 8GB RAM).

Once a build file has been created, open the file in XCode (on Mac). Connect your iPhone to the mac and click the target (Unity-iPhone). Set the deployment target to whatever iOS build you like (we use 12.1). Set the build target to be your iPhone. Make sure the bundle identifier is Kabaj. Make sure “Automatically manage signing” is toggled on, and click your created personal team. Then click the Play button in the top-left corner of the XCode window to begin building. You will be prompted to enter your password once or twice before the process finishes. You will likely also need to give permission to load apps from your developer team on your iPhone. Once all that is finished, you can run Armament on your iPhone.

PLATFORM BUILDS -- LIGHTING SETTINGS

The lighting options tends to affect build time by an exponential factor. There are three properties that affect build time related to lighting: AutoGenerate Lighting, located at the bottom of the settings page, Baked Global Illumination, and Realtime Global Illumination, which are both midway down the settings page. These attributes can be accessed via the Window menu (window -> rendering -> lighting settings). 

Turning off all of the aforementioned properties results in the fastest build time, but produces a dark, unlit game arena. This is adequate for quick tests such as synchronization testing, which requires building lots of clients, as well as slower computers. Not only will the build time be faster, but the runtime will perform much better than if it had complex lighting within the scene.

The next-fastest build setting (with respect to lighting) is to only select AutoGenerate Lighting. This will increase build and render times slightly, but the tradeoff is negligible when you can actually see the game arena. 

Realtime and Baked Global Illumination should only be selected when the workload to build the Global Illumination can be adequately handled by a powerful device. 8GB of RAM would be ideal (at a very minimum) to build using these settings. With 8GB of RAM it can take upwards of four hours to bake the scene’s global lighting. 
