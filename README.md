# ImprovedWorkbenches

This mod adds some QoL (Quality of Life) features to the workbench and bills overview screens:

- Adds an option to also count equipped items even when colonists are away from their home map (e.g., caravanning or raiding).

- A "Copy All" button has been added to the bills overview to copy all the bills in a workbench. The paste button will then paste all copied bills.

- When pasting bills, you can "Paste Link", which links the bills back to their originals. Any changes made to a linked bill will be mirrored to all bills in the chain. Links can be made across workbenches and can be broken manually later.

- When a workbench is selected, its Bills tab will automatically open. This can be disabled in the Mod Settings menu.

- Bills using "Do until X" now have a button to add arbitrary extra products to be counted. For example, you can create a bill to produce X Simple Meals and set that bill to also count Fine and Lavish Meals in the final count.

- Bills set to resume production when stock falls to a certain level now show that level in brackets on the workbench overview.

- A button has been added next to each bill, allowing the toggling of bill store mode between "Drop on Floor" and "Take to Best Stockpile" from the bills overview page.

- You can drag to reorder bills in a workbench's overview instead of having to use the up/down buttons. Dragging works from anywhere in a bill's background or use the drag box that has replaced the buttons.

- Navigation arrows have been added to the bill details window to navigate between bills in that workbench.

- Ability to rename bills.

- When a single bill is copied, a "Paste Into" button appears on every other bill, in the bill details and workbench overview screen. This button will paste all compatible settings from the source bill into the target bill, except for the output product itself. For example, you can create a number of tailoring jobs for different items of clothing, adjust the production counts and material filters for one, and paste these into all the others. Any job's settings can be pasted into another, but not all settings are compatible between all recipes; incompatible settings will not be copied.

- The Mod Options section for this mod lets you change the default store mode of new bills to "Drop on floor" instead of "Take to best stockpile".

- Fixes an inconsistency in vanilla where items without quality or hitpoints, like meals and medicine, are not counted if they are not in a stockpile.

- You can set a bill restriction that applies to the entire workbench, so all bills on that workbench share the same restriction. New bills and copied bills will automatically inherit this restriction.

- Can be added to an existing save.

# Installation

### STEAM INSTALLATION

Go to the [Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=935982361&) page and subscribe to the mod.

### NON-STEAM INSTALLATION

Download the [latest release](https://github.com/Falconne/ImprovedWorkbenches/releases) and unzip it into your RimWorld\Mods folder.  
More info: https://rimworldwiki.com/wiki/Installing_mods

# Building from source

This project can be built with Visual Studio or Visual Studio Code

### Visual Studio
- Just open the .slnx in Visual Studio
- Change the path to the ThirdPartyDependencies (Harmony) in the ThirdPartyDependencies.ps1 file in the build folder.
- If you are not using Steam or RimWorld is not installed in the default location (C:\Program Files (x86)\Steam\steamapps\common\RimWorld), you need to set an environment variable for the path to the RimWorld folder. 
With PowerShell, you can do this by running the following command: [System.Environment]::SetEnvironmentVariable("path_RimWorld", "D:\Games\RimWorld", [System.EnvironmentVariableTarget]::User)
- Change the Version `<VersionPrefix>` in the .csproj file. (The first 2 numbers indicate the RimWorld version, the third number for major mod updates, the last number for minor mod updates)
- build the project
- The mod will be in the `dist` directory at the root of the repo and copied to the RimWorld Mods folder.
- Start RimWorld and select the mod in the mod list.

### Visual Studio Code

Check the [VSCodeSetup.md](.vscode/VSCodeSetup.md) file for setup instructions.