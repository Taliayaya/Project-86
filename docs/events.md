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
5. [Input Events](#input-events)
    1. [Juggernaut Action Map](#juggernaut-action-map)
        1. [OnMove](#onmove)
        2. [OnFire](#onfire)
        3. [OnLookAround](#onlookaround)

## All Events

| Event Name                                        | Type                                | Description                                                                              |
|---------------------------------------------------|-------------------------------------|------------------------------------------------------------------------------------------|
| [PauseGame](#pausegame)                           |                                     | Emitted when the game is paused.                                                         |
| [ResumeGame](#resumegame)                         |                                     | Emitted when the game is resumed.                                                        |
| [UpdateGameParameter:\[name\]](#game-parameters)  | object *(depends on the parameter)* | Emitted when a game parameter is updated. The name of the parameter is in the event name |
| [OnMove](#onmove)                                 | Vector2                             | Emitted when the player move input is pressed                                            |
| [OnFire](#onfire)                                 | float                               | Emitted when the player fire input is pressed                                            |
| [OnLookAround](#onlookaround)                     | Vector2                             | Emitted when the mouse moves (delta mouse)                                               |

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
        <td></td>
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
        <td></td>
    <tr>
        <th>Listeners</th>
        <td>`SoundManager`</td>
    </tr>
</table>

## Game Events

### General

#### PauseGame

<table>
  <tr>
    <th>Name</th>
    <td>PauseGame</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the game is paused</td>
  </tr>
    <tr>
        <th>Emitters</th>
        <td></td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`SoundManager`, `GameManager`</td>
    </tr>
</table>

#### ResumeGame

<table>
  <tr>
    <th>Name</th>
    <td>ResumeGame</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the game is resumed after being paused (not called on start)</td>
  </tr>
    <tr>
        <th>Emitters</th>
        <td></td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td>`SoundManager`, `GameManager`</td>
    </tr>
</table>


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
        <td></td>
    </tr>
</table>


#### OnFire

<table>
  <tr>
    <th>Name</th>
    <td>OnFire</td>
  </tr>
  <tr>
    <th>Description</th>
    <td>Emitted when the player press the fire key (Left Mouse Button).
        The type is a float where 1 is pressed and 0 is released.
    </td>
  </tr>
    <tr>
        <th>Type</th>
        <td>float</td>
    </tr>
    <tr>
        <th>Emitters</th>
        <td>`InputManager`</td>
    </tr>
    <tr>
        <th>Listeners</th>
        <td></td>
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
        <td></td>
    </tr>
</table>


