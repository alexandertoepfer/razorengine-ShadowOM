# RazorEngine CG - ShadowOM
Proposal to optimise RazorEngine compilation of templates with multiple OMs
through introduction of a hidden Shadow OM similar to how Mozilla solved this problem in Web Components.
```csharp
foreach(var type in new [] { typeof(EquipmentPhase), typeof(EquipmentModule) }) {
    Engine.Razor.Compile(template, "templateKey", type);
}
```
Above approach to solving the multiple entity problem causes long term performance setbacks,
it seems that drawing inspiration from the ShadowDOM feature might pose to be a precaution to this issue.
```csharp
var result = Engine.Razor.RunCompile(template, "templateKey", typeof(Shadow), equipmentPhaseOM);
```
This heavily relies on the type structure being changed obviously, a method to retrieve the specified root is necessary,
in this case it will be valid for templates, but the core concept still applies. 

>Take a look into <code>Shadow.cs</code> to see how this was accomplished and to run the code visit https://dotnetfiddle.net/0mISCh

A template making use of the ShadowOM
would look like this depending what model types it processes:
```csharp
string template = @""
    @using Specifications;
    @inherits Razor.TemplateBase<Shadow>
    @using System;
    @{
        // Type of Asset depends on what was loaded into memory
        dynamic Asset = Model.Root();

        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // Intellisense support, one model
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        EquipmentPhase Asset = null;
        try {
            Asset = Model.To<EquipmentPhase>();
        } catch (InvalidCastException) {
            // Type not supported, could be that Model is EquipmentModule
            // which this template does not support
        }

        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        // Intellisense support, two models
        // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
        var modelList = new List<Type> { 
            typeof(EquipmentPhase), 
            typeof(EquipmentModule)
        };
        // Update modelList
        List<dynamic> updatedList = Model.Fit(modelList);
        // Assign model
        EquipmentPhase phAsset = updatedList[0];
        EquipmentModule emAsset = updatedList[1];
    }
    <!--
        @@file @(Asset.TypeIdentifier)_@(Asset.Name)_Info.log
        @@brief This file contains general information about the asset.
        Warning! This is a generated file. Manual changes will be omitted.
    -->
    @* Now certain code can be executed depending on what model was loaded *@
    @if (Model.type().Contains(""EquipmentPhase"")) {
        // Do something with equipmentPhase specific data
    }
    @if (Model.type().Contains(""EquipmentModule"")) {
        // Do something with equipmentModule specific data
    }
"";
```
