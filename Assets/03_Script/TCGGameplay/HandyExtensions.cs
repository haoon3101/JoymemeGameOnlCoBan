using UnityEngine;

public static class HandyExtensions
{
    public static float Jiggled(this float value)
    {
        return value * Random.Range(0.9f, 1.1f);
    }
}
