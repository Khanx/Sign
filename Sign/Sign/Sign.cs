using System.IO;
using System.Collections.Generic;

using Pipliz;
using Pipliz.JSON;

using Shared;
using NetworkUI;
using BlockEntities;
using NetworkUI.Items;
using colonyserver.Assets.UIGeneration;
using static colonyshared.NetworkUI.UIGeneration.WorldMarkerSettings;

namespace Sign
{
    [BlockEntityAutoLoader]
    public class SignType : IChangedWithType, IMultiBlockEntityMapping
    {
        public ItemTypes.ItemType TypeToRegister { get { return ItemTypes.GetType("Khanx.Sign"); } }

        public IEnumerable<ItemTypes.ItemType> TypesToRegister { get { return types; } }

        ItemTypes.ItemType[] types = new ItemTypes.ItemType[]
            {
                 ItemTypes.GetType("Khanx.Signx-"),
                 ItemTypes.GetType("Khanx.Signx+"),
                 ItemTypes.GetType("Khanx.Signz-"),
                ItemTypes.GetType("Khanx.Signz+")
            };

        public virtual void OnChangedWithType(Chunk chunk, BlockChangeRequestOrigin origin, Vector3Int blockPosition, ItemTypes.ItemType typeOld, ItemTypes.ItemType typeNew)
        {
            //OnRemove
            if (typeNew == BlockTypes.BuiltinBlocks.Types.air)
            {
                SignManager.signs.Remove(blockPosition);

                //Remove the marker
                if(origin.AsPlayer != null)
                    UIManager.RemoveMarker("Khanx.Sign" + blockPosition + origin.AsPlayer.Name, origin.AsPlayer);
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

    public struct Sign
    {
        public NetworkID owner;
        public string text;

        public Sign(NetworkID owner, string text)
        {
            this.owner = owner;
            this.text = text.Trim();
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

            if (!File.Exists(signFile))
                return;

            JSONNode json = JSON.Deserialize(signFile);

            foreach (JSONNode sign in json.LoopArray())
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
            if (File.Exists(signFile))
                File.Delete(signFile);

            if (signs.Count == 0)
                return;

            JSONNode json = new JSONNode(NodeType.Array);

            foreach (Vector3Int pos in signs.Keys)
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

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerClicked, "Khanx.Sign.OnPlayerClickedType")]
        public static void OnPlayerClicked(Players.Player player, Shared.PlayerClickedData playerClickedData)
        {
            if (playerClickedData.ClickType != PlayerClickedData.EClickType.Right)
                return;

            if (playerClickedData.HitType != PlayerClickedData.EHitType.Block)
                return;

            if (!ItemTypes.GetType(playerClickedData.GetVoxelHit().TypeHit).HasParentType(ItemTypes.GetType("Khanx.Sign")))
                return;

            Vector3Int position = playerClickedData.GetVoxelHit().BlockHit;

            if (!signs.ContainsKey(position))
                signs.Add(position, new Sign(player.ID, "-"));

            Sign s = signs[position];

            NetworkMenu signMenu = new NetworkMenu();
            signMenu.Identifier = "Sign";
            signMenu.LocalStorage.SetAs("header", "Sign");

            if (signs[position].owner == player.ID || PermissionsManager.HasPermission(player, "khanx.setsign"))
            {
                InputField inputField = new InputField("Khanx.Sign." + position.x + "." + position.y + "." + position.z, -1, 100);
                inputField.Multiline = true;

                //default value
                signMenu.LocalStorage.SetAs("Khanx.Sign." + position.x + "." + position.y + "." + position.z, s.text);

                signMenu.Items.Add(inputField);
            }
            else
            {
                signMenu.Items.Add(new Label(new LabelData(signs[position].text, UnityEngine.Color.white, UnityEngine.TextAnchor.MiddleCenter, 32)));
            }

            NetworkMenuManager.SendServerPopup(player, signMenu);
        }


        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerEditedNetworkInputfield, "Khanx.Sign.OnPlayerEditedNetworkInputfield")]
        public static void OnPlayerEditedNetworkInputfield(InputfieldEditCallbackData data)
        {
            if (data.InputfieldIdentifier.StartsWith("Khanx.Sign."))
            {
                data.Storage.TryGetAsOrDefault<string>(data.InputfieldIdentifier, out string text, "-");

                string[] sPosition = data.InputfieldIdentifier.Substring(11).Split('.'); // 11 = Khanx.Sign.
                Vector3Int position = new Vector3Int(int.Parse(sPosition[0]), int.Parse(sPosition[1]), int.Parse(sPosition[2]));

                Sign sign = signs.GetValueOrDefault(position, new Sign(data.Player.ID, text));

                sign.text = text;

                if (signs.ContainsKey(position))
                    signs.Remove(position);

                signs.Add(position, sign);
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerMoved, "Khanx.Sign.OnPlayerMoved")]
        public static void SendSignMarker(Players.Player player, UnityEngine.Vector3 position)
        {

            foreach (var singPosition in signs.Keys)
            {
                if (Math.ManhattanDistance(new Vector3Int(player.Position), singPosition) < 10)
                {
                    string singText = signs[singPosition].text;
                    if (singText.Length > 100)
                        singText = singText.Substring(0, 100) + "...";

                    UIManager.AddorUpdateWorldMarker("Khanx.Sign" + singPosition + player.Name,
                                                               singText,
                                                                singPosition + Vector3Int.up,
                                                                "Khanx.Sign",
                                                                ToggleType.AlwaysOn,
                                                                "Khanx.Sign",
                                                                player);
                }
                else
                    UIManager.RemoveMarker("Khanx.Sign" + singPosition + player.Name, player);
            }

        }

    }
}
