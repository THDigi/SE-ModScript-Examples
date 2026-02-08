using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using static VRageRender.MyBillboard; // for BlendTypeEnum to be used directly

namespace Digi.Examples
{
    /*
     * Example of various things that all come together to make an oriented bounding box check intersections with ALL nearby grids' blocks seen as boundingboxes themselves.
     * What it would look like in-game: https://i.imgur.com/TzMEk55.jpeg
     * 
     * Mind that this is not a very efficient way and certainly should NOT be done like this every tick. There's also better ways of doing things depending on your needs.
     * For example, this is overkill if you wanted to check if player is near/inside your own block, for that, use a gamelogic component and iterate online players instead, way less results that way.
     */
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class Example_OBBToGridBlockIntersection : MySessionComponentBase
    {
        // MyStringId in particular is merely a hashcode of a string, that's what it looks up or computes.
        // What the string or hash does depends on where you input this MyStringId,
        //   in the case of MySimpleObjectDraw/MyTransparentGeometry, it's a subtypeId from TransparentMaterials.sbc.
        MyStringId Material = MyStringId.GetOrCompute("Square");

        // list to re-use. Clear() does not dispose of the internal array, it remains allocated but the references are nulled/defaulted.
        List<MyEntity> TempEntities = new List<MyEntity>();

        public override void UpdateAfterSimulation()
        {
            if(MyAPIGateway.Gui.IsCursorVisible)
                return; // hide when looking at menus

            if(MyAPIGateway.Session?.Player?.Character == null)
                return; // no player or no character assigned

            // example of a way to require a key being pressed, more info in Example_InputReading.cs
            //if(!MyAPIGateway.Input.IsAnyCtrlKeyPressed())
            //    return;


            MyOrientedBoundingBoxD collider = ComputeCollider();

            // now we find nearby entities then only look for grids
            // for more info on how MyGamePruningStructure works: https://spaceengineers.wiki.gg/wiki/Modding/Scripting/Fast_lookup_in_a_volume

            TempEntities.Clear();

            BoundingBoxD worldBB = collider.GetAABB(); // an axis-aligned boundingbox that covers this OBB, required for the MyGamePruningStructure.

            MyGamePruningStructure.Instance.GetTopMostEntities(ref worldBB, TempEntities, MyEntityQueryType.Both);

            foreach(var ent in TempEntities)
            {
                // not using IMyCubeGrid because the internal class (MyCubeGrid) has some faster options we can use, like iterating blocks without an intermediary list
                var grid = ent as MyCubeGrid;
                if(grid != null)
                    CheckGrid(grid, collider);
            }

            TempEntities.Clear();
        }

        MyOrientedBoundingBoxD ComputeCollider()
        {
            // a copy of local character's position and orientation (a copy because MatrixD is a struct) then offset it by 5m towards character's forward.
            // This does not include the head rotation when walking, there's GetHeadMatrix() for that if needed.
            var colliderWorldMatrix = MyAPIGateway.Session.Player.Character.WorldMatrix;
            colliderWorldMatrix.Translation += colliderWorldMatrix.Forward * 5;

            // an arbitrary axis-aligned boundingbox (AABB) that represents a 2m box centered around 0,0,0 (therefore it's a local space box)
            var colliderLocalBB = new BoundingBoxD(-Vector3D.One, Vector3D.One);

            // an OBB is an AABB that has an orientation, more complex to compute than an AABB
            MyOrientedBoundingBoxD collider = new MyOrientedBoundingBoxD(colliderLocalBB, colliderWorldMatrix);

            // render our collider OBB, for more info on rendering shapes/textures see the Example_DrawingIn3D.cs
            Color colliderColor = Color.Yellow;
            MySimpleObjectDraw.DrawTransparentBox(ref colliderWorldMatrix, ref colliderLocalBB, ref colliderColor, MySimpleObjectRasterizer.Solid, 1,
                faceMaterial: Material, blendType: BlendTypeEnum.AdditiveTop);

            return collider;
        }

        void CheckGrid(MyCubeGrid grid, MyOrientedBoundingBoxD collider)
        {
            MatrixD gridWorldMatrix = grid.WorldMatrix;
            BoundingBoxD gridLocalBB = grid.PositionComp.LocalAABB;
            BoundingBoxD gridWorldBB = grid.PositionComp.WorldAABB;

            // render grid's AABB to see when it becomes a result of MyGamePruningStructure
            //   and as an example of drawing a worldAABB. Mind that we don't use grid's matrix at all.
            Color gridBBColor = Color.Blue * 0.9f;
            MySimpleObjectDraw.DrawTransparentBox(ref MatrixD.Identity, ref gridWorldBB, ref gridBBColor, MySimpleObjectRasterizer.Wireframe, 1,
                lineWidth: 0.01f, lineMaterial: Material, blendType: BlendTypeEnum.AdditiveTop);


            // an OBB over the grid to check if our OBB is even close to any blocks before iterating them.
            MyOrientedBoundingBoxD gridOBB = new MyOrientedBoundingBoxD(grid.PositionComp.LocalAABB, gridWorldMatrix);
            if(!collider.Intersects(ref gridOBB))
                return;

            // render our OBB too, it also helps show how the grid might be larger than there are blocks even after removing, but then updates.
            Color gridOBBColor = Color.Lime * 0.9f;
            MySimpleObjectDraw.DrawTransparentBox(ref gridWorldMatrix, ref gridLocalBB, ref gridOBBColor, MySimpleObjectRasterizer.Wireframe, 1,
                lineWidth: 0.005f, lineMaterial: Material, blendType: BlendTypeEnum.AdditiveTop);

            // transform the collider into the local space of the grid, to avoid doing this for each block
            MyOrientedBoundingBoxD localCollider = collider; // structs are copied
            localCollider.Transform(grid.PositionComp.WorldMatrixInvScaled); // mutates the struct, which is why a copy is required (it's already copied by the method parameter but this makes it clearer)


            // MyCubeGrid's GetBlocks() returns a HashSet<MySlimBlock> but MySlimBlock is not allowed, so we're telling foreach to cast it to the mod interface for it instead.
            // NOTE: It's not actually recommended to iterate blocks like this especially each-tick! more optimizations needed but those highly depend on the use case.
            //       Reminder that the Keen's discord server has #modding-programming channel where you can ask all sorts of things, including for suggestions to optimize your specific use case.
            foreach(IMySlimBlock block in grid.GetBlocks())
            {
                // all blocks are slimblocks, and some slimblocks can host a FatBlock entity.
                // most commonly, if a block has a rigid <Model> then it has a FatBlock/entity,
                //   otherwise deformable armor which does not, will have null FatBlock, and that also makes them cheaper to compute.

                // the addition of One is because Min and Max are positions. for example on 1x1x1 blocks they're the same number so would end up as 0 size.
                Vector3I sizeInCells = (block.Max - block.Min) + Vector3I.One;
                // size in meters but halved because it's useful later on
                Vector3 halfExtents = sizeInCells * grid.GridSizeHalf;

                // block's metric center, in grid's local space
                Vector3 centerGridLocal = Vector3.Lerp(block.Min, block.Max, 0.5f) * grid.GridSize;
                // which if you imagine a line from Min to Max (they're vectors), the middle point of that line is the center of the block.

                // the above can be replaced by:
                //Vector3D centerGridLocal;
                //block.ComputeScaledCenter(out centerGridLocal);
                //Vector3 halfExtents;
                //block.ComputeScaledHalfExtents(out halfExtents);
                // but it's probably slower - StopWatch is whitelisted so one can measure!


                // block's axis-aligned bounding box (AABB) that is in grid's space.
                // not accurate to turn this into world space because then the BB would be axis-aligned to world, and would not be able to follow the block's orientation.
                BoundingBoxD blockGridBB = new BoundingBoxD(centerGridLocal - halfExtents, centerGridLocal + halfExtents);
                // and while you can make OBBs out of each block, it would be slower to compute OBB-OBB intersections than OBB-AABB ones.

                // whether our custom OBB (after being transformed to grid space) overlaps or is contained in the grid-space block AABB
                bool intesects = localCollider.Intersects(ref blockGridBB);

                // render the block's boundingbox, we need it to be world space to render it.
                Color bbColor = intesects ? Color.Lime : Color.Gray;
                MySimpleObjectDraw.DrawTransparentBox(ref gridWorldMatrix, ref blockGridBB, ref bbColor, MySimpleObjectRasterizer.Solid, 1,
                    faceMaterial: Material, blendType: BlendTypeEnum.AdditiveTop);


                /* some other things that might be useful:

                // getting local matrix - the position and orientation in grid space (but still in meters as opposed to grid cells which is what block.Min and Max return).
                Matrix blockLocalMatrix;
                block.Orientation.GetMatrix(out blockLocalMatrix); // only gives orientation, which is why the next part is needed
                blockLocalMatrix.Translation = blockCenterGridLocal;

                // getting block's world matrix, by converting previously computed local to world space using the worldmatrix of object it's local to.
                // for more info on vector-matrix transforms: https://spaceengineers.wiki.gg/wiki/Scripting/Vector_Transformations_with_World_Matrices
                // this is a matrix-matrix transform though which ultimately does the same thing but for each of the first matrix's vectors (and order matters unlike normal multiplication!)
                MatrixD blockWorldMatrix = blockLocalMatrix * gridWorldMatrix;
                
                // alternate way to render where we use the block's worldmatrix, but as you can se this requires more computing overall.
                BoundingBoxD blockSpaceBB = new BoundingBoxD(-halfExtents, halfExtents);
                MySimpleObjectDraw.DrawTransparentBox(ref blockWorldMatrix, ref blockSpaceBB, ref bbColor, MySimpleObjectRasterizer.Solid, 1, faceMaterial: Material, lineMaterial: Material, blendType: BlendTypeEnum.AdditiveTop);
                */
            }
        }
    }
}