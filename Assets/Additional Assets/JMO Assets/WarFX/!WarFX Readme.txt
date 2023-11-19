War FX, version 1.8
2019/03/20
© 2019 - Jean Moreno
====================

PREFABS
-------
Effects are located in the "_Effects" folders, and "_Effects (Mobile)" for mobile versions.


CARTOON FX EASY EDITOR
----------------------
Open the editor in the menu:
GameObject -> CartoonFX Easy Editor

Change the options of the editor, select the GameObject(s) you want to change, and press the corresponding buttons in the editor to apply the changes.


CARTOON FX SPAWN SYSTEM
-----------------------
CFX_SpawnSystem allows you to easily preload your effects at the beginning of a Scene and get them later, avoiding the need to call Instantiate. It is highly recommended for mobile platforms!
Create an empty GameObject and drag the script on it. You can then add GameObjects to it with its custom interface.
To get an object in your code, use CFX_SpawnSystem.GetNextObject(object), where 'object' is the original reference to the GameObject (same as if you used Instantiate).
Use the CFX_SpawnSystem.AllObjectsLoaded boolean value to check when objects have finished loading.


TROUBLESHOOTING
---------------
* Almost all prefabs have auto-destruction scripts for the Demo scene; remove them if you do not want your particle system to destroy itself upon completion.
* Some effects might lack luminosity in Linear Color Space (Unity Pro only); you can correct this issue on a case by case basis by increasing either the Tint Color of each Material, or the Start Color on the effect's Particle System Inspector.


PLEASE LEAVE A REVIEW OR RATE THE PACKAGE IF YOU FIND IT USEFUL!
Enjoy! :)


CONTACT
-------
Questions, suggestions, help needed?
Contact me at:
jean.moreno.public+unity@gmail.com

I'd be happy to see any effects used in your project, so feel free to drop me a line about that! :)


UPDATE NOTES
------------
1.8.04
- Removed 'JMOAssets.dll', became obsolete with the Asset Store update notification system

1.8.03
- fixed small API deprecation (Unity 2017.4+)

1.8.02
- updated demo scene to use Unity UI system

1.8.01
- Cartoon FX Easy Editor: added "Hue Shift" slider
- Cartoon FX Easy Editor: improved UI
- CartoonFX Easy Editor: fixed scaling for Unity 2017.1+

1.8
- War FX is now free!
- Unity 2017.1 minor fixes
- all effects can now be scaled using the Transform component
- updated CartoonFX Easy Editor (bug fixes)

1.76.1
- Unity 5.5 compatibility

1.76
- fixed Spawn System property 'hideObjectsInHierarchy' not being saved properly
- added more options to the CFX Spawn System:
	* "Spawn as children of this GameObject": will spawn the instances as children of the Spawn System GameObject
	* "Only retrieve inactive GameObjects": will only retrieve GameObjects that are inactive
	* "Instantiate new instances if needed": will create new instances when no inactive instance is available

1.751
- removed Soft Particles for bullet holes

1.75
- updated demo scene
- fixed missing scripts

1.74
- fixed deprecated method warning in Unity 4.3+

1.73
- updated "JMO Assets" menu
- removed WFX_ParticleMeshBillboard, ends up being too expensive for mobile CPUs

1.72
- fixed warning in Unity 4.3+ for Spawn System editor

1.71
- fixed CFX_SpawnSystem not being set dirty when changed
- replaced WFX_AutoDestructShuriken with CFX_AutoDestructShuriken so that WarFX can be imported along the Cartoon FX packs
- using the same CFX_SpawnSystem as the Cartoon FX packs now

1.7
- updated CFX Editor
- updated max particle count for each prefab to lower memory usage
- removed all Lights for Mobile prefabs

1.61
- updated CFX Editor

1.6
- updated CFX Editor
 (now in Window > CartoonFX Easy Editor, and more options)
- added JMO Assets menu (Window -> JMO Assets), to check for updates or get support

1.52
- fixed other Unity 4.1 incompatibilities

1.51
- fixed fog for a shader

1.5
- fixed Compilation error for CFX_SpawnSystem in Unity 4.1
- fixed Cartoon FX editor scaling, now supports "Size by Speed"

1.4
- inclusion of Cartoon FX Spawn System

1.3
- fix fog colors for some shaders
- better colors in linear color space

1.2
- fix compilation error with DirectX 11

1.1
- added a more realistic Flame Thrower (WFX_FlameThrower Big Alt)
- added Asset Labels to the prefabs


NOTES
-----
M4 Weapon model from Unity 3.5 Bootcamp Demo
Bullet Holes textures edited from Unity 3.5 Bootcamp Demo