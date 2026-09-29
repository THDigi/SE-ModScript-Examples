using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace Digi.Examples // change to have your name and the mod name, to be identifiable for crashes
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class AdditiveSBC_FactionType : MySessionComponentBase
    {
        public override void LoadData()
        {
            // This script allows you to add or remove items and grids from FactionType definitions.
            // Originally you had to edit the script like shown below to configure it, but it was made simpler where the
            //   script loads the Data\EditFactionTypes.txt, see the example in this repository!
            LoadTextFile();

#if false // turn this true if you wish the below to function, but not recommended, use the aformentioned text file.

            // Example usage below, change the subtype if needed, then call the methods shown inside.
            // Duplicate the entire chunk to edit more subtypes.

            using(var e = new EditFactionType(this, subtype: "Builder"))
            {
                // None of the below function are required, and each one can be called multiple times if needed.

                // the <TypeId> and <SubtypeId> from the item, separated by /
                e.OffersList_Add("ConsumableItem/RadiationKit");
                e.OffersList_Remove("Datapad/Datapad");
                e.OffersList_Remove("Component/ZoneChip");

                //e.OrdersList_Add("...");
                //e.OrdersList_Remove("...");

                // these take prefab SubtypeId from inside the file (not file name)
                e.GridsForSale_Remove("Cargo Shuttle");
                //e.GridsForSale_Add("...");
            }

            //...
#endif
        }

        // do not edit below this point unless you absolutely understand what you are doing

        void LoadTextFile()
        {
            const string FilePath = @"Data\EditFactionTypes.txt";

            if(!MyAPIGateway.Utilities.FileExistsInModLocation(FilePath, ModContext.ModItem))
            {
                Log(FilePath, "File not found!", TErrorSeverity.Error);
                return;
            }

            char[] SplitBy = new char[] { ':' };
            EditFactionType edit = null;

            using(var reader = MyAPIGateway.Utilities.ReadFileInModLocation(FilePath, ModContext.ModItem))
            {
                string line;
                while((line = reader.ReadLine()) != null)
                {
                    if(string.IsNullOrWhiteSpace(line) || line.StartsWith("//"))
                        continue;

                    string[] data = line.Split(SplitBy, 2);

                    if(data.Length != 2)
                    {
                        Log(FilePath, $"Unexpected line '{line}', ignored.", TErrorSeverity.Warning);
                        continue;
                    }

                    string key = data[0].Trim();
                    string value = data[1].Trim();

                    switch(key.ToLowerInvariant())
                    {
                        case "factiontype":
                            edit?.Dispose(); // submit previous factiontype edit, if any
                            edit = new EditFactionType(this, value);
                            break;
                        case "+offer": edit.OffersList_Add(value); break;
                        case "-offer": edit.OffersList_Remove(value); break;
                        case "+order": edit.OrdersList_Add(value); break;
                        case "-order": edit.OrdersList_Remove(value); break;
                        case "+ship": edit.GridsForSale_Add(value); break;
                        case "-ship": edit.GridsForSale_Remove(value); break;
                        default:
                            Log(FilePath, $"Unknown key '{key}', ignored.", TErrorSeverity.Warning);
                            break;
                    }
                }
            }

            edit?.Dispose(); // submit the last factiontype edit
        }

        void Log(string file, string message, TErrorSeverity severity)
        {
            var context = new MyModContext();
            context.Init(ModContext.ModName, file, ModContext.ModPath);
            MyDefinitionErrors.Add(context, message, severity, writeToLog: true);
        }

        protected override void UnloadData()
        {
            // required for the changes to not persist into other loaded worlds
            foreach(EditFactionType edit in Edits)
            {
                edit.Undo();
            }
        }

        List<EditFactionType> Edits = new List<EditFactionType>();

        class EditFactionType : IDisposable
        {
            readonly MyModContext ModContext;
            readonly MyFactionTypeDefinition Def;

            readonly SerializableDefinitionId[] SellOriginal;
            readonly List<SerializableDefinitionId> Sell;

            readonly SerializableDefinitionId[] BuyOriginal;
            readonly List<SerializableDefinitionId> Buy;

            readonly string[] SellGridsOriginal;
            readonly List<string> SellGrids;

            public EditFactionType(AdditiveSBC_FactionType session, string subtype)
            {
                ModContext = (MyModContext)session.ModContext;

                if(!MyDefinitionManager.Static.TryGetDefinition<MyFactionTypeDefinition>(MyStringHash.GetOrCompute(subtype), out Def))
                {
                    Log($"Couldn't find FactionType definition with subtype: {subtype}", TErrorSeverity.Error);
                    Def = null;
                    return;
                }

                SellOriginal = Def.OffersList;
                Sell = Def.OffersList?.ToList() ?? new List<SerializableDefinitionId>();

                BuyOriginal = Def.OrdersList;
                Buy = Def.OrdersList?.ToList() ?? new List<SerializableDefinitionId>();

                SellGridsOriginal = Def.GridsForSale;
                SellGrids = Def.GridsForSale?.ToList() ?? new List<string>();

                session.Edits.Add(this);
            }

            public void OrdersList_Add(string itemId) => ModifyItems(itemId, buy: true, remove: false);
            public void OffersList_Add(string itemId) => ModifyItems(itemId, buy: false, remove: false);
            public void OrdersList_Remove(string itemId) => ModifyItems(itemId, buy: true, remove: true);
            public void OffersList_Remove(string itemId) => ModifyItems(itemId, buy: false, remove: true);

            void ModifyItems(string itemId, bool buy, bool remove)
            {
                if(Def == null)
                    return;

                MyDefinitionId id;
                if(!MyDefinitionId.TryParse(itemId, out id))
                {
                    Log($"Item's TypeId is invalid: {itemId}", TErrorSeverity.Error);
                    return;
                }

                {
                    MyPhysicalItemDefinition itemDef;
                    if(!MyDefinitionManager.Static.TryGetPhysicalItemDefinition(id, out itemDef))
                    {
                        Log($"Item does not exist in definitions: {id} - {(remove ? "Removing" : "Adding")} anyway.", TErrorSeverity.Error);
                        //return;
                    }
                }

                var list = buy ? Buy : Sell;
                if(remove)
                {
                    if(!list.Remove(id))
                    {
                        Log($"Failed to remove item from the {(buy ? "Orders" : "Offers")}List because it's not in the list: {id}", TErrorSeverity.Warning);
                    }
                }
                else
                {
                    if(list.Contains(id))
                        Log($"Item already exists in the {(buy ? "Orders" : "Offers")}List, added duplicate: {id}", TErrorSeverity.Warning);

                    list.Add(id);
                }
            }

            public void GridsForSale_Add(string prefabId) => ModifyShips(prefabId, remove: false);
            public void GridsForSale_Remove(string prefabId) => ModifyShips(prefabId, remove: true);

            void ModifyShips(string prefabId, bool remove)
            {
                {
                    MyPrefabDefinition prefabDef;
                    if(!MyDefinitionManager.Static.GetPrefabDefinitions().TryGetValue(prefabId, out prefabDef))
                    {
                        Log($"Prefab does not exist in definitions: {prefabId} - {(remove ? "Removing" : "Adding")} anyway.", TErrorSeverity.Error);
                        //return;
                    }
                }

                if(remove)
                {
                    if(!SellGrids.Remove(prefabId))
                        Log($"Failed to remove prefab from the GridsForSale list because it's not in the list: {prefabId}", TErrorSeverity.Warning);
                }
                else
                {
                    if(SellGrids.Contains(prefabId))
                        Log($"Prefab already exists in the GridsForSale list, added duplicate: {prefabId}", TErrorSeverity.Warning);

                    SellGrids.Add(prefabId);
                }
            }

            public void Undo()
            {
                Def.OffersList = SellOriginal;
                Def.OrdersList = BuyOriginal;
                Def.GridsForSale = SellGridsOriginal;
            }

            public void Dispose() // called by the using() statement when it exits its code block.
            {
                if(Def == null)
                    return;

                Def.OffersList = Sell.Count == 0 ? null : Sell.ToArray();
                Def.OrdersList = Buy.Count == 0 ? null : Buy.ToArray();
                Def.GridsForSale = SellGrids.Count == 0 ? null : SellGrids.ToArray();

                Def.Postprocess(); // it's empty right now but might not be in the future

                Log("Finished modifying", TErrorSeverity.Notice);
            }

            void Log(string message, TErrorSeverity severity = TErrorSeverity.Error)
            {
                if(Def != null)
                    message = $"(FactionType definition: {Def.Id.SubtypeName}) {message}";

                if(severity == TErrorSeverity.Notice)
                    MyLog.Default.WriteLine($"{ModContext.ModName} - Info: {message}");
                else
                    MyDefinitionErrors.Add(ModContext, message, severity, writeToLog: true);
            }
        }
    }
}
