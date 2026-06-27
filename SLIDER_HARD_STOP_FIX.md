# Slider Hard Stop Fix: Exponential Mapping

## Problem
The slider was hitting a hard stop at approximately `0.166666...` (1/6) and could not traverse the full range. Attempts to move the slider below this point would snap back to it.

## Root Cause
The original slider-to-scale mapping used a **quadratic (polynomial) function**:
```
scale = 4.5x² - 0.75x + 0.25
```

where `x` is the slider position in [0, 1].

**The mathematical issue:**
- The quadratic equation has **two roots** where `scale = 0.25` (the minimum scale)
- One root is at `x = 0` (left edge of slider) ✓
- The other root is at `x = 1/6 ≈ 0.16666...` ✓
- Between `x = 0` and `x = 1/6`, the quadratic function **rises** from 0.25 to a peak and back down to 0.25
- This creates a "dead zone": any slider position below 1/6 would map to scale 0.25, then the inverse would map back to 1/6
- Result: **slider thumb appears locked** at the 1/6 position

## Solution
Replaced the quadratic mapping with an **exponential mapping**:
```
scale = 0.25 * (16 ^ x)
```

where `x` is the slider position in [0, 1].

**Why exponential works:**
- **Monotonically increasing**: every slider position maps to a unique scale value
- **No dead zones**: slider movement anywhere in [0, 1] produces smooth, continuous changes
- **Same key points**:
  - Slider 0.0 → Scale 0.25 (0.25 × 16^0 = 0.25 × 1 = 0.25) ✓
  - Slider 0.5 → Scale 1.0 (0.25 × 16^0.5 = 0.25 × 4 = 1.0) ✓
  - Slider 1.0 → Scale 4.0 (0.25 × 16^1 = 0.25 × 16 = 4.0) ✓
- **Smooth control**: finer adjustments possible near the slider's edges

## Inverse Function
The inverse of the exponential mapping is the logarithm:
```
x = log₁₆(scale / 0.25) = log(scale / 0.25) / log(16)
```

## Changes Made

### File: `AutoNum/Model/SizingModel.cs`

**Function: `SliderToScale(double sliderPosition)`**
- Replaced quadratic formula with exponential: `0.25 * Math.Pow(16.0, clamped)`
- Updated documentation to explain exponential mapping
- Removed quadratic coefficients (a, b, c)

**Function: `ScaleToSlider(double scale)`**
- Replaced quadratic solving (discriminant, quadratic formula) with logarithmic inverse
- Updated documentation to explain log-based inversion
- Now computes: `Math.Log(ratio) / Math.Log(16.0)` where `ratio = scale / 0.25`
- Clamps result to [0, 1] to handle out-of-range scales gracefully

## Result
The slider now:
- ✓ Moves smoothly across the entire [0, 1] range
- ✓ Produces continuous scale changes from 0.25 to 4.0
- ✓ Has no dead zones or hard stops
- ✓ Maintains the same key mapping points (0.0→0.25, 0.5→1.0, 1.0→4.0)
- ✓ Provides consistent, predictable feel across the full range

## Testing
After applying this fix, you should be able to:
1. Move the slider all the way to the left (0) — it should smoothly map to scale 0.25
2. Move the slider through the middle (0.5) — it should pass through scale 1.0
3. Move the slider all the way to the right (1) — it should reach scale 4.0
4. No "hard stops" or "sticky" regions anywhere in the range

The slider should feel responsive and smooth throughout its entire travel.
