public static class RandomHelper {
    public static T Random<T>(this T[] collection) {
        if (collection != null && collection.Length > 0) {
            return collection[UnityEngine.Random.Range(0, collection.Length)];
        }

        return default;
    }

    public static bool Chance(int percent) {
        return UnityEngine.Random.Range(0, 100) <= percent;
    }
}
