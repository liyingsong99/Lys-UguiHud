# UGUI HUD

高性能的UGUI扩展包，用于图文混排和3D HUD显示，支持大量UI元素的批量渲染优化。

## 核心功能

### 1. 图文混排 (RichText)

- 在文本中嵌入Sprite图标
- 支持图标动态填充（血条、进度条）
- 基于AtlasData的sprite加载系统
- 语法：`<quad name=图标名 size=32 />`

### 2. 批量渲染 (RichTextRender)

- 将多个Text/Image合并为单个Mesh
- **大幅减少DrawCall**（数百个UI元素 → 1个DrawCall）
- 自动处理65535顶点限制（超限自动分割）
- 适用场景：大量血条、名字、Buff图标

### 3. 智能模式自动选择

系统会根据层级结构自动选择最优渲染模式：

| 模式 | 触发条件 | 说明 |
|------|----------|------|
| `ERTM_MergeText` | 检测到父级RichTextRender | **批量合并渲染（推荐）** |
| `ERTM_3DText` | 无父级RichTextRender | 独立3D文本/图像渲染 |

**✅ 完全自动化，无需手动配置！**

## 使用方法

### 快速开始

#### 1. 创建图集数据

使用AtlasBuilder工具生成AtlasData：

1. 将sprite图片放在同一文件夹（如 `Assets/Sprites/Icons`）
2. 右键文件夹 → 使用Atlas Builder生成图集
3. 自动生成：
   - `Icons_Atlas.png` - 打包后的atlas纹理
   - `Icons_AtlasData.asset` - sprite信息数据

#### 2. 配置RichText组件

```
Inspector中的RichText组件：

Atlas Settings:
├─ Atlas Data: 拖入 Icons_AtlasData.asset
└─ Atlas Texture: 自动从AtlasData同步

Render Settings:
└─ m_UiMode: 自动选择（只读）
   [绿色Info框] 显示自动选择原因
```

#### 3. 使用图文混排

```csharp
RichText richText = GetComponent<RichText>();
richText.text = "生命 <quad name=heart size=24 /> x3";

// 动态修改图标填充度（血条、进度条）
richText.SetSpriteFillAmount("heart", 0.5f);  // 50%
```

### 批量渲染优化（自动）

**只需在父节点添加RichTextRender，子节点自动批量渲染！**

```
GameObject (RichTextRender)              // 添加此组件即可
├─ Text1 (RichText)                      // 自动选择 ERTM_MergeText
├─ Text2 (RichText)                      // 自动选择 ERTM_MergeText
├─ Image1 (RichImage)                    // 自动选择 ERTM_MergeText
└─ Image2 (RichImage)                    // 自动选择 ERTM_MergeText
```

**结果：** 4个UI元素 → 1个DrawCall，完全自动！

### 实战示例

#### 场景1：大量玩家血条（零配置）

```csharp
// 父节点添加RichTextRender - 仅此一步！
GameObject hudParent = new GameObject("HUD_Batch");
hudParent.AddComponent<RichTextRender>();

// 创建100个血条 - 自动批量渲染，仅1个DrawCall
for (int i = 0; i < 100; i++)
{
    GameObject hud = new GameObject($"Player_{i}");
    hud.transform.SetParent(hudParent.transform);

    RichText nameText = hud.AddComponent<RichText>();
    nameText.m_AtlasData = atlasData;  // 设置图集数据
    nameText.text = $"Player{i} <quad name=vip size=16 />";
    // UIMode自动选择为ERTM_MergeText，无需设置！
}
```

#### 场景2：独立3D文本（自动识别）

```csharp
// 没有RichTextRender父节点 - 自动选择独立渲染
GameObject floatingText = new GameObject("FloatingDamage");
RichText damageText = floatingText.AddComponent<RichText>();
damageText.text = "-999";
// UIMode自动选择为ERTM_3DText
```

#### 场景3：Sprite语法

```csharp
RichText text = GetComponent<RichText>();

// 基础语法
text.text = "Gold <quad name=coin size=20 /> x100";

// 指定宽高
text.text = "HP <quad name=hp_bar w=100 h=10 />";

// Sliced模式（九宫格）
text.text = "背景 <quad name=panel t=s w=200 h=100 />";
```

## 高级特性

### AtlasData系统

#### 创建AtlasData（推荐使用AtlasBuilder）

手动创建（不推荐）：
1. 右键 → Create → UGUI HUD → Atlas Data
2. 设置 `atlasTexture`
3. 添加sprite信息到 `sprites` 列表

#### AtlasData优势

- ✅ 不依赖Unity SpriteAtlas（跨版本兼容）
- ✅ 轻量级ScriptableObject
- ✅ 支持运行时动态创建Sprite
- ✅ 完整UV信息，shader直接采样
- ✅ 内置AtlasBuilder工具链

### 自动模式选择机制

**工作原理：**

1. **OnEnable时检测**：每次组件启用时自动检测父级
2. **智能判断**：
   - 找到RichTextRender → `ERTM_MergeText`（批量渲染）
   - 未找到 → `ERTM_3DText`（独立渲染）
3. **Editor反馈**：Inspector实时显示选择原因

**Inspector显示：**

```
Render Settings
m_UiMode: ERTM_MergeText [只读]

[绿色Info框]
Auto-selected: ERTM_MergeText (RichTextRender detected in parent)
Batch rendering enabled for optimal performance.
```

**移动对象：**
- 拖拽到有RichTextRender的父节点下 → 自动切换到批量渲染
- 拖拽出来成为独立对象 → 自动切换到独立渲染

### 顶点限制保护

- **自动检测**：实时计算总顶点数
- **智能分割**：超过65,000顶点时自动分批渲染
- **稳定性**：避免顶点溢出导致的渲染错误
- **透明处理**：开发者无需关心内部细节

**示例输出：**

```
[RichTextRender] Total vertices (80000) exceeds limit (65000).
Splitting into multiple meshes. This will increase drawcalls.
```

## 性能对比

| 场景 | 传统方案 | 本包方案 | 优化效果 |
|------|----------|----------|----------|
| 100个血条 | 100 DrawCall | 1 DrawCall | **99%降低** |
| 500个名字 | 500 DrawCall | 1-8 DrawCall | **98%降低** |
| 顶点超限 | 渲染错误/崩溃 | 自动分割稳定运行 | **避免崩溃** |
| 配置工作量 | 每个组件手动设置 | 零配置自动选择 | **节省100%时间** |

## 最佳实践

### ✅ 推荐做法

1. **使用AtlasBuilder生成图集**
   - 自动打包sprite
   - 自动生成AtlasData
   - 自动配置纹理导入设置

2. **批量场景添加RichTextRender**
   - 在HUD根节点添加
   - 子节点自动批量渲染
   - 无需任何额外配置

3. **独立对象直接使用**
   - 不需要特殊设置
   - 系统自动选择独立渲染

4. **动态创建也能自动优化**
   ```csharp
   // 运行时创建，自动选择正确模式
   GameObject newText = new GameObject("DynamicText");
   newText.transform.SetParent(hudParent.transform);  // 有RichTextRender
   RichText text = newText.AddComponent<RichText>();  // 自动ERTM_MergeText
   ```

### ❌ 避免的做法

1. ~~手动设置UIMode~~ - 已自动化，字段为只读
2. ~~担心模式配置错误~~ - 系统自动防错
3. ~~移动对象后重新配置~~ - 自动重新评估
4. ~~使用Texture2D代替AtlasData~~ - AtlasData更强大

## 技术规格

- **Unity版本**：2020.3+ (推荐Unity 6000+)
- **渲染管线**：Built-in, URP
- **顶点限制**：单Mesh最大65,000顶点（自动分割）
- **Shader**：`UI/RichText`（支持双图层混合）
- **自动化程度**：100%（UIMode零配置）

### 空格和Sprite处理机制

系统内部采用了智能的文本处理机制来确保quad标签正确渲染：

1. **空格转换**：所有普通空格自动转换为不间断空格（U+00A0），防止Unity TextGenerator压缩或裁剪空格
2. **占位符替换**：quad标签被替换为可见占位符 `■`（U+25A0 BLACK SQUARE），确保TextGenerator为其生成顶点
3. **动态定位**：运行时在TextGenerator输出中查找占位符位置，精确替换为sprite顶点
4. **边界检查**：自动检测sprite是否有顶点生成，若RectTransform过小导致裁剪会发出警告

**优势：**
- ✅ 支持quad标签前后任意数量空格
- ✅ 自动处理文本末尾的quad标签
- ✅ 无需担心TextGenerator的文本处理行为
- ✅ 精确的顶点索引计算

## Quad标签语法

### 基础参数

| 参数 | 说明 | 示例 |
|------|------|------|
| `name` | sprite名称（必需） | `name=icon4` |
| `size` | 正方形尺寸 | `size=20` |
| `w` / `h` | 指定宽高 | `w=100 h=50` |
| `t` | 类型（s=Sliced） | `t=s` |

### 示例

```csharp
// 基础用法
"<quad name=icon size=20 />"

// 指定宽高
"<quad name=icon w=32 h=16 />"

// Sliced模式（九宫格拉伸）
"<quad name=panel t=s w=200 h=100 />"

// 组合使用
"HP: <quad name=heart size=16 /> 100/100"

// 多个空格也能正常处理（自动转换为不间断空格）
"2222     <quad name=friend_box1 size=20 />"

// 支持完整参数名
"<quad name=friend_box1 size=20 />"  // name, size
"<quad n=friend_box1 s=20 />"        // 简写: n, s, w, h, t
```

## 故障排除

### 问题：Sprite不显示

**解决方案：**
1. 检查 `m_AtlasData` 是否已设置
2. 确认sprite名称与AtlasData中的名称匹配
3. 查看Console是否有警告信息

### 问题：DrawCall没有减少

**解决方案：**
1. 确认父节点有 `RichTextRender` 组件
2. 检查Inspector显示是否为 `ERTM_MergeText`
3. 确保所有子对象使用相同材质

### 问题：顶点数超限警告

**解决方案：**
1. 系统会自动分割，无需担心
2. 如果DrawCall过多，考虑拆分为多个RichTextRender组
3. 减少单个场景的UI元素数量

### 问题：Sprite显示时出现警告 "placeholder has no vertices"

**原因：**
RectTransform尺寸过小，Unity TextGenerator裁剪了文本末尾的内容，导致sprite占位符没有生成顶点。

**解决方案：**
1. 增加RichText组件的RectTransform宽度/高度
2. 启用"Best Fit"或增加字体大小
3. 检查是否有文本溢出边界

## 更新日志

### v2.1 (当前版本)

- ✅ 修复quad标签与空格一起使用时的显示问题
- ✅ 实现智能占位符机制，支持任意数量空格
- ✅ 新增完整参数名支持（name, size等）
- ✅ 优化空格处理：自动转换为不间断空格防止压缩
- ✅ 改进顶点索引计算，更加精确和可靠
- ✅ 新增RectTransform尺寸不足的警告提示

### v2.0

- ✅ 新增AtlasData系统替代SpriteAtlas
- ✅ 实现UIMode完全自动选择
- ✅ Editor实时显示自动选择状态
- ✅ RichText和RichImage统一自动化
- ✅ 移除ERTM_UI模式（存在缺陷）
- ✅ 优化Inspector布局和提示信息

### v1.0 (原版本)

- 基础图文混排功能
- 批量渲染支持
- 顶点限制保护

## 示例场景

参考场景：`Samples~/RichText.unity`

## 鸣谢

核心功能基于 [RichText by HuaHua](https://github.com/506638093/RichText.git)
