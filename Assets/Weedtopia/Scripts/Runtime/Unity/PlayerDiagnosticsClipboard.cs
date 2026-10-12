using System;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

[assembly: InternalsVisibleTo("Weedtopia.Game.AssetTests")]
[assembly: InternalsVisibleTo("Weedtopia.Game.PlayModeTests")]

namespace Weedtopia.Unity
{
    public enum DiagnosticsCopyResult { Copied, Empty, Failed }

    /// <summary>Explicit support action; exports the bounded, already redacted journal without reading a game session.</summary>
    public static class PlayerDiagnosticsClipboard
    {
        public static DiagnosticsCopyResult Copy()
            => Copy(LocalPlayerDiagnostics.ReadSnapshot, value => GUIUtility.systemCopyBuffer = value);

        internal static DiagnosticsCopyResult Copy(Func<string> readSnapshot, Action<string> writeClipboard)
        {
            try
            {
                string snapshot = readSnapshot();
                if (string.IsNullOrWhiteSpace(snapshot)) return DiagnosticsCopyResult.Empty;
                if (Encoding.UTF8.GetByteCount(snapshot) > LocalPlayerDiagnostics.MaximumFileBytes)
                    return DiagnosticsCopyResult.Failed;
                writeClipboard(snapshot);
                return DiagnosticsCopyResult.Copied;
            }
            catch (Exception) { return DiagnosticsCopyResult.Failed; }
        }
    }
}
