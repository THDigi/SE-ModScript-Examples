using System;
using System.Collections.Generic;

namespace Digi
{
    /// <summary>
    /// Instantiate and then use <see cref="MakeEdit{TObj, TVal}(TObj, Action{TObj, TVal}, TVal, TVal)"/> to store edits to instanced objects or <see cref="MakeStaticEdit{TVal}(Action{TVal}, TVal, TVal)"/> for static.
    /// When you want to undo the edits made call <see cref="UndoAll"/> which will restore them and clear the stored edits.
    /// </summary>
    public class UndoableEditToolset
    {
        readonly List<IUndoableEdit> Edits = new List<IUndoableEdit>();

        /// <summary>
        /// Add an edit which can then be undone using the given setter.
        /// 
        /// <para>Usage example:</para>
        /// <para>MyCubeBlockDefinition def = ...</para>
        /// <para>Edits.MakeEdit(def, (d,v) => d.Mass = v, originalValue: def.Mass, newValue: 50);</para>
        /// <para>Which will edit mass now to 50 then when calling <see cref="UndoAll()"/> it'll reset it to whatever def.Mass was.</para>
        /// </summary>
        /// <typeparam name="TObj">The object type, no need to enter this manually.</typeparam>
        /// <typeparam name="TVal">The value type, no need to enter this manually.</typeparam>
        /// <param name="editObject">object reference to be edited</param>
        /// <param name="setter">a callback for the field/prop to set on the object</param>
        /// <param name="originalValue">the current value of the field/prop (which will be used with the setter later to undo it)</param>
        /// <param name="newValue">the value to set now (which uses the setter callback)</param>
        public void MakeEdit<TObj, TVal>(TObj editObject, Action<TObj, TVal> setter, TVal originalValue, TVal newValue)
               where TObj : class
        {
            Edits.Add(new UndoableEdit<TObj, TVal>(editObject, setter, originalValue, newValue));
        }

        /// <summary>
        /// Similar to <see cref="MakeEdit{TObj, TVal}(TObj, Action{TObj, TVal}, TVal, TVal)"/> but without the instance.
        /// </summary>
        /// <typeparam name="TVal">The value type, no need to enter this manually.</typeparam>
        /// <param name="setter">a callback for the field/prop to set on the object</param>
        /// <param name="originalValue">the current value of the field/prop (which will be used with the setter later to undo it)</param>
        /// <param name="newValue">the value to set now (which uses the setter callback)</param>
        public void MakeStaticEdit<TVal>(Action<TVal> setter, TVal originalValue, TVal newValue)
        {
            Edits.Add(new UndoableEditStatic<TVal>(setter, originalValue, newValue));
        }

        /// <summary>
        /// Reverts all edits made so far then removes them from the internal list.
        /// <para>The edits are reverted in from last to first to properly undo multiple edits on the same thing.</para>
        /// <para>Always call this before making new changes and when mod unloads.</para>
        /// </summary>
        public void UndoAll()
        {
            for(int i = Edits.Count - 1; i >= 0; i--)
            {
                Edits[i].Restore();
            }

            Edits.Clear();
        }

        private interface IUndoableEdit
        {
            void Restore();
        }

        private class UndoableEdit<TObj, TVal> : IUndoableEdit where TObj : class
        {
            readonly TObj EditObject;
            readonly TVal OriginalValue;
            readonly Action<TObj, TVal> Setter;

            public UndoableEdit(TObj editObject, Action<TObj, TVal> setter, TVal originalValue, TVal newValue)
            {
                EditObject = editObject;
                Setter = setter;
                OriginalValue = originalValue;
                Setter.Invoke(EditObject, newValue);
            }

            public void Restore()
            {
                Setter.Invoke(EditObject, OriginalValue);
            }
        }

        private class UndoableEditStatic<TVal> : IUndoableEdit
        {
            readonly TVal OriginalValue;
            readonly Action<TVal> Setter;

            public UndoableEditStatic(Action<TVal> setter, TVal originalValue, TVal newValue)
            {
                Setter = setter;
                OriginalValue = originalValue;
                Setter.Invoke(newValue);
            }

            public void Restore()
            {
                Setter.Invoke(OriginalValue);
            }
        }
    }
}
