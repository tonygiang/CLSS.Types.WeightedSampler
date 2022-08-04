# CLSS.Types.WeightedSampler

### Problem

Sampling a list of elements with weight is a common use case without built-in support in the standard library.

```
var rng = new System.Random();
var weights = new double[collection.Count];
for (int i = 0; i < weights.Length; ++i)
  weights[i] = ConvertElementToWeights(collection[i]);
var weightStages = weights
  .Select((w, i) => weights.Take(i + 1).Sum());
var roll = rng.NextDouble() * weights.Sum();
int selectedIndex = 0;
foreach (var ws in weightStages)
{
  if (ws > roll) break;
  ++selectedIndex;
}
```

Above is a seemingly correct weighted randomization implementation that contains some obvious and non-obvious performance and correctness issues (negative weights are accepted and added to the weight sum). These pitfalls are often overlooked when you have to write weighted randomization on the fly.

### Solution

`WeightedSampler<T>` is a struct type that encapsulates around an `IList<T>` collection to efficiently sample its elements. At factory construction, it takes in a weight selector function with a `Func<T, double>` signature. 

```
using CLSS;
using System.Linq;

public struct District
{
  public string Name;
  public int Population;
}

var districts = new List<District>()
{
  new District { Name = "A", Population = 200 },
  new District { Name = "B", Population = 600 },
  new District { Name = "C", Population = 400 }
};
var districtSampler = WeightedSampler<District>
  .From(districts, d => d.Population);

// Distribution test
var samples = new District[12000];
for (int i = 0; i < samples.Length; ++i)
  samples[i] = districtSampler.Sample();
Console.WriteLine($"District A: {samples.Count(s => s.Name == "A")}"); // District A: 1987
Console.WriteLine($"District B: {samples.Count(s => s.Name == "B")}"); // District B: 6081
Console.WriteLine($"District C: {samples.Count(s => s.Name == "C")}"); // District C: 3932
```

The probability of each element being chosen for each roll is its own weight divided by the sum of all the element's weights. If the specified weight selector function returns a negative weight, it will be treated no differently than 0 weight.

`WeightedSampler<T>` can also call `SampleIndex` to select only the index number, not the weighted element itself.

#### Usage Notes

- Under the hood, `WeightedSampler<T>` relies on an array that is a snapshot of respective weights (matching by index number) at a point in time. This array is snapshotted once at creation of a `WeightedSampler<T>`. If runtime condition causes the source list to mutate or the weights to change, it is necessary to call `RefreshWeights` from a `WeightedSampler<T>` to continue getting correct sampling results. 

```
districts.Add(new District { Name = "D", Population = 500 });
districtSampler.RefreshWeights(); // source list mutated, taking another snapshot
```

- `RefreshWeights` contains an allocation and is intentionally not automatically done. You should be mindful of where to call this.

- Each sampling call from a `WeightedSampler<T>` instance creates no garbage and is safe to use in hot code path. But be mindful of weight correctness.

- **Advanced Manual Mode**: By omitting the weight selector function at construction or leaving it `null`, you can still use the sampling methods of `WeightedSampler<T>`, but you are on your own to ensure the correctness of the `Weights` array and the `WeightSum` field yourself. Both are modifiable at will. Calling `RefreshWeights` while having a `null` weight selector will throw an exception. 

Internally, this package uses and depends on the `DefaultRandom` package in CLSS to save on the allocation of a new `System.Random` instance.

Optionally, `Sample` and `SampleIndex` also take in a `System.Random` of your choosing in case you want a custom-seeded random number generator:

```
using CLSS;

var districtSampler = WeightedSampler<District>
  .From(districts, d => d.Population, customrng);
```

If you are on .NET 6, you can pass in [`System.Random.Shared`](https://docs.microsoft.com/en-us/dotnet/api/system.random.shared).

`GetWeightedRandom` and the `WeightedSampler<T>` type fulfill similar roles. They have their own trade-offs. The table below compares their key differences:

| Factors | `GetWeightedRandom` | `WeightedSampler<T>` |
| ---     | ---                 | ---               |
| Memory allocation per invocation | 1 array equal in length to source list. | No allocation. |
| Syntax | Extension method, called directly from `IList<T>` types. | Wrapper struct around a list.
| Reflect changes | All list and member mutations are reflected. | Changes in element weights and list mutations are not reflected until manually refreshed. |

**Note**: `GetWeightedRandom` works on all types implementing the [`IList<T>`](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ilist-1) interface, *including raw C# array*.

##### This package is a part of the [C# Language Syntactic Sugar suite](https://github.com/tonygiang/CLSS).