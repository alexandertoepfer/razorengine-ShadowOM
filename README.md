# RazorEngine CG - ShadowOM
Proposal to optimise RazorEngine compilation of templates with multiple OMs
through introduction of a hidden Shadow OM similar to how Mozilla solved this problem in Web Components.
```c
foreach(var type in supportedTypes) {
    Engine.Razor.Compile(template, "templateKey", type);
}
```
Above approach to solving the multiple entity problem causes long term performance setbacks,
it seems that drawing inspiration from the ShadowDOM feature might pose to be a precaution to this issue.
```c
Engine.Razor.Compile(template, "templateKey", ShadowOM);
```
This heavily relies on the type structure being changed obviously, a method to retrieve the specified root is necessary,
in this case not a document but the core concept still applies.
