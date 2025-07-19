## Setup

1. Download and install:

   - [Visual Studio Code](https://code.visualstudio.com/)
   - [.NET Core SDK](https://dotnet.microsoft.com/download/dotnet-core) 8.0 or 9.0 (only needed for dotnet buildtool)
   - [.NET Framework 4.7.2 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net472). RimWorld framework.

2. Clone, pull, or download this template.

3. Install VS Code Extensions:


   - [C# Dev Kit](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit)
   - [Task Explorer](https://marketplace.visualstudio.com/items?itemName=spmeesseman.vscode-taskexplorer)

    or when on a Visual Studio Code fork: 
    - [DotRush](https://open-vsx.org/extension/nromanov/dotrush)
    - [Task Manager](https://open-vsx.org/extension/cnshenj/vscode-task-manager)


## First Steps
Errors and missing dependencies are solved on the first build.

1. Run task `CONFIG-change mod name` to set a new name for your mod
    <details>
      <summary>or manually change mod name </summary>
      
      - Update the mod name in `About.xml`
      - Rename the project folder in `src`
      - Change the namespace in `Main.cs`
      - Rename the `.csproj` file
      - Update `Rootspace` and `AssemblyName` in the `.csproj` file
      - Rename the `.slnx` file
      - Update the project path in the `.slnx` file
    </details>
2. Build Mod `CTRL + SHIFT + B` or run task `build` in [Task Explorer](https://marketplace.visualstudio.com/items?itemName=spmeesseman.vscode-taskexplorer).
3. Start RimWorld.

#### Troubleshooting

- **ThirdPartyDependencies**: Ensure paths to third-party DLLs in `build\ThirdPartyDependencies.ps1` are correctly specified and enclosed in quotes.
- **Environment variables**: Verify that the environment variable for RimWorld, `RimWorldInstallationPath`, is correctly configured. If the path is not set, the script will prompt you to set one. If the game is installed in the standard Steam folder on C:, the environment variable is set automatically.
- **Dependencies not found**: Verify that the RimWorld dll files are in the lib folder. Reload project by saving .csproj file. 
- **RimWorld path**: If RimWorld is not installed in the default path (C:\Program Files (x86)\Steam\steamapps\common\RimWorld), you need to set a environment variable for `path_RimWorld`. Easy way to do this is to run the task `CONFIG-change RimWorld path`.

## Additional notes

### Tasks & Scripts

Main tasks for automation:

- `RELEASE-build` - Standard task for building your mod.
  Includes tasks: compile + postbuild.
- `RELEASE-build & Start RimWorld` - Builds the mod and starts RimWorld
- `RELEASE-postbuild` - Runs only the postbuild step (packaging, copying to RimWorld Mods folder, etc).

- `UTILITY-clean` - Removes temp files that are created by the build process.
- `UTILITY-copyDependencies` - Copies required RimWorld and third-party DLLs into your `lib` folder.

- `LAUNCH_start dnSPY` - Launches dnSpy with the current dll file.
- `LAUNCH_start RimWorld` - Task that starts RimWorld directly from VS Code.
- `LAUNCH_start RimWorld -quicktest` - Starts RimWorld and loads dev quicktest map.

Debug tasks:
- `DEBUG-Build` - Debug build with .pdb files and debug symbols.
- `DEBUG-Build & Start RimWorld` - Debug build and start RimWorld.
- `DEBUG-Postbuild` - Debug postbuild process.

Configuration tasks:
- `CONFIG-install RimWorld debug` - Install RimWorld Doorstop for debugging.
- `CONFIG-remove RimWorld debug` - Remove RimWorld Doorstop files.
- `CONFIG-change mod name` - Automated task to rename your mod and update all related files.
- `CONFIG-change RimWorld path` - Set the path to your RimWorld installation.
- `CONFIG-change dnSpy path` - Set the path to your dnSpy executable.

### Project Structure

- `src/` - Source code directory
- `mod-structure/` - Template files for the mod (About.xml, etc.)
- `build/` - Build scripts and utilities
- `lib/` - Local dependencies (copied from RimWorld installation)
- `dist/` - Build output directory

### Decompile Assembly

Click while holding `CTRL` on imported RimWorld function to see them decompiled.  
Example: `CTRL` + `CLICK` on `Log` in `Main.cs` -> `Log.cs` from `Assembly-CSharp.dll` opens.

With [ilspy-vscode](https://marketplace.visualstudio.com/items?itemName=icsharpcode.ilspy-vscode) extension it's possible to decompile the Assemblies directly in VS Code.  
Right-click on `Assembly-CSharp.dll` in `localDependencies` and select `Decompile selected assembly`.  
Now you can see the ilspy window with the decompiled assembly which includes the important RimWorld functions.
Alternatively, use an external program like [dnSpy](https://github.com/dnSpyEx/dnSpy)


### Debug

Using [RimWorld Doorstop](https://github.com/pardeike/Rimworld-Doorstop) to enable Debugging:

1. Just run the task `CONFIG-install RimWorld debug`.
2. Download and run [dnSpy](https://github.com/dnSpyEx/dnSpy).
3. Open Assembly-CSharp.dll (usually in `"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll"`).
4. Open your mod assembly.dll in (usually in `"C:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\YOURMOD\VERSION\Assemblies\YOURMOD.dll"`).
5. Open Debug Menu in dnSpy `F5` and select Unity (Connect).
6. Add the IP Address `127.0.0.1` and the Port `55555` and start debugger. (IP can be skipped).

The Debugger is running in the background. With `F9` you can set breakpoints.

> When you set a breakpoint in the file `Assembly-CSharp.dll` -> `RimWorld` -> `Pawn_DraftController`
> -> `public bool Drafted` -> `set` -> `if(value == this.draftedInt)` Line: 24
> The game stops at your breakpoint when you draft a pawn in the game.

Run the task `CONFIG-remove RimWorld debug` to remove the files in your RimWorld installation.
