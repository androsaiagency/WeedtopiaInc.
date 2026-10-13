using System;
using System.Linq;
using NUnit.Framework;
using Weedtopia.Domain;

namespace Weedtopia.Tests.EditMode
{
    public sealed class LocalizationTests
    {
        private static LanguageDefinition[] Registry()=>new[]{new LanguageDefinition("de","Deutsch","de-DE"),new LanguageDefinition("en","English","en-GB")};
        private static LocaleDocument Document(string language,params LocaleText[] entries)=>new LocaleDocument{language=language,entries=entries};
        private static LocaleText Entry(string key,string value)=>new LocaleText{key=key,value=value};
        [Test]public void SelectPublishesOnceAndInvalidCodesPreserveCurrentLanguage()
        {
            var service=new LocalizationService(Registry(),new[]{Document("de",Entry("greeting","Hallo {0}")),Document("en",Entry("greeting","Hello {0}"))});
            int changes=0;service.Changed+=()=>changes++;Assert.That(service.Get("greeting"),Is.EqualTo("Hallo {0}"));
            Assert.That(service.Select("en"),Is.True);Assert.That(service.Format("greeting","Mila"),Is.EqualTo("Hello Mila"));
            Assert.That(service.Select("en"),Is.True);Assert.That(service.Select("unknown"),Is.False);Assert.That(changes,Is.EqualTo(1));Assert.That(service.Language,Is.EqualTo("en"));
        }
        [Test]public void MissingTranslationFallsBackAndUnknownKeyRemainsVisible()
        {
            var service=new LocalizationService(Registry(),new[]{Document("de",Entry("missing","Deutsch")),Document("en")});service.Select("en");
            Assert.That(service.Get("missing"),Is.EqualTo("Deutsch"));Assert.That(service.MissingCount("en"),Is.EqualTo(1));Assert.That(service.Get("new.key"),Is.EqualTo("[new.key]"));
        }
        [Test]public void RejectsChangedPlaceholdersAndDuplicateKeys()
        {
            Assert.Throws<ArgumentException>(()=>new LocalizationService(Registry(),new[]{Document("de",Entry("text","{0:N0}")),Document("en",Entry("text","{1:N0}"))}));
            Assert.Throws<ArgumentException>(()=>new LocalizationService(Registry(),new[]{Document("de",Entry("text","A"),Entry("text","B")),Document("en")}));
        }
        [Test]public void CulturesFormatNumbersWithoutChangingAmountSerialization()
        {
            var service=new LocalizationService(Registry(),new[]{Document("de",Entry("number","{0:N2}")),Document("en",Entry("number","{0:N2}"))});
            var amount=new Amount(1234.5);string saved=amount.Serialize();Assert.That(service.Format("number",1234.5),Is.EqualTo("1.234,50"));
            service.Select("en");Assert.That(service.Format("number",1234.5),Is.EqualTo("1,234.50"));Assert.That(amount.Serialize(),Is.EqualTo(saved));
            service.Select("de");Assert.That(service.Format("number",new Amount(12.5)),Is.EqualTo("12,50"));
            Assert.That(amount.Serialize(),Is.EqualTo(saved));
        }
        [Test]public void IndependentDefinitionLookupsDoNotShareMutableLanguageState()
        {
            var a=new LocalizationService(Registry(),new[]{Document("de",Entry("test","Deutsch")),Document("en",Entry("test","English"))});
            var b=new LocalizationService(Registry(),new[]{Document("de",Entry("test","Deutsch")),Document("en",Entry("test","English"))});
            var defs=new GameDefinitions{TextLookup=a};a.Select("en");Assert.That(defs.Text("test"),Is.EqualTo("English"));Assert.That(b.Get("test"),Is.EqualTo("Deutsch"));
        }
    }
}
