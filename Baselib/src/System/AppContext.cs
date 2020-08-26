
namespace system
{

    public sealed class AppContext
    {

        public static bool TryGetSwitch (string switchName, out bool isEnabled)
        {
            // should query property or environment variable?
            isEnabled = false;
            return false;
        }

    }



    public static class AppContextDefaultValues
    {
        // internal static readonly string SwitchNoAsyncCurrentCulture = "Switch.System.Globalization.NoAsyncCurrentCulture";
        // internal static readonly string SwitchEnforceJapaneseEraYearRanges = "Switch.System.Globalization.EnforceJapaneseEraYearRanges";
        // internal static readonly string SwitchFormatJapaneseFirstYearAsANumber = "Switch.System.Globalization.FormatJapaneseFirstYearAsANumber";
        // internal static readonly string SwitchEnforceLegacyJapaneseDateParsing = "Switch.System.Globalization.EnforceLegacyJapaneseDateParsing";
        internal static readonly string SwitchThrowExceptionIfDisposedCancellationTokenSource = "Switch.System.Threading.ThrowExceptionIfDisposedCancellationTokenSource";
        // internal static readonly string SwitchPreserveEventListnerObjectIdentity = "Switch.System.Diagnostics.EventSource.PreserveEventListnerObjectIdentity";
        // internal static readonly string SwitchUseLegacyPathHandling = "Switch.System.IO.UseLegacyPathHandling";
        // internal static readonly string SwitchBlockLongPaths = "Switch.System.IO.BlockLongPaths";
        // internal static readonly string SwitchDoNotAddrOfCspParentWindowHandle = "Switch.System.Security.Cryptography.DoNotAddrOfCspParentWindowHandle";
        // internal static readonly string SwitchSetActorAsReferenceWhenCopyingClaimsIdentity = "Switch.System.Security.ClaimsIdentity.SetActorAsReferenceWhenCopyingClaimsIdentity";
        // internal static readonly string SwitchIgnorePortablePDBsInStackTraces = "Switch.System.Diagnostics.IgnorePortablePDBsInStackTraces";
        // internal static readonly string SwitchUseNewMaxArraySize = "Switch.System.Runtime.Serialization.UseNewMaxArraySize";
        // internal static readonly string SwitchUseConcurrentFormatterTypeCache = "Switch.System.Runtime.Serialization.UseConcurrentFormatterTypeCache";
        // internal static readonly string SwitchUseLegacyExecutionContextBehaviorUponUndoFailure = "Switch.System.Threading.UseLegacyExecutionContextBehaviorUponUndoFailure";
        // internal static readonly string SwitchCryptographyUseLegacyFipsThrow = "Switch.System.Security.Cryptography.UseLegacyFipsThrow";
        // internal static readonly string SwitchDoNotMarshalOutByrefSafeArrayOnInvoke = "Switch.System.Runtime.InteropServices.DoNotMarshalOutByrefSafeArrayOnInvoke";
        internal static readonly string SwitchUseNetCoreTimer = "Switch.System.Threading.UseNetCoreTimer";
    }

}
