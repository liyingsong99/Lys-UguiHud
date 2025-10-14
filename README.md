# UGUI HUD

A high-performance UGUI extension for text-image mixing and 3D HUD display.

## Features

- **High Performance**: Supports massive health bars, avatars, names, and buff icons with only 1 SetPass call
- **Text-Image Mixing**: Rich text component with inline sprite support
- **3D HUD Support**: Render UI elements in 3D world space
- **Multiple Render Modes**:
  - `ERTM_UI`: Standard 2D UI mode
  - `ERTM_3DText`: 3D text rendering mode
  - `ERTM_MergeText`: Optimized batch rendering mode
- **Flexible Image Types**: Supports Simple and Sliced sprite rendering
- **Fill Amount Control**: Dynamic fill amount for health bars and progress indicators

## Installation

### Install via Package Manager

1. Open Unity Package Manager
2. Click "+" button and select "Add package from git URL"
3. Enter the repository URL

### Install via Packages Manifest

Add to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "uguihud": "https://github.com/liyingsong99/Lys-UguiHud.git"
  }
}
```

## Quick Start

### Using RichText Component

1. Create a UI Text object in your scene
2. Replace the `Text` component with `RichText`
3. Configure the settings:
   - Set `m_UiMode` to desired render mode
   - Assign `m_AtlasTexture` for sprite atlas
   - Use `<quad>` tags in text for inline sprites

#### Sprite Syntax

```
<quad name=sprite_name size=32 width=1 />
```

- `name`: Sprite name in the atlas
- `size`: Display size in pixels
- `width`: Width multiplier (optional)

### Using RichImage Component

1. Create a UI Image object
2. Replace the `Image` component with `RichImage`
3. Set `m_UiMode` to `ERTM_MergeText` for batch rendering

### Setting Fill Amount

```csharp
richText.SetSpriteFillAmount("health_bar", 0.75f);
```

## Performance Optimization

### Batch Rendering with RichTextRender

For optimal performance with multiple HUD elements:

1. Create a parent object with `RichTextRender` component
2. Set child `RichText`/`RichImage` components to `ERTM_MergeText` mode
3. All child elements will be batched into a single draw call

### Best Practices

- Use sprite atlases to minimize texture switching
- Enable `ERTM_MergeText` mode for batch rendering
- Limit the number of unique materials
- Use object pooling for dynamic HUD elements

## API Reference

### RichText

**Properties:**

- `m_AtlasTexture`: Texture2D atlas for sprites
- `m_UiMode`: Render mode (UI/3DText/MergeText)
- `text`: Text content with sprite tags

**Methods:**

- `SetSpriteFillAmount(string name, float amount)`: Set fill amount for named sprite
- `Mesh Mesh()`: Get the generated mesh (for 3D modes)

### RichImage

**Properties:**

- `m_UiMode`: Render mode (UI/MergeText)

**Methods:**

- `Mesh Mesh()`: Get the generated mesh

## Examples

See the included demo scene in `Scenes/Rich Text` for practical examples.

## Requirements

- Unity 2020.3 or higher
- URP
- UGUI (com.unity.ugui)

## License

See [LICENSE](LICENSE) file for details.

## Author

Lys

注意： 本仓库核心功能搬运自： <https://github.com/506638093/RichText.git>
原作者：HuaHua
