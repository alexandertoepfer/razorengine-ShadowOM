# RazorEngine - ShadowOM
Proposal to optimise RazorEngine compilation of templates with multiple OMs
through introduction of a hidden ShadowOM similar to how Mozilla solved this problem in Web Components, in order to compile templates with encapsulated OMs. 
>Inspired by https://developer.mozilla.org/en-US/docs/Web/Web_Components/Using_shadow_DOM
```csharp
foreach(var type in new [] { typeof(Type1), typeof(Type2) }) {
  Engine.Razor.Compile(template, "templateKey", type);
}
```
Above approach to solving the multiple OMs problem causes long term performance setbacks and it fundamentally breaks Intellisense due to the TemplateBase dynamic,
however it seems that drawing inspiration from the ShadowDOM feature might pose to be a precaution to this issue. Let's assume:
```csharp
var result = Engine.Razor.RunCompile(template, "templateKey", typeof(ShadowOM), type1OM);
```
This heavily relies on the type structure being changed obviously, a method to retrieve the specified root (just like in ShadowDOM) is necessary,
in this scenario the use case will be valid for templates, but the core concept still applies. 

```csharp
public abstract class ShadowOM {
  public dynamic Root(); // returns the original object.
  public T To<T>(); // returns the original object as type T. throws InvalidCastException
  public bool Is(Type type); // type checking.
  public String Type(); // returns the original object type as String.
  public NullValueDictionary<Type, dynamic> In(Type[] list); // returns collection with matching type.
  public bool Has(String prop); // returns whether a property exists.
};
```
Old approach to template example with multiple OMs, notice that there is no type safety, the template takes any model of any type (that was compiled) and therefore Intellisense does not work, the only reasonable way is checking an indicator property to figure out what type it could have been, but then again you'd have to half guess its properties...

```csharp
string oldTemplate = @""
  @inherits Razor.TemplateBase<dynamic>
  @using System;
  @{
    // Type of Model depends on what was loaded into memory
    dynamic OM = Model;
  }
  @if (OM.Prefix == "1") {
    // OM is Type1
  }
  @else if (OM.Prefix == "2") {
    // OM is Type2
  }
"";
```

New approach with ShadowOM:
```csharp
string template = @""
  @inherits Razor.TemplateBase<ShadowOM>
  @using System;
  @{
    // Type of Model depends on what was loaded into memory
    /* dynamic OM = Model.Root(); */

    // Object Models to be supported
    Type type1 = typeof(Type1), type2 = typeof(Type2);

    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // Examples with strong typed variable, Intellisense
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    Type1? t1OM = null;

    try {
      t1OM = Model.To<Type1>();
    } catch (InvalidCastException) {
      // Can not be cast to Type1
      // Model.To<Type2>();
      return;
    }

    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    // Examples with strong typed variable, both models, Intellisense
    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+

    // Get strong typed objects from model
    var nvdModelSet = Model.In(new [] { type1, type2 });

    if (!nvdModelSet.Values.Any(x => x != null))
      // Can not be cast to neither Type1, Type2
      return;

    // Assign models
    //Type1? t1OM = nvdModelSet[type1];
    Type2? t2OM = nvdModelSet[type2];
  }
  @* Old approach with dynamic *@
  /*@if (OM != null) {
    var res = $@""
      @file {OM.Prefix}_{OM.Name}_{(OM.Has("Suffix") ? OM.Suffix + "_" : "")}Info.log
      @brief This file contains general information.
      Warning! This is a generated file. Manual changes will be omitted.
    "";
    @res
  }*/
  @* Template for both models *@
  @switch (Model.Type()) {
    case nameof(Type1):
      var res = $@""
        @file {t1OM.Prefix}_{t1OM.Name}_Info.log
        @brief This file contains general information.
        Warning! This is a generated file. Manual changes will be omitted.
      "";
      @res
    break;
    
    case nameof(Type2):
      if (t2OM != null) {
        var res = $@""
          @file {t2OM.Prefix}_{t2OM.Name}_{t2OM.Suffix}_Info.log
          @brief This file contains general information.
          Warning! This is a generated file. Manual changes will be omitted.
        "";
        @res
      }
    break;
  }
  @if (Model.Is(type2) && t2OM != null) {
    var res = $@""
      @file {t2OM.Prefix}_{t2OM.Name}_{t2OM.Suffix}_Info.log
      @brief This file contains general information.
      Warning! This is a generated file. Manual changes will be omitted.
    "";
    @res
  }
"";
```
>Take a look into <code>Shadow.cs</code> to see how this was accomplished and to run the code visit https://dotnetfiddle.net/keixGq
