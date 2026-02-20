using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Farming;

public static class GreenhouseSlotParser
{
	private const int GridSize = 10;

	private static readonly HashSet<(int X, int Z)> ExcludedCenterPlusSlots = new() {
		(4, 4), (5, 4), (4, 5), (5, 5),
		(4, 3), (5, 3), (4, 6), (5, 6),
		(3, 4), (3, 5), (6, 4), (6, 5)
	};

	private static readonly Dictionary<(int X, int Z), int> SlotToIndex;
	private static readonly (int X, int Z)[] IndexToSlot;

	static GreenhouseSlotParser() {
		SlotToIndex = new Dictionary<(int X, int Z), int>();
		var indexToSlot = new List<(int X, int Z)>();

		var index = 0;
		for (var z = 0; z < GridSize; z++) {
			for (var x = 0; x < GridSize; x++) {
				if (ExcludedCenterPlusSlots.Contains((x, z))) continue;

				SlotToIndex[(x, z)] = index;
				indexToSlot.Add((x, z));
				index++;
			}
		}

		IndexToSlot = indexToSlot.ToArray();
		if (IndexToSlot.Length != 88)
			throw new InvalidOperationException(
				$"Expected 88 tracked greenhouse slots, got {IndexToSlot.Length}.");
	}

	public static (long Low, long High) EncodeSlots(IEnumerable<GreenhouseSlotUnlock>? slots) {
		ulong low = 0;
		ulong high = 0;

		if (slots is null) return (0, 0);

		foreach (var slot in slots) {
			if (!SlotToIndex.TryGetValue((slot.X, slot.Z), out var index))
				continue;

			if (index < 64)
				low |= 1UL << index;
			else
				high |= 1UL << (index - 64);
		}

		return (unchecked((long)low), unchecked((long)high));
	}

	public static List<GreenhouseSlotUnlock> DecodeSlots(long lowMask, long highMask) {
		var result = new List<GreenhouseSlotUnlock>();

		var low = unchecked((ulong)lowMask);
		var high = unchecked((ulong)highMask);

		for (var index = 0; index < IndexToSlot.Length; index++) {
			var isUnlocked = index < 64
				? (low & (1UL << index)) != 0
				: (high & (1UL << (index - 64))) != 0;

			if (!isUnlocked) continue;

			var (x, z) = IndexToSlot[index];
			result.Add(new GreenhouseSlotUnlock {
				X = x,
				Z = z
			});
		}

		return result;
	}
}
