using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace Weedtopia.Editor.GameSetup
{
    /// <summary>Explicit scoped repair and second-pass idempotence check for the language/UI revision.</summary>
    public static class LocalizationRevisionValidation
    {
        public static void Install()
        {
            BurningHillsLayoutInstaller.RequireSafeEditor();
            LocalizationAuthoring.Apply();
            PremiumConfirmationAuthoring.Apply();
            var before=AuthoredHashes();
            LocalizationAuthoring.Apply();
            PremiumConfirmationAuthoring.Apply();
            var after=AuthoredHashes();
            Directory.CreateDirectory("Docs/LocalizationRevision/2026-10-06");
            var changed=before.Keys.Union(after.Keys).Where(path=>!before.TryGetValue(path,out var oldHash)||!after.TryGetValue(path,out var newHash)||oldHash!=newHash).ToArray();
            File.WriteAllText("Docs/LocalizationRevision/2026-10-06/install.json",JsonUtility.ToJson(new Receipt
            {utc=DateTime.UtcNow.ToString("O"),status=changed.Length==0?"passed":"failed",idempotent=changed.Length==0,changedOnSecondPass=changed},true));
            if(changed.Length>0)throw new InvalidOperationException("Localization repair is not idempotent: "+string.Join(", ",changed));
            AssetDatabase.SaveAssets();
        }
        private static Dictionary<string,string> AuthoredHashes()
        {
            using(var hash=SHA256.Create())
            {
                var files=Directory.GetFiles("Assets/Weedtopia","*",SearchOption.AllDirectories)
                    .Where(path=>path.EndsWith(".unity",StringComparison.Ordinal)||path.EndsWith(".prefab",StringComparison.Ordinal)||path.EndsWith(".asset",StringComparison.Ordinal))
                    .OrderBy(path=>path,StringComparer.Ordinal);
                return files.ToDictionary(path=>path,path=>BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-",""),StringComparer.Ordinal);
            }
        }
        [Serializable]private sealed class Receipt{public string utc,status;public bool idempotent;public string[] changedOnSecondPass;}
    }
}
