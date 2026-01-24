using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JToken = Newtonsoft.Json.Linq.JToken;

public static class _ExtMethodsJson
{
    /// <summary>
    /// Loads a JToken from a file with the specified path and filename.
    /// <br />*Call using a newly constructed JToken and access the return value for result.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="filename">The filename.</param>
    /// <returns>The loaded JToken.</returns>
    public static JToken LoadJsonFromFile(this JToken THIS, string path, string filename)
    {
        if (!Directory.Exists(path)) return null;
        if (!File.Exists(Path.Combine(path, $"{filename}.json"))) return null;
        using StreamReader load = File.OpenText($"{path}/{filename}.json");
        THIS = JObject.Parse(load.ReadToEnd());
        return THIS;
    }
    /// <summary>
    /// Saves this JToken to a file with the specified path and filename.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="filename">The filename.</param>
    public static void SaveToFile(this JToken THIS, string path, string filename)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        using StreamWriter file = File.CreateText(Path.Combine(path, $"{filename}.json"));
        file.WriteLine(THIS.ToString());
    }

    /// <summary>
    /// Serializes the input object into a JToken. 
    /// <br />*Compatible with ICustomSerialized
    /// <br />*Must be used with one a newly constructed JToken via new().
    /// </summary>
    /// <param name="OBJ">The Source Object to be Serialized.</param>
    /// <returns></returns>
    public static JToken Serialize(this JToken THIS, object OBJ)
    {
        THIS = typeof(ICustomSerialized).IsAssignableFrom(OBJ.GetType()) 
            ? (OBJ as ICustomSerialized).Serialize() 
            : JObject.FromObject(OBJ);
        return THIS;
    }

    /// <summary>
    /// Deserializes this Token into the desired Type.
    /// </summary>
    /// <typeparam name="T">The Type to Deserialize into.</typeparam>
    /// <returns>The Deserialized Value.</returns>
    public static T Deserialize<T>(this JToken THIS)
    {
        if (typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
        {
            var Result = Activator.CreateInstance<T>() as ICustomSerialized;
            Result.Deserialize(THIS);
            return (T)Result;
        }
        else return THIS.Value<T>();
    }
    /// <summary>
    /// Attempts to Deserialize this Token into the desired Type.
    /// </summary>
    /// <typeparam name="T">The Type to Deserialize into.</typeparam>
    /// <param name="result"></param>
    /// <returns>Whether the Deserialization was succesful.</returns>
    public static bool TryDeserialize<T>(this JToken THIS, out T result)
    {
        if (typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
        {
            var IResult = Activator.CreateInstance<T>() as ICustomSerialized;
            IResult.Deserialize(THIS);
            result = (T)IResult;
        }
        else result = THIS.Value<T>();
        return result != null;
    }

    /// <summary>
    /// Populates an existing object using this Token.
    /// </summary>
    /// <param name="target">The Target object.</param>
    public static void DeserializeInto(this JToken THIS, object target)
    {
        var Custom = target as ICustomSerialized;
        if (Custom != null) Custom.Deserialize(THIS);
        else
            using (JsonReader sr = THIS.CreateReader())
                JsonSerializer.CreateDefault().Populate(sr, target);
    }

    /// <summary>
    /// Converts and Returns the Value as the desired Type.
    /// </summary>
    /// <typeparam name="T">The Type to Convert to.</typeparam>
    /// <returns>A Converted Value.</returns>
    public static T As<T>(this JToken THIS) => THIS.Value<T>();

}

public interface ICustomSerialized
{
    /// <summary>
    /// Serializes the object into a JToken. (Can be overridden along with its Deserialize Counterpart.)
    /// </summary>
    /// <returns> The Json representation.</returns>
    public JToken Serialize();
    /// <summary>
    /// Deserializes a JToken and populates this object with its data. (Can be overridden along with its Serialize Counterpart.)
    /// </summary>
    /// <param name="Data">The Json representation to be Deserialized.</param>
    public void Deserialize(JToken Data);

}

//FAILED PRIOR IMPLEMENTATION, PAY NO MIND.
/*
public class Json
{
    public string raw;

    public Json() => raw = "";
    public Json(string input) => raw = input;
    public static implicit operator string(Json obj) => obj.raw;
    public static implicit operator Json(string input) => new(input);
    public static implicit operator Json(JToken input) => new(input.ToString());
    //public static explicit operator Json(object input) => new(input as string);

    public static Json LoadJsonFromFile(string path, string filename)
    {
        if (!Directory.Exists(path)) return null;
        if (!File.Exists($"{path}/{filename}.json")) return null;
        using StreamReader load = File.OpenText($"{path}/{filename}.json");
        return new(load.ReadToEnd());
    }
    public void SaveToFile(string path, string filename)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        using StreamWriter file = File.CreateText($"{path}/{filename}.json");
        file.WriteLine(raw);
    }

    //Improve more later (Add Indenting, make more Add functions, refactor to use "Begin/End Field" praxis, etc.)
    public class Builder : Json
    {
        public Builder() { raw = "{\n"; }

        public static Builder operator +(Builder B, string S)
        {
            B.raw += S;
            return B;
        }

        public void AddFieldHeader(string name) => this.raw += $"\"{name}\":";
        public void AddField(string name, object value, bool last = false) => this.raw += $"\"{name}\": {Serialize(value).raw}" + IsLast(last);
        public void AddString(string name, object value, bool last = false) => this.raw += $"\"{name}\": \"{value}\"" + IsLast(last);
        public void AddList<T>(string name, List<T> value, bool last = false)
        {
            string result = $"\"{name}\":\n[\n";
            for (int i = 0; i < value.Count; i++) result += Serialize(value[i]) + IsLast(i == value.Count-1);
            this.raw += result + "\n]" + IsLast(last) + "\n";
        }

        public Json Result()
        {
            raw += "\n}";
            return this;
        }

        public static string IsLast(bool yes) => yes ? "" : ",\n";
    }

    /// <summary>
    /// Serializes any object fed to it, including ones inheriting AND not inheriting from IJson.
    /// </summary>
    /// <param name="OBJ">The Object to be Serialized.</param>
    /// <returns>The Json representation.</returns>
    public static Json Serialize(object OBJ) => 
        typeof(ICustomSerialized).IsAssignableFrom(OBJ.GetType()) ? 
            (OBJ as ICustomSerialized).Serialize() : 
            (Json)JsonConvert.SerializeObject(OBJ);

    /// <summary>
    /// Deserializes this Json representation into the type desired. (Better know what you're doing.)
    /// </summary>
    /// <typeparam name="T">The Type of Object you're trying to obtain.</typeparam>
    /// <returns>The Deserialized Object.</returns>
    public T Deserialize<T>()
    {
        if (typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
        {
            var Result = Activator.CreateInstance<T>() as ICustomSerialized;
            Result.Deserialize(this);
            return (T)Result;
        }
        else if (raw[0] != '{' && raw[0] != '[') return new JRaw(raw).Value<T>();
        else return JsonConvert.DeserializeObject<T>(this);
    }
    /// <summary>
    /// Deserializes a Json representation into the type desired. (Better know what you're doing.)
    /// </summary>
    /// <typeparam name="T">The Type of Object you're trying to obtain.</typeparam>
    /// <param name="Data">The Json representation you're trying to Deserialize.</param>
    /// <returns>The Deserialized Object.</returns>
    public static T Deserialize<T>(Json Data)
    {
        if (typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
        {
            var Result = Activator.CreateInstance<T>() as ICustomSerialized;
            Result.Deserialize(Data);
            return (T)Result;
        }
        else if (Data.raw[0] != '{' && Data.raw[0] != '[') return new JRaw(Data).Value<T>();
        else return JsonConvert.DeserializeObject<T>(Data);
    }

    /// <summary>
    /// Attempts to Deserialize this Json representation into the desired type.
    /// </summary>
    /// <typeparam name="T">The Type of Object you're trying to obtain.</typeparam>
    /// <param name="result">The Deserialized Object.</param>
    /// <returns>Whether the Deserialization was successful.</returns>
    public bool TryDeserialize<T>(out T result)
    {
        if (typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
        {
            result = Activator.CreateInstance<T>();
            (result as ICustomSerialized).Deserialize(this);
        }
        else if (raw[0] != '{' && raw[0] != '[') result = new JRaw(raw).Value<T>();
        else result = JsonConvert.DeserializeObject<T>(this);
        return result != null;
    }
    /// <summary>
    /// Attempts to Deserialize a Json representation into the desired type.
    /// </summary>
    /// <typeparam name="T">The Type of Object you're trying to obtain.</typeparam>
    /// <param name="Data">The Json representation you're trying to Deserialize.</param>
    /// <param name="result">The Deserialized Object.</param>
    /// <returns>Whether the Deserialization was successful.</returns>
    public static bool TryDeserialize<T>(Json Data, out T result)
    {
        if(typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
        {
            result = Activator.CreateInstance<T>();
            (result as ICustomSerialized).Deserialize(Data);
        }
        else if (Data.raw[0] != '{' && Data.raw[0] != '[') result = new JRaw(Data).Value<T>();
        else result = JsonConvert.DeserializeObject<T>(Data);
        return result != null;
    }

    /// <summary>
    /// Deserializes this Json representation and populates an object with its data.
    /// </summary>
    /// <param name="target">The Object to be Populated.</param>
    public void DeserializeInto(object target)
    {
        var Custom = target as ICustomSerialized;
        if(Custom != null) Custom.Deserialize(this);
        else JsonConvert.PopulateObject(raw, target);
    }
    /// <summary>
    /// Deserializes a Json representation and populates an object with its data.
    /// </summary>
    /// <param name="input">The Json representation to be Deseralized.</param>
    /// <param name="target">The Object to be Populated.</param>
    public static void DeserializeInto(Json input, object target)
    {
        if (target is ICustomSerialized Custom) Custom.Deserialize(input);
        else JsonConvert.PopulateObject(input, target);
    }

    /// <summary>
    /// Deserializes a Json representation and populates an object with its data.
    /// </summary>
    /// <param name="input">The Json representation to be Deseralized.</param>
    /// <param name="target">The Object to be Populated.</param>
    public static void DeserializeInto(JToken input, object target)
    {
        if (target is ICustomSerialized Custom) Custom.Deserialize(input);
        else JsonConvert.PopulateObject(input.ToString(), target);
    }


    public Dictionary<string, Json> ToDictionary()
    {
        JObject X = JsonConvert.DeserializeObject<JObject>(this);
        Dictionary<string, Json> result = new();

        foreach (KeyValuePair<string, JToken> item in X) result.Add(item.Key, item.Value.ToString());
        return result;
    }
    public Json[] ToArray()
    {
        JToken[] X = JsonConvert.DeserializeObject<JToken[]>(this);
        Json[] result = new Json[X.Length];
        for (int i = 0; i < result.Length; i++) result[i] = X[i].ToString();
        return result;
    }
    public JToken ToJToken() => Deserialize<JToken>();

    public T Value<T>() => new JRaw(raw).Value<T>();
}*/