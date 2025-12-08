



```
 _    _       _       _                                                   
| |  | |     | |     | |                                                  
| |__| | __ _| |_ ___| |_ _ __ __ ___   ____ _  __ _  __ _ _ __  ______ _ 
|  __  |/ _` | __/ __| __| '__/ _` \ \ / / _` |/ _` |/ _` | '_ \|_  / _` |
| |  | | (_| | |_\__ \ |_| | | (_| |\ V / (_| | (_| | (_| | | | |/ / (_| |
|_|  |_|\__,_|\__|___/\__|_|  \__,_| \_/ \__,_|\__, |\__,_|_| |_/___\__,_|
                                                __/ |                     
                                               |___/                    
```                                                 

## A mod for the game Stardew Valley that allows the player to gift special hats to NPCs.
### By Tabitha Rowland

### Features
- Gift two premade custom hats to NPCs in game. Santa Hat and Pumpkin Hat.
- Remove hats by clicking an NPC while holding any hat item, or use the console command.
- Add and use your own custom hats to gift to NPCs in Stardew Valley.
- Gives you a hat gallery in the form of a chest where you can grab hats infinitely.
- Some console commands! 
### Commands

```hat_all "hat name"``` gives all the NPCs a hat if the hat name is valid. Example: ```hat_all "Santa Hat"```

```hat_remove <npc name>``` removes the hat from the specified NPC. Example: ```hat_remove Abigail```

```hat_clear``` removes all hats from all NPCs.


### Installation

Installation is as easy as any other Stardew Valley mod. Use SMAPI to download and install or manually place the folder into your Stardew Valley mods subfolder. 

### Adding Your Own Hats
**Requirements:** a **16x64** png file of your hat art including 4 rederings of the hat. The png is made of 4 16x16 squares. These follow the following order: front, left side, back, right side.

To add your own hat art to the game, 
1. Exit the game if open.
2. Place a copy of your 16x64 png file into the assets folder. 
3. Name the file whatever-hat.png where "whatever" is a descriptor for your type of hat. 
4. Next time you boot up the game, your hat will be available to gift to NPCs!

### Limitations
You can only have custom hat options equal to the amount of slots in a chest (36). Follow **Adding Your Own Hats** and delete an older hat to make room. 

### Bugs For Furutre Fixes
There are a few minor bugs that have been allowed to survive due to time constraints. Here are some I am aware of. If you find more, let me know!

>Some NPC sprite animations are not compatible with this mod. For example, Mayor Lewis works in his garden kneeling which causes his hat to float. 

This is due to many of the 33 NPCs having custom unique animations. In this mod I account only for turning, walking, and jumping as they are the most common. 

>Sometimes the NPCs accept the hat as though it is a gift. This will use the gift limit. 

This is something I programmed around and happens very rarely. In theory it should not be happening now that my items are entirely custom, but if you see it happening, let me know! At least the hats are a universally liked item, so hopefully this won't cause you any trouble. 

>Hat box items do not sort properly into stacks of the same type if not manually grouped. The hatbox also allows for stacks of more than the standard 36. 

This is what I'd call a feature more than a bug ;)

>You can see bits of the characters sprites peaking out behind the hats. 

Yes... If anyone has sprite sheet versions of all NPCs and their animations where they are bald- please share. 

>Willy and Wizard won't wear hats

This is a continuation of the issue above. These characters already wear large hats that will aggressively peek out behind the hats this mod implements.

>Hats will sit on the top layer of the world, and get stuck when animations like the bus begins. 

Fixing this would require a huuge overhaul of the way hats are drawn. Hope you understand!

>Dialogue art doesn't show the characters in the hats. 

Couldn't quite get this to work in time. It's on the list for future updates!

>Hats don't always fit in with the in game lighting. 

This is for the same reason as the layering issue. 