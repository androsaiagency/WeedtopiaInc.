using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Weedtopia.Domain
{
    public interface ITextLookup { string Get(string key); }
    [Serializable] public sealed class LocaleText { public string key, value; }
    [Serializable] public sealed class LocaleDocument { public string language; public LocaleText[] entries; }
    public sealed class LanguageDefinition
    {
        public readonly string Code, NativeName;
        public readonly CultureInfo Culture;
        public LanguageDefinition(string code, string nativeName, string culture)
        { Code=code; NativeName=nativeName; Culture=CultureInfo.GetCultureInfo(culture); }
    }
    /// <summary>Indexed, UI-independent catalog. Stable keys, explicit fallback and atomic language changes.</summary>
    public sealed class LocalizationService : ITextLookup
    {
        private readonly Dictionary<string, Dictionary<string,string>> catalogs;
        private readonly Dictionary<string,string> aliases;
        private readonly LanguageDefinition[] languages;
        private readonly string fallback;
        private static readonly Regex Placeholder=new Regex(@"\{\d+(?:,[^}:]+)?(?::[^}]+)?\}");
        public IReadOnlyList<LanguageDefinition> Languages => languages;
        public string Language { get; private set; }
        public CultureInfo Culture => languages.First(l=>l.Code==Language).Culture;
        public event Action Changed;
        public LocalizationService(IEnumerable<LanguageDefinition> definitions, IEnumerable<LocaleDocument> documents, string defaultLanguage="de")
        {
            languages=definitions.ToArray(); fallback=defaultLanguage;
            if(languages.Length==0||languages.Select(l=>l.Code).Distinct().Count()!=languages.Length)throw new ArgumentException("Invalid language registry");
            catalogs=new Dictionary<string,Dictionary<string,string>>(StringComparer.Ordinal);
            foreach(var document in documents)
            {
                var index=new Dictionary<string,string>(StringComparer.Ordinal);
                foreach(var entry in document.entries??Array.Empty<LocaleText>())
                {
                    if(string.IsNullOrWhiteSpace(entry.key)||string.IsNullOrWhiteSpace(entry.value))throw new ArgumentException("Empty localized text");
                    if(index.ContainsKey(entry.key))throw new ArgumentException("Duplicate localized key: "+entry.key);
                    index.Add(entry.key,entry.value);
                }
                catalogs.Add(document.language,index);
            }
            if(!catalogs.ContainsKey(fallback)||languages.Any(l=>!catalogs.ContainsKey(l.Code)))throw new ArgumentException("Missing language catalog");
            foreach(var catalog in catalogs.Values)
                foreach(var entry in catalog)
                    if(catalogs[fallback].TryGetValue(entry.Key,out var original)&&!Tokens(original).SequenceEqual(Tokens(entry.Value)))
                        throw new ArgumentException("Localized placeholders differ: "+entry.Key);
            aliases=new Dictionary<string,string>(StringComparer.Ordinal);
            foreach(var entry in catalogs[fallback])if(!aliases.ContainsKey(entry.Value))aliases.Add(entry.Value,entry.Key);
            Language=fallback;
        }
        private static string[] Tokens(string value)=>Placeholder.Matches(value).Cast<Match>().Select(m=>m.Value).OrderBy(s=>s,StringComparer.Ordinal).ToArray();
        public bool Select(string code)
        {
            if(!languages.Any(l=>l.Code==code))return false;
            if(Language==code)return true;
            Language=code;Changed?.Invoke();return true;
        }
        public string Get(string key)
        {
            if(key==null)return "";
            if(catalogs[Language].TryGetValue(key,out var value))return value;
            return catalogs[fallback].TryGetValue(key,out value)?value:"["+key+"]";
        }
        public string Format(string key,params object[] arguments)=>string.Format(Culture,Get(key),arguments);
        public string KeyForSource(string source)=>source!=null&&aliases.TryGetValue(source,out var key)?key:null;
        public int MissingCount(string code)=>catalogs[fallback].Keys.Count(k=>!catalogs[code].ContainsKey(k));
    }
    /// <summary>Presentation-only facade for static formatters and authored labels. Gameplay receives ITextLookup independently.</summary>
    public static class GameText
    {
        public static LocalizationService Current { get; private set; }
        public static event Action Changed;
        public static string Get(string key)=>Current?.Get(key)??"["+key+"]";
        public static string Format(string key,params object[] arguments)=>Current?.Format(key,arguments)??"["+key+"]";
        public static void Use(LocalizationService service)
        {
            if(Current!=null)Current.Changed-=Broadcast;
            Current=service;if(Current!=null)Current.Changed+=Broadcast;Broadcast();
        }
        private static void Broadcast()=>Changed?.Invoke();
        public static void Release(LocalizationService service){if(ReferenceEquals(Current,service))Use(null);}
    }
}
