using System.Collections.Generic;
using UnityEngine;

namespace FruitSort
{
    [CreateAssetMenu(fileName = "EffectLibrary", menuName = "FruitSort/Effect Library")]
    public sealed class EffectLibrary : ScriptableObject
    {
        [SerializeField] List<EffectDefinition> effects = new List<EffectDefinition>();

        readonly Dictionary<GameEffectType, EffectDefinition> _definitions =
            new Dictionary<GameEffectType, EffectDefinition>();

        void OnEnable() => RebuildLookup();
        void OnValidate() => RebuildLookup();

        public void Configure(IEnumerable<EffectDefinition> definitions)
        {
            effects = definitions == null
                ? new List<EffectDefinition>()
                : new List<EffectDefinition>(definitions);
            RebuildLookup();
        }

        public bool TryGetDefinition(GameEffectType effectType, out EffectDefinition definition)
        {
            return _definitions.TryGetValue(effectType, out definition);
        }

        void RebuildLookup()
        {
            _definitions.Clear();
            for (int i = 0; i < effects.Count; i++)
            {
                EffectDefinition definition = effects[i].Normalized();
                if (definition.effectType == GameEffectType.None) continue;
                _definitions[definition.effectType] = definition;
            }
        }
    }
}
