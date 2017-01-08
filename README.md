# IPA
Illusion Plugin Architecture (Reloaded)

## How To Install

1. Download a release (https://github.com/Eusth/IPA/releases)
2. Extract the contents into the game folder
3. Drag & drop the game exe onto **IPA.exe**
4. Start the game as usual

## How To Develop

1. Create a new **Class Librar** C# project (.NET 2.0 to 3.5 (for LINQ))
2. Download a release and add **IllusionPlugin.dll** to your references
3. Implement `IPlugin` or `IEnhancedPlugin`
4. Build the project and copy the DLL into the Plugins folder of the game

## How To Keep The Game Patched

If you don't want to drag & drop the game exe onto IPA.exe everytime you replace CSharp-Assembly.dll, do this:

1. Copy the file **Launcher.exe** from the **IPA** folder into your game folder
2. Name it like the game exe but append "_Patched" (e.g. **Game.exe** -> **Game_Patched.exe**)
3. Always start the game with the "_Patched" exe
