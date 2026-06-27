# Slider Mapping: Quadratic vs. Exponential

## Visual Comparison

### QUADRATIC MAPPING (OLD - Broken)
```
Scale
  4.0 |                                  ●
	  |                                /   \
  2.5 |                              /       \
	  |                            /           \
  1.0 |            ●             /               \
	  |          /   \         /                   \  
  0.5 |        /       \     /                       \
	  |      /           \ /                           \
  0.25|●───────────────────────────────────────────────●
	  |
	  +─────────────────────────────────────────────────
	  0    1/6   1/3   1/2   2/3   5/6    1.0
	  ↑     ↑                           ↑
	  LEFT  HARD  DEAD ZONE             RIGHT
		   STOP

Problem: Scale = 0.25 at TWO slider positions (0 and 1/6)
Result: Slider thumb sticks at 0.166666... because both 0 and 1/6 map to 0.25,
		and the inverse always picks 1/6
```

### EXPONENTIAL MAPPING (NEW - Fixed)
```
Scale
  4.0 |                                           ●
	  |                                         /
  3.5 |                                      /
	  |                                    /
  3.0 |                                  /
	  |                                /
  2.5 |                             /
	  |                           /
  2.0 |                        /
	  |                      /
  1.5 |                    /
	  |                 /
  1.0 |              ●
	  |           /
  0.5 |        /
	  |     /
  0.25|●
	  |
	  +─────────────────────────────────────────────────
	  0.0        0.25        0.5         0.75        1.0
	  ↑                      ↑                       ↑
	  LEFT              CENTER/DEFAULT            RIGHT
					(Scale = 1.0)

Advantage: Monotonically increasing
Each slider position → unique scale value
Smooth, continuous, NO dead zones
```

## Mathematical Difference

### Quadratic: `scale = 4.5x² - 0.75x + 0.25`

**Problem with inverse:**
```
Solving:  4.5x² - 0.75x + 0.25 = scale
		  4.5x² - 0.75x + (0.25 - scale) = 0

Using quadratic formula: x = (-b ± √(b² - 4ac)) / 2a

For scale = 0.25:
		  4.5x² - 0.75x + 0 = 0
		  x(4.5x - 0.75) = 0

		  Two solutions: x₁ = 0, x₂ = 0.75/4.5 = 1/6

Inverse mapping must choose one → always picks 1/6 ✗
```

### Exponential: `scale = 0.25 × 16ˣ`

**Simple, unambiguous inverse:**
```
Solving:  0.25 × 16ˣ = scale
		  16ˣ = scale / 0.25
		  x × log(16) = log(scale / 0.25)
		  x = log(scale / 0.25) / log(16)

For ANY scale in [0.25, 4.0]:
		  EXACTLY ONE x value in [0, 1] ✓

Example: scale = 0.25
		 x = log(0.25/0.25) / log(16) = log(1) / log(16) = 0 / log(16) = 0 ✓
```

## Round-Trip Behavior

### OLD (Quadratic) - BROKEN
```
User moves slider to position 0.05:
  0.05 → SliderToScale → 0.25 (clamped minimum)
		 → LabelManager clamps to 0.25
		 → ScaleToSlider → ???
		 → Two possible answers: 0 or 0.16666...
		 → Picks 0.16666...
  Result: Slider jumps to 0.16666... ✗
```

### NEW (Exponential) - FIXED
```
User moves slider to position 0.05:
  0.05 → SliderToScale → 0.25 × 16^0.05 ≈ 0.297
		 → LabelManager clamps to 0.297
		 → ScaleToSlider → log(0.297/0.25) / log(16) ≈ 0.0498 ≈ 0.05
  Result: Slider stays near 0.05, smooth motion ✓
```

## Summary Table

| Aspect | Quadratic | Exponential |
|--------|-----------|------------|
| Formula | 4.5x² - 0.75x + 0.25 | 0.25 × 16ˣ |
| Monotonic | ✗ Non-monotonic | ✓ Monotonically increasing |
| Dead Zones | ✓ YES (0 to 1/6) | ✗ None |
| Inverse | ✗ Ambiguous (quadratic formula) | ✓ Unambiguous (logarithm) |
| Hard Stops | ✓ YES at 0.16666... | ✗ No |
| Smoothness | ✗ Jumpy at extremes | ✓ Smooth throughout |
| User Experience | ✗ Broken | ✓ Fixed |

---

**Conclusion:** The exponential mapping provides a smooth, monotonic, unambiguous transformation that allows full slider range utilization without dead zones or hard stops.
