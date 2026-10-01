using GLTranslate.Providers.Baidu.Internal;
using System.Security.Cryptography;
using System.Text;

namespace GLTranslate.Providers.Baidu.Tests;

/// <summary>
/// Verifies the signatures Baidu checks its requests by.
/// </summary>
public sealed class BaiduSignatureTests
{
    [Fact]
    public void ForText_IsTheExampleOfTheDocumentation()
    {
        // The example of the platform's documentation for the text endpoint:
        // md5("2015063000000001apple654781234567890").
        string sign = BaiduSignature.ForText("2015063000000001", "apple", "65478", "1234567890");

        Assert.Equal("a1a7461d92e5194c5cae3182b5b24de1", sign);
    }

    [Fact]
    public void ForText_SignsTheTextAsItIs()
    {
        string sign = BaiduSignature.ForText("id", "good morning", "salt", "key");

        Assert.Equal(Convert.ToHexStringLower(MD5.HashData(Encoding.UTF8.GetBytes("idgood morningsaltkey"))), sign);
    }

    [Fact]
    public void ForImage_SignsTheHashOfTheImageNotTheImage()
    {
        byte[] image = [1, 2, 3, 4];

        string imageHash = Convert.ToHexStringLower(MD5.HashData(image));
        string expected = Convert.ToHexStringLower(
            MD5.HashData(Encoding.UTF8.GetBytes("id" + imageHash + "salt" + "APICUID" + "mac" + "key")));

        Assert.Equal(expected, BaiduSignature.ForImage("id", image, "salt", "APICUID", "mac", "key"));
    }

    [Fact]
    public void ForText_IsThirtyTwoLowercaseHexadecimalCharacters()
    {
        string sign = BaiduSignature.ForText("id", "text", "salt", "key");

        Assert.Equal(32, sign.Length);
        Assert.Equal(sign.ToLowerInvariant(), sign);
    }
}
