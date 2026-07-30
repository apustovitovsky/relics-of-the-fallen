using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;
using System.IO;
using EasyButtons;
using GAS;
using System.Threading.Tasks;

namespace GAS {
#pragma warning disable 0618
    public static class GameplayTags {
        public static GameplayTagLibrary library;
    }

    [CreateAssetMenu(menuName = "GAS/GameplayTagLibrary", fileName = "GameplayTagLibrary")]
    [Serializable]
    public class GameplayTagLibrary : SingletonScriptableObjectLibrary<GameplayTagLibrary, GameplayTag>
    {

        private readonly Dictionary<GameplayTag, GameplayTag[]> tagHierarchies = new();

        /// <summary>
        /// Returns the cached hierarchy containing a gameplay tag and its available parents.
        /// </summary>
        internal IReadOnlyList<GameplayTag> GetHierarchy(
            GameplayTag tag)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(
                    nameof(tag));
            }

            EnsureTagHierarchies();

            if (
                tagHierarchies.TryGetValue(
                    tag,
                    out GameplayTag[] hierarchy))
            {
                return hierarchy;
            }

            throw new InvalidOperationException(
                $"Gameplay tag '{tag.name}' is absent from the gameplay tag library.");
        }

        private void EnsureTagHierarchies()
        {
            if (
                tagHierarchies.Count ==
                itemList.Count)
            {
                return;
            }

            BuildTagHierarchies();
        }

        private void BuildTagHierarchies()
        {
            if (
                itemDictionary.Count !=
                itemList.Count)
            {
                itemDictionary =
                    itemList.ToDictionary(
                        tag => tag.name,
                        tag => tag);
            }

            tagHierarchies.Clear();

            for (
                int tagIndex = 0;
                tagIndex < itemList.Count;
                tagIndex++)
            {
                GameplayTag tag =
                    itemList[tagIndex];

                var hierarchy =
                    new List<GameplayTag>(4)
                    {
                tag
                    };

                string fullName =
                    tag.name;

                int separatorIndex =
                    fullName.LastIndexOf('.');

                while (separatorIndex > 0)
                {
                    string parentName =
                        fullName.Substring(
                            0,
                            separatorIndex);

                    if (
                        itemDictionary.TryGetValue(
                            parentName,
                            out GameplayTag parentTag))
                    {
                        hierarchy.Add(
                            parentTag);
                    }

                    separatorIndex =
                        fullName.LastIndexOf(
                            '.',
                            separatorIndex - 1);
                }

                tagHierarchies.Add(
                    tag,
                    hierarchy.ToArray());
            }
        }

        protected override void Refresh()
        {
            base.Refresh();

            BuildTagHierarchies();
        }

        public List<GameplayTag> GetByNames(List<string> tagNames) { //full name, including parents. A.B.XYZ
            List<GameplayTag> foundTag = itemList.Where(tag => tagNames.Contains(tag.name)).ToList();
            return foundTag;
        }

        public GameplayTag GetByIndex(int index) {
            return itemList[index];
        }

        [Button]
        public void LogStaticReference() {
            Debug.Log($"static ref: {GameplayTags.library}");
        }

        // [Button]
        // [Tooltip("Create tags from a list of tagNames separated by comma. e.g. Tag1, Special.Tag2, A.B.C.Tag3...")]
        // public void CreateAssetsForGameplayTagNames(string names) {//creates the gameplayTag SO assets for given strings. so we could serialize/deserialize all tags
        //     List<string> tagNames = names.Split(',').ToList();
        //     // tagNames.ForEach(tagName => )
        // }

        public bool SerializeString() {
            string s = JsonUtility.ToJson(this, true);
            Debug.Log(s);
            return true;
        }

        public bool IsParent(GameplayTag child, GameplayTag parent) { //Checks if tag is parent of another
            if (child.name.Contains(parent.name)) return true;
            else return false;
        }



    }

#pragma warning restore 0618
}