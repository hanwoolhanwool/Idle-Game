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
