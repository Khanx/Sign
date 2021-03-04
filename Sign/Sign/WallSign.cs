using ModLoaderInterfaces;
using Shared;

namespace Sign
{
    class WallSign : IOnTryChangeBlock
    {
        // Thanks to Zun for this code!
        public void OnTryChangeBlock(ModLoader.OnTryChangeBlockData data)
        {
            if (data.CallbackOrigin != ModLoader.OnTryChangeBlockData.ECallbackOrigin.ClientPlayerManual)
            {
                return;
            }

            if (data.PlayerClickedData.HitType != PlayerClickedData.EHitType.Block)
            {
                return;
            }

            if (data.TypeOld != BlockTypes.BuiltinBlocks.Types.air)
            {
                return;
            }

            var rootTestType = ItemTypes.GetType("Khanx.Sign");

            if (data.TypeNew.ParentItemType != rootTestType)
            {
                return;
            }

            var hitData = data.PlayerClickedData.GetVoxelHit();
            switch (hitData.SideHit)
            {
                case VoxelSide.xPlus:
                    data.TypeNew = ItemTypes.GetType("Khanx.WallSignx+");
                    break;
                case VoxelSide.xMin:
                    data.TypeNew = ItemTypes.GetType("Khanx.WallSignx-");
                    break;
                case VoxelSide.zPlus:
                    data.TypeNew = ItemTypes.GetType("Khanx.WallSignz+");
                    break;
                case VoxelSide.zMin:
                    data.TypeNew = ItemTypes.GetType("Khanx.WallSignz-");
                    break;
            }
        }
    }
}
