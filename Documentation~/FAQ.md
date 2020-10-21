# Frequently asked questions

## Getting started with Tiny

To begin learning about DOTs, refer to the [Entities Component System](https://docs.unity3d.com/Packages/com.unity.entities@0.14/manual/index.html) (ECS) online documentation for general information about DOTs development. Then join the [Project Tiny](https://forum.unity.com/forums/project-tiny.151/) forum for active discussion with fellow users and developers. With every release of Project Tiny, developers share demo projects that showcase how they can be built using Project Tiny.

## Assemblies contained in the package

| **Unity.2D.Entities**                          | Common components and systems                         |
| ---------------------------------------------- | ----------------------------------------------------- |
| **Unity.2D.Entities.Authoring**                | Authoring for common 2D components                    |
| **Unity.2D.Entities.Hybrid**                   | Common editor components and systems                  |
| **Unity.2D.Entities.SpriteRenderer**           | SpriteRenderer specific components and systems        |
| **Unity.2D.Entities.SpriteRenderer.Authoring** | SpriteRenderer specific authoring                     |
| **Unity.2D.Entities.SpriteRenderer.Hybrid**    | SpriteRenderer specific editor components and systems |

## Changing the Sprite used by a Sprite Renderer

To change the Sprite use by the Sprite Renderer, change the selected Entity in the Sprite property of the Sprite Renderer to another Entity with a Sprite component. The Sprite Renderer will start using the updated Sprite data for rendering. See below for a code example:

```c#
var renderer = EntityManager.GetComponentData<SpriteRenderer>(spriteRendererEntity);
renderer.Sprite = newSpriteEntity;
EntityManager.SetComponentData(spriteRendererEntity, renderer);
```

## Ensuring Sprite Renderers are drawn as a batch

Rendering with 2D Entities utilizes the same rendering pipeline as non-Dots Projects, and uses the same [Draw call batching](https://docs.unity3d.com/Manual/DrawCallBatching.html) system as non-Dots Projects as well.

## Updating the sorting order of a Sprite Renderer at runtime

All Entities with a Sprite Renderer component come with a Renderer2D component. If the sorting values in the Renderer2D component are updated, the Sprite Renderer will be sorted according to the changes. Similar to the default [2D Sorting](https://docs.unity3d.com/Manual/2DSorting.html) of [Sprite Renderers](https://docs.unity3d.com/Manual/class-SpriteRenderer.html), [Unity.U2D.Entities.SpriteRenderers](xref:Unity.U2D.Entities.SpriteRenderer) are sorted according to the following order:

1. Sort Layer. Higher value means closer to the camera.
2. Sort Order. Higher value means closer to the camera.
3. Distance between the position of the Renderer and the Camera along the Camera’s view direction. For the default 2D setting, this is along the (0, 0, 1) axis.

See below for a code example:

```c#
public class Example : SystemBase
{
   protected override void OnUpdate()
   {
       // Loop over all Entities with SpriteRenderer and Renderer2D components
       Entities
           .WithAll<SpriteRenderer>()
           .ForEach((ref Renderer2D renderer) =>
           {
               // Set SortLayer to 1
               renderer.Layer = 1;
               // Set SortOrder to 2
               renderer.Order = 2;
           }).Schedule();
   }
}
```
