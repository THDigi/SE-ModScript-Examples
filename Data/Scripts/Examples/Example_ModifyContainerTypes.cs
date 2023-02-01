using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Game;
using VRage.Game.Components;

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_ModifyContainerTypes : MySessionComponentBase
    {
        public override void LoadData()
        {
            {
                // example of adding a new item to it
                AddItem("Component/Computer", frequency: 2.5, min: 10, max: 20);

                // example of removing one
                RemoveItem("Ore/Iron");
                // and you can even add it back with changes
                AddItem("Ore/Iron", frequency: 1.0, min: 500, max: 600);

                // you can also change the CountMin and CountMax of the container type if you wish:
                //SetCountMin = 2;
                //SetCountMax = 5;

                ToContainerType("CargoLargeMining1A"); // this finalizes the changes to the given container subtype
            }

            // and more chunks like this if you need to edit more containertypes...
            //{
            //    AddItem("Component/Computer", frequency: 2.5, min: 10, max: 20);
            //    RemoveItem("Ore/Iron");
            //    ToContainerType("CargoLargeMining1B");
            //}



            CleanUp(); // do not remove
        }

        // no need to modify anything below

        int? SetCountMin = null;
        int? SetCountMax = null;

        HashSet<MyDefinitionId> RemoveItems = new HashSet<MyDefinitionId>(MyDefinitionId.Comparer);
        List<MyObjectBuilder_ContainerTypeDefinition.ContainerTypeItem> AddItems = new List<MyObjectBuilder_ContainerTypeDefinition.ContainerTypeItem>();

        void CleanUp()
        {
            RemoveItems = null;
            AddItems = null;
        }

        void AddItem(string id, double frequency, double min, double max)
        {
            MyDefinitionId defId;
            if(!MyDefinitionId.TryParse(id, out defId))
                return;

            AddItems.Add(new MyObjectBuilder_ContainerTypeDefinition.ContainerTypeItem()
            {
                Id = defId,
                Frequency = (float)frequency,
                AmountMin = min.ToString(),
                AmountMax = max.ToString(),
            });
        }

        void RemoveItem(string id)
        {
            MyDefinitionId defId;
            if(!MyDefinitionId.TryParse(id, out defId))
                return;

            RemoveItems.Add(defId);
        }

        void ToContainerType(string containerTypeSubtypeId)
        {
            try
            {
                MyContainerTypeDefinition ctDef = MyDefinitionManager.Static.GetContainerTypeDefinition(containerTypeSubtypeId);
                if(ctDef != null)
                {
                    for(int i = 0; i < ctDef.Items.Length; i++)
                    {
                        MyContainerTypeDefinition.ContainerTypeItem item = ctDef.Items[i];
                        if(RemoveItems.Contains(item.DefinitionId))
                            continue;

                        AddItems.Add(new MyObjectBuilder_ContainerTypeDefinition.ContainerTypeItem()
                        {
                            Id = item.DefinitionId,
                            Frequency = item.Frequency,
                            AmountMin = item.AmountMin.SerializeString(),
                            AmountMax = item.AmountMax.SerializeString(),
                        });
                    }

                    // cannot edit the live definition because it has a private array that needs resizing, but cannot be because it is private.
                    MyObjectBuilder_ContainerTypeDefinition OB = (MyObjectBuilder_ContainerTypeDefinition)ctDef.GetObjectBuilder();

                    // base stuff missed by GetObjectBuilder()
                    OB.DLCs = ctDef.DLCs;

                    OB.CountMin = SetCountMin ?? ctDef.CountMin;
                    OB.CountMax = SetCountMax ?? ctDef.CountMax;
                    OB.Items = AddItems.ToArray();

                    ctDef.Init(OB, (MyModContext)ModContext); // also marking this def as modified by this mod
                }
            }
            finally
            {
                RemoveItems.Clear();
                AddItems.Clear();
                SetCountMin = null;
                SetCountMax = null;
            }
        }
    }
}
