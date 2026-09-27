using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MERE SOULS: 対峙する2本の前線塹壕とNo Man's Landを実測値ベースで構築する。
///
/// 寸法の根拠(第一次大戦・西部戦線の前線塹壕):
///   発掘深さ 1.8m + 胸壁(土嚢) 0.6m = 床から胸壁天端まで 2.4m。
///   立ち目線 1.65m なので床に立つと天端の 0.75m 下となり、外が見えない。
///   火点 0.76m に乗ると目線が天端とほぼ同じ高さに届く。
///   しゃがみ目線 1.00m なら火点上でも天端の下に収まり、隠れられる。
///   この「頭を上げないと見えない / 上げた瞬間だけ晒される」関係が塹壕戦の核。
///
///   ただし天端と同じ高さでは前方の地面が見えないため、胸壁に銃眼を空けている。
///   実物でも土嚢の隙間や鋼板の銃眼から狙った。射界が銃眼に絞られること自体が、
///   敵にとっては「どこから覗くか分かっている」という攻防になる。
///
///   射撃区画(fire bay)は 6m、その間に横木(traverse)1.5m を挟む鋸歯構造。
///   横木は縦射(enfilade)対策で、塹壕の長軸に射線を通させないためのもの。
///   通路は横木の背後に引っ込んだ待避部(recess)を回り込む。
///
/// グレーボックスなのでプリミティブのみで組む。地面を掘り抜けないため、
/// 砲弾孔と聴音哨は「穴」ではなく地表の縁・土嚢で遮蔽を表現している。
/// </summary>
public class TrenchSectorBuilder : MonoBehaviour
{
    [Header("塹壕の断面 (実測値ベース・単位m)")]
    public float digDepth = 1.8f;
    public float bayWidth = 2.0f;
    public float recessWidth = 2.0f;
    public float fireStepDepth = 0.6f;
    public float fireStepHeight = 0.76f;
    public float parapetHeight = 0.6f;
    public float parapetThickness = 0.9f;
    public float paradosHeight = 0.75f;

    /// <summary>
    /// 銃眼(loophole)。火点に立った目線は胸壁天端とほぼ同じ高さにしかならず、
    /// 天端越しでは No Man's Land の地面が見えない。実物でも土嚢の隙間や鋼板の銃眼から狙った。
    /// 敷居を低くした窓を空けることで、そこからだけ地面が見えて撃てるようにする。
    /// </summary>
    public float loopholeWidth = 0.6f;
    public float loopholeSillHeight = 0.25f;

    [Header("塹壕の平面")]
    public float frontage = 60f;
    public float bayLength = 6f;
    public float traverseWidth = 1.5f;
    public float rearGroundDepth = 24f;

    /// <summary>射撃区画の背後を埋める土塊を両端でこれだけ短くし、待避部への通り口を開ける。</summary>
    public float passageOpening = 1.3f;

    [Header("No Man's Land")]
    public float noMansLand = 65f;
    public float wireBeltStart = 4f;
    public float wireBeltDepth = 10f;
    public float sapHeadDistance = 20f;

    private Material earthMat;
    private Material sandbagMat;
    private Material duckboardMat;
    private Material wireMat;

    public void BuildSector()
    {
        CreateMaterials();

        BuildTrenchLine("TrenchLine_South", 0f, +1f);
        BuildTrenchLine("TrenchLine_North", noMansLand, -1f);
        BuildNoMansLand();
    }

    private void CreateMaterials()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");
        if (lit == null) lit = Shader.Find("Diffuse");

        earthMat = new Material(lit) { color = new Color(0.27f, 0.24f, 0.20f) };
        sandbagMat = new Material(lit) { color = new Color(0.48f, 0.44f, 0.33f) };
        duckboardMat = new Material(lit) { color = new Color(0.36f, 0.30f, 0.22f) };
        wireMat = new Material(lit) { color = new Color(0.18f, 0.17f, 0.16f) };
    }

    /// <summary>最小コーナーとサイズでボックスを置く。塹壕は高さ方向の積み上げが多いのでこの形が扱いやすい。</summary>
    private GameObject Box(string name, Transform parent, Vector3 min, Vector3 size, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = min + size * 0.5f;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        return go;
    }

    /// <summary>
    /// 1本の前線塹壕を構築する。frontZ は胸壁側(敵に向いた面)のZ座標、facing は敵のいる向き(+1 / -1)。
    /// 塹壕本体は「掘った穴」なので、周囲に土塊を置いて残った隙間が通路になる。
    /// </summary>
    private void BuildTrenchLine(string lineName, float frontZ, float facing)
    {
        Transform line = new GameObject(lineName).transform;
        line.SetParent(transform, false);

        // ローカル座標では常に「敵のいる方向 = +Z」として組み、facing=-1 の線はY軸180度回転で対置する。
        // 回転するとローカル+Xがワールド-Xを向くため、原点は反対端に置く必要がある。
        line.localPosition = new Vector3(facing > 0f ? -frontage * 0.5f : frontage * 0.5f, 0f, frontZ);
        if (facing < 0f) line.localRotation = Quaternion.Euler(0f, 180f, 0f);

        float floorY = -digDepth;
        float channelWidth = bayWidth + recessWidth;

        // --- 射撃区画と横木のX方向の割り付け ---
        List<Vector2> bays = new List<Vector2>();
        List<Vector2> traverses = new List<Vector2>();
        float pitch = bayLength + traverseWidth;
        float cursor = 0f;
        while (cursor < frontage - 0.01f)
        {
            float bayEnd = Mathf.Min(cursor + bayLength, frontage);
            bays.Add(new Vector2(cursor, bayEnd));
            if (bayEnd >= frontage - 0.01f) break;

            traverses.Add(new Vector2(bayEnd, Mathf.Min(bayEnd + traverseWidth, frontage)));
            cursor += pitch;
        }

        // --- 床 ---
        Box($"{lineName}_Floor_Dirt", line,
            new Vector3(0f, floorY - 0.4f, -channelWidth),
            new Vector3(frontage, 0.4f, channelWidth), earthMat);

        // 踏み板(duckboard)。足音の材質判定が名前を見るので "Wood" を含める。
        for (int i = 0; i < bays.Count; i++)
        {
            Vector2 bay = bays[i];
            Box($"Duckboard_Wood_{i}", line,
                new Vector3(bay.x, floorY, -bayWidth + 0.1f),
                new Vector3(bay.y - bay.x, 0.06f, bayWidth - fireStepDepth - 0.2f), duckboardMat);
        }

        // --- 敵側の土塊(No Man's Landの地面になる)と胸壁 ---
        Box($"{lineName}_ForwardEarth_Dirt", line,
            new Vector3(0f, floorY, 0f),
            new Vector3(frontage, digDepth, parapetThickness), earthMat);

        // --- 胸壁と銃眼 ---
        // 射撃区画ごとに2カ所、敷居だけ残した窓を空ける。ここからしか前方の地面は見えない。
        List<Vector2> loopholes = new List<Vector2>();
        foreach (Vector2 bay in bays)
        {
            foreach (float frac in new[] { 0.33f, 0.66f })
            {
                float center = bay.x + (bay.y - bay.x) * frac;
                loopholes.Add(new Vector2(center - loopholeWidth * 0.5f, center + loopholeWidth * 0.5f));
            }
        }

        float wallCursor = 0f;
        for (int i = 0; i < loopholes.Count; i++)
        {
            Vector2 hole = loopholes[i];
            if (hole.x > wallCursor)
            {
                Box($"Parapet_{i}_Sandbags", line,
                    new Vector3(wallCursor, 0f, 0f),
                    new Vector3(hole.x - wallCursor, parapetHeight, parapetThickness), sandbagMat);
            }

            Box($"LoopholeSill_{i}_Sandbags", line,
                new Vector3(hole.x, 0f, 0f),
                new Vector3(hole.y - hole.x, loopholeSillHeight, parapetThickness), sandbagMat);

            wallCursor = hole.y;
        }
        if (wallCursor < frontage)
        {
            Box($"Parapet_End_Sandbags", line,
                new Vector3(wallCursor, 0f, 0f),
                new Vector3(frontage - wallCursor, parapetHeight, parapetThickness), sandbagMat);
        }

        // --- 火点(fire step) ---
        // 射撃区画にだけ設置する。ここに乗ると目線が胸壁天端を超える。
        for (int i = 0; i < bays.Count; i++)
        {
            Vector2 bay = bays[i];
            Box($"FireStep_{i}", line,
                new Vector3(bay.x, floorY, -fireStepDepth),
                new Vector3(bay.y - bay.x, fireStepHeight, fireStepDepth), earthMat);
        }

        // --- 横木(traverse) ---
        // 射撃区画と同じ奥行きを塞ぐ土塊。ここで射線が切れ、通路は背後の待避部へ迂回する。
        for (int i = 0; i < traverses.Count; i++)
        {
            Vector2 t = traverses[i];
            Box($"Traverse_{i}_Dirt", line,
                new Vector3(t.x, floorY, -bayWidth),
                new Vector3(t.y - t.x, digDepth, bayWidth), earthMat);

            Box($"Traverse_{i}_Cap_Sandbags", line,
                new Vector3(t.x, 0f, -bayWidth),
                new Vector3(t.y - t.x, parapetHeight, bayWidth), sandbagMat);
        }

        // --- 射撃区画の背後を埋める土塊 + 区画ごとの背壁(parados) ---
        // 両端を passageOpening だけ短くして、待避部へ抜ける通り口を残す。
        // ここを塞ぐと各区画が孤立し、塹壕として繋がらなくなる。
        for (int i = 0; i < bays.Count; i++)
        {
            Vector2 bay = bays[i];
            float fillStart = bay.x + passageOpening;
            float fillEnd = bay.y - passageOpening;
            if (fillEnd - fillStart < 0.2f) continue;

            Box($"RearFill_{i}_Dirt", line,
                new Vector3(fillStart, floorY, -channelWidth),
                new Vector3(fillEnd - fillStart, digDepth, recessWidth), earthMat);

            Box($"Parados_{i}_Sandbags", line,
                new Vector3(fillStart, 0f, -bayWidth - 0.9f),
                new Vector3(fillEnd - fillStart, paradosHeight, 0.9f), sandbagMat);
        }

        // --- 後方の地面と、塹壕系全体の背壁 ---
        Box($"{lineName}_RearEarth_Dirt", line,
            new Vector3(0f, floorY, -channelWidth - rearGroundDepth),
            new Vector3(frontage, digDepth, rearGroundDepth), earthMat);

        // 背壁は胸壁より高い。稜線に姿が浮かび上がるのを防ぐための実際の措置。
        // 待避部と通り口は後方に開いているので、こちらは全長に通す。
        Box($"{lineName}_RearParados_Sandbags", line,
            new Vector3(0f, 0f, -channelWidth - 0.9f),
            new Vector3(frontage, paradosHeight, 0.9f), sandbagMat);

        // --- スポーン地点(射撃区画の床) ---
        for (int i = 0; i < bays.Count; i += 3)
        {
            GameObject spawn = new GameObject($"Spawn_{lineName}_{i}");
            spawn.transform.SetParent(line, false);
            spawn.transform.localPosition = new Vector3(
                (bays[i].x + bays[i].y) * 0.5f, floorY + 0.1f, -bayWidth * 0.5f);
        }
    }

    private void BuildNoMansLand()
    {
        Transform nml = new GameObject("NoMansLand").transform;
        nml.SetParent(transform, false);

        // 地面の両端は各塹壕の前面土塊が担うので、ここでは中間帯だけを敷く。
        float gapStart = parapetThickness;
        float gapEnd = noMansLand - parapetThickness;
        Box("NML_Ground_Dirt", nml,
            new Vector3(-frontage * 0.5f, -digDepth, gapStart),
            new Vector3(frontage, digDepth, gapEnd - gapStart), earthMat);

        BuildWireBelt(nml, "WireBelt_South", wireBeltStart);
        BuildWireBelt(nml, "WireBelt_North", noMansLand - wireBeltStart - wireBeltDepth);

        BuildCraters(nml);
        BuildSapHead(nml, "SapHead_South", -8f, sapHeadDistance);
        BuildSapHead(nml, "SapHead_North", 8f, noMansLand - sapHeadDistance);
    }

    /// <summary>
    /// 鉄条網の帯。実際には数十m幅で敷かれ、突破口が限られることで攻撃側を機関銃の射界へ誘導した。
    /// ここでも通り抜けられる隙間を数カ所だけ空け、進路を絞る役割を持たせる。
    /// </summary>
    private void BuildWireBelt(Transform parent, string name, float zStart)
    {
        Transform belt = new GameObject(name).transform;
        belt.SetParent(parent, false);

        const float postSpacing = 3f;
        const float rowSpacing = 2.5f;
        int rows = Mathf.Max(1, Mathf.RoundToInt(wireBeltDepth / rowSpacing));
        int posts = Mathf.Max(2, Mathf.RoundToInt(frontage / postSpacing));

        // 突破口。ここだけ鉄条網が切れていて、両軍ともここへ吸い寄せられる。
        float[] gapCenters = { -frontage * 0.25f, frontage * 0.2f };

        for (int r = 0; r < rows; r++)
        {
            float z = zStart + r * rowSpacing;
            for (int p = 0; p < posts; p++)
            {
                float x = -frontage * 0.5f + p * postSpacing;

                bool inGap = false;
                foreach (float g in gapCenters)
                {
                    if (Mathf.Abs(x - g) < 3.0f) { inGap = true; break; }
                }
                if (inGap) continue;

                Box($"WirePost_{r}_{p}", belt,
                    new Vector3(x - 0.06f, 0f, z - 0.06f),
                    new Vector3(0.12f, 1.1f, 0.12f), wireMat);

                // 支柱間に張られた線。当たり判定を持たせ、進路を物理的に阻む。
                Box($"WireSpan_{r}_{p}", belt,
                    new Vector3(x, 0.35f, z - 0.05f),
                    new Vector3(postSpacing, 0.5f, 0.1f), wireMat);
            }
        }
    }

    /// <summary>砲弾孔。プリミティブでは地面を掘れないので、盛り上がった縁で遮蔽を作る。</summary>
    private void BuildCraters(Transform parent)
    {
        Transform craters = new GameObject("ShellCraters").transform;
        craters.SetParent(parent, false);

        // 毎回同じ配置にしたいので、生成中だけ乱数列を固定して後で元に戻す。
        Random.State prevState = Random.state;
        Random.InitState(20260926);

        for (int i = 0; i < 26; i++)
        {
            float x = Random.Range(-frontage * 0.5f + 2f, frontage * 0.5f - 2f);
            float z = Random.Range(3f, noMansLand - 3f);
            float radius = Random.Range(1.4f, 3.4f);
            float rimHeight = Random.Range(0.45f, 0.95f);

            // Unityのシリンダーは高さ2・半径0.5が既定なので、スケールは実寸の半分を入れる。
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = $"CraterRim_{i}_Dirt";
            rim.transform.SetParent(craters, false);
            rim.transform.localPosition = new Vector3(x, rimHeight * 0.5f, z);
            rim.transform.localScale = new Vector3(radius * 2f, rimHeight * 0.5f, radius * 2f);
            rim.GetComponent<Renderer>().sharedMaterial = earthMat;
        }

        Random.state = prevState;
    }

    /// <summary>
    /// 聴音哨(sap head)。前線から前方へ突出した位置で、1〜2名が敵の動きを聞くために詰めた。
    /// No Man's Landの中間に孤立した遮蔽を置き、前に出る価値と危険を同時に作る。
    /// 本来は掘った穴だが、地面を掘り抜けないので土嚢の胸壁として表現している。
    /// </summary>
    private void BuildSapHead(Transform parent, string name, float x, float z)
    {
        Transform sap = new GameObject(name).transform;
        sap.SetParent(parent, false);
        sap.localPosition = new Vector3(x, 0f, z);

        const float inner = 2.4f;
        const float wall = 0.8f;
        const float sideHeight = 1.3f;   // しゃがめば隠れ、立つと肩から上が出る高さ
        const float frontHeight = 0.9f;  // 敵側だけ低くして覗けるようにする

        Box("Sap_Wall_Left", sap, new Vector3(-inner * 0.5f - wall, 0f, -inner * 0.5f),
            new Vector3(wall, sideHeight, inner), sandbagMat);
        Box("Sap_Wall_Right", sap, new Vector3(inner * 0.5f, 0f, -inner * 0.5f),
            new Vector3(wall, sideHeight, inner), sandbagMat);
        Box("Sap_Wall_Rear", sap, new Vector3(-inner * 0.5f - wall, 0f, -inner * 0.5f - wall),
            new Vector3(inner + wall * 2f, sideHeight, wall), sandbagMat);
        Box("Sap_Wall_Front", sap, new Vector3(-inner * 0.5f - wall, 0f, inner * 0.5f),
            new Vector3(inner + wall * 2f, frontHeight, wall), sandbagMat);

        GameObject marker = new GameObject("SapHead_ListeningPost");
        marker.transform.SetParent(sap, false);
    }
}
