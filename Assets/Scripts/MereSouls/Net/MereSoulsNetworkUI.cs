using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

/// <summary>
/// 2〜3人で試すためだけの接続UI。ロビーも入力欄の作り込みもせず、
/// 同じLAN内でホストを立てて繋ぐところまでを最短で済ませる。
/// </summary>
public class MereSoulsNetworkUI : MonoBehaviour
{
    public string joinAddress = "127.0.0.1";
    public ushort port = 7777;

    private GUIStyle boxStyle;
    private GUIStyle labelStyle;

    void OnGUI()
    {
        NetworkManager net = NetworkManager.Singleton;
        if (net == null) return;

        InitStyles();

        GUILayout.BeginArea(new Rect(Screen.width - 236f, 16f, 220f, 190f), boxStyle);

        if (!net.IsClient && !net.IsServer)
        {
            GUILayout.Label("■ 接続", labelStyle);
            GUILayout.Space(4f);

            GUILayout.Label("相手のIPアドレス", labelStyle);
            joinAddress = GUILayout.TextField(joinAddress);

            GUILayout.Space(4f);
            if (GUILayout.Button("ホストとして開く")) StartHost(net);
            if (GUILayout.Button("参加する")) StartClient(net);
        }
        else
        {
            string role = net.IsHost ? "ホスト" : (net.IsServer ? "サーバー" : "クライアント");
            GUILayout.Label($"■ {role}", labelStyle);

            // 接続者一覧はサーバー側しか持っていない。クライアントから読むと警告が出る。
            if (net.IsServer)
            {
                GUILayout.Label($"接続中の兵士: {net.ConnectedClientsIds.Count}", labelStyle);
            }
            else
            {
                GUILayout.Label(net.IsConnectedClient ? "接続済み" : "接続中...", labelStyle);
            }

            GUILayout.Space(6f);
            if (GUILayout.Button("切断")) net.Shutdown();
        }

        GUILayout.EndArea();
    }

    private void StartHost(NetworkManager net)
    {
        ConfigureTransport(net, "0.0.0.0");
        net.StartHost();
    }

    private void StartClient(NetworkManager net)
    {
        ConfigureTransport(net, joinAddress);
        net.StartClient();
    }

    private void ConfigureTransport(NetworkManager net, string address)
    {
        UnityTransport transport = net.GetComponent<UnityTransport>();
        if (transport == null) return;

        transport.SetConnectionData(address, port);
    }

    private void InitStyles()
    {
        if (boxStyle != null) return;

        Texture2D bg = new Texture2D(1, 1);
        bg.SetPixel(0, 0, new Color(0.05f, 0.07f, 0.1f, 0.88f));
        bg.Apply();

        boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = bg;
        boxStyle.padding = new RectOffset(10, 10, 8, 8);

        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
    }
}
