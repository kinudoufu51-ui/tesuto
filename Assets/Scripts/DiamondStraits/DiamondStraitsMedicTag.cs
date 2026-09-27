using UnityEngine;

/// <summary>
/// 「今このプレイヤーは看護兵として振る舞っているか」を示すだけの目印。
/// 兵科選択システム(フェーズ4)が入るまでの暫定措置で、本実装では兵科データの一部になる。
/// </summary>
public class DiamondStraitsMedicTag : MonoBehaviour
{
    public bool isMedic = true;

    [Tooltip("1人での検証用: このキーで看護兵/分隊員の切り替えができる。実プレイでは兵科選択に置き換わる。")]
    public KeyCode debugToggleKey = KeyCode.K;

    void Update()
    {
        if (Input.GetKeyDown(debugToggleKey))
        {
            isMedic = !isMedic;
        }
    }
}
