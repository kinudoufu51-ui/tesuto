using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 豆戦車をネットワーク上で成立させる追加コンポーネント。MiniTankController 自体は
/// ネットワークを一切知らない設計を保ったまま、これを後から足すだけで同期が有効になる
/// (ネットワーク無しの検証シーンでは、このコンポーネントごと外せばよい)。
///
/// 操縦手が乗っている間だけ、その人のクライアントを NetworkObject のオーナーにする。
/// オーナー権威の OwnerNetworkTransform と組み合わせることで、実際に操縦している人の
/// ローカル物理シミュレーションがそのまま他クライアントへ伝わる(兵士の移動同期と同じ考え方)。
///
/// 非オーナー側は Rigidbody を Kinematic にする。物理を動かしたまま NetworkTransform で
/// 位置を上書きすると、重力や衝突とネットワーク補正が競合してガタつくため。
/// </summary>
[RequireComponent(typeof(MiniTankController))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class MiniTankNetworkSync : NetworkBehaviour
{
    private MiniTankController controller;
    private Rigidbody body;

    void Awake()
    {
        controller = GetComponent<MiniTankController>();
        body = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        controller.OnDriverEntered += HandleDriverEntered;
        controller.OnDriverExited += HandleDriverExited;
    }

    void OnDisable()
    {
        controller.OnDriverEntered -= HandleDriverEntered;
        controller.OnDriverExited -= HandleDriverExited;
    }

    public override void OnNetworkSpawn()
    {
        ApplyKinematicState();
    }

    public override void OnGainedOwnership()
    {
        ApplyKinematicState();
    }

    public override void OnLostOwnership()
    {
        ApplyKinematicState();
    }

    private void ApplyKinematicState()
    {
        body.isKinematic = !IsOwner;
    }

    private void HandleDriverEntered(UniversalFPSController driver)
    {
        DiamondStraitsNetworkPlayer netPlayer = driver.GetComponent<DiamondStraitsNetworkPlayer>();
        if (netPlayer == null) return; // ネットワーク無しの検証プレイヤーは所有権を動かす必要が無い

        RequestOwnershipServerRpc(netPlayer.OwnerClientId);
    }

    private void HandleDriverExited()
    {
        RequestOwnershipServerRpc(NetworkManager.ServerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestOwnershipServerRpc(ulong newOwnerClientId)
    {
        NetworkObject.ChangeOwnership(newOwnerClientId);
    }
}
