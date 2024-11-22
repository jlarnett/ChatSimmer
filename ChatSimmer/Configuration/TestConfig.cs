namespace ChatSimmer.Configuration;

public static class TestConfig
{
    /// <summary>
    /// Gets the value of environment variable and converts it to the correct type. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="key"></param>
    /// <param name="defaultValue"></param>
    /// <returns>value in type of T</returns>
    public static T? GetVarFor<T>(string key, T? defaultValue = default)
    {
        var value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }

        return value is T t ? t : (T)Convert.ChangeType(value, typeof(T));
    }
}
