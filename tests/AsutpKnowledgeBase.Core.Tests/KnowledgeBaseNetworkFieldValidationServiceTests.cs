using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseNetworkFieldValidationServiceTests
{
    [Theory]
    [InlineData("192.168.0.10", "255.255.255.0", "192.168.0.1")]
    [InlineData("10.0.0.10", "/24", "10.0.0.1")]
    [InlineData("10.0.0.10", "24", "")]
    [InlineData("", "", "")]
    public void ValidateInterfaceAddressFields_ForValidValues_ReturnsSuccess(
        string ipAddress,
        string subnetMask,
        string gateway)
    {
        var result = KnowledgeBaseNetworkFieldValidationService.ValidateInterfaceAddressFields(
            ipAddress,
            subnetMask,
            gateway);

        Assert.True(result.IsSuccess);
        Assert.Equal(KbNetworkInterfaceAddressField.None, result.Field);
    }

    [Fact]
    public void ValidateInterfaceAddressFields_ForInvalidIp_ReturnsIpFailure()
    {
        var result = KnowledgeBaseNetworkFieldValidationService.ValidateInterfaceAddressFields(
            "10.0.0.300",
            "255.255.255.0",
            "10.0.0.1");

        Assert.False(result.IsSuccess);
        Assert.Equal(KbNetworkInterfaceAddressField.IpAddress, result.Field);
        Assert.Contains("IP-адрес", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateInterfaceAddressFields_ForShortIpForm_ReturnsIpFailure()
    {
        var result = KnowledgeBaseNetworkFieldValidationService.ValidateInterfaceAddressFields(
            "10",
            "255.255.255.0",
            "10.0.0.1");

        Assert.False(result.IsSuccess);
        Assert.Equal(KbNetworkInterfaceAddressField.IpAddress, result.Field);
    }

    [Fact]
    public void ValidateInterfaceAddressFields_ForNonContiguousMask_ReturnsMaskFailure()
    {
        var result = KnowledgeBaseNetworkFieldValidationService.ValidateInterfaceAddressFields(
            "10.0.0.10",
            "255.0.255.0",
            "10.0.0.1");

        Assert.False(result.IsSuccess);
        Assert.Equal(KbNetworkInterfaceAddressField.SubnetMask, result.Field);
        Assert.Contains("Маска", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidateInterfaceAddressFields_ForInvalidGateway_ReturnsGatewayFailure()
    {
        var result = KnowledgeBaseNetworkFieldValidationService.ValidateInterfaceAddressFields(
            "10.0.0.10",
            "255.255.255.0",
            "gateway");

        Assert.False(result.IsSuccess);
        Assert.Equal(KbNetworkInterfaceAddressField.Gateway, result.Field);
        Assert.Contains("Шлюз", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("12")]
    [InlineData("12 m")]
    [InlineData("12,5 м")]
    [InlineData("0.5m")]
    [InlineData("")]
    public void ValidateConnectionFields_ForValidLength_ReturnsSuccess(string length)
    {
        var result = KnowledgeBaseNetworkFieldValidationService.ValidateConnectionFields(length);

        Assert.True(result.IsSuccess);
        Assert.Equal(KbNetworkConnectionField.None, result.Field);
    }

    [Theory]
    [InlineData("0 m")]
    [InlineData("-1 m")]
    [InlineData("12 mm")]
    [InlineData("about 12 m")]
    public void ValidateConnectionFields_ForInvalidLength_ReturnsLengthFailure(string length)
    {
        var result = KnowledgeBaseNetworkFieldValidationService.ValidateConnectionFields(length);

        Assert.False(result.IsSuccess);
        Assert.Equal(KbNetworkConnectionField.Length, result.Field);
        Assert.Contains("Длина", result.ErrorMessage, StringComparison.Ordinal);
    }
}
