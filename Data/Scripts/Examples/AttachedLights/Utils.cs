using System;
using System.IO;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;

namespace Digi.AttachedLights
{
    public static class Utils
    {
        /// <summary>
        /// Gets the flare definition for getting SubGlares into lights.
        /// NOTE: The subglare type is prohibited so I can't return that directly, which is why I'm returning the definition.
        /// </summary>
        public static MyFlareDefinition GetFlareDefinition(string flareSubtypeId)
        {
            if(string.IsNullOrEmpty(flareSubtypeId))
                throw new ArgumentException("flareSubtypeId must not be null or empty!");

            var flareDefId = new MyDefinitionId(typeof(MyObjectBuilder_FlareDefinition), flareSubtypeId);
            var flareDef = MyDefinitionManager.Static.GetDefinition(flareDefId) as MyFlareDefinition;

            if(flareDef == null)
                throw new Exception($"Couldn't find flare subtype {flareSubtypeId}");

            return flareDef;
        }

        /// <summary>
        /// Converts a relative mod path into a full path for local machine.
        /// NOTE: Do not start <paramref name="relativeTexturePath"/> with a slash!
        /// </summary>
        public static string GetModTextureFullPath(string relativeTexturePath)
        {
            return Path.Combine(AttachedLightsSession.Instance.ModContext.ModPath, relativeTexturePath);
        }
    }
}
