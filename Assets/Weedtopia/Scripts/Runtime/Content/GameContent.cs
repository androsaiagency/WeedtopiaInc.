using System;
using UnityEngine;
using Weedtopia.Domain;

namespace Weedtopia.Content
{
    [CreateAssetMenu(menuName="Weedtopia/Game/Content",fileName="BurningHillsContent")]
    public sealed class GameContent : ScriptableObject
    {
        [SerializeField] private TextAsset definitions;
        public TextAsset DefinitionsAsset => definitions;
        public GameDefinitions Load()
        {
            if(definitions==null)throw new InvalidOperationException("Content asset has no definitions reference");
            var data=JsonUtility.FromJson<GameDefinitions>(definitions.text);data.Validate();return data;
        }
#if UNITY_EDITOR
        public void EditorConfigure(TextAsset asset) { if(definitions==null)definitions=asset; }
#endif
    }
}
