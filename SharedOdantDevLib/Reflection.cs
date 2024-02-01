using System.Reflection;

namespace OdantDev;
public static class Reflection
{
    /// <summary>
    /// Extension method to map properties from an object to a new instance of type T.
    /// </summary>
    /// <typeparam name="T">The target type to map to.</typeparam>
    /// <param name="source">The source object to map properties from.</param>
    /// <returns>A new instance of type T with properties mapped from the source object.</returns>
    public static T Map<T>(this object source)
    {
        var output = Activator.CreateInstance(typeof(T));
        var sourceType = source.GetType();

        foreach (var outputProperty in typeof(T).GetProperties())
        {
            if (sourceType.GetProperty(outputProperty.Name) is PropertyInfo sourceProperty && sourceProperty.CanWrite)
            {
                var value = sourceProperty.GetValue(source);
                outputProperty.SetValue(output, value);
            }
        }

        return (T)output;
    }

    /// <summary>
    /// Returns a _private_ Property Value from a given Object. Uses Reflection.
    /// Throws a ArgumentOutOfRangeException if the Property is not found.
    /// </summary>
    /// <typeparam name="T">Type of the Property</typeparam>
    /// <param name="obj">Object from where the Property Value is returned</param>
    /// <param name="propName">Propertyname as string.</param>
    /// <returns>PropertyValue</returns>
    public static T GetPrivatePropertyValue<T>(this object obj, string propName)
    {
        if (obj == null) throw new ArgumentNullException("obj");
        PropertyInfo pi = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (pi == null) throw new ArgumentOutOfRangeException("propName", string.Format("Property {0} was not found in Type {1}", propName, obj.GetType().FullName));
        return (T)pi.GetValue(obj, null);
    }

    /// <summary>
    /// Returns a private Property Value from a given Object. Uses Reflection.
    /// Throws a ArgumentOutOfRangeException if the Property is not found.
    /// </summary>
    /// <typeparam name="T">Type of the Property</typeparam>
    /// <param name="obj">Object from where the Property Value is returned</param>
    /// <param name="propName">Propertyname as string.</param>
    /// <returns>PropertyValue</returns>
    public static T GetPrivateFieldValue<T>(this object obj, string propName)
    {
        if (obj == null) throw new ArgumentNullException("obj");
        Type t = obj.GetType();
        FieldInfo fi = null;
        while (fi == null && t != null)
        {
            fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            t = t.BaseType;
        }
        if (fi == null) throw new ArgumentOutOfRangeException("propName", string.Format("Field {0} was not found in Type {1}", propName, obj.GetType().FullName));
        return (T)fi.GetValue(obj);
    }

    /// <summary>
    /// Sets a _private_ Property Value from a given Object. Uses Reflection.
    /// Throws a ArgumentOutOfRangeException if the Property is not found.
    /// </summary>
    /// <typeparam name="T">Type of the Property</typeparam>
    /// <param name="obj">Object from where the Property Value is set</param>
    /// <param name="propName">Propertyname as string.</param>
    /// <param name="val">Value to set.</param>
    /// <returns>PropertyValue</returns>
    public static void SetPrivatePropertyValue<T>(this object obj, string propName, T val)
    {
        Type t = obj.GetType();
        if (t.GetProperty(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) == null)
            throw new ArgumentOutOfRangeException("propName", string.Format("Property {0} was not found in Type {1}", propName, obj.GetType().FullName));
        t.InvokeMember(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.SetProperty | BindingFlags.Instance, null, obj, new object[] { val });
    }

    /// <summary>
    /// Set a private Property Value on a given Object. Uses Reflection.
    /// </summary>
    /// <typeparam name="T">Type of the Property</typeparam>
    /// <param name="obj">Object from where the Property Value is returned</param>
    /// <param name="propName">Propertyname as string.</param>
    /// <param name="val">the value to set</param>
    /// <exception cref="ArgumentOutOfRangeException">if the Property is not found</exception>
    public static void SetPrivateFieldValue<T>(this object obj, string propName, T val)
    {
        if (obj == null) throw new ArgumentNullException("obj");
        Type t = obj.GetType();
        FieldInfo fi = null;
        while (fi == null && t != null)
        {
            fi = t.GetField(propName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            t = t.BaseType;
        }
        if (fi == null) throw new ArgumentOutOfRangeException("propName", string.Format("Field {0} was not found in Type {1}", propName, obj.GetType().FullName));
        fi.SetValue(obj, val);
    }

    /// <summary>
    /// Invoke a private Method on a given Object. Uses Reflection.
    /// </summary>
    /// <typeparam name="T">Type of the Property</typeparam>
    /// <param name="obj">Object from where the Property Value is returned</param>
    /// <param name="methodName">Method name.</param>
    /// <param name="parameters">Method parameters</param>
    /// <exception cref="ArgumentOutOfRangeException">if the Method is not found</exception>
    public static T InvokePrivateMethod<T>(this object obj, string methodName, params object[] parameters)
    {
        return (T)InvokePrivateMethod(obj, methodName, parameters);
    }

    /// <summary>
    /// Invoke a private Method on a given Object. Uses Reflection.
    /// </summary>
    /// <param name="obj">Object from where the Property Value is returned</param>
    /// <param name="methodName">Method name.</param>
    /// <param name="parameters">Method parameters</param>
    /// <exception cref="ArgumentOutOfRangeException">if the Method is not found</exception>
    public static object InvokePrivateMethod(this object obj, string methodName, params object[] parameters)
    {
        if (obj == null) throw new ArgumentNullException("obj");

        Type t = obj.GetType();
        MethodInfo mi = null;
        while (mi == null && t != null)
        {
            mi = t.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            t = t.BaseType;
        }
        if (mi == null) throw new ArgumentOutOfRangeException("methodName", string.Format("Field {0} was not found in Type {1}", methodName, obj.GetType().FullName));
        return mi.Invoke(obj, parameters);
    }

}