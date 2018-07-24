using System.IO;
using System.Collections.Generic;

using Pipliz;
using Pipliz.JSON;

using ExtendedAPI.Types;
using ExtendedAPI.Commands;
using Shared;
using NetworkUI;

namespace Sign
{
    [AutoLoadCommand]
    public class SignCommand : BaseCommand
    {
        public static Dictionary<NetworkID, string> setSign = new Dictionary<NetworkID, string>();

        public SignCommand() { startWith.Add("/setsign"); }

        public override bool TryDoCommand(Players.Player player, string command)
        {
            string text = command.Substring(command.IndexOf(" ") + 1);

            setSign.Add(player.ID, text);
            return true;
        }
    }

    [AutoLoadType]
    public class SignType : BaseType
    {
        public SignType() { key = "Khanx.Sign"; }

        public override void OnRightClickOn(Players.Player player, Box<PlayerClickedData> boxedData)
        {
            Vector3Int position = boxedData.item1.VoxelHit;

            if(!SignManager.signs.ContainsKey(position))
                return;

            if(SignCommand.setSign.ContainsKey(player.ID))
            {
                if(SignManager.signs[position].owner == player.ID || Permissions.PermissionsManager.HasPermission(player, "khanx.setsign"))
                {
                    Sign sign = SignManager.signs[position];

                    SignManager.signs[position] = new Sign(sign.owner, SignCommand.setSign[player.ID]);

                    SignCommand.setSign.Remove(player.ID);

                    Pipliz.Chatting.Chat.Send(player, "<color=lime>You have changed the text of the sign.</color>");

                    Players.Player owner = Players.GetPlayer(sign.owner);

                    if(player != owner)
                        if(null != owner && owner.IsConnected)
                            Pipliz.Chatting.Chat.Send(owner, string.Format("<color=lime>{0} has changed the text of your sign.</color>", player.Name));
                }
                else
                {
                    Pipliz.Chatting.Chat.Send(player, "<color=orange>Only the owner of the sign can change it.</color>");
                    SignCommand.setSign.Remove(player.ID);
                }
            }

            NetworkMenu menu = new NetworkMenu();

            menu.LocalStorage.SetAs("header", "Sign");
            menu.Items.Add(new NetworkUI.Items.Label(new LabelData(SignManager.signs[position].text, UnityEngine.Color.white, UnityEngine.TextAnchor.MiddleCenter, 32)));

            NetworkMenuManager.SendServerPopup(player, menu);
        }

        public override void RegisterOnAdd(Vector3Int position, ushort newType, Players.Player causedBy)
        {
            SignManager.signs.Add(position, new Sign(causedBy.ID, "-"));
            Pipliz.Chatting.Chat.Send(causedBy, "<color=lime>You can use /setsign <text> & use the sign to set the text of the sign.</color>");
        }

        public override void RegisterOnRemove(Vector3Int position, ushort type, Players.Player causedBy)
        {
            SignManager.signs.Remove(position);
        }
    }

    public struct Sign
    {
        public NetworkID owner;
        public string text;

        public Sign(NetworkID owner, string text)
        {
            this.owner = owner;
            this.text = text;
        }
    }

    [ModLoader.ModManager]
    public static class SignManager
    {
        public static string signFile;
        public static Dictionary<Vector3Int, Sign> signs = new Dictionary<Vector3Int, Sign>();

        [ModLoader.ModCallback(ModLoader.EModCallbackType.AfterWorldLoad, "Khanx.Sign.Load")]
        public static void LoadSigns()
        {
            signFile = "./gamedata/savegames/" + ServerManager.WorldName + "/signs.json";

            if(!File.Exists(signFile))
                return;

            JSONNode json = JSON.Deserialize(signFile);

            foreach(JSONNode sign in json.LoopArray())
            {
                Vector3Int pos = (Vector3Int)sign["position"];
                NetworkID owner = NetworkID.Parse(sign.GetAs<string>("owner"));
                string text = sign.GetAs<string>("text");

                signs.Add(pos, new Sign(owner, text));
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnAutoSaveWorld, "Khanx.Sign.AutoSave")]
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnQuit, "Khanx.Sign.Save")]
        public static void SaveSigns()
        {
            if(File.Exists(signFile))
                File.Delete(signFile);

            if(signs.Count == 0)
                return;

            JSONNode json = new JSONNode(NodeType.Array);

            foreach(Vector3Int pos in signs.Keys)
            {
                Sign sign = signs[pos];

                JSONNode jsonSign = new JSONNode(NodeType.Object);
                jsonSign.SetAs("position", (JSONNode)pos);
                jsonSign.SetAs("owner", sign.owner.ToString());
                jsonSign.SetAs("text", sign.text);

                json.AddToArray(jsonSign);
            }

            JSON.Serialize(signFile, json, 2);
        }

    }
}
