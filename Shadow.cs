using System;
using System.Collections.Generic;
using System.Linq;

/*   ____       ____  _     ____  ____  ____  _        ____  ____     _  _____ ____  _____    _      ____  ____  _____ _    
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
		public static object RunCompile(string template,
						string templateKey,
						Type modelType,
						object objectModel) => null;
		
		public static object Compile(string template,
					     string templateKey,
					     Type modelType) => null;
	};
};

// NullValueDictionary class to directly assign nullables after look-up and 
// avoid stuff like Dict.ContainsKey(...)? Dict[...] : null and KeyNotFoundException
// when we want to have null entries for not populated OMs.
public class NullValueDictionary<T, U> : Dictionary<T, U> where U : class {
    new public U this[T key] {
		get {
            this.TryGetValue(key, out var val);
            return val;
        }
    }
}

// This will be used for dynamic cast redirection to recover classes, it's replacing the
// missing ObjectModel class without affecting the XML structure of <equipmentPhase>, <equipmentModule>, etc.
// It's basically a type recovery strategy in place to make compilation faster ;)
// The idea is to squash objects in memory into a hidden Shadow OM for faster compilation
// while being able to easily retrieve the original during runtime of templates.
public abstract class Shadow {
	public string Type() => AssetType.Name;
	protected abstract Type AssetType { get; }
	
	// This method returns the original object.
	public virtual dynamic Root() => Convert.ChangeType(this, AssetType);
	
	// This method returns the original object as type T.
	public virtual T To<T>() => (T) Convert.ChangeType(this, typeof(T));
	
	// This method can be used for model type checking.
	public virtual bool Is(Type type) => type == AssetType;
	
	// This method takes a list of model types and populates the matching type.
	public virtual NullValueDictionary<String,dynamic> In(Type[] list) {
		var results = new NullValueDictionary<String,dynamic>();
		foreach (var item in list)
		{
			if (Is(item)) {
				var instance = Activator.CreateInstance(item);
				instance = this.Root();
				results.Add(item.Name, instance);
				break;
			}
		}
		return results;
	}
};

// Current Asset classes with their recovery type implemented for the Shadow OM,
// the templates can easily retrieve the root again with this information, as highlighted below.
public class EquipmentPhase : Shadow {
	sealed protected override Type AssetType { get; } = typeof(EquipmentPhase);
	public string TypeIdentifier { get; } = "PH";
	public string Name { get; init; }
	
};
public class EquipmentModule : Shadow {
	sealed protected override Type AssetType { get; } = typeof(EquipmentModule);
	public string TypeIdentifier { get; } = "EM";
	public string Name { get; init; }
	
};

// Proof of concept for Shadow OM class
public class Program {
	public static void Main() {
		// The usual ObjectModels from the XML into the classes
		var ph = new EquipmentPhase { Name = "AdjustPHAcid" };
		var em = new EquipmentModule { Name = "ControlDoV001" };
		
		// The generic compilation type used for razor, alias the models
		var shadows = new [] { (ph as Shadow), (em as Shadow) };
		
		// The root objects which have been specified originally
		List<dynamic> roots = shadows.Select(m => m.Root()).ToList();
		
		// The root classes of the specifications
		var types = shadows.Select(m => m.Type());
		
		// Usual required calls to Razor with a template supporting both asset types without Shadow
		Engine.Razor.Compile(template, "templateKey", typeof(EquipmentModule));
		Engine.Razor.Compile(template, "templateKey", typeof(EquipmentPhase));
		
		// Compilation of the same template with a generic shadow object model which works with any asset type
		var result = Engine.Razor.RunCompile(template, "templateKey", typeof(Shadow), ph);
		
		// Proof that asset is fully recoverable from Shadow
		dynamic phRoot = roots[0], emRoot = roots[1];
		
		// Given types from the specifications as expected
		Console.WriteLine(types.Aggregate((i, j) => i + "," + j));
		
		// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
		// Given values from the specifications as expected :)
		// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
		Console.WriteLine($@"
		<!--
			@file {phRoot.TypeIdentifier}_{phRoot.Name}_Info.log
			@brief This file contains general information about the asset.
			Warning! This is a generated file. Manual changes will be omitted.
		-->
		");
		Console.WriteLine($@"
		<!--
			@file {emRoot.TypeIdentifier}_{emRoot.Name}_Info.log
			@brief This file contains general information about the asset.
			Warning! This is a generated file. Manual changes will be omitted.
		-->
		");
		
		// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
		// Examples with strong typed variable, Intellisense
		// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
		var model = shadows[0]; // Example Model
		EquipmentPhase? phAsset = null;
		
		try {
			phAsset = model.To<EquipmentPhase>();
		} catch (InvalidCastException) {
			// Type not supported, could be that Model is EquipmentModule
			// model.To<EquipmentModule>();
		}
		
		// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
		// Examples with strong typed variable, both models, Intellisense
		// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
		model = shadows[1]; // Example Model
		
		// Get strong typed objects from model
		var nvdModelSet = model.In(new [] { typeof(EquipmentPhase), typeof(EquipmentModule) });

		// Assign model
		//EquipmentPhase? phAsset = nvdModelSet["EquipmentPhase"];
		EquipmentModule? emAsset = nvdModelSet["EquipmentModule"];
		
		// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
		// Given values from the specifications as expected :)
		// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
		Console.WriteLine($@"
		<!--
			@file {phAsset.TypeIdentifier}_{phAsset.Name}_Info.log
			@brief This file contains general information about the asset.
			Warning! This is a generated file. Manual changes will be omitted.
		-->
		");
		Console.WriteLine($@"
		<!--
			@file {emAsset.TypeIdentifier}_{emAsset.Name}_Info.log
			@brief This file contains general information about the asset.
			Warning! This is a generated file. Manual changes will be omitted.
		-->
		");
	}
}

// Compiled with .NET 6 on https://dotnetfiddle.net/
