using Unity.Netcode.Components;

/// <summary>
/// 移動を所有者権威にする。既定の NetworkTransform はサーバー権威で、
/// そのままだと自分の操作が一往復してから反映され、塹壕の角で撃ち合うには遅すぎる。
///
/// 2〜3人の身内対戦なので、移動をサーバーで検証する必要はない。
/// </summary>
public class OwnerNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative() => false;
}
