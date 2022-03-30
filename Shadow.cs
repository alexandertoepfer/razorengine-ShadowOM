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
public abstract class ShadowOM {
  // Type of deserialized OM
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
  public bool Has(String prop) => this.GetType().GetProperty(prop) != null;
  
  // This method takes a list of model types and populates the matching type.
  public NullValueDictionary<Type, dynamic> In(Type[] list) => 
	  list.Where(x => Is(x)).ToList().Any() ? 
	  new NullValueDictionary<Type, dynamic> {{
		  list.Where(x => Is(x)).Single(), 
		  new Func<dynamic>(() => {
			  var root = this.Root();
			  var properties = root.GetType().GetProperties();
			  var instance = Activator.CreateInstance(list.Where(x => Is(x)).Single());
			  var writeReadProps = ((IEnumerable<dynamic>)properties).Where(prop => prop.CanRead && prop.CanWrite);
			  foreach (var prop in writeReadProps) {
				  object copyValue = prop.GetValue(root);
				  prop.SetValue(instance, copyValue);
			  }
			  return instance;
		  }).Invoke()
	  }} : throw new InvalidCastException();
};

// Current Classes with their recovery type implemented for the Shadow OM,
// the templates can easily retrieve the root again with this information, as highlighted below.
public class Type1 : ShadowOM {
  sealed protected override Type ModelType { get; } = typeof(Type1);
  public string Prefix { get; } = "1";
  public string? Name { get; init; }
};
public class Type2 : ShadowOM {
  sealed protected override Type ModelType { get; } = typeof(Type2);
  public string Prefix { get; } = "2";
  public string? Name { get; init; }
  public string? Suffix { get; init; }
};

public class Program {
  public static void Main() {
    // The usual ObjectModels from the XML into the classes
    var t1 = new Type1 { Name = "Name1" };
    var t2 = new Type2 { Name = "Name2", Suffix = "Ext" };

    // The generic compilation type used for razor, alias the models
    ShadowOM[] shadows = new [] { (t1 as ShadowOM), (t2 as ShadowOM) };

    // The root objects which have been specified originally
    List<dynamic> roots = shadows.Select(m => m.Root()).ToList();

    // The root classes of the specifications
    var types = shadows.Select(m => m.Type());
    var names = roots.Select(m => m.Name);

    // Proof that information is fully recoverable from Shadow
    dynamic t1Root = roots[0], t2Root = roots[1];

    // Given types from the specifications as expected
    Console.WriteLine(types.Aggregate((i, j) => i + "," + j));
    Console.WriteLine(names.Aggregate((i, j) => i + "," + j));
	  
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // Given values from the specifications as expected
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    Console.WriteLine($@"
      <!--
      @file {t1Root.Prefix}_{t1Root.Name}_{(t1Root.Has("Suffix") ? t1Root.Suffix + "_" : "")}Info.log
      @brief This file contains general information.
      Warning! This is a generated file. Manual changes will be omitted.
      -->
    ");
    Console.WriteLine($@"
      <!--
      @file {t2Root.Prefix}_{t2Root.Name}_{(t2Root.Has("Suffix") ? t2Root.Suffix + "_" : "")}Info.log
      @brief This file contains general information.
      Warning! This is a generated file. Manual changes will be omitted.
      -->
    ");

    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // Intellisense example with one model
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
    // Intellisense example with both models
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    var model2 = shadows[1]; // Example Model
    dynamic? nvdModelSet = null;

    try {
      nvdModelSet = model2.In(new [] { typeof(Type1), typeof(Type2) });
    } catch (InvalidCastException) {
      // Can not be cast to neither Type1, Type2
      // Model.To<Type3>();
      return;
    }

    // Assign models
    //Type1? t1OM = nvdModelSet[typeof(Type1)];
    Type2? t2OM = nvdModelSet[typeof(Type2)];

    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // Given values from the specifications as expected
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    switch (model1.Type()) {
      case nameof(Type1):
        Console.WriteLine($@"
          <!--
          @file {t1OM.Prefix}_{t1OM.Name}_Info.log
          @brief This file contains general information.
          Warning! This is a generated file. Manual changes will be omitted.
          -->
        ");
      break;

      case nameof(Type2):
        Console.WriteLine($@"
          <!--
          @file {t2OM.Prefix}_{t2OM.Name}_{t2OM.Suffix}_Info.log
          @brief This file contains general information.
          Warning! This is a generated file. Manual changes will be omitted.
          -->
        ");
      break;
    }
					  
    if (model2.Is(typeof(Type2))) {
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
