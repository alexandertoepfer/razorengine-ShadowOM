#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

/*  ____       ____  _     ____  ____  ____  _        ____  ____     _  _____ ____  _____    _      ____  ____  _____ _    
*  ||""||     / ___\/ \ /|/  _ \/  _ \/  _ \/ \  /|  /  _ \/  __\   / |/  __//   _\/__ __\  / \__/|/  _ \/  _ \/  __// \  
*  ||__||     |    \| |_||| / \|| | \|| / \|| |  ||  | / \|| | //   | ||  \  |  /    / \    | |\/||| / \|| | \||  \  | |   
*  [ -=.]`)   \___ || | ||| |-||| |_/|| \_/|| |/\||  | \_/|| |_\\/\_| ||  /_ |  \__  | |    | |  ||| \_/|| |_/||  /_ | |_/\
*  ====== 0   \____/\_/ \|\_/ \|\____/\____/\_/  \|  \____/\____/\____/\____\\____/  \_/    \_/  \|\____/\____/\____\\____/
*/
// by Alexander Töpfer & Phillip Töpfer
// Inspired by Web Components Shadow DOM Encapsulation
// Resources => https://developer.mozilla.org/en-US/docs/Web/Web_Components/Using_shadow_DOM

// Dummy class to highlight usage in main
public static class Engine {
  public static class Razor {
    public static object? RunCompile(string template,
                                     string templateKey,
                                     Type modelType,
                                     object objectModel) => null;

    public static object? Compile(string template,
                                  string templateKey,
                                  Type modelType) => null;
  };
};

// NullValueDictionary class to directly assign nullables after look-up and 
// avoid stuff like Dict.ContainsKey(...)? Dict[...] : null and KeyNotFoundException
// when we want to have null entries for not populated OMs.
public class NullValueDictionary<T, U> : Dictionary<T, U> where U : class 
                                                          where T : notnull {
  new public U? this[T key] {
    get {
      this.TryGetValue(key, out var val);
      return val;
    }
  }
}

// This will be used for dynamic cast redirection to recover classes, it's replacing the
// missing ObjectModel class without affecting the XML structure of <Type1>, <Type2>, etc.
// It's basically a type recovery strategy in place to make compilation faster.
// The idea is to squash objects in memory into a hidden ShadowOM for faster compilation
// while being able to easily retrieve the original during runtime of templates.
public abstract class Shadow {
  protected abstract Type ModelType { get; }

  // This method returns the type as String.
  public String Type() => ModelType.Name;

  // This method returns the original object.
  public dynamic Root() => Convert.ChangeType(this, ModelType);

  // This method returns the original object as type T.
  public T To<T>() => (T) Convert.ChangeType(this, typeof(T));

  // This method can be used for type checking.
  public bool Is(Type type) => type == ModelType;

  // This method can test whether a property exists
  public bool HasProperty(String prop) => this.GetType().GetProperty(prop) != null;
  
  // This method takes a list of model types and populates the matching type.
  public NullValueDictionary<Type, dynamic> In(Type[] list) {
    var results = new NullValueDictionary<Type, dynamic>();
    var item = list.Where(x => Is(x)).Single();
    var instance = Activator.CreateInstance(item);
    instance = this.Root();
    results.Add(item, instance);
    return results;
  }
};

// Current Asset classes with their recovery type implemented for the Shadow OM,
// the templates can easily retrieve the root again with this information, as highlighted below.
public class Type1 : Shadow {
  sealed protected override Type ModelType { get; } = typeof(Type1);
  public string Prefix { get; } = "1";
  public string? Name { get; init; }
};
public class Type2 : Shadow {
  sealed protected override Type ModelType { get; } = typeof(Type2);
  public string Prefix { get; } = "2";
  public string? Name { get; init; }
  public string? Suffix { get; init; }
};

// Proof of concept for Shadow OM class
public class Program {
  public static void Main() {
    // The usual ObjectModels from the XML into the classes
    var t1 = new Type1 { Name = "Type1" };
    var t2 = new Type2 { Name = "Type2", Suffix = "Ext" };

    // The generic compilation type used for razor, alias the models
    Shadow[] shadows = new [] { (t1 as Shadow), (t2 as Shadow) };

    // The root objects which have been specified originally
    List<dynamic> roots = shadows.Select(m => m.Root()).ToList();

    // The root classes of the specifications
    var types = shadows.Select(m => m.Type());

    var template = "";

    // Usual required calls to Razor with a template supporting both asset types without Shadow
    Engine.Razor.Compile(template, "templateKey", typeof(Type1));
    Engine.Razor.Compile(template, "templateKey", typeof(Type2));

    // Compilation of the same template with a generic shadow object model which works with any asset type
    var result = Engine.Razor.RunCompile(template, "templateKey", typeof(Shadow), t1);

    // Proof that asset is fully recoverable from Shadow
    dynamic t1Root = roots[0], t2Root = roots[1];

    // Given types from the specifications as expected
    Console.WriteLine(types.Aggregate((i, j) => i + "," + j));

    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // Given values from the specifications as expected :)
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    Console.WriteLine($@"
      <!--
      @file {t1Root.Prefix}_{t1Root.Name}_{(t1Root.HasProperty("Suffix") ? t1Root.Suffix + "_" : "")}Info.log
      @brief This file contains general information.
      Warning! This is a generated file. Manual changes will be omitted.
      -->
    ");
    Console.WriteLine($@"
      <!--
      @file {t2Root.Prefix}_{t2Root.Name}_{(t2Root.HasProperty("Suffix") ? t2Root.Suffix + "_" : "")}Info.log
      @brief This file contains general information.
      Warning! This is a generated file. Manual changes will be omitted.
      -->
    ");

    // Object Model types
    Type type1 = typeof(Type1), type2 = typeof(Type2);

    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // Examples with strong typed variable, Intellisense
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    var model1 = shadows[0]; // Example Model
    Type1? t1OM = null;

    try {
      t1OM = model1.To<Type1>();
    } catch (InvalidCastException) {
      // Can not be cast to Type1
      // model.To<Type2>();
      return;
    }

    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // Examples with strong typed variable, both models, Intellisense
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    var model2 = shadows[1]; // Example Model

    // Get strong typed objects from model
    var nvdModelSet = model2.In(new [] { type1, type2 });

    if (!nvdModelSet.Values.Any(x => x != null))
      // Can not be cast to neither Type1, Type2
      return;

    // Intellisense
    //Type1? t1OM = nvdModelSet[type1];
    Type2? t2OM = nvdModelSet[type2];

    // Assign models
    List<dynamic> models = nvdModelSet.Values.ToList();

    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // Given values from the specifications as expected :)
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    if (model1.Is(type1) && (t1OM != null) /* && models[0] != null */) {
      Console.WriteLine($@"
        <!--
        @file {t1OM.Prefix}_{t1OM.Name}_Info.log
        @brief This file contains general information.
        Warning! This is a generated file. Manual changes will be omitted.
        -->
      ");
    }
    if (model2.Is(type2) && (t2OM != null) /* && models[1] != null */) {
      Console.WriteLine($@"
        <!--
        @file {t2OM.Prefix}_{t2OM.Name}_{t2OM.Suffix}_Info.log
        @brief This file contains general information.
        Warning! This is a generated file. Manual changes will be omitted.
        -->
      ");	
    }
  }
}

// Compiled with .NET 6 on https://dotnetfiddle.net/
