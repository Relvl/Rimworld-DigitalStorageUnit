[H1]Digital Storage Unit[/H1]

The mod adds a storage system that allows you to store a large number of items in one place, allowing pawns to move around the map less when collecting resources for a recipe.

The mod is based on the awesome [url=https://steamcommunity.com/workshop/filedetails/?id=2033979700 ]Project: Rimfactory Rewived[/url] mod, so let's say a THANK YOU VERY MUCH to these guys!

In fact, initially I just wanted to remove everything superfluous from the PRF, leaving only the DSU, but I woke up only after I rewrote most of the code, leaving only the general idea and textures...

The mod does almost not affect the performance of the game, and even improves it in complex modpacks. Myths that the game cannot work adequately when thousands of items are collected on one cell are only myths.

It may take a lot of work on the balance of recipes.

Perhaps there are tricky bugs, I didn't have the opportunity to thoroughly test the mod for months - I just played my favorite game, and along the way wrote this mod.

[url=https://github.com/Relvl/Rimworld-DigitalStorageUnit ]Source code on Github[/url]

[H1]Compatibility[/H1]
The mod is compatible with most mods I know: 
    [url=https://steamcommunity.com/sharedfiles/filedetails/?id=2890901044 ]Combat Extended[/url], 
    [url=https://steamcommunity.com/sharedfiles/filedetails/?id=1617282896 ]Deep Storage[/url] (and even synergizes with it), 
    [url=https://steamcommunity.com/sharedfiles/filedetails/?id=1279012058 ]Pick Up And Haul[/url], 
    [url=https://steamcommunity.com/sharedfiles/filedetails/?id=949498803 ]Haul to stack[/url], 
any mods that increase the stack size, etc.

The mod was tested on the [b]HardcoreSK[/b] modpack, on top of which another dozen 30 extras were installed; I played several tries on such an modpack, everything is fine.

For obvious reasons, the mod is not compatible with [url=https://steamcommunity.com/workshop/filedetails/?id=2033979700 ]Project: Rimfactory Rewived[/url] (but it is not necessary, there is its own DSU).

[H1]License, maintenance, and all that[/H1]
All parts of this mod can be used for any purpose that does not violate the laws of your country and the license agreement of Steam and Rimworld.

Add bug reports, suggestions for improvements, and new ideas on Github Issues!

Add Merge Requests on Github with improvements and development of the idea, if you wish! Only improvements, corrections, and development of the original idea will be accepted. I don't approve of mods that have a lot of unrelated mechanics.

If you want to translate the mod into your language, please write to me in Discord (#Johnson1893), or create a Merge Request in Github.

[b]If you want to port the mod to another version of the game[/b] - please do not make another mods with names like `revived`,`continued`, `1.100` - just write to me in Discord (#Johnson1893, RU/EN), and we will agree to add you to the list of maintainers on github and in the steam. Please stop cluttering up Steam with a bunch of different versions of the same mod.

[H1]Mini wiki[/H1]

[H3]Digital Storage Unit[/H3]
A structure that can accommodate a large number of objects.
Each stored item increases the energy consumption of the DSU.
In the lack of energy, objects do not disappear, but the DSU cannot take in new items.
If the building is destroyed, all stored items will be destroyed!
The DSU must be built in a room completely covered with ceilings.

[H3]DSU Access Point[/H3]
A structure that allows pawns to request items from the DSU for their own needs.
In the mod settings, you can increase the imaginary distance to the DSU so that pawns with a higher priority try to pick up items from the Point.

[H3]DSU Import Bus[/H3]
A structure that moves all items from the area to the DSU. Pawns can store items with it if the path to the Bus is closer than to the DSU.
In the mod settings, you can increase the imaginary distance to the DSU so that pawns with a higher priority carry the stored items to the nearest Bus.

[H3]DSU Export Bus[/H3]
A structure that places the specified items from the DSU into the area.
Pawns cannot request items from it.
Pawns will try to carry items back to the storage if the Bus priority is lower, or if there is no storage area in the area of action.

[H3]DSU Data Extender[/H3]
A structure that allows you to increase the storage limit of the DSU located in the same room.
If there is more than one DSU in the room, it won't work.

[H3]Hardmode[/H3]
It can be enabled in the mod options.
Enables the mechanics of heating the DSU. It will start to emit [b]a lot of heat[/b].
If the DSU overheats - itself, the stored items, and all the buildings in the room will begin to receive increasing damage.
It will require cooling the room with air conditioners or other methods.
Enabling this mode increases the storage limit by 40%, and decreases the power consumption by 10%;