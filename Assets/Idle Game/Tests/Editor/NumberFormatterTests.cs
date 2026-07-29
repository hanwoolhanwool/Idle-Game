using NUnit.Framework;

/// <summary>
/// 큰 수 축약 표기 검증.
/// <para>
/// 방치형에서 재화는 자릿수가 계속 늘어난다. 표기가 어긋나면 "10억을 100만으로 읽는" 오해가
/// 생기고, 그것이 곧 밸런스 판단 착오로 이어진다. 경계값을 특히 촘촘히 잠근다.
/// </para>
/// </summary>
public sealed class NumberFormatterTests
{
    [TestCase(0L, "0")]
    [TestCase(7L, "7")]
    [TestCase(999L, "999")]
    public void 축약경계_미만은_숫자를_그대로_보여준다(long value, string expected)
    {
        // 999를 "999"가 아니라 "1.00K"로 보여주면 오히려 정보가 줄어든다.
        Assert.AreEqual(expected, NumberFormatter.Format(value));
    }

    [TestCase(1000L, "1.00K")]
    [TestCase(1234L, "1.23K")]
    [TestCase(12345L, "12.3K")]
    [TestCase(123456L, "123K")]
    public void 유효숫자_3자리를_유지한다(long value, string expected)
    {
        // 자릿수가 커져도 문자열 길이가 일정해야 UI 레이아웃이 흔들리지 않는다.
        Assert.AreEqual(expected, NumberFormatter.Format(value));
    }

    [TestCase(1000000L, "1.00M")]
    [TestCase(1000000000L, "1.00B")]
    [TestCase(1000000000000L, "1.00T")]
    [TestCase(1000000000000000L, "1.00Qa")]
    [TestCase(1000000000000000000L, "1.00Qi")]
    public void 단위가_1000배마다_올라간다(long value, string expected)
    {
        Assert.AreEqual(expected, NumberFormatter.Format(value));
    }

    [Test]
    public void 반올림으로_1000이_되는_경계는_다음_단위로_올린다()
    {
        // 999,999를 "1000K"로 적으면 단위 규칙이 무너진다. "1.00M"이 맞다.
        Assert.AreEqual("1.00M", NumberFormatter.Format(999999L));
    }

    [Test]
    public void long_최댓값도_무너지지_않는다()
    {
        // 재화가 상한까지 인플레이션돼도 표기가 깨지거나 예외가 나면 안 된다.
        Assert.AreEqual("9.22Qi", NumberFormatter.Format(long.MaxValue));
    }

    [TestCase(-1500L, "-1.50K")]
    [TestCase(-42L, "-42")]
    public void 음수도_부호를_유지한다(long value, string expected)
    {
        // 차감 내역·디버그 표시에서 음수가 그대로 흐를 수 있다.
        Assert.AreEqual(expected, NumberFormatter.Format(value));
    }

    [Test]
    public void NaN과_무한대는_앱을_멈추지_않고_기호로_흘린다()
    {
        // 밸런스 설정 실수로 0 나누기가 나올 수 있다. 예외 대신 눈에 띄는 문자열로 드러낸다.
        Assert.AreEqual("—", NumberFormatter.Format(double.NaN));
        Assert.AreEqual("—", NumberFormatter.Format(double.PositiveInfinity));
        Assert.AreEqual("—", NumberFormatter.Format(double.NegativeInfinity));
    }

    [Test]
    public void FormatExact은_천단위_구분기호를_넣는다()
    {
        // 상점 가격·정산처럼 축약이 정보를 버리면 곤란한 곳에서 쓴다.
        Assert.AreEqual("1,234,567", NumberFormatter.FormatExact(1234567L));
    }

    [Test]
    public void FormatExact은_로케일에_흔들리지_않는다()
    {
        // 시스템 로케일이 유럽식(1.234.567)이면 자릿수 오해가 생긴다. InvariantCulture로 고정한다.
        Assert.AreEqual("1,000", NumberFormatter.FormatExact(1000L));
    }
}
