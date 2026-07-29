using System;
using System.Globalization;

/// <summary>
/// 방치형 게임의 큰 수를 사람이 읽을 수 있는 짧은 문자열로 바꾼다.
/// 골드가 <see cref="long"/>이라 후반부에는 자릿수가 20자리에 육박하는데,
/// 그대로 표시하면 UI를 넘치고 크기 비교도 불가능해진다.
/// <para>
/// 표기 규칙은 <b>유효숫자 3자리 + 단위</b>다(1234 → "1.23K", 45600 → "45.6K", 987000 → "987K").
/// 자릿수가 변해도 문자열 길이가 일정해 UI 레이아웃이 흔들리지 않는다.
/// </para>
/// </summary>
public static class NumberFormatter
{
    /// <summary>
    /// 1000배씩 올라가는 단위. 배열 끝을 넘는 값은 마지막 단위로 뭉뚱그린다.
    /// <see cref="long"/>의 최댓값(약 9.22e18)이 "9.22Qi"로 표현되므로 M0 범위에서는 충분하다.
    /// 재화 인플레이션이 이 위로 올라가면 항목만 덧붙이면 된다(OCP).
    /// </summary>
    private static readonly string[] Units = { "", "K", "M", "B", "T", "Qa", "Qi" };

    /// <summary>축약을 시작하는 경계. 이 아래는 원래 숫자가 더 읽기 쉽다.</summary>
    private const double AbbreviateThreshold = 1000d;

    /// <summary>정수 재화(골드 등)를 축약 표기한다.</summary>
    public static string Format(long value) => Format((double)value);

    /// <summary>
    /// 실수 값(스탯·DPS 등)을 축약 표기한다.
    /// <para>
    /// 반환값은 매번 새 문자열이라 <b>매 프레임 호출은 피한다</b>.
    /// HUD는 값이 실제로 바뀐 프레임에만 갱신하도록 설계되어 있다(<c>PlayerHudBinder</c>).
    /// </para>
    /// </summary>
    public static string Format(double value)
    {
        // NaN·무한대는 밸런스 설정 실수로 나올 수 있다. 앱을 멈추는 대신 눈에 띄는 문자열로 흘린다.
        if (double.IsNaN(value) || double.IsInfinity(value))
            return "—";

        bool negative = value < 0d;
        double abs = Math.Abs(value);

        if (abs < AbbreviateThreshold)
            return (negative ? "-" : string.Empty) + ((long)abs).ToString(CultureInfo.InvariantCulture);

        // 로그 대신 나눗셈 반복을 쓰는 이유: Math.Log(1000, 1000)이 부동소수 오차로 0.999…가 되어
        // 단위가 한 칸 밀리는 사고를 막는다. 최대 6회라 비용도 무시할 수 있다.
        int tier = 0;
        double scaled = abs;
        while (scaled >= AbbreviateThreshold && tier < Units.Length - 1)
        {
            scaled /= AbbreviateThreshold;
            tier++;
        }

        // 999.6K가 "1000K"로 반올림되는 경계를 한 단계 올려 "1.00M"으로 표기한다.
        if (scaled >= 999.5d && tier < Units.Length - 1)
        {
            scaled /= AbbreviateThreshold;
            tier++;
        }

        return (negative ? "-" : string.Empty) + FormatMantissa(scaled) + Units[tier];
    }

    /// <summary>
    /// 천 단위 구분 기호를 넣은 정확한 표기(예: 1,234,567).
    /// 축약이 정보를 버리면 곤란한 곳 — 상점 가격·정산 내역 — 에서 쓴다.
    /// </summary>
    public static string FormatExact(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>
    /// 가수부를 유효숫자 3자리로 맞춘다. 소수 자릿수를 크기에 따라 바꾸는 이유는
    /// "1.23K"와 "123K"의 정보량을 같게 유지하기 위해서다.
    /// </summary>
    private static string FormatMantissa(double scaled)
    {
        if (scaled >= 100d)
            return scaled.ToString("F0", CultureInfo.InvariantCulture);

        return scaled >= 10d
            ? scaled.ToString("F1", CultureInfo.InvariantCulture)
            : scaled.ToString("F2", CultureInfo.InvariantCulture);
    }
}
