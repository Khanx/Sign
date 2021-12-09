using System.Collections.Generic;

using Pipliz;


using NetworkUI;
using NetworkUI.Items;

using colonyserver.Assets.UIGeneration;
using static colonyshared.NetworkUI.UIGeneration.WorldMarkerSettings;
using Shared;

using Saving;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace Sign
{
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

    public struct Vector3
    {
        public int x, y, z;

        public Vector3(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3Int getVector3Int()
        {
            return new Vector3Int(x, y, z);
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
            WorldDB worldDataBase = ServerManager.SaveManager.WorldDataBase;

            if (worldDataBase != null)
            {
                if (worldDataBase.TryGetWorldKeyValue("Signs", out JToken jSigns) && jSigns != null)
                {
                    signs = JsonConvert.DeserializeObject<List< KeyValuePair<Vector3Int, Sign>>>(jSigns.ToString(), new Vector3IntConverter()).ToDictionary(kx => kx.Key, kx => kx.Value);
                }
            }
        }

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnAutoSaveWorld, "Khanx.Sign.AutoSave")]
        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnQuit, "Khanx.Sign.Save")]
        public static void SaveSigns()
        {
            WorldDB worldDataBase = ServerManager.SaveManager.WorldDataBase;
            if (worldDataBase != null)
            {
                if (signs.Count == 0)
                {
                    worldDataBase.SetWorldKeyValue("Signs", "");
                }
                else
                {
                    string json = JsonConvert.SerializeObject(signs.ToArray(), new Vector3IntConverter());
                    worldDataBase.SetWorldKeyValue("Signs", JToken.Parse(json));
                }
                    
            }
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

            NetworkMenu signMenu = new NetworkMenu
            {
                Identifier = "Sign"
            };
            signMenu.LocalStorage.SetAs("header", "Sign");

            if (signs[position].owner == player.ID || PermissionsManager.HasPermission(player, "khanx.setsign"))
            {
                InputField inputField = new InputField("Khanx.Sign." + position.x + "." + position.y + "." + position.z, -1, 100)
                {
                    Multiline = true
                };

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
                string text = data.Storage.GetAsOrDefault<string>(data.InputfieldIdentifier, "-");

                string[] sPosition = data.InputfieldIdentifier.Substring(11).Split('.'); // 11 = Khanx.Sign.
                Vector3Int position = new Vector3Int(int.Parse(sPosition[0]), int.Parse(sPosition[1]), int.Parse(sPosition[2]));

                Sign sign = signs.GetValueOrDefault(position, new Sign(data.Player.ID, text));

                sign.text = text;

                if (signs.ContainsKey(position))
                    signs.Remove(position);

                signs.Add(position, sign);
            }
        }

        public static readonly int markerDistance = 10;

        [ModLoader.ModCallback(ModLoader.EModCallbackType.OnPlayerMoved, "Khanx.Sign.OnPlayerMoved")]
        public static void SendSignMarker(Players.Player player, UnityEngine.Vector3 position)
        {
            foreach (var singPosition in signs.Keys)
            {
                if (Math.ManhattanDistance(new Vector3Int(player.Position), singPosition) < markerDistance)
                {
                    string singText = signs[singPosition].text;
                    if (singText.Length > 100)
                        singText = singText.Substring(0, 100) + "...";

                    UIManager.AddorUpdateWorldMarker("Khanx.Sign" + singPosition + player.Name,
                                                               singText,
                                                                singPosition + Vector3Int.up,
                                                                ItemTypes.GetType("Khanx.Sign").Icon,
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
