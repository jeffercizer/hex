using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public static class Serializer
{
    public static void Serialize<T>(BinaryWriter writer, T obj)
    {
        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = property.GetValue(obj);
            Type type = property.PropertyType;

            if (type == typeof(int)) writer.Write((int)value);
            else if (type == typeof(float)) writer.Write((float)value);
            else if (type == typeof(bool)) writer.Write((bool)value);
            else if (type == typeof(string)) writer.Write(value as string ?? "");
            else if (type.IsEnum) writer.Write(Convert.ToInt32(value));
            else if (typeof(IList).IsAssignableFrom(type))
            {
                var list = (IList)value;
                writer.Write(list.Count);
                foreach (var item in list)
                    Serialize(writer, item);
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                var dict = (IDictionary)value;
                writer.Write(dict.Count);
                foreach (DictionaryEntry entry in dict)
                {
                    Serialize(writer, entry.Key);
                    Serialize(writer, entry.Value);
                }
            }
            else if (value != null) // Nested objects
            {
                Serialize(writer, value);
            }
        }
    }

    public static T Deserialize<T>(BinaryReader reader) where T : new()
    {
        var obj = new T();

        foreach (var property in typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Type type = property.PropertyType;

            if (type == typeof(int)) property.SetValue(obj, reader.ReadInt32());
            else if (type == typeof(float)) property.SetValue(obj, reader.ReadSingle());
            else if (type == typeof(bool)) property.SetValue(obj, reader.ReadBoolean());
            else if (type == typeof(string)) property.SetValue(obj, reader.ReadString());
            else if (type.IsEnum) property.SetValue(obj, Enum.ToObject(type, reader.ReadInt32()));
            else if (typeof(IList).IsAssignableFrom(type))
            {
                Type itemType = type.GetGenericArguments()[0]; // Get list element type
                var list = (IList)Activator.CreateInstance(type);
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                    list.Add(DeserializeDynamic(reader, itemType));
                property.SetValue(obj, list);
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                Type keyType = type.GetGenericArguments()[0]; // Get dictionary key type
                Type valueType = type.GetGenericArguments()[1]; // Get dictionary value type
                var dict = (IDictionary)Activator.CreateInstance(type);
                int count = reader.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    var key = DeserializeDynamic(reader, keyType);
                    var value = DeserializeDynamic(reader, valueType);
                    dict.Add(key, value);
                }
                property.SetValue(obj, dict);
            }
            else property.SetValue(obj, DeserializeDynamic(reader, type));
        }

        return obj;
    }

    public static object DeserializeDynamic(BinaryReader reader, Type type)
    {
        if (type == typeof(int)) return reader.ReadInt32();
        if (type == typeof(float)) return reader.ReadSingle();
        if (type == typeof(bool)) return reader.ReadBoolean();
        if (type == typeof(string)) return reader.ReadString();
        if (type.IsEnum) return Enum.ToObject(type, reader.ReadInt32());

        if (Nullable.GetUnderlyingType(type) != null) // Handling nullable types
        {
            bool hasValue = reader.ReadBoolean();
            if (!hasValue) return null;
            type = Nullable.GetUnderlyingType(type);
        }

        var method = typeof(Serializer).GetMethod("Deserialize", BindingFlags.Public | BindingFlags.Static);
        var genericMethod = method.MakeGenericMethod(type);
        return genericMethod.Invoke(null, new object[] { reader });
    }

}
