using System;
using UnityEngine;

namespace Weedtopia.Unity
{
    public enum ProfileAvatarGender { Male, Female }
    [Serializable] public sealed class ProfileAvatar
    {
        public string id;
        public ProfileAvatarGender gender;
        public Sprite portrait;
    }
    [CreateAssetMenu(menuName = "Weedtopia/Player Avatar Catalog")]
    public sealed class PlayerAvatarCatalog : ScriptableObject
    {
        public ProfileAvatar[] entries;
        public Sprite Portrait(string id)
        {
            var entry = Array.Find(entries, value => value.id == id);
            if (entry?.portrait == null) throw new InvalidOperationException("Missing player avatar: " + id);
            return entry.portrait;
        }
    }
}
