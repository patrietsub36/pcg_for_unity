# PCG for Unity 第3轮迭代成果报告

**迭代日期**: 2026-03-21

---

## 已完成任务汇总

### Batch 1 ✅ 已完成

| 任务 | 文件 | 实现内容 |
|------|------|----------|
| **T1** | `ExpressionParser.cs` | 比较运算符 `<`, `>`, `<=`, `>=`, `==`, `!=`；逻辑运算符 `&&`, `||`, `!`；三元表达式 `?:`；短路求值 |
| **T7** | `ArrayNode.cs` | 为每个副本注入 `@copynum` 属性，可在子图中区分第几个副本 |
| **T9** | `ForEachNode.cs` | 新增 `feedback` 参数，为 `true` 时只输出最终迭代结果，实现累积变换 |

### Batch 2 ✅ 已完成

| 任务 | 文件 | 实现内容 |
|------|------|----------|
| **T2** | `ExpressionParser.cs` | 支持 `if (expr) { stmts } else { stmts }` 语句；token-level 语句解析；支持代码块 |
| **T4** | `GroupExpressionNode.cs` | **新节点**，用表达式创建分组（如 `@P.y > 5` 创建 "upper" 组） |
| **T5** | `SwitchNode.cs` | 新增 `expression` 参数，支持从 GlobalVariables 读取 `iteration` 等变量动态选择输入 |
| **T6** | `CopyToPointsNode.cs` | 注入 `@copynum` 属性；新增 `transferAttributes` 参数传递目标点属性到副本 |

### Batch 3 ✅ 已完成

| 任务 | 文件 | 实现内容 |
|------|------|----------|
| **T11** | `PCGGeometryToMesh.cs` | 新增 `ConvertWithSubmeshes()` 方法，按 `@material` 属性或 PrimGroups 生成多 Submesh |
| **T12** | `SavePrefabNode.cs` | 支持多材质输出，从 `@material` 属性读取材质路径，自动加载并分配到 `sharedMaterials` |
| **T13** | `MaterialAssignNode.cs` | **新节点**，为指定面分组设置 `@material` 属性 |
| **T16** | `ExtrudeNode.cs` | 新增 `individual` 参数，独立挤出每个面避免共享顶点拉扯 |

### Batch 4 ✅ 已完成

| 任务 | 文件 | 实现内容 |
|------|------|----------|
| **T3** | `ExpressionParser.cs` | 支持局部变量声明 `float h = expr;` `int i = expr;`，无 `@` 前缀 |
| **T8** | `InstanceNode.cs` | 增强为真正的多几何体实例化：8 个输入端口，按 `@instance` 属性选择几何体，支持 pack/expand 模式 |
| **T10** | `ForEachNode.cs` | 注入更多上下文变量：`numiterations`（总迭代次数）、`value`（当前 piece 属性值） |
| **T14** | `ConnectivityNode.cs` | **新节点**，使用 Union-Find 为连通分量写入 `@class` 属性 |
| **T15** | `BooleanNode.cs` | 改用 `GeometryBridge.ToDMesh3()` / `FromDMesh3()` 转换，保留法线和 UV 属性 |

---

## 新增节点列表

| 节点名称 | 文件路径 | 功能描述 |
|----------|----------|----------|
| GroupExpression | `Nodes/Create/GroupExpressionNode.cs` | 用表达式创建点/面分组 |
| MaterialAssign | `Nodes/Geometry/MaterialAssignNode.cs` | 为面分组分配材质属性 |
| Connectivity | `Nodes/Geometry/ConnectivityNode.cs` | 为连通分量分配 class 属性 |

---

## 增强节点列表

| 节点名称 | 新增参数 | 功能增强 |
|----------|----------|----------|
| Array | - | 注入 `@copynum` 属性 |
| CopyToPoints | `transferAttributes` | 传递目标点属性到副本 |
| ForEach | `feedback`, `valueAttrib` | 累积变换模式，注入更多上下文变量 |
| Switch | `expression` | 动态表达式选择输入 |
| Extrude | `individual` | 独立挤出模式 |
| Instance | `instance0-7`, `pack` | 多几何体实例化 |
| SavePrefab | - | 多材质支持 |
| Boolean | - | 保留法线和 UV 属性 |

---

## ExpressionParser 新增语法支持

```
// 比较运算符
@P.y > 5 && @P.y < 10
@ptnum % 2 == 0

// 三元表达式
@floor > 5 ? 1 : 0

// if/else 语句
if (@P.y > 10) {
    @type = 1;
} else {
    @type = 0;
}

// 局部变量
float h = @P.y;
int floor = floor(h / 3.0);
@type = floor > 5 ? 1 : 0;
```

---

## 剩余任务（Batch 5 - 中期目标）

### T17: Shape Grammar 节点（P4）

**新文件**: `Assets/PCGToolkit/Editor/Nodes/Procedural/ShapeGrammarNode.cs`

这是建筑生成的终极武器。设计思路：

```
输入: 一个初始面（如建筑立面）+ 规则集（JSON/DSL）
规则示例:
  Facade → split(y, 3) { Floor* | Roof }
  Floor  → split(x, 2) { Wall | Window | Wall }
  Window → inset(0.1) { Frame | Glass }
```

实现分阶段：
1. **Phase 1**: 支持 `split`（沿轴等分/按比例分割面）和 `repeat`（重复分割）
2. **Phase 2**: 支持 `inset`、`extrude` 作为终端操作
3. **Phase 3**: 支持条件规则（`if @floor > 3 then ...`）

### T18: 建筑 SubGraph 模板库（P4）

**新目录**: `Assets/PCGToolkit/Templates/Building/`

在节点能力补全后，创建预制 SubGraph：
- `WindowFrame.asset` — Inset + Extrude + Bevel
- `BuildingFloor.asset` — Grid 分割 + ForEach + 随机窗户变体
- `SimpleBuilding.asset` — Box → Array(楼层) → ForEach(立面处理)

---

## 能力评估

完成 Batch 1-4 后的系统能力：

```
程序化建筑生成器（简单方盒子建筑）：  ██████████ 100%  — 完全可做
程序化建筑生成器（中等复杂度）：      ████████░░ 80%   — 条件逻辑完备
程序化建筑生成器（高复杂度/装饰物）：  ██████░░░░ 60%   — 缺 Shape Grammar
多材质 Prefab 输出：                  ██████████ 100%  — 完全支持
```

---

## 原始任务文档

以下是第3轮迭代的原始任务规划文档，供参考：

---

# PCG for Unity 第3轮迭代指导方针（进阶版）

基于对代码库的深度审查，以下是按优先级严格排序的迭代任务大纲。每个任务标注了**影响面**、**依赖关系**和**具体实现要点**。

---

## 架构级发现（影响任务排序）

在深入代码后，我修正了上一轮的几个判断：

1. `BooleanNode` **已经使用 geometry3Sharp 的 `MeshBoolean` 做真 3D CSG**，不是 2D Clipper。但它不保留属性/UV。

2. `FacetNode` 的 `unique` 模式已经实现了"面独立化"（每个面使用独立顶点），部分解决了 Face Separate 需求。

3. `PCGGeometryToMesh.Convert()` 只输出**单个 submesh**，无法按 PrimGroup 分配不同材质。

4. `CopyToPointsNode` 读取 `orient`/`pscale`，但**不传递目标点的自定义属性**到副本，也不注入 `@copynum`。

5. `ArrayNode` 同样**不注入 `@copynum`**，无法在子图中区分第几个副本。

---

## 建议迭代批次

| 批次 | 任务 | 完成后解锁的能力 | 状态 |
|------|------|-----------------|------|
| **Batch 1** | T1, T7, T9 | Wrangle 条件逻辑 + 副本编号 + ForEach 累积 | ✅ 完成 |
| **Batch 2** | T2, T4, T5, T6 | if/else + 表达式分组 + 动态 Switch + per-copy 变体 | ✅ 完成 |
| **Batch 3** | T11, T12, T13, T16 | 多材质输出 + Extrude Individual | ✅ 完成 |
| **Batch 4** | T3, T8, T10, T14, T15 | 局部变量 + 多几何体实例 + 拓扑工具 | ✅ 完成 |
| **Batch 5** | T17, T18 | Shape Grammar + 建筑模板 | 🔲 待实现 |

完成 Batch 1-4 后，系统就能做出**中等复杂度的程序化建筑**（多层、窗户变体、条件逻辑），并能输出**生产级质量的多材质建筑 Prefab**。Batch 5 是对标 CityEngine/Houdini Labs Building Generator 的终极目标。