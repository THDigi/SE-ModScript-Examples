using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Definitions;
using VRage.Utils;
using VRageMath;

namespace Digi.Experiments
{
    // Because Data\Game\Game.sbc has all those session components which might change on a new game version so it's a pain to upkeep
    //   you can instead use this script to surgically change only some things without touching the session components.

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_ModifyGameDefinition : MySessionComponentBase
    {
        // Set to true to get each step of what this script is doing outputted to SE log, DS console and F11 menu too (as notice-level).
        // You can find these by searching for the local mod folder name or published name.
        const bool Verbose = true;

        void SetupDefinition()
        {
            // usage example:

            using(var def = new GameDefChange("Space")) // subtypeId of the game definition to edit
            {
                // only uncomment what you want to change

                //def.ExplosionAmmoVolumeMin = 0.0;
                //def.ExplosionAmmoVolumeMax = 0.0;
                //def.ExplosionRadiusMin = 0.0;
                //def.ExplosionRadiusMax = 0.0;
                //def.ExplosionDamagePerLiter = 0.0;
                //def.ExplosionDamageMax = 0.0;

                // required if you change explosion settings, same behavior as https://spaceengineers.wiki.gg/wiki/Modding/Reference/SBC/Game_Definition#Default
                // the script cannot read the <Default> in the original definition so that's why this is needed.
                //def.OverrideDefault();

                // examples of all the ways you can manipulate the colors list
                // reminder that encounter colors only works on "Space" subtypeId regardless of what game def world wants, this script will also warn if you use these elsewhere.
                //def.RemoveAllEncounterColors();
                //def.RemoveEncounterColorHex("#174973");
                //def.RemoveEncounterColorRGB(23, 73, 115);
                //def.AddEncounterColorHex("#FF00FF");
                //def.AddEncounterColorRGB(0, 128, 255);

                // hint: can add duplicate colors to increase their chance
            }

            // then repeat the above chunk if you want to edit other game definition subtypeIds, although that is often unnecessary
        }


        // no need to change anything below this point

        static Example_ModifyGameDefinition Instance;
        StringBuilder VerboseLog = (Verbose ? new StringBuilder(1024) : null);

        public override void LoadData()
        {
            try
            {
                Instance = this;
                if(Verbose) LogVerbose($"SetupDefinition() starting...");
                SetupDefinition();
                if(Verbose) LogVerbose($"SetupDefinition() finished.");
            }
            catch(Exception e)
            {
                LogError($"Error during code execution: {e.ToString()}{(Verbose ? "\n\nVerbose log so far:\n" + VerboseLog : "")}", TErrorSeverity.Critical);
            }
            finally
            {
                Instance = null;

                if(Verbose)
                {
                    string msg = VerboseLog.ToString();
                    MyDefinitionErrors.Add((MyModContext)ModContext, msg, TErrorSeverity.Notice, writeToLog: false);
                    MyLog.Default.WriteLineAndConsole($"[{ModContext.ModName}] {msg}");
                    VerboseLog = null;
                }
            }
        }

        class GameDefChange : IDisposable
        {
            public double? ExplosionAmmoVolumeMin = null;
            public double? ExplosionAmmoVolumeMax = null;
            public double? ExplosionRadiusMin = null;
            public double? ExplosionRadiusMax = null;
            public double? ExplosionDamagePerLiter = null;
            public double? ExplosionDamageMax = null;

            readonly MyGameDefinition Def;
            readonly List<ColorDefinitionRGBA> EncounterColors = new List<ColorDefinitionRGBA>(8);
            bool EncounterColorChanges = false;
            bool UpdateDefaultDef = false;

            public GameDefChange(string subtypeId)
            {
                if(Verbose) LogVerbose($"Starting edits on {subtypeId}...");
                Def = MyDefinitionManager.Static.GetDefinition<MyGameDefinition>(subtypeId);
                if(Def == null)
                {
                    LogError($"Can't find GameDefinition with subtypeId: '{subtypeId}', no changes can be made to it!");
                    if(Verbose) LogVerbose($"FAILED: cannot find '{subtypeId}' game definition! Stoping here for this edit.");
                    return;
                }

                if(Def.RandomColorsForPrefabSpawn != null)
                {
                    EncounterColors.EnsureCapacity(MathHelper.GetNearestBiggerPowerOfTwo(Def.RandomColorsForPrefabSpawn.Count));
                    EncounterColors.AddList(Def.RandomColorsForPrefabSpawn);
                }
            }

            public void RemoveAllEncounterColors()
            {
                if(Verbose) LogVerbose($"Removed all encounter colors.");
                EncounterColors.Clear();
                EncounterColorChanges = true;
            }

            public void AddEncounterColorHex(string hex)
            {
                if(string.IsNullOrWhiteSpace(hex))
                {
                    LogError("Hex string cannot be empty!");
                    if(Verbose) LogVerbose($"ERROR: AddEncounterColorHex() was given null or empty string!");
                    return;
                }

                var add = new ColorDefinitionRGBA(hex);

                if(Verbose)
                {
                    bool dupe = false;
                    for(int i = EncounterColors.Count - 1; i >= 0; i--)
                    {
                        var color = EncounterColors[i];
                        if(color.R == add.R && color.G == add.G && color.B == add.B)
                        {
                            LogVerbose($"AddEncounterColorHex() added DUPLICATE with data: {hex} ({add.R},{add.G},{add.B}) - duplicates are fine if you intend this color to have a higher chance.");
                            dupe = true;
                            break;
                        }
                    }

                    if(!dupe)
                        LogVerbose($"AddEncounterColorHex() added new with data: {hex} ({add.R},{add.G},{add.B})");
                }

                EncounterColors.Add(add);
                EncounterColorChanges = true;
            }

            public void AddEncounterColorRGB(byte r, byte g, byte b)
            {
                var add = new ColorDefinitionRGBA(r, g, b);

                if(Verbose)
                {
                    bool dupe = false;
                    for(int i = EncounterColors.Count - 1; i >= 0; i--)
                    {
                        var color = EncounterColors[i];
                        if(color.R == add.R && color.G == add.G && color.B == add.B)
                        {
                            LogVerbose($"AddEncounterColorRGB() added DUPLICATE with data: {add.R},{add.G},{add.B} - duplicates are fine if you intend this color to have a higher chance.");
                            dupe = true;
                            break;
                        }
                    }

                    if(!dupe)
                        LogVerbose($"AddEncounterColorRGB() added new with data: {add.R},{add.G},{add.B}");
                }

                EncounterColors.Add(add);
                EncounterColorChanges = true;
            }

            public void RemoveEncounterColorHex(string hex)
            {
                if(string.IsNullOrWhiteSpace(hex))
                {
                    LogError("Hex string cannot be empty!");
                    if(Verbose) LogVerbose($"ERROR: RemoveEncounterColorHex() was given null or empty string!");
                    return;
                }

                var find = new ColorDefinitionRGBA(hex);

                for(int i = 0; i < EncounterColors.Count; i++)
                {
                    var color = EncounterColors[i];
                    if(color.R == find.R && color.G == find.G && color.B == find.B)
                    {
                        EncounterColors.RemoveAt(i);
                        EncounterColorChanges = true;
                        if(Verbose) LogVerbose($"RemoveEncounterColorHex() succesful with data: {hex} ({find.R},{find.G},{find.B})");
                        return;
                    }
                }

                LogError($"Could not find color to remove: {hex} ({find.R},{find.G},{find.B})");
                if(Verbose) LogVerbose($"ERROR: RemoveEncounterColorHex() failed, could not find: {hex} ({find.R},{find.G},{find.B})");
            }

            public void RemoveEncounterColorRGB(byte r, byte g, byte b)
            {
                for(int i = 0; i < EncounterColors.Count; i++)
                {
                    var color = EncounterColors[i];
                    if(color.R == r && color.G == g && color.B == b)
                    {
                        EncounterColors.RemoveAt(i);
                        EncounterColorChanges = true;
                        if(Verbose) LogVerbose($"RemoveEncounterColorRGB() succesful with data: {r},{g},{b}");
                        return;
                    }
                }

                LogError($"Could not find color to remove: {r},{g},{b}");
                if(Verbose)
                {
                    var find = new ColorDefinitionRGBA(r, g, b);
                    LogVerbose($"ERROR: RemoveEncounterColorRGB() failed, could not find: {find.R},{find.G},{find.B}");
                }
            }

            public void OverrideDefault()
            {
                UpdateDefaultDef = true;
            }

            public void Dispose() // automatically called by the using() code block once it exits its inner context.
            {
                if(Def == null)
                {
                    if(Verbose) LogVerbose("Finalization skipped because definition was not found");
                    return;
                }

                if(Verbose) LogVerbose($"Finalizing definition edits...");

                Assign(Def.ExplosionAmmoVolumeMin, (v) => Def.ExplosionAmmoVolumeMin = v, ExplosionAmmoVolumeMin, nameof(ExplosionAmmoVolumeMin));
                Assign(Def.ExplosionAmmoVolumeMax, (v) => Def.ExplosionAmmoVolumeMax = v, ExplosionAmmoVolumeMax, nameof(ExplosionAmmoVolumeMax));
                Assign(Def.ExplosionRadiusMin, (v) => Def.ExplosionRadiusMin = v, ExplosionRadiusMin, nameof(ExplosionRadiusMin));
                Assign(Def.ExplosionRadiusMax, (v) => Def.ExplosionRadiusMax = v, ExplosionRadiusMax, nameof(ExplosionRadiusMax));
                Assign(Def.ExplosionDamagePerLiter, (v) => Def.ExplosionDamagePerLiter = v, ExplosionDamagePerLiter, nameof(ExplosionDamagePerLiter));
                Assign(Def.ExplosionDamageMax, (v) => Def.ExplosionDamageMax = v, ExplosionDamageMax, nameof(ExplosionDamageMax));

                if(EncounterColorChanges)
                {
                    Def.RandomColorsForPrefabSpawn = EncounterColors;

                    if(Def.Id.SubtypeName != "Space")
                    {
                        string msg = "Changed encounter colors on a definition that isn't \"Space\" subtype, the game will not care about this! It only reads the \"Space\" definition's encounter colors.\nThe value was changed anyway in case Keen fixed this oddity in future versions.";
                        if(Verbose) LogVerbose($"Warning: {msg}");
                        LogError(msg, TErrorSeverity.Warning);
                    }
                }

                if(Verbose) LogVerbose($"Final EncounterColors ({EncounterColors.Count}) - {(EncounterColorChanges ? "with the changes" : "there were no changes")}: {(EncounterColors.Count > 0 ? "\n - " + string.Join("\n - ", EncounterColors.Select(c => $"{c.R},{c.G},{c.B}")) : "N/A")}");

                if(UpdateDefaultDef)
                {
                    var defaultId = MyGameDefinition.Default;

                    if(defaultId == Def.Id)
                    {
                        if(Verbose) LogVerbose($"Done! {Def.Id.SubtypeName} has been edited! Skipped editing default definition because this is the default one.");
                    }
                    else
                    {
                        var defaultDef = MyDefinitionManager.Static.GetDefinition<MyGameDefinition>(defaultId);
                        if(defaultDef == null)
                        {
                            string msg = $"Can't find '{defaultId}'...? Something changed with the game!";
                            if(Verbose) LogVerbose($"ERROR: {msg}");
                            LogError(msg);
                            return;
                        }

                        if(Verbose) LogVerbose($"Overriding '{defaultDef.Id}' with the same values as as '{Def.Id.SubtypeName}' has.");

                        defaultDef.ExplosionAmmoVolumeMin = Def.ExplosionAmmoVolumeMin;
                        defaultDef.ExplosionAmmoVolumeMax = Def.ExplosionAmmoVolumeMax;
                        defaultDef.ExplosionRadiusMin = Def.ExplosionRadiusMin;
                        defaultDef.ExplosionRadiusMax = Def.ExplosionRadiusMax;
                        defaultDef.ExplosionDamagePerLiter = Def.ExplosionDamagePerLiter;
                        defaultDef.ExplosionDamageMax = Def.ExplosionDamageMax;
                        defaultDef.RandomColorsForPrefabSpawn = Def.RandomColorsForPrefabSpawn; // just in case Keen fixes encounter colors lookup location

                        if(Verbose) LogVerbose($"Done! {Def.Id.SubtypeName} and {defaultId.SubtypeName} have been edited!");
                    }
                }
                else
                {
                    if(Verbose) LogVerbose($"Done! {Def.Id.SubtypeName} has been edited!");
                }
            }

            void Assign(float existingValue, Action<float> setter, double? value, string label)
            {
                if(value.HasValue)
                {
                    setter.Invoke((float)value.Value);
                    if(Verbose) LogVerbose($"Changed {label} to {(float)value.Value} (was {existingValue})");
                }
                else
                {
                    if(Verbose) LogVerbose($"Leaving {label} as it is ({existingValue}).");
                }
            }
        }

        static void LogError(string message, TErrorSeverity severity = TErrorSeverity.Error)
        {
            MyDefinitionErrors.Add((MyModContext)Instance.ModContext, message, severity, writeToLog: false);
            MyLog.Default.WriteLineAndConsole($"[{Instance.ModContext.ModName}] {message}");
        }

        static void LogVerbose(string msg)
        {
            Instance.VerboseLog.Append(msg).AppendLine();
        }
    }
}
