using System;
using Sandbox.ModAPI;

namespace Digi
{
    /// <summary>
    /// Designed for appending custom conditions to Visible/Enabled of terminal controls or toolbar actions so that they can be hidden for specific conditions/subtypes/whatever.
    /// </summary>
    public class TerminalChainedDelegate
    {
        /// <summary>
        /// <paramref name="originalFunc"/> should always be the delegate this replaces, to properly chain with other mods doing the same.
        /// <para><paramref name="customFunc"/> should be your custom condition to append to the chain.</para>
        /// <para>As for <paramref name="checkOR"/>, leave false if you want to hide controls by returning false with your <paramref name="customFunc"/>.</para>
        /// <para>Otherwise set to true if you want to force-show otherwise hidden controls by returning true with your <paramref name="customFunc"/>.</para> 
        /// </summary>
        public static Func<IMyTerminalBlock, bool> Create(Func<IMyTerminalBlock, bool> originalFunc, Func<IMyTerminalBlock, bool> customFunc, bool checkOR = false)
        {
            return new TerminalChainedDelegate(originalFunc, customFunc, checkOR).ResultFunc;
        }

        readonly Func<IMyTerminalBlock, bool> OriginalFunc;
        readonly Func<IMyTerminalBlock, bool> CustomFunc;
        readonly bool CheckOR;

        TerminalChainedDelegate(Func<IMyTerminalBlock, bool> originalFunc, Func<IMyTerminalBlock, bool> customFunc, bool checkOR)
        {
            OriginalFunc = originalFunc;
            CustomFunc = customFunc;
            CheckOR = checkOR;
        }

        bool ResultFunc(IMyTerminalBlock block)
        {
            if(block?.CubeGrid == null)
                return false;

            bool originalCondition = (OriginalFunc == null ? true : OriginalFunc.Invoke(block));
            bool customCondition = (CustomFunc == null ? true : CustomFunc.Invoke(block));

            if(CheckOR)
                return originalCondition || customCondition;
            else
                return originalCondition && customCondition;
        }
    }
}
