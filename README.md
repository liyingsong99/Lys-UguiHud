# UGUI HUD

高性能的UGUI扩展包，用于图文混排和3D HUD显示，支持大量UI元素的批量渲染优化。

## 核心功能

### 1. 图文混排 (RichText)

- 在文本中嵌入Sprite图标
- 支持图标动态填充（血条、进度条）
- 语法：`<quad name=图标名 size=32 />`

### 2. 批量渲染 (RichTextRender)

- 将多个Text/Image合并为单个Mesh
- **大幅减少DrawCall**（数百个UI元素 → 1个DrawCall）
- 自动处理65535顶点限制（超限自动分割）
- 适用场景：大量血条、名字、Buff图标

### 3. 三种渲染模式

| 模式 | 说明 | 适用场景 |
|------|------|----------|
| `ERTM_UI` | 标准UGUI模式 | 普通UI |
| `ERTM_3DText` | 3D文本独立渲染 | 独立3D HUD |
| `ERTM_MergeText` | 批量合并渲染 | **性能优化（推荐）** |

## 使用方法

### 基础使用

#### 1. 图文混排

```csharp
// 替换Text组件为RichText
RichText richText = GetComponent<RichText>();
richText.m_AtlasTexture = spriteAtlas;  // 设置图集
richText.text = "生命 <quad name=heart size=24 /> x3";

// 动态修改图标填充度（血条、进度条）
richText.SetSpriteFillAmount("heart", 0.5f);  // 50%
```

#### 2. 批量渲染优化（关键）

```
GameObject (RichTextRender)              // 父节点：批量管理器
├─ Text1 (RichText, mode=MergeText)     // 子节点：自动合并
├─ Text2 (RichText, mode=MergeText)
├─ Image1 (RichImage, mode=MergeText)
└─ Image2 (RichImage, mode=MergeText)
```

**结果：** 4个UI元素 → 1个DrawCall

### 实战示例

#### 场景1：大量玩家血条

```csharp
// 父节点添加RichTextRender
GameObject hudParent = new GameObject("HUD_Batch");
hudParent.AddComponent<RichTextRender>();

// 创建100个血条（仅1个DrawCall）
for (int i = 0; i < 100; i++)
{
    GameObject hud = new GameObject($"Player_{i}");
    hud.transform.SetParent(hudParent.transform);

    RichText nameText = hud.AddComponent<RichText>();
    nameText.m_UiMode = ERichTextMode.ERTM_MergeText;  // 关键设置
    nameText.text = $"Player{i} <quad name=vip size=16 />";
}
```

#### 场景2：Buff图标列表

```csharp
RichText buffText = GetComponent<RichText>();
buffText.text = "Buffs: <quad name=buff_atk size=20 /> <quad name=buff_def size=20 />";
```

## 性能特性

### 顶点限制保护（新增）

- **自动检测**：实时计算总顶点数
- **智能分割**：超过65,000顶点时自动分批渲染
- **稳定性**：避免顶点溢出导致的渲染错误
- **透明处理**：开发者无需关心内部细节

**示例输出：**

```
[RichTextRender] Total vertices (80000) exceeds limit (65000).
Splitting into multiple meshes. This will increase drawcalls.
```

### 性能对比

| 场景 | 传统方案 | 本包方案 | 优化效果 |
|------|----------|----------|----------|
| 100个血条 | 大量 DrawCall | 1-2 DrawCall | **90%降低** |
| 500个名字 | 大量 DrawCall | 1-8 DrawCall | **90%降低** |
| 顶点超限 | 渲染错误/崩溃 | 自动分割稳定运行 | **避免崩溃** |

## 注意事项

1. **必须设置图集**：`m_AtlasTexture` 需要指定Sprite Atlas
2. **父子结构**：批量渲染需要父节点添加 `RichTextRender`
3. **模式设置**：子节点必须设置 `m_UiMode = ERTM_MergeText`
4. **材质统一**：同一批次内使用相同材质（Shader: `UI/RichText`）
5. **顶点监控**：超限时关注Console警告，考虑UI拆分

## 技术规格

- **Unity版本**：2020.3+
- **渲染管线**：支持URP
- **顶点限制**：单Mesh最大65,000顶点（自动分割）
- **Shader**：`UI/RichText`（支持双图层）

## 示例场景

参考场景：`Samples~/RichText.unity`

## 鸣谢

核心功能基于 [RichText by HuaHua](https://github.com/506638093/RichText.git)
