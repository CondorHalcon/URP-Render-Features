# URP Render Features

> A collection of Unity URP render features built by [@CondorHalcon](https://github.com/CondorHalcon).

## Features

### Screen Space Outlines

> Draws screen space outlines along edges. The edge detection uses view normals and screen depth.

```csharp
namespace CondorHalcon.URPRenderFeatures
{
    public class ScreenSpaceOutlines : ScriptableRenderFeature
    {
        // normal texture settings
        private class ViewSpaceNormalsTextureSettings {}
        // outline settings
        private class ScreenSpaceOutlinesSettings {}
        // normal texture pass
        private class ViewSpaceNormalsTexturePass : ScriptableRenderPass {}
        // outline pass
        private class ScreenSpaceOutlinesPass : ScriptableRenderPass {}
    }
}
```
