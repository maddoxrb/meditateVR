# Sole Survivor
<img width="1024" height="1024" alt="image" src="https://github.com/user-attachments/assets/d03bbac1-182d-4bbc-8e3e-e57baf2da70c" />

# Overarching Epics
## 1. Player Movement
- Add in VR Interactions and Walking
- Add in Sprinting Mechanics
- Adjust Physics to Support Low Gravity Jumps

## 2. Scene Development
- Playable Map of Mars
- Proper Constraints to Prevent Escaping the Map
- Adjust Physics to Alter Gravity Strength
- Limit Mesh Colliders to Improve Performance

## 3. Martian Enemies
- Animated Martian Enemy Model
- AI Implemented to Chase Nearest Player
- Attacks Animated and Deal Damage to Player
- Enemy Health Affected by Ray Gun

## 4. Gun
- Properly Sized and Interactable Gun Model
- SFX and Shooting Mechanics Implemented
- Projectiles Deal Damage to Enemies
- Shooting Controls Mapped to Controller

## 5. Networking
- Join Page Created
- Players Can Join a Server

# Sprint 5

## What Was Added?
1. **Movement**: Removed the Jetpack and replaced with low gravity jumps, and reduced sprint speed to limit motion sickness. 
2. **Environment**: Added new start game UI to the starting scene, alongside instructions for onboarding new players. Tweaked SFX to better fit environment and sync with real time interactions.
3. **Enemies**: Added in three new martian enemy versions, updated navigation algorithms to support multiplayer, enemies consistently pull the game environment for the closest Player Object and will adjust their navigation accordingly. 
4. **Weapons**: Added in burst fire to the mp5 model, fixed incorrect hand poses and grab points for both the mp5 and pistol.
5. **Networking**: Added in player spawn networking, alongside a new spacesuit player model wth custom animated hands. Overhauled game logic to fully support multiplayer networking. UI screens, martian enemies, guns, and player prefab are now all networked. game logic has been updated such that any players who have died can be revived if another active player survives a wave. Updated martian enemies to use MecaAnimation component so that animations are synced across players as well. All dynamic components are synced to the host authority, and all static components (scene objects, etc) are local only. 


## Progress By Epic
## 1. Player Movement
- ✚ Added low-gravity jumps and decreased sprint speed for motion sickness
  
## 2. Scene Development
- ✚ New help UI and start screens
- + Updated audio and SFX

## 3. Martian Enemies
- ✚ 3 new martian enemy variants, spawned at random
- + Martians networked and animated over the network

## 4. Gun
- ✚ Updated mp5 to support burst fire mode

## 5. Networking
- + All dynamic objects now networked
- + Enemy spawn and wave logic hosted by one authority and synced to all players
- + Guns, enemies, and enemy deaths network synced
- + New player prefab and spawn logic added

  
## Pitch Video
[Video](https://drive.google.com/file/d/1gpJb3w7YjE2ujNXxBBe4JinIacngG6k8/view?usp=sharing)

## Build APK
[APK](https://drive.google.com/file/d/1ZMffTKipgJCnQBlwru4eV1TmKWMdebvW/view?usp=sharing)


# Sprint 4

## What Was Added?
1. **Movement**: Physics were tweaked to create a smoother feel to player movement, including jetpack, walking, and sprint.
2. **Environment**: Overhauled environment to create a more visually interesting atmosphere. Added new terrain with more complex sand dunes and a more realistic texture. Edited lighting configuration to dim the atmosphere and apply a reddish hue. Added in colored fog to create a darker more mysterious environment. 
3. **Enemies**: Added in a new more reralistic enemy model with animations to match the environment improvements. Tweaked AI navigation and terrain navmesh. Improved damage detection to more accurately diminish player health when attacked by martian enemies 
4. **Weapons**: Expanded the number of available weapons in game. Added in AR, MP5, Shotgun, and Rocket Launcher models all equipped with code to spawn and fire bullets. Most models now have hand poses that allow for a natrual lock onto the handle when grabbed, further refinements to come in sprint 5.


## Progress By Epic
## 1. Player Movement
- ✚ Tweaked jetpack and sprint physics
  
## 2. Scene Development
- ✚ Modified Start Scene
- ✚ Added colored ambient environment fog
- ✚ Added new sand terrain with dunes and valleys

## 3. Martian Enemies
- ✚ Added new martian model with animations
- ✚ Refined AI navigation and Navmesh surface

## 4. Gun
- ✚ Added: AR, MP5, Shotgun, & Rocket Launcher models
- ✚ Implemented firing and bullet spawning logic for each model.
  
## Progress Video
[Video](https://drive.google.com/file/d/1gpJb3w7YjE2ujNXxBBe4JinIacngG6k8/view?usp=sharing)

## Build APK
[APK](https://drive.google.com/file/d/1ZMffTKipgJCnQBlwru4eV1TmKWMdebvW/view?usp=sharing)



# Sprint 3

## What Was Added?
This sprint was oriented towards setting up the general gameplay logic, improving movement and interactions, and starting networking.
1. **Movement**: One longstanding issue we had was that gravity and movement affects caused held objects to lag behind the hand, creating a very poor user experience. We tried multiple different methods to fix, for instance using a unity event trigger to turn gravity on and off on the held object, but none of these worked. Ultimately, we had to create a new scene with base physics, and completely overhaul our original player prefab to reset any possible issues with the interaction and camera rig. We migrated all of our movement logic to the First Person Locomotor and increased the number of physics parameters to be able to fine tune the feel of the motion options (sprint and jetpack).
2. **Environment**: Added in environment fog to increase the immersion of the martian atmosphere. Additionally added in randomly generated asteroids that will fall to the ground and trigger an explosion effect, in the next sprint we will add logic to cause damage to nearby players. Additionally, we added in a starting scene that will host the start game logic. 
3. **Enemies**: Further refined the AI logic and Nav Agent mappings to make more error-resistant enemies. Additionally, added an EnemySpawner object which controls the main gameplay flow. Currently, there are 5 waves, each wave begins with a UI splashscreen that displays the wave number, and then a set number of enemies will spawn at an increasing rate at each wave. For now wave 1 has one enemy, wave 2 two, and so on. A wave is complete when all of the enemies are killed by players.
4. **Player Health**: Players now take damage when attacked by martian enemies. An increasing red vignette will indicate current player health, and after all player health is gone, a revive splash screen will be displayed.
5. **Networking**: Added in networking to the gun asset to ensure bullets and gun movement is seen for all players. 

## Progress By Epic
## 1. Player Movement
- ✚ Fixed object lag
- ✚ Overhauled jetpack and sprint logic
- ✚ Added player health and death UI
  
## 2. Scene Development
- ✚ Added start scene
- ✚ Added ambient environment fog
- ✚ Added random asteroid impacts
- ✚ Added wave UI splashscreens

## 3. Martian Enemies
- ✚ Added wave spawn logic
- ✚ Further refined AI navigation

## 4. Gun
- ✚ Added networking

## 5. Networking
- ✚ Added gun & bullet networking
  
## Progress Video
[Video](https://drive.google.com/file/d/1xzsVCwhVBPrTrb2JKVdJYCSM2jhwh-vU/view?usp=sharing)
## Build APK
[APK](https://drive.google.com/file/d/1HCQyUzNWgOIXVnyfa7RXT4cpz7g4lycS/view?usp=sharing)



# Sprint 2

## What Was Added?
This sprint was oriented towards finalizing player movement controls, expanding and improving the environment, and setting up enemy interactions.
1. **Movement**: Two new movement methods were introduced in this sprint. First, holding down the left joystick allows the player to sprint. We plan to add particle effects to this motion in the future, and potentially a limit, but this will be dependent upon how we feel about the gameplay mechanics down the line. Additionally, holding down the right joystick will allow a player to thrust upwards via their jetpack. This feature has sounds and a particle system attatched. We are still playing around with the speed/cooldown, however this is all adjustable via the inspector. 
2. **Environment**: Given that our new movements allow for increased speed and distance of travel, we wanted to expand the playable map to accompany more enemies and players. We introduced a ring structure of mountains to prevent players from falling off the map, added in new space-themed prefabs, and enhanced the atmosphere with new SFX
3. **Enemies & AI**: Our initial AI had some issues, which we have worked to improve in this sprint. One issue we had was that AI agents were floating across the map, so we re-baked the navmesh on the map and set the `Use Geometry` setting to physics, which helped to align the navmesh with the elevations of the actual ground. Additionally, we tweaked the agent settings by increasing height, max slope, and step-height to support movement upwards on angled surfaces. We also adjusted our AI script to ensure that agents continually faced the player. There is still more work to be done to further refine the AI aspect. We also added in attack and death animations to the enemies. Attacks are triggered when enemies are within a certain proximity of the player, and deaths occur when an agent is collided with by an object above a certain force threshold.
4. **Gun**: This sprint also introduced our first weapon, a pistol that can be grabbed and fired using the trigger button of the hand currently holding the object. The gun has been set up to display an animation when fired, as well as shoot a projectile out. We are still working to improve the grab placement of the gun.
5. **Player Model & Loading Screen**: Added visual player representation and scene transition system. For scene transitions, we created a dedicated LoadingScene with a VR-compatible UI canvas featuring a progress bar and "Loading..." text displayed in world space. Implemented the LoadingManager script to handle asynchronous scene loading with visual progress feedback, ensuring smooth
  transitions between game scenes. 

## Progress By Epic
## 1. Player Movement
- ✚ Sprint Mechanic (Left Thumbstick)
- ✚ Jetpack Mechanic (Right Thumbstick)

## 2. Scene Development
- ✚ Expands playable map space for new movement controls
- ✚ Environment SFX Added (Game Music, jetpack, etc)
- ✚ Jetpack Particle System Added

## 3. Martian Enemies
- ✚ Navmesh improved to cover scene
- ✚ Navagent Model tweaked to allow AI agents to climb sloped surfaces
- ✚ Navagent parameters tweaked to prevent agent spin, and keep line of focus directed on player
- ✚ Martian enemy attack animation added when in proximity of player
- ✚ Martian enemy death animation added, occurs when a configurable collision force is detected on the enemy

## 4. Gun
- ✚ Gun prefab added with animations and sound
- ✚ Shooting script added that generates and sends a bullet in the direction of the gun
- ✚ Bullets can trigger enemy death animation

## 5. Networking
...

## Progress Video
https://drive.google.com/file/d/1wVwT3teqShavH_SNcHPdWOesL8cijl31/view?usp=sharing
## Build APK
https://drive.google.com/file/d/1F1ON5CXfdKleUZUxAtMNkgZLpOwUHPD4/view?usp=sharing

# Sprint 1

## What Was Added?

This sprint was designed to get a few basic requirements underway so that we could begin refining, testing, and developing as soon as possible. 
1. **Movement**: An intitial player object was created that includes the `PlayerController`, and `FirstPersonController` scripts. This allows us to move the player in Unity via arrows/WASD for testing, and additionally move in VR either physically or through the use of the left thumbstick. Additionally, the Camera Rig and Interactions building blocks were added to set up the first-person camera perspective and object grabbing. One issue we ran into was that when you first add the Camera Rig and then add the Interactions building block, both components include prefabs for the controllers. This led to very glitchy movement, so to fix this we removed the controller prefabs from the Camera Rig and rely solely on those in the Interactions block.
2. **Interactions**: As mentioned above, we added the interactions rig which allows us to use our controllers to pick up grabbable objects and throw them around with physics enabled. One issue we ran into was that a grabable object, when touched to the player, would enact a force upon the player and throw them backwards. To fix, we implemented three seperate collision layers: scene, player, and grabbable, and enforced that grabables and player layers could not interact. Additionally, we added in a raygun prefab and altered its properties so that it too could become interactable.
3. **Environment**: We set up an initial map of Mars using the `PolyAngel_SpacePack` that will act as our main playing ground. We added in colliders to all necessary elements and experimented with some other SFX like dust particles. One thing we discovered through research is that mesh colliders tend to be more resource intensive, so where applicable we will prioritize box/capsule colliders if it does not have a major effect on player experience.
4. **Enemies & AI**: Additionally, we located an alien prefab that will act as our enemies for the game. We were able to get a basic walking animation complete. Additionally, we added in a basic script to follow the player. There are some issues with the enemy movement in regards to angled surfaces that will need to be worked out, but for now we have a basic starting place. We have these updates, while in development, in a seperate scene from `Mars_Main`.

## Progress By Epic
## 1. Player Movement
- ✚ XR Camera Rig Added
- ✚ XR Interactions Rig Added
- ✚ Player Movement in VR via Controller

## 2. Scene Development
- ✚ Initial Map Created
- ✚ SFX In Testing

## 3. Martian Enemies
- ✚ Alien Prefab Added to Project
- ✚ Walking Animation Added
- ✚ Basic Tracking AI Added

## 4. Ray Gun
- ✚ Ray Gun Prefab Added
- ✚ Prefab Made Interactable

## 5. Networking
...

## Progress Video
https://www.loom.com/share/34699570500e498397ed2b023c2b2cdc?sid=a3b1d842-2174-484f-86b7-410f54adb888



# 📒 Project Notes and Findings (Updated As Discovered)

## 🎯 Colliders
- Adding a **Mesh Collider** to a prefab won’t directly work if the prefab has sub-components.  
- Either use a **Box Collider** or add a mesh collider to each sub-component individually.

## 🏗️ Building
- When building with multiple scenes:  
  - Go to **File → Build Settings → Scenes**.  
  - Move the scene you want to load **to the top of the list**.

## 🎮 Grabbable Items
1. On Mesh:  
   - Turn **off** `isTrigger` (prevents falling through floor)  
   - Turn **on** Gravity  
   - Turn **off** Kinematics  
   - Change **Collision Detection** from *Discrete* → *Continuous*  

2. To prevent objects from “pushing back” and sending the player flying:  
   - Create **3 layers**: Player, Scene, Grabbable  
   - Go to **Project Settings → Physics → Collision Matrix**  
   - Uncheck the interaction between **Grabbables ↔ Player**

  
# Team Roles

---

## Maddox Barron  
- **CS Major**
- **Agile Board Director**
- **XR Movement & Interactions**  
- **Physics Development**
- **Playtester**

---

## Landon Prince 
- **CS Major**
- **GitHub Manager**  
- **Model Animation**
- **Playable Environment Development**
- **Playtester**

---

## Nick Bui  
- **CS Major**
- **Networking Lead**
- **Backend Game Logic**  
- **Playtester**

---

## Matthew McCullough 
- **CS Major**
- **Audio/Visual Designer**
- **In-Game UI Design**
- **Playtester**
