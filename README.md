# Sartorius Stedim - ShadowOM
Proposal to optimise RazorEngine compilation of templates with multiple OMs
through introduction of a hidden Shadow OM similar to how it's done with Web Components.
```c
foreach(var type in supportedTypes) {
    Engine.Razor.Compile(template, "templateKey", type);
}
```
Above approach to solving the multiple entity problem causes long term performance setbacks,
it seems that Mozialla's ShadowDOM feature might pose to be a precaution to this issue.
```c
Engine.Razor.Compile(template, "templateKey", ShadowOM);
```
This heavily relies on the type structure obviously.
