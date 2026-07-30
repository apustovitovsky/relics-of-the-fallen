using System;
using System.Collections.Generic;

namespace GAS
{
    public sealed class GameplayEffectSpec
    {
        public GameplayEffect Definition
        {
            get;
        }

        public GameplayEffectSO DefinitionAsset
        {
            get;
        }

        public GameplayEffectContextHandle EffectContext
        {
            get;
            private set;
        }

        public AbilitySystemComponent Source =>
            EffectContext.IsValid
                ? EffectContext.Get().GetOriginalInstigatorAbilitySystemComponent()
                : null;

        private readonly Dictionary<GameplayTag, float> m_SetByCallerMagnitudes = new();

        private readonly List<AttributeModifierSpec> m_ModifierSpecs = new();

        private readonly Dictionary<AttributeCaptureDefinition, float> m_CapturedAttributeMagnitudes = new();

        public IReadOnlyList<AttributeModifierSpec> ModifierSpecs => m_ModifierSpecs;

        public float Level
        {
            get;
        }

        public float Duration
        {
            get;
            internal set;
        }

        public string ApplicationGuid
        {
            get;
        }

        /// <summary>
        /// Creates runtime application data using an explicit gameplay effect context.
        /// </summary>
        public GameplayEffectSpec(
            GameplayEffect definition,
            GameplayEffectContextHandle effectContext,
            float level,
            string applicationGuid = null)
        {
            if (!effectContext.IsValid)
            {
                throw new ArgumentException(
                    "Gameplay effect context must be valid.",
                    nameof(effectContext));
            }

            AbilitySystemComponent source =
                effectContext
                    .Get()
                    .GetOriginalInstigatorAbilitySystemComponent();

            if (source == null)
            {
                throw new InvalidOperationException(
                    "Outgoing gameplay effect context has no instigator ability system.");
            }

            Definition =
                definition ?? throw new ArgumentNullException(
                    nameof(definition));

            EffectContext =
                effectContext;

            Level =
                level;

            Duration =
                GetInitialDuration(
                    Definition);

            ApplicationGuid =
                applicationGuid;

            InitializeModifierSpecs();

            CaptureSourceSnapshots();
        }

        /// <summary>
        /// Creates runtime application data from an asset using an explicit effect context.
        /// </summary>
        public GameplayEffectSpec(
            GameplayEffectSO definitionAsset,
            GameplayEffectContextHandle effectContext,
            float level,
            string applicationGuid = null)
            : this(
                GetDefinition(
                    definitionAsset),
                effectContext,
                level,
                applicationGuid)
        {
            DefinitionAsset =
                definitionAsset;
        }

        /// <summary>
        /// Creates runtime gameplay effect data from evaluated network state.
        /// </summary>
        public GameplayEffectSpec(
            GameplayEffectSO definitionAsset,
            GameplayEffectContextHandle effectContext,
            float level,
            float duration,
            IReadOnlyList<float> evaluatedModifierMagnitudes)
        {
            if (definitionAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(definitionAsset));
            }

            if (!effectContext.IsValid)
            {
                throw new ArgumentException(
                    "Evaluated gameplay effect context must be valid.",
                    nameof(effectContext));
            }

            if (evaluatedModifierMagnitudes == null)
            {
                throw new ArgumentNullException(
                    nameof(evaluatedModifierMagnitudes));
            }

            DefinitionAsset =
                definitionAsset;

            Definition =
                GetDefinition(
                    definitionAsset);

            EffectContext =
                effectContext;

            Level =
                level;

            Duration =
                duration;

            InitializeModifierSpecs();

            if (
                m_ModifierSpecs.Count !=
                evaluatedModifierMagnitudes.Count)
            {
                throw new InvalidOperationException(
                    "Replicated modifier count does not match the effect definition.");
            }

            for (
                int index = 0;
                index < m_ModifierSpecs.Count;
                index++)
            {
                m_ModifierSpecs[index].SetEvaluatedMagnitude(
                    evaluatedModifierMagnitudes[index]);
            }
        }

        /// <summary>
        /// Copies runtime gameplay effect data for a separate application.
        /// </summary>
        public GameplayEffectSpec(
            GameplayEffectSpec sourceSpec)
        {
            if (sourceSpec == null)
            {
                throw new ArgumentNullException(
                    nameof(sourceSpec));
            }

            Definition =
                sourceSpec.Definition;

            DefinitionAsset =
                sourceSpec.DefinitionAsset;

            EffectContext =
                sourceSpec.EffectContext;

            Level =
                sourceSpec.Level;

            Duration =
                sourceSpec.Duration;

            ApplicationGuid =
                sourceSpec.ApplicationGuid;

            foreach (
                KeyValuePair<GameplayTag, float> pair
                in sourceSpec.m_SetByCallerMagnitudes)
            {
                m_SetByCallerMagnitudes.Add(
                    pair.Key,
                    pair.Value);
            }

            foreach (
                KeyValuePair<
                    AttributeCaptureDefinition,
                    float> pair
                in sourceSpec.m_CapturedAttributeMagnitudes)
            {
                m_CapturedAttributeMagnitudes.Add(
                    pair.Key,
                    pair.Value);
            }

            foreach (
                AttributeModifierSpec modifierSpec
                in sourceSpec.m_ModifierSpecs)
            {
                m_ModifierSpecs.Add(
                    new AttributeModifierSpec(
                        modifierSpec));
            }

            if (Source != null)
            {
                CaptureSourceNonSnapshots();
            }
        }

        /// <summary>
        /// Replaces the shared effect context with an independent duplicate.
        /// </summary>
        public void DuplicateEffectContext()
        {
            EffectContext =
                EffectContext.Duplicate();
        }

        private static GameplayEffect GetDefinition(
            GameplayEffectSO definitionAsset)
        {
            if (definitionAsset == null)
            {
                throw new ArgumentNullException(
                    nameof(definitionAsset));
            }

            if (definitionAsset.ge == null)
            {
                throw new InvalidOperationException(
                    $"Gameplay effect asset '{definitionAsset.name}' has no definition.");
            }

            return definitionAsset.ge;
        }

        /// <summary>
        /// Returns the initial specification duration for a gameplay effect definition.
        /// </summary>
        private static float GetInitialDuration(
            GameplayEffect definition)
        {
            return
                definition.durationType switch
                {
                    GameplayEffectDurationType.Instant =>
                        GameplayEffectConstants.InstantApplication,

                    GameplayEffectDurationType.Infinite =>
                        GameplayEffectConstants.InfiniteDuration,

                    GameplayEffectDurationType.Duration =>
                        definition.durationValue,

                    _ =>
                        throw new ArgumentOutOfRangeException(
                            nameof(definition),
                            definition.durationType,
                            "Unsupported gameplay effect duration type.")
                };
        }

        /// <summary>
        /// Creates modifier specs from current definitions or legacy modifier data.
        /// </summary>
        private void InitializeModifierSpecs()
        {
            if (
                Definition.ModifierDefinitions.Count >
                0)
            {
                foreach (
                    AttributeModifierDefinition modifierDefinition
                    in Definition.ModifierDefinitions)
                {
                    m_ModifierSpecs.Add(
                        new AttributeModifierSpec(
                            modifierDefinition));
                }

                return;
            }

            if (
                Definition.modifiers == null ||
                Definition.modifiers.Count == 0)
            {
                return;
            }

            foreach (
                Modifier legacyModifier
                in Definition.modifiers)
            {
                if (legacyModifier == null)
                {
                    throw new InvalidOperationException(
                        "A gameplay effect cannot contain a null legacy modifier.");
                }

                AttributeModifierDefinition modifierDefinition =
                    new(
                        legacyModifier.attributeName,
                        AttributeModifierOperation.Additive,
                        new ConstantMagnitude(
                            legacyModifier.GetValue(
                                Definition)));

                m_ModifierSpecs.Add(
                    new AttributeModifierSpec(
                        modifierDefinition));
            }
        }

        /// <summary>
        /// Captures initial non-snapshot values from the effect source.
        /// </summary>
        private void CaptureSourceNonSnapshots()
        {
            foreach (
                AttributeModifierSpec modifierSpec
                in m_ModifierSpecs)
            {
                foreach (
                    AttributeCaptureDefinition capture
                    in modifierSpec
                        .Definition
                        .Magnitude
                        .GetAttributeCaptures())
                {
                    if (
                        capture.Snapshot ||
                        capture.Source !=
                        AttributeCaptureSource.Source)
                    {
                        continue;
                    }

                    CaptureAttribute(
                        capture,
                        Source);
                }
            }
        }

        /// <summary>
        /// Captures attribute data required from the gameplay effect target.
        /// </summary>
        public void CaptureAttributeDataFromTarget(
            AbilitySystemComponent target)
        {
            if (target == null)
            {
                throw new ArgumentNullException(
                    nameof(target));
            }

            foreach (
                AttributeModifierSpec modifierSpec
                in m_ModifierSpecs)
            {
                foreach (
                    AttributeCaptureDefinition capture
                    in modifierSpec
                        .Definition
                        .Magnitude
                        .GetAttributeCaptures())
                {
                    if (
                        capture.Source !=
                        AttributeCaptureSource.Target)
                    {
                        continue;
                    }

                    CaptureAttribute(
                        capture,
                        target);
                }
            }
        }

        /// <summary>
        /// Calculates and stores all modifier magnitudes for this application.
        /// </summary>
        public void CalculateModifierMagnitudes()
        {
            foreach (
                AttributeModifierSpec modifierSpec
                in m_ModifierSpecs)
            {
                float magnitude =
                    modifierSpec
                        .Definition
                        .Magnitude
                        .Calculate(this);

                modifierSpec.SetEvaluatedMagnitude(
                    magnitude);
            }
        }

        /// <summary>
        /// Sets a runtime magnitude identified by a gameplay tag.
        /// </summary>
        public void SetSetByCallerMagnitude(
            GameplayTag tag,
            float magnitude)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(
                    nameof(tag));
            }

            m_SetByCallerMagnitudes[tag] =
                magnitude;
        }

        /// <summary>
        /// Returns a required runtime magnitude identified by a gameplay tag.
        /// </summary>
        public float GetSetByCallerMagnitude(
            GameplayTag tag)
        {
            if (tag == null)
            {
                throw new ArgumentNullException(
                    nameof(tag));
            }

            if (
                !m_SetByCallerMagnitudes.TryGetValue(
                    tag,
                    out float magnitude))
            {
                throw new InvalidOperationException(
                    $"SetByCaller magnitude '{tag.name}' was not provided.");
            }

            return magnitude;
        }

        /// <summary>
        /// Captures source attributes configured for snapshot evaluation.
        /// </summary>
        private void CaptureSourceSnapshots()
        {
            foreach (
                AttributeModifierSpec modifierSpec
                in m_ModifierSpecs)
            {
                foreach (
                    AttributeCaptureDefinition capture
                    in modifierSpec
                        .Definition
                        .Magnitude
                        .GetAttributeCaptures())
                {
                    if (
                        !capture.Snapshot ||
                        capture.Source !=
                        AttributeCaptureSource.Source)
                    {
                        continue;
                    }

                    CaptureAttribute(
                        capture,
                        Source);
                }
            }
        }

        /// <summary>
        /// Captures one attribute value from an ability system component.
        /// </summary>
        private void CaptureAttribute(
            AttributeCaptureDefinition capture,
            AbilitySystemComponent owner)
        {
            if (capture == null)
            {
                throw new ArgumentNullException(
                    nameof(capture));
            }

            Attribute attribute =
                owner.GetAttribute(
                    capture.Attribute);

            float capturedValue =
                capture.ValueType ==
                AttributeCaptureValueType.BaseValue
                    ? attribute.BaseValue
                    : attribute.CurrentValue;

            m_CapturedAttributeMagnitudes[capture] =
                capturedValue;
        }

        /// <summary>
        /// Returns a previously captured attribute magnitude.
        /// </summary>
        internal float GetCapturedAttributeMagnitude(
            AttributeCaptureDefinition capture)
        {
            if (capture == null)
            {
                throw new ArgumentNullException(
                    nameof(capture));
            }

            if (
                !m_CapturedAttributeMagnitudes.TryGetValue(
                    capture,
                    out float magnitude))
            {
                throw new InvalidOperationException(
                    $"Attribute capture '{(capture.Attribute != null ? capture.Attribute.name : null)}' is not available.");
            }

            return magnitude;
        }
    }
}