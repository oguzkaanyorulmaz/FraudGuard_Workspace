namespace FraudGuard.Domain.Common.Constants
{
    public static class RuleThresholdConstants
    {
        public const int VelocityTimeWindowMinutes = 5;
        public const int VelocityMaxAllowed = 2;
        public const decimal HighAmountThreshold = 100000m;
        public const int MaxBruteForceAttempts = 3;
    }
}