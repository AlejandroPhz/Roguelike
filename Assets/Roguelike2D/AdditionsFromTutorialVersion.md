# Changelog from Tutorial

Below is a list of all the changes added to the extened version compared to the one taught in the tutorial.

## World Settings Scriptable Object

To simplify defining different rule based on the current level the player is at, a new
ScriptableObjects is introduced :

### World Settings

The World Setting Scriptable Object store all the data needed to generate a board based on the
current level. It find the entry with the highest level number that is not superior to the current
level (e.g. if we have entries for level 0, 4 and 8 and current level is 5, it will use the level 4
entry) and use its data (width, height and a range of number of destructible walls) to generate that
level.

It also hold 3 references :

- The World theme used. World theme define the graphical theme of the world, listing all the ground
  and blocker tiles as well as destructible wall cell object prefabs that were before defined
  directly inside the BoardManager. **The theme also define which player prefab to uses** which mean
  the player is now instantiated by the GameManager instead of directly in the scene
- The Items settings that defines for a given level which objects to uses and how many to place.
- The Enemies Settings that defines which enemies (and how many of them) to randomly pick and place
  for a given level.

## Moving Camera

Cinemachine was added to allows the camera to be dynamic and follow the player as level can now be
bigger. The GameManager ensure that the player is assigned as the target of the camera. The camera
use the dead zone system : if the tracked player object move outside of the defined deadzone in the
center of the screen, it recenter the camera on the player.

## Moving Object Systems

The movement of player & enemies between the cell smoothly have been abstracted into a generic
system (created and stored in the GameManager).

Anything can now add a move request to that system, and the system will be moving the given
transform toward the given point.

A move request parameters are :
- The transform to move
- The end point to move to
- the speed at which to move
- If the move should returning to its starting point when reaching the end (allow to play "bump"
  animation where the object go forth and back)
- A priority number (see Priority below)
- an optional callback that get called when the requested move is finished
- an option callback when the request start moving for the first time

### Priority

When a move request is made, it will be placed in a list based on its priority. All moves in a lower priority will finish before moves in a higher 
priority starts. For example, Player attacks are set with priority 0 but enemies attacks have a priority of 1, meaning the player will finish its attack 
move before enemies start theirs, so it's more readable visually.

### Skip ahead

The system have a skip ahead function called when the character take a new action. This allow to "fast forward" all animations to the end. This is
useful to allow the player to play faster, otherwise they would need to wait for all enemy movement to be done before being able to take an action
again.

## IAttack interface

The tutorial version simply use `PlayerWantToEnter` function to detect if a player want to enter in a destructible wall or an enemy space, and the 
wall/enemy will trigger the attack animation. But this create a problem that the only way to attack is to move on something (so you can't do special
attack) and the player will not know it attacks something before the attack is done (which make handling different animations for attack impossible)

So instead this version use an interface, `IAttackable` that have a function `Damaged` that is called when the Attackable is damaged.
CellObject can implement (wallObject and Enemy do) and if the player try to enter a cell that contains a CellObject that have this interface, 
it calls `Damaged` on it and each object can react differently. The Player can also start a special animation.

In this project, this is used to queue a movement in the Moving Object System that will make the player "bump" against the thing it attack to help
visualize the attack better

## Stats

Stats have been added to the player and enemies. There is 3 stats :

- Attack that change how many damages is made
- Defenses that block damage 1 to 1
- Speed used in the Energy system (see below) to act more often than other entities

To display those stats a new UI have been added

## Energy System

In the tutorial version, the TurnManager is ticked by the player when acting (which make the enemies acts) then the player move. This mean player will 
always act second, and there is no way to be "faster" than other things, as everyone get one turn every time.

One way traditional roguelike handles that is by using an "energy system". Each turn, every entity get a certain amount of energy based on their stats
(in this case, Speed). We give 10 energy per point of speed. So a player with a speed of 2 will get 20 energy, but an enemy with a speed of 1 will get 
10 energy 

Then every actions will cost an amount of energy, each entities get to do one action, paying the energy cost. Then it wrap around and every entities
that still have energy get the chance to do another action etc. When no entity have any energy left, the turn is finished, every one get new energy
and it goes around again.

Here is an example turn with the Player having a speed of 2, Enemy A with a speed of 1, Enemy B with a Speed of 3:

- All entities get their energy. Player get 20, A get 10 and B 30.
- Wait for Player input
- Player Move, which cost 10 energy, Player have 10
- A Move, paying 10, A have 0 energy
- B Move, paying 10, B have 20 Energy
- Go back to player, player still have 10 energy, so wait for input
- Player Move, paying 10, player have 0 energy
- A have 0, so doesn't get to act
- B have 20, move, pay 10, have 10.
- Go back to player. Player have 0 energy, so they are done for this turn
- A have 0 so doesn't act
- B have 10, move paying 10, now have 0.
- All entities have 0 energy, the turn is done
- New Turn, everyone get energy based on speed (so Player 20, A 10 and B 30)

You can see that during that sample turn, the player moved 2 time, A only move once but B moved 3 time. meaning the player can outrun A but B will catchup.

With this system, you can do interesting thing like making a heavy attack that cost 20 energy instead of 10 but does twice the damage.
You can even allow entities to go into negative energy amount, meaning they delay their next turns (as they won't be able to act again until their energy level
goes back above 0)

## Menu, Pause and Saving

A save system was added, and to be able to use it, a main menu and a pause menu.

In the main menu there is a :

- Continue button, which will be enabled if a save file exist. When clicking it, it will load the main scene which then, 
detecting a save file, will load it
- New button which will delete the save file if it exist and load the main scene (as there is no save file anymore a new 
game will be generated)

The game is a roguelike, meaning you cannot "go back" to a previous save. Save are only there to "suspend" a game until 
you can go back to it later. This is why if a save is present when a game start, it load it then delete it.

When in the pause menu, Save & Exit is chosen the game save :

- The Version of the save file (see _Save Versioning_ below)
- The current level
- The amount of food left
- The state of the Random Number Generator
- Then save the Board
  - Save the width and height
  - Go over each tile and save the passable state and the object in it if there is one
- Final Save the Player
  - Which save its current stats and position

Most of the saving is pretty straightforward : using a BinaryWriter, we write in a file the binary representation of
all the data we want, meaning we can, using a BinaryReader, read them back in the same order they were written in.

But it needs some special code to handle CellObject. During game, those are instances that are somewhere in memory and
our CellData points toward those instance. that means we need to re-instantiate them when we load a game, which mean
we need to know **which** Prefab was the source of each CellObject.

For this, on game init the GameManager call `RegisterObjectLookup()` on the `WorldSetting` which will go over every objects
referenced in it (Items, Enemies, Exit cell etc.) and :
- save their name in a serialized property called ID. As this is serialized, when that prefab get instantiated, the
new instance will have the same value in that property, effectively storing the "parent prefab" ID in the new CellObject
- Register that prefab in a new class `ReferenceDatabase` stored in the GameManager that have a lookup table of 
Id -> Prefab so a prefab can be retrieved just from its ID.

So when saving, a cell just save the ID of its containing CellObject to the save file. Then when loading we can :

- read the ID back
- Ask the `ReferenceDatabase` for the prefab corresponding to the given ID
- Instantiate that prefab

The same thing is done for the Tiles, for the same reasons (we need to retrieve which tile is in which cell)

### Note on using Name as ID

We use the name of our prefab/tile as their ID for lookup. This is obviously prone to error : 
- if two object share a name, only one will be in the lookup table
- if the object is renamed, older save won't work anymore (as the object will be looked up with its old name).

A proper full way of doing this would be to assign **Unique ID** to each Prefab CellObject and Tile, either with a 
manually set string property (e.g. the Exit Cell would have a `exit_cell` id the developer write in the inspector, so 
the Object can be renamed as long as this property isn't changed, the prefab will always be matched properly) or through 
editor scripting to assign automatically created uniqueID.

But in order to keep that sample as close to the original tutorial and not add a lot of code making it harder to parse
we use the object name here.

### Save Versioning

The first value we write in the save file is a Version number, which is a static int defined in the GameManager. This is
to help identify when a save was created. If as you develop/patch the game you add new data to the save file, this could
break the loading, as you need to read in the same order as you wrote.

For example : let's say you add water in addition to food. You save the water after the food in the saving code. But 
a save from a version before you add water would break as when you load it in the new version. Trying to read the
amount of water (which doesn't exist in that save file) would read the next data, the beginning of the random state.

But if the version is increased, the loading code can check if the version of the save file is equal or superior to that
before trying to read water, so when loading an old save file, it won't attempt to read water and won't break everything

Of course, manually increased save version can be error prone, so other thing can be used like the
build version, etc. As long as its a number that will go up, you can easily check if a save file was
created before something was added.