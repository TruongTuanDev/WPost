using System;
using System.Text.RegularExpressions;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public static class GtinValidator
    {
        private static readonly Regex NumericRegex = new(@"^\d+$", RegexOptions.Compiled);

        /// <summary>
        /// Validates GTIN structure according to GS1 rules:
        /// 1. Only ASCII digits (0-9).
        /// 2. Length in {8, 12, 13, 14}.
        /// 3. GS1 Modulo 10 Check Digit calculation.
        /// </summary>
        public static GtinStructureStatus ValidateStructure(string? rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return GtinStructureStatus.FAIL_FORMAT;
            }

            var clean = rawValue.Trim();

            // Check non-numeric characters (e.g. Cyrillic/Latin 'O', spaces, dashes)
            if (!NumericRegex.IsMatch(clean))
            {
                return GtinStructureStatus.FAIL_NON_NUMERIC;
            }

            // Check length
            if (clean.Length != 8 && clean.Length != 12 && clean.Length != 13 && clean.Length != 14)
            {
                return GtinStructureStatus.FAIL_FORMAT;
            }

            // Calculate check digit
            int expectedCheckDigit = CalculateGs1CheckDigit(clean.Substring(0, clean.Length - 1));
            int actualCheckDigit = clean[clean.Length - 1] - '0';

            if (expectedCheckDigit != actualCheckDigit)
            {
                return GtinStructureStatus.FAIL_CHECK_DIGIT;
            }

            return GtinStructureStatus.PASS;
        }

        /// <summary>
        /// Calculates the GS1 Mod-10 check digit for any payload string (without check digit).
        /// Weights alternate 3 and 1 from right to left.
        /// </summary>
        public static int CalculateGs1CheckDigit(string payloadWithoutCheckDigit)
        {
            if (string.IsNullOrEmpty(payloadWithoutCheckDigit)) return 0;

            int sum = 0;
            int weight = 3;

            for (int i = payloadWithoutCheckDigit.Length - 1; i >= 0; i--)
            {
                char c = payloadWithoutCheckDigit[i];
                if (c < '0' || c > '9') return -1;

                int digit = c - '0';
                sum += digit * weight;
                weight = (weight == 3) ? 1 : 3;
            }

            return (10 - (sum % 10)) % 10;
        }

        /// <summary>
        /// Converts structurally valid GTIN to standard 14-digit representation with leading zeros.
        /// </summary>
        public static string? ToCanonical14(string? rawValue)
        {
            if (ValidateStructure(rawValue) != GtinStructureStatus.PASS)
            {
                return null;
            }

            var clean = rawValue!.Trim();
            return clean.PadLeft(14, '0');
        }

        /// <summary>
        /// Evaluates if two GTIN representations share the same identity (e.g. GTIN-13 vs 14-digit padded).
        /// </summary>
        public static bool AreSameIdentity(string? gtinA, string? gtinB)
        {
            var canonicalA = ToCanonical14(gtinA);
            var canonicalB = ToCanonical14(gtinB);

            if (canonicalA == null || canonicalB == null) return false;
            return string.Equals(canonicalA, canonicalB, StringComparison.Ordinal);
        }
    }
}
