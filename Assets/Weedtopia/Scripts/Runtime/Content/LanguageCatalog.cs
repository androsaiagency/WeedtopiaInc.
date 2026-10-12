using System;
using System.Linq;
using UnityEngine;
using Weedtopia.Domain;

namespace Weedtopia.Content
{
    [Serializable] public sealed class AuthoredLanguage
    {
        public string code, nativeName, culture;
        public TextAsset catalog;
    }
    /// <summary>Explicit authored references; all translations ship offline with the player.</summary>
    [CreateAssetMenu(menuName="Weedtopia/Game/Language Catalog",fileName="LanguageCatalog")]
    public sealed class LanguageCatalog : ScriptableObject
    {
        public string fallback="de";
        public AuthoredLanguage[] languages;
        public Font bodyFont, headingFont;
        public LocalizationService Create()
        {
            if(languages==null||languages.Any(l=>l.catalog==null))throw new InvalidOperationException("Missing authored language file");
            return new LocalizationService(languages.Select(l=>new LanguageDefinition(l.code,l.nativeName,l.culture)),languages.Select(l=>
            {
                var document=JsonUtility.FromJson<LocaleDocument>(l.catalog.text);
                if(document.language!=l.code)throw new InvalidOperationException("Language catalog code mismatch");return document;
            }),fallback);
        }
    }
}
