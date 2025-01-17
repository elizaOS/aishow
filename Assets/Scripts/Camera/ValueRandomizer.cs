using UnityEngine;

public class ValueRandomizer
{
    public class Range
    {
        public float min;
        public float max;
        
        public Range(float min, float max)
        {
            this.min = min;
            this.max = max;
        }
        
        public float GetRandom()
        {
            return Random.Range(min, max);
        }
    }

    public static float AddVariation(float baseValue, float variationPercent)
    {
        float variation = Random.Range(-variationPercent, variationPercent);
        return baseValue * (1f + variation);
    }

    public static float LerpWithVariation(float start, float end, float t, float variationPercent)
    {
        float lerpedValue = Mathf.Lerp(start, end, t);
        return AddVariation(lerpedValue, variationPercent);
    }

    public static float GetRandomInRange(Range range, float variationPercent = 0)
    {
        float baseValue = range.GetRandom();
        return AddVariation(baseValue, variationPercent);
    }
}