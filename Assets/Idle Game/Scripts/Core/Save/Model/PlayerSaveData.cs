using System;
using System.Collections.Generic;

/// <summary>
/// 세이브 파일의 루트 DTO(순수 데이터 — Unity 의존 없음).
/// "원인을 저장하고 결과는 재계산한다" — 최종 공격력이 아니라 레벨을 저장하고,
/// 로드 시 <c>PlayerLevelTable</c>로 스탯을 다시 산출한다(정본 §5.1).
/// M0 범위는 Progression·Wallet 두 섹션뿐이며, 나머지 섹션은 단계별로 얹는다.
/// </summary>
[Serializable]
public sealed class PlayerSaveData
{
    /// <summary>스키마 버전. 마이그레이션(<see cref="ISaveMigration"/>)의 기준값.</summary>
    public int Version = SaveService.CurrentVersion;

    public ProgressionSaveSection Progression = new();
    public WalletSaveSection Wallet = new();
    public WorldSaveSection World = new();
}

/// <summary>성장 상태 섹션. <c>PlayerProgressionState</c>와 1:1 매핑된다.</summary>
[Serializable]
public sealed class ProgressionSaveSection
{
    public int Level = 1;
    public int Exp;
    public int PromotionTier;
}

/// <summary>
/// 재화 섹션. 통화를 <c>(CurrencyId, Amount)</c> 목록으로 두어 보석·토큰 추가가
/// 엔트리 추가로 끝나게 한다(정본 §12). <c>Dictionary</c>가 아닌 <c>List</c>인 이유는
/// <c>JsonUtility</c>가 딕셔너리를 직렬화하지 못하기 때문이다(정본 §9).
/// </summary>
[Serializable]
public sealed class WalletSaveSection
{
    public List<CurrencyEntry> Balances = new();

    /// <summary>해당 통화의 잔액. 없으면 0(신규 통화가 추가돼도 로드가 깨지지 않는다).</summary>
    public long GetAmount(string currencyId)
    {
        for (int i = 0; i < Balances.Count; i++)
        {
            if (Balances[i].CurrencyId == currencyId)
                return Balances[i].Amount;
        }

        return 0;
    }

    /// <summary>해당 통화의 잔액을 기록한다. 기존 엔트리가 있으면 갱신, 없으면 추가.</summary>
    public void SetAmount(string currencyId, long amount)
    {
        for (int i = 0; i < Balances.Count; i++)
        {
            if (Balances[i].CurrencyId == currencyId)
            {
                Balances[i].Amount = amount;
                return;
            }
        }

        Balances.Add(new CurrencyEntry { CurrencyId = currencyId, Amount = amount });
    }
}

/// <summary>통화 하나의 잔액. 금액은 인플레이션 대비 <see cref="long"/>(정본 §11).</summary>
[Serializable]
public sealed class CurrencyEntry
{
    public string CurrencyId;
    public long Amount;
}

/// <summary>
/// 월드 진행 섹션(스키마 v2에서 추가). 스테이지 진척과 <b>시간 기준선</b>을 담는다.
/// <para>
/// 스테이지를 배열 인덱스가 아니라 <see cref="StageId"/> 문자열로 저장하는 이유:
/// 인덱스를 쓰면 스테이지를 중간에 하나 끼워 넣는 순간 모든 기존 유저의 진행이 한 칸씩 밀린다.
/// 식별자는 순서와 무관하며, 목록에서 사라진 id는 첫 스테이지로 폴백하면 된다.
/// </para>
/// </summary>
[Serializable]
public sealed class WorldSaveSection
{
    /// <summary>현재 스테이지 식별자. 비어 있으면 첫 스테이지에서 시작한다.</summary>
    public string StageId = string.Empty;

    /// <summary>현재 스테이지에서 누적한 처치 수.</summary>
    public int KillsInStage;

    /// <summary>
    /// 최근 실측 처치율(초당). 오프라인 보상 환산의 입력이다.
    /// 이론값이 아니라 실측을 쓰는 이유는 온라인과 오프라인이 같은 지표를 공유하게 하기 위함이다.
    /// </summary>
    public float KillsPerSecond;

    /// <summary>
    /// 마지막 저장 시각(UTC Ticks). <c>DateTime</c>을 직접 담지 않는 이유는
    /// <c>JsonUtility</c>가 이를 직렬화하지 못하기 때문이다.
    /// 0이면 기준선이 없다는 뜻이라 오프라인 보상을 지급하지 않는다.
    /// </summary>
    public long LastSaveUtcTicks;
}
