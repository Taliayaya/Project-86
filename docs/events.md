# Events Documentation

This file contains all the events that are emitted and their description.

## Table of Contents

1. [All Events](#all-events)
2. [Sound Events](#sound-events)
3. [Game Parameters](#game-parameters)
    1. [Sound](#sound)
        1. [UpdateGameParameter:musicVolume](#updategameparametermusicvolume)
        2. [UpdateGameParameter:sfxVolume](#updategameparametersfxvolume)
4. [Game Events](#game-events)
    1. [General](#general)
        1. [OnPause](#onpause)
        2. [OnResume](#onresume)
    2. [Player](#player)
        1. [OnTakeDamage](#ontakedamage)
        2. [OnDeath](#ondeath)
5. [Input Events](#input-events)
    1. [Juggernaut Action Map](#juggernaut-action-map)
        1. [OnMove](#onmove)
        2. [OnFire](#onfire)
        3. [OnLookAround](#onlookaround)
       4. [OnZoomIn](#onzoomin)
       5. [OnZoomOut](#onzoomout)
       6. [OnPause](#onpause)
    2. [Pause Action Map](#pause-action-map)
        1. [OnResume](#onresume)
2. [HUD Events](#hud-events)
    1. [OnUpdateCompass](#onupdatecompass)
    2. [OnUpdateXRotation](#onupdatexrotation)
    3. [OnUpdateHealth](#onupdatehealth)
    4. [OnUpdatePrimaryAmmo](#onupdateprimaryammo)
    5. [OnUpdateSecondary{\[Left\] or \[Right\]}Ammo](#onupdatesecondaryammo)

## All Events

| Event Name                                                             | Type                                | Description                                                                              |
|------------------------------------------------------------------------|-------------------------------------|------------------------------------------------------------------------------------------|
| [OnPause](#onpause)                                                    |                                     | Emitted when the game is paused.                                                         |
| [OnResume](#onresume)                                                  |                                     | Emitted when the game is resumed.                                                        |
| [UpdateGameParameter:\[name\]](#game-parameters)                       | object *(depends on the parameter)* | Emitted when a game parameter is updated. The name of the parameter is in the event name |
| [OnMove](#onmove)                                                      | Vector2                             | Emitted when the player move input is pressed                                            |
| [OnPrimaryFire](#onprimaryfire)                                        |                                     | Emitted when the player primary fire input is pressed                                    |
| [OnSecondaryFire](#onsecondaryfire)                                    |                                     | Emitted when the player secondary fire input is pressed                                  |
| [OnLookAround](#onlookaround)                                          | Vector2                             | Emitted when the mouse moves (delta mouse)                                               |
| [OnZoomIn](#onzoomin)                                                  |                                     | Emitted when the player zoom in input is pressed (scroll wheel up)                       |
| [OnZoomOut](#onzoomout)                                                |                                     | Emitted when the player zoom out input is pressed (scroll wheel down)                    |
| [OnZoomChange](#onzoomchange)                                          | MechaController.Zoom (enum)         | Emitted when the player zoom changes (to update the UI)                                  |
| [OnTakeDamage](#ontakedamage)                                          | float                               | Emitted when the player takes damage                                                     |
| [OnDeath](#ondeath)                                                    |                                     | Emitted when the player dies                                                             |
| [OnUpdateCompass](#onupdatecompass)                                    | float                               | Emitted when the linear compass HUD angle needs to be updated (Y Rotation)               |
| [OnUpdateXRotation](#onupdatexrotation)                                | float                               | Emitted when the side bar angle needs to be updated (X Rotation)                         |
| [OnUpdateHealth](#onupdatehealth)                                      | float                               | Emitted when the health HUD needs to be updated                                          |
| [OnUpdatePrimaryAmmo](#onupdateprimaryammo)                            | int                                 | Emitted when the primary ammo HUD needs to be updated                                    |
| [OnUpdateSecondary{\[Left\] or \[Right\]}Ammo](#onupdatesecondaryammo) | int                                 | Emitted when the secondary ammo HUD needs to be updated                                  |

## Sound Events

## Game Parameters

### Sound

#### UpdateGameParameter:musicVolume

<table>
  <tr>
    <th>Name</th>
    <td>UpdateGameParameter:musicVolume</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the music volume is updated. The float value corresponds to the dB.</td>
  </tr>
    <tr>
        <th>Type</th>
        <td>float</td>
    </tr>
    <tr>
        <th>Range</th>
        <td>[ -80; -12 ]</td>
    </tr>
    <tr>
        <th>Emitters</th>
        <td>`SettingsMenu`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`SoundManager`</td>
    </tr>
</table>

#### UpdateGameParameter:sfxVolume

<table>
  <tr>
    <th>Name</th>
    <td>UpdateGameParameter:sfxVolume</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the sfx volume is updated. The float value corresponds to the dB.</td>
  </tr>
    <tr>
        <th>Type</th>
        <td>float</td>
    </tr>
    <tr>
        <th>Range</th>
        <td>[ -80; 0 ]</td>
    </tr>
    <tr>
        <th>Emitters</th>
        <td>`SettingsMenu`</td>
    <tr>
        <th>Listeners</th>
        <td>`SoundManager`</td>
    </tr>
</table>

## Game Events

### General

#### OnPause

<table>
  <tr>
    <th>Name</th>
    <td>OnPause</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the game is paused</td>
  </tr>
    <tr>
        <th>Emitters</th>
        <td>InputManager</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`SoundManager`, `GameManager`, `HUDManager`, `SettingsMenu`</td>
    </tr>
</table>

#### OnResume

<table>
  <tr>
    <th>Name</th>
    <td>OnResume</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the game is resumed after being paused (not called on start)</td>
  </tr>
    <tr>
        <th>Emitters</th>
        <td>`InputManager`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`SoundManager`, `GameManager`, `HUDManager`, `SettingsMenu`</td>
    </tr>
</table>

### Player

#### OnTakeDamage

<table>
  <tr>
    <th>Name</th>
    <td>OnTakeDamage</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emit when the player takes damage. The float value corresponds to the new health.
</td>
  </tr>
    <tr>
        <th>Type</th>
        <td>float</td>
    </tr>
    <tr>
        <th>Emitters</th>
        <td>`MechaController`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td></td>
    </tr>
</table>


#### OnDeath

<table>
  <tr>
    <th>Name</th>
    <td>OnDeath</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emit when the player dies. (Hp reached 0)
</td>
  </tr>
    <tr>
        <th>Emitters</th>
        <td>`MechaController`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td></td>
    </tr>
</table>


#### OnUpdateHealth


## Input Events

### Juggernaut Action Map

#### OnMove

<table>
  <tr>
    <th>Name</th>
    <td>OnMove</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the player press a movement key (WASD).
        The type is a Vector2 where x is the horizontal input and y the vertical input, negative values are left and down, positive values are right and up.    
</td>
  </tr>
    <tr>
        <th>Type</th>
        <td>Vector2</td>
    </tr>
    <tr>
        <th>Emitters</th>
        <td>`InputManager`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`MechaController`</td>
    </tr>
</table>


#### OnPrimaryFire

<table>
  <tr>
    <th>Name</th>
    <td>OnPrimaryFire</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the player press the fire key (Left Mouse Button).
    </td>
  </tr>
    <tr>
        <th>Emitters</th>
        <td>`InputManager`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`WeaponModule`</td>
    </tr>
</table>

#### OnSecondaryFire

<table>
  <tr>
    <th>Name</th>
    <td>OnSecondaryFire</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the player PRESS and RELEASE the secondary fire key (Right Mouse Button).
    </td>
  </tr>
    <tr>
        <th>Emitters</th>
        <td>`InputManager`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`WeaponModule`</td>
    </tr>
</table>

#### OnLookAround

<table>
  <tr>
    <th>Name</th>
    <td>OnLookAround</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the player moves the mouse (delta mouse).
        The type is a Vector2 where x is the horizontal input and y the vertical input, negative values are left and down, positive values are right and up.
    </td>
  </tr>
    <tr>
        <th>Type</th>
        <td>Vector2</td>
    </tr>
    <tr>
        <th>Emitters</th>
        <td>`InputManager`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`MechaController`</td>
    </tr>
</table>

#### OnZoomIn

<table>
  <tr>
    <th>Name</th>
    <td>OnZoomIn</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the player press the zoom in key (Mouse Scroll Up).
    </td>
  </tr>
    <tr>
        <th>Emitters</th>
        <td>`InputManager`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`MechaController`</td>
    </tr>
</table>

#### OnZoomOut

<table>
  <tr>
    <th>Name</th>
    <td>OnZoomOut</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the player press the zoom out key (Mouse Scroll Down).
    </td>
  </tr>
    <tr>
        <th>Emitters</th>
        <td>`InputManager`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`MechaController`</td>
    </tr>
</table>

#### OnPause

More information in the [Game Events > OnPause](#onpause) section.

### Pause Action Map

#### OnResume

More information in the [Game Events > OnResume](#onresume) section.

## UI

### HUD

#### OnZoomChange

<table>
  <tr>
    <th>Name</th>
    <td>OnZoomChange</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the zoom level changes. The type is an enum with the following values:
        <ul>
            <li>Zoom.Default</li>
            <li>ZoomLevel.X2</li>
            <li>ZoomLevel.X4</li>
            <li>ZoomLevel.X8</li>
        </ul>
</td>
  </tr>
    <tr>
        <th>Type</th>
        <td>MechaController.Zoom (enum)</td>
    </tr>
    <tr>
        <th>Emitters</th>
        <td>MechaController</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`HUDManager`</td>
    </tr>
</table>

#### OnUpdateCompass

<table>
  <tr>
    <th>Name</th>
    <td>OnUpdateCompass</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emit when the linear compass HUD angle needs to be updated (Y Rotation). The float value corresponds to the new angle (deg).
</td>
  </tr>
    <tr>
        <th>Type</th>
        <td>float</td>
    </tr>
    <tr>
        <th>Emitters</th>
        <td>`MechaController`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`HUDManager`</td>
    </tr>
</table>

#### OnUpdateXRotation

<table>
  <tr>
    <th>Name</th>
    <td>OnUpdateXRotation</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emit when the side bar HUD angle needs to be updated (X Rotation). The float value corresponds to the new angle (deg).
</td>
  </tr>
    <tr>
        <th>Type</th>
        <td>float</td>
    </tr>
    <tr>
        <th>Emitters</th>
        <td>`MechaController`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`HUDManager`</td>
    </tr>
</table>

#### OnUpdatePrimaryAmmo

<table>
  <tr>
    <th>Name</th>
    <td>OnUpdatePrimaryAmmo</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emit when the primary ammo HUD needs to be updated. The int value corresponds to the ammo remaining.
</td>
  </tr>
    <tr>
        <th>Type</th>
        <td>int</td>
    </tr>
    <tr>
        <th>Emitters</th>
        <td>`WeaponModule`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`HUDManager`</td>
    </tr>
</table>


#### OnUpdateSecondaryAmmo

<table>
  <tr>
    <th>Name</th>
    <td>OnSecondaryAmmo{[Left] or [Right]</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emit when the secondary ammo HUD needs to be updated. The int value corresponds to the ammo remaining. It can be targeted to the left or right magazine of the HUD.
</td>
  </tr>
    <tr>
        <th>Type</th>
        <td>int</td>
    </tr>
    <tr>
        <th>Emitters</th>
        <td>`WeaponModule`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`HUDManager`</td>
    </tr>
</table>
