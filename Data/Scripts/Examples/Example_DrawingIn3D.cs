using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;
using static VRageRender.MyBillboard; // for the BlendTypeEnum to be shorter, not required

namespace Digi.Examples
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Example_DrawingIn3D : MySessionComponentBase
    {
        // commonly useful materials
        MyStringId MaterialSolid = MyStringId.GetOrCompute("Square"); // subtypeId from transparent materials SBC
        MyStringId MaterialCircle = MyStringId.GetOrCompute("WhiteDot");
        MyStringId MaterialLaser = MyStringId.GetOrCompute("WeaponLaser");

        public override void Draw()
        {
            // remember that this method gets called even when game is paused
            // it's not necessary to draw here in particular, you can draw in any of the update methods but your inputs might be too old, for example if using camera matrix.

            LineExamples();
            CylinderExample();
        }

        void LineExamples()
        {
            // this matrix is updated at Draw() stage, in any other update methods it's 1 frame behind.
            MatrixD camMatrix = MyAPIGateway.Session.Camera.WorldMatrix;

            // Color can implicitly convert to Vector4 that 2nd param wants.
            // Color * float makes it transparent.
            // If you define it as Vector4 you can set values past 1 to apply more intensity, making it bloom (if post processing is enabled).
            Color color = Color.Red * 0.5f;

            Vector3D start = camMatrix.Translation + camMatrix.Forward * 3 + camMatrix.Down * 1f;
            Vector3D target = Vector3D.Zero;
            Vector3 direction = (target - start); // not normalized can work too if you give 1 to the line length
            float lineLength = 1f;
            float lineThickness = 0.05f;
            float dotRadius = lineThickness * 3;

            // How the billboard interacts with world/lighting. What each value does:
            //   Standard - regular, affected by tonemapping and post-processing.
            //   AdditiveBottom - same as regular but gets rendered under objects.
            //   AdditiveTop - same as regular but gets rendered over objects.
            //   SDR and LDR (they're the same integer) - ignores tonemapping but still affected by post processing, also does NOT get sorted by distance to camera.
            //   PostPP - ignores tonemapping and post-processing, also does NOT get sorted by distance to camera.
            BlendTypeEnum blendType = BlendTypeEnum.Standard;

            // all billboard methods accessible right now only live one tick

            // a line with dots at each end
            MyTransparentGeometry.AddLineBillboard(MaterialSolid, color, start, direction, lineLength, lineThickness, blendType);
            MyTransparentGeometry.AddPointBillboard(MaterialCircle, color, start, dotRadius, 0f, blendType: blendType);
            MyTransparentGeometry.AddPointBillboard(MaterialCircle, color, start + direction, dotRadius, 0f, blendType: blendType);

            // glowling line example (requires post processing to see the bloom)
            MyTransparentGeometry.AddLineBillboard(MaterialSolid, Color.White.ToVector4() * 100, Vector3D.Zero + Vector3.Forward * 1, Vector3.Forward, 10f, 0.05f, BlendTypeEnum.Standard);

            // fake "glowing" line example (no post processing required)
            MyTransparentGeometry.AddLineBillboard(MaterialLaser, Color.White.ToVector4(), Vector3D.Zero + Vector3.Backward * 1, Vector3.Backward, 10f, 0.3f, BlendTypeEnum.SDR);
        }

        void CylinderExample()
        {
            MatrixD matrix = MatrixD.CreateWorld(Vector3D.Zero, Vector3.Forward, Vector3.Up);
            Vector4 color = (Color.Lime * 0.75f).ToVector4();

            float baseRadius = 0.25f;
            float topRadius = 2f;
            float height = 10f;

            // how many subdivisions it does, for round objects it's 360/wireDivRatio so it must be a number that can divide properly.
            // best to use 360/deg to input the degrees that each rotation step is done at.
            int wireDivRatio = 360 / 15;

            bool wireframe = true; // DrawTransparentCylinder() only has wireframe internally so this is a pointless param
            float wireframeThickness = 0.05f;

            MySimpleObjectDraw.DrawTransparentCylinder(ref matrix, baseRadius, topRadius, height, ref color, wireframe, wireDivRatio, wireframeThickness, MaterialSolid);
        }
    }
}