using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRageRender.Messages;

namespace Digi.Experiments
{
    // AssetModifiers are not meant to be moddable by SBC (it doesn't look in mod paths and you have to override the entire thing).
    // This is the flexible alternative to adding your own materials to existing skins to be affected by them.

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_AppendToSkin : MySessionComponentBase
    {
        void SetupMaterials() // add/edit only in this function
        {
            // syntax:
            //ModifySkin("SkinSubtypeId");
            //Material("MaterialName", "cm/ng/add/alphamask", PathInMod/PathInGame(@"relative\path\to\file.dds"));

            //Material() will affect the skin from the last called ModifySkin().
            //Material() and ModifySkin() can be called as many times as you want.
            // Remember that material names are not isolated to your mod, name them unique if they're meant to be.

            // examples:

            //ModifySkin("Weldless");
            //Material("Fancy", "cm", PathInMod(@"Textures\Stuff\Things_cm.dds"));
            //Material("SpookyVanishing", "alphamask", PathInGame(@"Textures\Debug\Black.dds"));

            //ModifySkin("Plastic");
            //Material("PaintedMetal_Colorable", "cm", PathInGame(@"Textures\Models\Cubes\armor\large_square_plate_cm.dds"));




        }

        // don't need to change anything below unless you know what you're doing.

        /// <summary>
        /// cloned from VRageRender.Messages.MyTextureType because not whitelisted.
        /// does not seem to actually be used as flags though.
        /// </summary>
        [Flags]
        enum TextureType
        {
            Unspecified = 0x0,
            ColorMetal = 0x1,
            NormalGloss = 0x2,
            Extensions = 0x4,
            Alphamask = 0x8
        }

        struct SkinOriginalData
        {
            public readonly DictionaryReader<MyStringId, MyTextureChange> TextureChanges;

            public SkinOriginalData(MyDefinitionManager.MyAssetModifiers assetRender)
            {
                // needs to be cloned
                TextureChanges = new Dictionary<MyStringId, MyTextureChange>(assetRender.SkinTextureChanges);
            }
        }

        string Skin; // Currently edited skin, for less repetitive methods.

        Dictionary<string, SkinOriginalData> OriginalSkinData;

        public override void LoadData()
        {
            if(MyAPIGateway.Utilities.IsDedicated)
                return; // DS doesn't need any of this

            OriginalSkinData = new Dictionary<string, SkinOriginalData>();
            SetupMaterials();
        }

        protected override void UnloadData()
        {
            // undo changes because they leak
            if(OriginalSkinData != null)
            {
                foreach(var skinKv in OriginalSkinData)
                {
                    string skin = skinKv.Key;
                    var data = skinKv.Value;
                    MyDefinitionManager.MyAssetModifiers assetRender = MyDefinitionManager.Static.GetAssetModifierDefinitionForRender(skin);

                    if(assetRender.SkinTextureChanges == null)
                        continue;

                    assetRender.SkinTextureChanges.Clear();

                    foreach(var texKv in data.TextureChanges)
                    {
                        assetRender.SkinTextureChanges.Add(texKv.Key, texKv.Value);
                    }

                    // reminder that assetRender.MetalnessColorable is not settable here
                }
            }
        }

        void ModifySkin(string skin)
        {
            Skin = skin;
        }

        /// <summary>
        /// Adds or overrides the material for the specified skin.
        /// Skin must be an existing subtype, this does not add new skins (and adding new ones won't work anyway, they don't show up in color picker).
        /// </summary>
        void Material(string material, string type, string filePath)
        {
            try
            {
                if(Skin == null)
                {
                    MyDefinitionErrors.Add((MyModContext)ModContext, $"{nameof(ModifySkin)}() was not called.", TErrorSeverity.Error);
                    return;
                }

                MyDefinitionId skinId = new MyDefinitionId(typeof(MyObjectBuilder_AssetModifierDefinition), Skin);
                MyAssetModifierDefinition assetDef = MyDefinitionManager.Static.GetAssetModifierDefinition(skinId);

                if(assetDef == null)
                {
                    MyDefinitionErrors.Add((MyModContext)ModContext, $"Cannot find asset modifier definition: {Skin}", TErrorSeverity.Error);
                    return;
                }

                MyDefinitionManager.MyAssetModifiers assetRender = MyDefinitionManager.Static.GetAssetModifierDefinitionForRender(Skin);

                if(assetRender.SkinTextureChanges == null)
                {
                    MyDefinitionErrors.Add((MyModContext)ModContext, $"Cannot asset nodifier for render definition: {Skin}\nLikely a change in game code, report to author.", TErrorSeverity.Error);
                    return;
                }

                if(!OriginalSkinData.ContainsKey(Skin))
                    OriginalSkinData[Skin] = new SkinOriginalData(assetRender);

                // not really necessary and complex to undo (because it certainly leaks) so it's disabled.
                #region Modify asset definition
                //int textureDefIndex = -1;
                //MyObjectBuilder_AssetModifierDefinition.MyAssetTexture textureDef = default(MyObjectBuilder_AssetModifierDefinition.MyAssetTexture);
                //
                //for(int i = 0; i < assetDef.Textures.Count; i++)
                //{
                //    MyObjectBuilder_AssetModifierDefinition.MyAssetTexture td = assetDef.Textures[i];
                //    if(td.Location == material && (int)td.Type == (int)type)
                //    {
                //        textureDef = td;
                //        textureDefIndex = i;
                //        break;
                //    }
                //}
                //
                //if(textureDefIndex == -1)
                //{
                //    // not found, add it
                //    textureDef = new MyObjectBuilder_AssetModifierDefinition.MyAssetTexture()
                //    {
                //        Location = material,
                //        Filepath = filePath,
                //        Type = CastHax(textureDef.Type, type),
                //    };
                //
                //    assetDef.Textures.Add(textureDef);
                //}
                //else
                //{
                //    // exists, only override path to file
                //    textureDef.Filepath = filePath;
                //    assetDef.Textures[textureDefIndex] = textureDef;
                //}
                #endregion

                #region Modify asset definition for render
                MyStringId materialId = MyStringId.GetOrCompute(material);
                MyTextureChange changesCopy;
                if(!assetRender.SkinTextureChanges.TryGetValue(materialId, out changesCopy))
                {
                    changesCopy = new MyTextureChange();
                }

                if(type.Equals("cm", StringComparison.OrdinalIgnoreCase))
                    changesCopy.ColorMetalFileName = filePath;
                else if(type.Equals("ng", StringComparison.OrdinalIgnoreCase))
                    changesCopy.NormalGlossFileName = filePath;
                else if(type.Equals("add", StringComparison.OrdinalIgnoreCase))
                    changesCopy.ExtensionsFileName = filePath;
                else if(type.Equals("alphamask", StringComparison.OrdinalIgnoreCase))
                    changesCopy.AlphamaskFileName = filePath;
                else
                {
                    MyDefinitionErrors.Add((MyModContext)ModContext, $"Bad Type given: {type}; expected cm/ng/add/alphamask", TErrorSeverity.Error);
                    return;
                }

                // add or replace the changes because MyTextureChange is a struct
                assetRender.SkinTextureChanges[materialId] = changesCopy;
                #endregion

                // NOTE: MetalnessColorable cannot be changed from defRender because it's a struct copy; it only works with the SkinTextureChanges because that is a reference.
            }
            catch(Exception e)
            {
                MyDefinitionErrors.Add((MyModContext)ModContext, $"Code error/exception: {e}", TErrorSeverity.Critical);
            }
        }

        /// <summary>
        /// All asset paths need to be full paths and this one appends the full path of the current mod to the relative path given
        /// </summary>
        string PathInMod(string relativePath)
        {
            if(!MyAPIGateway.Utilities.FileExistsInModLocation(relativePath, ModContext.ModItem))
                MyDefinitionErrors.Add((MyModContext)ModContext, $"Cannot find texture relative to mod: {relativePath}", TErrorSeverity.Error);

            return Path.Combine(ModContext.ModPath, relativePath);
        }

        /// <summary>
        /// All relative paths point in the game's Content, this is just for ease of use.
        /// </summary>
        string PathInGame(string relativePath) => relativePath;

        /// <summary>
        /// A little hack to input a prohibited type (typeRef) if you have an object that can cast to it (obj).
        /// </summary>
        //static T CastHax<T>(T typeRef, object obj) => (T)obj;
    }
}