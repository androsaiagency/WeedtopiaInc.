using System;
using UnityEngine;
using Weedtopia.Domain;

namespace Weedtopia.Content
{
    [CreateAssetMenu(menuName="Weedtopia/Game/Content",fileName="BurningHillsContent")]
    public sealed class GameContent : ScriptableObject
    {
        [SerializeField] private TextAsset definitions;
        [SerializeField] private LanguageCatalog languageCatalog = default;
        public LanguageCatalog Languages => languageCatalog;
        public TextAsset DefinitionsAsset => definitions;
        public GameDefinitions Load()
        {
            if(definitions==null)throw new InvalidOperationException("Content asset has no definitions reference");
            var data=JsonUtility.FromJson<GameDefinitions>(definitions.text);data.TextLookup=languageCatalog?.Create();data.Validate();return data;
        }
#if UNITY_EDITOR
        public void EditorConfigure(TextAsset asset) { if(definitions==null)definitions=asset; }
#endif
    }
}
