using System.Collections.Generic;
using System.Text;

public class GameParamater {
    private Dictionary<string, object> paramaters;

    protected GameParamater() {
        paramaters = new Dictionary<string, object>();
    }

    public static GameParamater Create() {
        return new GameParamater();
    }

    public bool ContainsKey(string key) {
        return paramaters.ContainsKey(key);
    }

    public GameParamater SetParam<T>(string key, T value, object setter = null) {
        paramaters[key] = value;
        return this;
    }

    public T GetParam<T>(string key, T defaultValue = default(T)) {
        if (paramaters.TryGetValue(key, out object value)) {
            if (value is T result) {
                return result;
            }
        }
        return defaultValue;
    }

    public override string ToString() {
        StringBuilder builder = new StringBuilder();
        foreach (var item in paramaters) {
            builder.Append($"\n{item.Key}: {item.Value}");
        }
        return builder.ToString();
    }
}