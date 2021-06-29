using System.Collections.Generic;
using System.Linq;

using Pipliz;

using BlockEntities;
using colonyserver.Assets.UIGeneration;

namespace Sign
{
    [BlockEntityAutoLoader]
    public class SignType : IChangedWithType, IMultiBlockEntityMapping
    {
        public ItemTypes.ItemType TypeToRegister { get { return ItemTypes.GetType("Khanx.Sign"); } }

        public IEnumerable<ItemTypes.ItemType> TypesToRegister { get { return types; } }

        readonly ItemTypes.ItemType[] types = new ItemTypes.ItemType[]
            {
                 // Thanks to Boneidle for the models!
                 ItemTypes.GetType("Khanx.Signx-"),
                 ItemTypes.GetType("Khanx.Signx+"),
                 ItemTypes.GetType("Khanx.Signz-"),
                 ItemTypes.GetType("Khanx.Signz+"),
                 ItemTypes.GetType("Khanx.WallSignx-"),
                 ItemTypes.GetType("Khanx.WallSignx+"),
                 ItemTypes.GetType("Khanx.WallSignz-"),
                 ItemTypes.GetType("Khanx.WallSignz+")
            };

        public virtual void OnChangedWithType(Chunk chunk, BlockChangeRequestOrigin origin, Vector3Int blockPosition, ItemTypes.ItemType typeOld, ItemTypes.ItemType typeNew)
        {
            //OnRemove
            if (typeNew == BlockTypes.BuiltinBlocks.Types.air)
            {
                SignManager.signs.Remove(blockPosition);

                //Remove the marker
                foreach (var mapNPlayer in Players.PlayerDatabase.Where(pl => pl.Value.ConnectionState == Players.EConnectionState.Connected).ToList())
                {
                    Players.Player player = mapNPlayer.Value;
                    if (Math.ManhattanDistance(new Vector3Int(player.Position), blockPosition) <= SignManager.markerDistance)
                        UIManager.RemoveMarker("Khanx.Sign" + blockPosition + player.Name, player);
                }
            }

            //OnAdd
            if (typeOld == BlockTypes.BuiltinBlocks.Types.air)
            {
                if (SignManager.signs.ContainsKey(blockPosition))
                    return;

                if (origin.Type == BlockChangeRequestOrigin.EType.Player)
                    SignManager.signs.Add(blockPosition, new Sign(origin.AsPlayer.ID, "-"));
                else
                    SignManager.signs.Add(blockPosition, new Sign(new NetworkID(), "-"));
            }
        }
    }
}
