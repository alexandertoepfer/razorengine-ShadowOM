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

>Take a look into <code>Shadow.cs</code> to see how this was accomplished and to run the code visit https://dotnetfiddle.net/NKmRaP

A template making use of the ShadowOM
would look like this depending what model types it processes:
```csharp
string template = @""
    @inherits Razor.TemplateBase<Shadow>
    @using System;
    @{
        // Type of Asset depends on what was loaded into memory
        dynamic OM = Model.Root();

        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // Intellisense support, template with one model
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        Type1? type1OM = null;

        try {
            type1OM = Model.To<Type1>();
        } catch (InvalidCastException) {
            // Type not supported, could be that Model is Type2
            // model.To<Type2>();
        }

        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // Intellisense support, template with multiple models
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        Type1? type1OM = null;
        Type2? type2OM = null;

        // Get strong typed objects from model
        var nvdModelSet = Model.In(new [] { typeof(Type1), typeof(Type2) });

        // Assign model
        type1OM = nvdModelSet[""Type1""];
        type2OM = nvdModelSet[""Type2""];
    }
    <!--
    @@file @(OM.Prefix)_@(OM.Name)_Info.log
    @@brief This file contains general information.
    Warning! This is a generated file. Manual changes will be omitted.
    -->
    @* Now certain code can be executed with only type1OMs or type2OMs *@
    @if (Model.Is(typeof(Type1))) {
        // Do something with equipmentPhase specific data
    }
    @if (Model.Is(typeof(Type2))) {
        // Do something with equipmentModule specific data
    }
"";
```
