using System.Globalization;
using System.Text.RegularExpressions;

namespace AsutpKnowledgeBase.Services
{
    public enum KbNetworkInterfaceAddressField
    {
        None = 0,
        IpAddress = 1,
        SubnetMask = 2,
        Gateway = 3
    }

    public enum KbNetworkConnectionField
    {
        None = 0,
        Length = 1
    }

    public sealed class KnowledgeBaseNetworkFieldValidationResult
    {
        public bool IsSuccess { get; init; }

        public KbNetworkInterfaceAddressField Field { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseNetworkConnectionFieldValidationResult
    {
        public bool IsSuccess { get; init; }

        public KbNetworkConnectionField Field { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;
    }

    public static class KnowledgeBaseNetworkFieldValidationService
    {
        public static KnowledgeBaseNetworkFieldValidationResult ValidateInterfaceAddressFields(
            string? ipAddress,
            string? subnetMask,
            string? gateway)
        {
            if (!IsBlankOrIpv4Address(ipAddress))
            {
                return Failure(
                    KbNetworkInterfaceAddressField.IpAddress,
                    "Поле \"IP-адрес\" должно содержать IPv4-адрес, например 192.168.0.10.");
            }

            if (!IsBlankOrSubnetMask(subnetMask))
            {
                return Failure(
                    KbNetworkInterfaceAddressField.SubnetMask,
                    "Поле \"Маска\" должно содержать IPv4-маску, например 255.255.255.0, или префикс /24.");
            }

            if (!IsBlankOrIpv4Address(gateway))
            {
                return Failure(
                    KbNetworkInterfaceAddressField.Gateway,
                    "Поле \"Шлюз\" должно содержать IPv4-адрес, например 192.168.0.1.");
            }

            return new KnowledgeBaseNetworkFieldValidationResult { IsSuccess = true };
        }

        public static KnowledgeBaseNetworkConnectionFieldValidationResult ValidateConnectionFields(string? length)
        {
            if (!IsBlankOrCableLength(length))
            {
                return ConnectionFailure(
                    KbNetworkConnectionField.Length,
                    "Поле \"Длина\" должно содержать положительное число, например 12, 12 m или 12,5 м.");
            }

            return new KnowledgeBaseNetworkConnectionFieldValidationResult { IsSuccess = true };
        }

        private static KnowledgeBaseNetworkFieldValidationResult Failure(
            KbNetworkInterfaceAddressField field,
            string errorMessage) =>
            new()
            {
                IsSuccess = false,
                Field = field,
                ErrorMessage = errorMessage
            };

        private static KnowledgeBaseNetworkConnectionFieldValidationResult ConnectionFailure(
            KbNetworkConnectionField field,
            string errorMessage) =>
            new()
            {
                IsSuccess = false,
                Field = field,
                ErrorMessage = errorMessage
            };

        private static bool IsBlankOrIpv4Address(string? value) =>
            string.IsNullOrWhiteSpace(value) || IsIpv4Address(value.Trim());

        private static bool IsBlankOrCableLength(string? value)
        {
            string normalized = (value ?? string.Empty).Trim().Replace('\u00A0', ' ');
            if (string.IsNullOrWhiteSpace(normalized))
                return true;

            Match match = Regex.Match(
                normalized,
                @"^(?<value>\d+(?:[,.]\d+)?)\s*(?<unit>m|м|meter|meters|метр|метра|метров)?$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!match.Success)
                return false;

            string numericText = match.Groups["value"].Value.Replace(',', '.');
            return decimal.TryParse(
                    numericText,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out decimal parsedLength) &&
                parsedLength > 0M;
        }

        private static bool IsBlankOrSubnetMask(string? value)
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                return true;

            if (normalized.StartsWith("/", StringComparison.Ordinal))
                normalized = normalized[1..].Trim();

            if (int.TryParse(normalized, out int prefixLength))
                return prefixLength is >= 0 and <= 32;

            return IsContiguousIpv4Mask(normalized);
        }

        private static bool IsIpv4Address(string value) =>
            TryParseDottedIpv4(value, out _);

        private static bool IsContiguousIpv4Mask(string value)
        {
            if (!TryParseDottedIpv4(value, out byte[] bytes))
                return false;

            uint mask =
                (uint)bytes[0] << 24 |
                (uint)bytes[1] << 16 |
                (uint)bytes[2] << 8 |
                bytes[3];
            uint inverted = ~mask;
            return (inverted & (inverted + 1)) == 0;
        }

        private static bool TryParseDottedIpv4(string value, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            string[] parts = value.Split('.');
            if (parts.Length != 4)
                return false;

            var parsed = new byte[4];
            for (int index = 0; index < parts.Length; index++)
            {
                if (!byte.TryParse(parts[index], out byte part))
                    return false;

                parsed[index] = part;
            }

            bytes = parsed;
            return true;
        }
    }
}
