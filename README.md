# RazorEngine - ShadowOM
Proposal to optimise RazorEngine compilation of templates with multiple OMs
through introduction of a hidden Shadow OM similar to how Mozilla solved this problem in Web Components.
```csharp
foreach(var type in new [] { typeof(Type1), typeof(Type2) }) {
    Engine.Razor.Compile(template, "templateKey", type);
}
```
Above approach to solving the multiple entity problem causes long term performance setbacks,
it seems that drawing inspiration from the ShadowDOM feature might pose to be a precaution to this issue.
```csharp
var result = Engine.Razor.RunCompile(template, "templateKey", typeof(Shadow), type1OM);
```
This heavily relies on the type structure being changed obviously, a method to retrieve the specified root is necessary,
in this case it will be valid for templates, but the core concept still applies. 

```csharp
public abstract class Shadow {
    public dynamic Root(); // returns the original object.
    public T To<T>(); // returns the original object as type T. throws InvalidCastException
    public bool Is(Type type); // type checking.
    public NullValueDictionary<String, dynamic> In(Type[] list); // returns collection with matching type.
};
```

>Take a look into <code>Shadow.cs</code> to see how this was accomplished and to run the code visit https://dotnetfiddle.net/toIzoe

A template making use of the ShadowOM
would look like this depending what model types it processes:
```csharp
string template = @""
    @inherits Razor.TemplateBase<Shadow>
    @using System;
    @{
        // Type of Asset depends on what was loaded into memory
        dynamic OM = Model.Root();

        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
	// Examples with strong typed variable, Intellisense
	// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
	Type1? t1OM = null;

	try {
		t1OM = Model.To<Type1>();
	} catch (InvalidCastException) {
		// Could be that Model is Type2
		// Model.To<Type2>();
		return;
	}

	// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
	// Examples with strong typed variable, both models, Intellisense
	// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

	// Get strong typed objects from model
	var nvdModelSet = Model.In(new [] { typeof(Type1), typeof(Type2) });

	// Assign model
	Type1? t1OM = nvdModelSet["Type1"];
	Type2? t2OM = nvdModelSet["Type2"];
		
	if ((new List<dynamic?> { t1OM2, t2OM }).All(x => (x == null)))
		// Could be that Model is neither Type1, Type2
		return;
    }
    @* Now certain code can be executed with only type1OMs or type2OMs *@
    @if (Model.Is(typeof(Type1))) {
	<!--
	@@type Type1
	@@file @(t1OM.Prefix)_@(t1OM.Name)_{(t1OM.HasProperty("Suffix") ? t1OM.Suffix + "_" : "")}Info.log
	@@brief This file contains general information.
	Warning! This is a generated file. Manual changes will be omitted.
	-->
    }
    @if (Model.Is(typeof(Type2))) {
	<!--
	@@type Type2
	@@file @(t2OM.Prefix)_@(t2OM.Name)_{(t2OM.HasProperty("Suffix") ? t2OM.Suffix + "_" : "")}Info.log
	@@brief This file contains general information.
	Warning! This is a generated file. Manual changes will be omitted.
	-->
    }
"";
```
