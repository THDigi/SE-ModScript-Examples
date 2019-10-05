using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Lights;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRageMath;
using VRageRender.Lights;

namespace Digi.AttachedLights
{
    public partial class AttachedLightsSession
    {
        // First off your models need at least one 'customlight_' prefixed dummy for this script to add lights at.
        // The dummy orientation is also used for spotlights.
        //
        // Examples of defining your blocks:
        //
        // Add(Configurator, typeof(MyObjectBuilder_Passage));
        //   that adds all Passage type blocks with the specified Configurator.
        //
        // Add(Configurator, typeof(MyObjectBuilder_TerminalBlock), "ControlPanel", "SmallControlPanel");
        //   that TerminalBlock types with the specified subtypes and specified Configurator.
        //
        // You can also define same type multiple times but it has to be with different subtypes.
        // If you define one type with no subtypes and the same type again with specified subtypes,
        //  the specified subtypes will get the defined configurator and everything else will get the configurator
        //  that was defined on the no-subtype one.
        //
        // To find out what `MyObjectBuilder_` you need just look at the block TypeId and append that.
        //
        //
        // Next the config itself, you can define as many as you want and even call others inside it.
        // Format, things in <> are editable.
        //
        // LightConfigurator <Name> = (dummyName, light, blockLogic) =>
        // {
        //      <Code>
        // };
        //
        // see the example configurator below for all available properties and sample conditions.

        void Setup()
        {
            Add(ExampleConfig, typeof(MyObjectBuilder_TerminalBlock), "ControlPanel", "SmallControlPanel"); // vanilla control panels only
        }

        // These are functions that get called for every dummy in the block so you can configure each dummy differently
        LightConfigurator ExampleConfig = (dummyName, light, blockLogic) =>
        {
            //blockLogic.MaxViewRange = 50; // defines at which range light turns off; it's faster computationally to not define this if you don't need it.

            // comment out any section/property you don't want to set

            // Point light properties
            light.LightOn = true;
            light.Color = new Color(0, 255, 0); // RGB
            light.Range = 5f;
            light.Falloff = 1f;
            light.Intensity = 5f;
            light.PointLightOffset = 0f; // offset light source towards block forward(?), I don't think it moves the glare too.
            //light.DiffuseFactor = 1f; // not sure what numbers do in this

            // Spotlight properties
            light.LightType = MyLightType.SPOTLIGHT;
            light.ReflectorOn = true;
            light.ReflectorColor = new Color(255, 155, 0); // RGB
            light.ReflectorIntensity = 10f;
            light.ReflectorRange = 100; // how far the projected light goes
            light.ReflectorConeDegrees = 90; // projected light angle in degrees, max 179.
            light.ReflectorTexture = @"Textures\Lights\reflector_large.dds"; // NOTE: for textures inside your mod you need to use: Utils.GetModTextureFullPath(@"Textures\someFile.dds");
            light.CastShadows = true;
            //light.ReflectorGlossFactor = <num>f; // affects gloss in some way

            // Glare properties... which don't seem to work...
            light.GlareOn = true;
            light.GlareSize = new Vector2(1, 1); // glare size in X and Y.
            light.GlareIntensity = 2;
            light.GlareMaxDistance = 50;
            light.SubGlares = Utils.GetFlareDefinition("InteriorLight").SubGlares; // subtype name from flares.sbc
            light.GlareType = MyGlareTypeEnum.Normal; // usable values: MyGlareTypeEnum.Normal, MyGlareTypeEnum.Distant, MyGlareTypeEnum.Directional
            light.GlareQuerySize = 0.5f; // glare "box" size, affects occlusion and fade occlussion
            light.GlareQueryShift = 1f; // no idea



            // Examples of differentiating between dummies
            // 
            // Contents of this condition only apply to 'customlight_light1'.
            // if(dummyName.Equals(DUMMY_PREFIX + "light1", StringComparison.OrdinalIgnoreCase))
            // {
            //     <code>
            // }
            //
            // Contents of this condition apply to any dummy name that starts with 'customlight_num'.
            // if(dummyName.StartsWith(DUMMY_PREFIX + "num", StringComparison.OrdinalIgnoreCase))
            // {
            //     <code>
            // }


            // You can also call other configurators inside configurators to apply their changes and then do minor tweaks afterwards.
            // The way to do that is (without the <> ofc):
            //
            // blockLogic.Session.<ConfiguratorNameHere>.Invoke(dummyName, light, blockLogic);
            //
            // NOTE: don't call the same configurator inside itself or you'll freeze/crash the game.


            // Properties that are automatically computed.
            // Do NOT set these unless you know what you're doing.
            //light.ParentID
            //light.Position
            //light.ReflectorDirection
            //light.ReflectorUp
        };
    }
}
