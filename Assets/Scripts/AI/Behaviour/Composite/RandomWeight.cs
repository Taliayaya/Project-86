using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine;


namespace Unity.Behavior
{
    /// <summary>
    /// Executes a random branch.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "Random Weight", story: "Random using weights [Weights]", category: "Flow", id: "3ec329cc9c414fd88aa9113e7c422f1b")]
    internal partial class RandomWeightComposite : Composite
    {
        [SerializeReference] public BlackboardVariable<List<float>> Weights;
    
        int m_RandomIndex = 0;
        float m_TotalWeight = 0;


        /// <inheritdoc cref="OnStart" />
        protected override Status OnStart()
        {
            if (Weights.Value.Count != Children.Count)
                LogFailure("Weights and children count mismatch. Weights: " + Weights.Value.Count + ", Children: " + Children.Count + ".");
            m_RandomIndex = GetRandomIndexUsingWeights();
            if (m_RandomIndex < Children.Count)
            {
                var status = StartNode(Children[m_RandomIndex]);
                if (status == Status.Success || status == Status.Failure)
                    return status;

                return Status.Waiting;
            }

            return Status.Success;
        }

        /// <summary>
        /// Calculates and returns a random index based on the weights provided in the Weights list.
        /// </summary>
        /// <returns>The index of the selected item based on the probability weights.</returns>
        private int GetRandomIndexUsingWeights()
        {
            if (m_TotalWeight == 0)
                m_TotalWeight = Weights.Value.Sum();
            float random = UnityEngine.Random.Range(0, m_TotalWeight);

            float weightsSum = 0;
            for (int i = 0; i < Weights.Value.Count; i++)
            {
                weightsSum += Weights.Value[i];
                if (random <= weightsSum)
                    return i;
            }
            
            // Should never happen, but just in case.
            return UnityEngine.Random.Range(0, Children.Count);
        }

        /// <inheritdoc cref="OnUpdate" />
        protected override Status OnUpdate()
        {
            var status = Children[m_RandomIndex].CurrentStatus;
            if (status == Status.Success || status == Status.Failure)
                return status;

            return Status.Waiting;
        }
    }
}