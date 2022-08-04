// A part of the C# Language Syntactic Sugar suite.

using System;
using System.Collections.Generic;

namespace CLSS
{
  /// <summary>
  /// An encapsulation around an <see cref="IList{T}"/> collection for the
  /// purpose of randomly sampling from it with weighted probability.
  /// </summary>
  /// <typeparam name="T">The type of element contained in the source list.
  /// </typeparam>
  public struct WeightedSampler<T>
  {
    /// <summary>
    /// The list which the <see cref="WeightedSampler{T}"/> will take elements
    /// from.
    /// </summary>
    public IList<T> SourceList;

    /// <summary>
    /// The function that generates an element's corresponding weight. If this
    /// field is null, you will not be able to use <see cref="RefreshWeights"/>
    /// and <see cref="Weights"/> and <see cref="WeightSum"/> must be updated
    /// manually.
    /// </summary>
    public Func<T, double> WeightSelector;

    /// <summary>
    /// A snapshot of each element's corresponding sampling weight.
    /// </summary>
    public double[] Weights;

    /// <summary>
    /// The sum of all sampling weights.
    /// </summary>
    public double WeightSum;

    /// <summary>
    /// Optional custom-seeded random number generator to use for the sample
    /// rolls.
    /// </summary>
    public Random RNG;

    /// <summary>
    /// Creates an instance of <see cref="WeightedSampler{T}"/> from an
    /// <see cref="IList{T}"/> collection.
    /// </summary>
    /// <param name="sourceList">The list which the
    /// <see cref="WeightedSampler{T}"/> will take elements from.</param>
    /// <param name="weightSelector">The function that generates an element's
    /// corresponding weight. It is possible to omit this argument or leave it
    /// null if you want to manually update <see cref="Weights"/> and
    /// <see cref="WeightSum"/>.</param>
    /// <param name="rng">Optional custom-seeded random number generator to use
    /// for the sample rolls.</param>
    /// <returns>A new <see cref="WeightedSampler{T}"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="sourceList"/> is
    /// null.</exception>
    public static WeightedSampler<T> From(IList<T> sourceList,
      Func<T, double> weightSelector = null,
      Random rng = null)
    {
      if (sourceList == null) throw new ArgumentNullException("sourceList");
      if (rng == null) rng = DefaultRandom.Instance;
      return new WeightedSampler<T>()
      {
        SourceList = sourceList,
        WeightSelector = weightSelector,
        Weights = new double[sourceList.Count],
        RNG = rng
      }.RefreshWeights();
    }

    /// <summary>
    /// Projects each element of the source list into its respective sampling
    /// weight and saves a snapshot of the results and their sum. This method
    /// will not succeed if the <see cref="WeightedSampler{T}"/> has a null
    /// weight selector.
    /// </summary>
    /// <returns>The source <see cref="WeightedSampler{T}"/>.</returns>
    /// <exception cref="ArgumentNullException"><see cref="WeightSelector"/> is
    /// null.</exception>
    public WeightedSampler<T> RefreshWeights()
    {
      if (WeightSelector == null)
        throw new ArgumentNullException("WeightSelector");
      if (Weights.Length != SourceList.Count)
        Weights = new double[SourceList.Count];
      WeightSum = 0.0;
      for (int i = 0; i < Weights.Length; ++i)
      {
        var weight = WeightSelector(SourceList[i]);
        if (weight < 0.0) weight = 0.0;
        Weights[i] = weight;
        WeightSum += weight;
      }
      return this;
    }

    /// <summary>
    /// Returns a random index with weighted probabilities taken from a saved
    /// snapshot of sampling weights. Weights lesser than or equal to 0 will be
    /// ignored.
    /// </summary>
    /// <returns>A weight-distributed randomly-selected index.</returns>
    public int SampleIndex()
    {
      int idx = 0;
      for (var roll = RNG.NextDouble() * WeightSum;
        idx < Weights.Length;
        ++idx)
      {
        if (Weights[idx] > roll) break;
        roll -= Weights[idx];
      }
      return idx;
    }

    /// <summary>
    /// Returns a random element from the source list with weighted
    /// probabilities taken from a saved snapshot of sampling weights. Weights
    /// lesser than or equal to 0 will be ignored.
    /// </summary>
    /// <returns>A weight-distributed randomly-selected element from the source
    /// list.</returns>
    public T Sample() { return SourceList[SampleIndex()]; }
  }
}
