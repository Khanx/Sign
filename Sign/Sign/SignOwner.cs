using System.Collections.Generic;

using Pipliz;

using Chatting;

namespace Sign
{
    [ChatCommandAutoLoader]
    public class SignOwner : IChatCommand
    {
        public bool TryDoCommand(Players.Player player, string chat, List<string> splits)
        {
            if (!chat.Trim().ToLower().Equals("/sign_owner"))
                return false;

            if (!PermissionsManager.CheckAndWarnPermission(player, "khanx.signowner"))
            {
                return true;
            }

            foreach (var singPosition in SignManager.signs.Keys)
            {
                if (Math.ManhattanDistance(new Vector3Int(player.Position), singPosition) < SignManager.markerDistance)
                {
                    Sign sign = SignManager.signs[singPosition];

                    if (Players.TryGetPlayer(sign.owner, out Players.Player plOwner))
                        Chat.Send(player, string.Format("{0} is the owner of the sign located in ({1}, {2}, {3})", plOwner.Name, singPosition.x, singPosition.y, singPosition.z));
                    else
                        Chat.Send(player, string.Format("<color=red>The owner has not been identified</color> of the sign located in ({0}, {1}, {2})", singPosition.x, singPosition.y, singPosition.z));
                }
            }

            return true;
        }
    }
}