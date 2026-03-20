# 
## 仓库: No78Vino/pcg_for_unity (branch: main)

---

# Phase 1: P0 级 Bug 修复（阻断性问题）

---

## BUG-1: PCGEdgeData 序列化字段名不一致 — 图保存/加载时连线端口名丢失

### 问题描述
`PCGEdgeData` 中序列化字段是 `OutputPort` / `InputPort`，但 `[NonSerialized]` 的运行时字段 `OutputPortName` / `InputPortName` 被大量代码引用。保存后再加载时，`OutputPortName`/`InputPortName` 为 null，导致所有连线的端口名信息丢失。

### 修改方案
**策略**: 移除 `[NonSerialized]` 的 `OutputPortName` / `InputPortName` 字段，全局替换为 `OutputPort` / `InputPort`。

### 文件 1: `Assets/PCGToolkit/Editor/Graph/PCGGraphData.cs`

第 68-78 行，将 `PCGEdgeData` 改为：
```csharp
[Serializable]
public class PCGEdgeData
{
    public string OutputNodeId;
    public string OutputPort;
    public string InputNodeId;
    public string InputPort;
}
```
删除第 75-77 行的 `[NonSerialized] OutputPortName` 和 `InputPortName`。

第 151-164 行，`AddEdge` 方法改为：
```csharp
public PCGEdgeData AddEdge(string outputNodeId, string outputPortName,
    string inputNodeId, string inputPortName)
{
    var edge = new PCGEdgeData
    {
        OutputNodeId = outputNodeId,
        OutputPort = outputPortName,
        InputNodeId = inputNodeId,
        InputPort = inputPortName,
    };
    Edges.Add(edge);
    return edge;
}
```

### 文件 2: `Assets/PCGToolkit/Editor/Graph/PCGGraphExecutor.cs`

第 217 行: `edge.OutputPortName` → `edge.OutputPort`
第 219 行: `edge.InputPortName` → `edge.InputPort`
第 236 行: `edge.OutputPortName` → `edge.OutputPort`
第 239 行: `edge.InputPortName` → `edge.InputPort`

### 文件 3: `Assets/PCGToolkit/Editor/Graph/PCGAsyncGraphExecutor.cs`

第 302 行: `edge.OutputPortName` → `edge.OutputPort`
第 304 行: `edge.InputPortName` → `edge.InputPort`
第 322 行: `edge.OutputPortName` → `edge.OutputPort`
第 326 行: `edge.InputPortName` → `edge.InputPort`

### 文件 4: `Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs`

第 398 行: `OutputPortName = outputVisual.FindPortSchemaName(edge.output)` → `OutputPort = outputVisual.FindPortSchemaName(edge.output)`
第 400 行: `InputPortName = inputVisual.FindPortSchemaName(edge.input)` → `InputPort = inputVisual.FindPortSchemaName(edge.input)`
第 460 行: `edgeData.OutputPortName` → `edgeData.OutputPort`
第 461 行: `edgeData.InputPortName` → `edgeData.InputPort`
第 658 行: `edgeData.OutputPortName` → `edgeData.OutputPort`
第 659 行: `edgeData.InputPortName` → `edgeData.InputPort`
第 666 行: `edgeData.InputPortName` → `edgeData.InputPort`
第 736 行: `OutputPortName = outputVisual.FindPortSchemaName(edge.output)` → `OutputPort = outputVisual.FindPortSchemaName(edge.output)`
第 738 行: `InputPortName = inputVisual.FindPortSchemaName(edge.input)` → `InputPort = inputVisual.FindPortSchemaName(edge.input)`

**验证**: 全局搜索 `OutputPortName` 和 `InputPortName`，确保所有 .cs 文件中的引用都已替换为 `OutputPort` 和 `InputPort`。

---

## BUG-2: ExtrudeNode 非挤出面的顶点索引错乱

### 问题描述
`Assets/PCGToolkit/Editor/Nodes/Geometry/ExtrudeNode.cs` 第 141-157 行：
- 非挤出面使用原始索引 `geo.Primitives[i].Clone()` 添加到 `result.Primitives`
- 但 `result.Points` 中的顶点是挤出面新创建的，原始索引在新几何体中无效
- 未涉及的原始顶点被追加到末尾，但面索引没有重映射

### 修改方案
重写 Execute 方法的顶点管理逻辑。先将所有原始顶点复制到 result，保持 1:1 索引映射，然后挤出面的新顶点追加到末尾。

将 `Assets/PCGToolkit/Editor/Nodes/Geometry/ExtrudeNode.cs` 的 Execute 方法（第 41-159 行）替换为：

```csharp
public override Dictionary<string, PCGGeometry> Execute(
    PCGContext ctx,
    Dictionary<string, PCGGeometry> inputGeometries,
    Dictionary<string, object> parameters)
{
    var geo = GetInputGeometry(inputGeometries, "input").Clone();
    string group = GetParamString(parameters, "group", "");
    float distance = GetParamFloat(parameters, "distance", 0.5f);
    float inset = GetParamFloat(parameters, "inset", 0f);
    int divisions = Mathf.Max(1, GetParamInt(parameters, "divisions", 1));
    bool outputFront = GetParamBool(parameters, "outputFront", true);
    bool outputSide = GetParamBool(parameters, "outputSide", true);

    if (geo.Primitives.Count == 0)
    {
        ctx.LogWarning("Extrude: 输入几何体没有面");
        return SingleOutput("geometry", geo);
    }

    var result = new PCGGeometry();

    // 第一步：复制所有原始顶点到 result（保持 1:1 索引映射）
    for (int i = 0; i < geo.Points.Count; i++)
    {
        result.Points.Add(geo.Points[i]);
    }

    // 确定要挤出的面
    HashSet<int> primsToExtrude = new HashSet<int>();
    if (!string.IsNullOrEmpty(group) && geo.PrimGroups.TryGetValue(group, out var groupPrims))
    {
        primsToExtrude = groupPrims;
    }
    else
    {
        for (int i = 0; i < geo.Primitives.Count; i++)
            primsToExtrude.Add(i);
    }

    // 第二步：添加未挤出的原始面（索引仍然有效，因为原始顶点已在 result 中）
    for (int i = 0; i < geo.Primitives.Count; i++)
    {
        if (!primsToExtrude.Contains(i))
        {
            result.Primitives.Add((int[])geo.Primitives[i].Clone());
        }
    }

    // 第三步：处理挤出面
    foreach (int primIdx in primsToExtrude)
    {
        var prim = geo.Primitives[primIdx];
        if (prim.Length < 3) continue;

        Vector3 normal = CalculateFaceNormal(geo.Points, prim);

        Vector3 center = Vector3.zero;
        foreach (int idx in prim) center += geo.Points[idx];
        center /= prim.Length;

        // 第一层使用原始顶点索引
        int[] prevLayerVertices = (int[])prim.Clone();

        for (int d = 1; d <= divisions; d++)
        {
            float t = (float)d / divisions;
            float offset = distance * t;
            float insetAmount = inset * t;

            int[] layerVertices = new int[prim.Length];
            for (int i = 0; i < prim.Length; i++)
            {
                Vector3 origPos = geo.Points[prim[i]];
                Vector3 toCenter = center - origPos;
                Vector3 newPos = origPos + normal * offset + toCenter.normalized * insetAmount;

                int newIdx = result.Points.Count;
                result.Points.Add(newPos);
                layerVertices[i] = newIdx;
            }

            // 创建侧面
            if (outputSide)
            {
                for (int i = 0; i < prim.Length; i++)
                {
                    int next = (i + 1) % prim.Length;
                    result.Primitives.Add(new int[]
                    {
                        prevLayerVertices[i], prevLayerVertices[next],
                        layerVertices[next], layerVertices[i]
                    });
                }
            }

            prevLayerVertices = layerVertices;
        }

        // 输出顶面
        if (outputFront)
        {
            int[] frontPrim = new int[prim.Length];
            for (int i = 0; i < prim.Length; i++)
            {
                frontPrim[i] = prevLayerVertices[prim.Length - 1 - i];
            }
            result.Primitives.Add(frontPrim);
        }
    }

    return SingleOutput("geometry", result);
}
```

---

## BUG-3: SubGraphNode 引用不存在的类型和方法

### 问题描述
`Assets/PCGToolkit/Editor/Nodes/Utility/SubGraphNode.cs`:
- 第 47 行: `PCGGraphAsset` 类型不存在（代码库中只有 `PCGGraphData`）
- 第 59 行: `ctx.SetExternalInput()` 方法不存在
- 第 72 行: `ctx.TryGetExternalOutput()` 方法不存在

### 修改方案

**文件 1**: `Assets/PCGToolkit/Editor/Core/PCGContext.cs`

在类末尾（第 103 行 `}` 之前）添加以下方法：

```csharp
/// <summary>
/// 设置外部输入（用于 SubGraph 注入数据）
/// </summary>
public void SetExternalInput(string key, PCGGeometry geometry)
{
    NodeOutputCache[$"__external_input__.{key}"] = geometry;
}

/// <summary>
/// 尝试获取外部输出（用于 SubGraph 读取子图结果）
/// </summary>
public bool TryGetExternalOutput(string key, out PCGGeometry geometry)
{
    return NodeOutputCache.TryGetValue($"__external_output__.{key}", out geometry);
}

/// <summary>
/// 设置外部输出（由子图的 Output 节点调用）
/// </summary>
public void SetExternalOutput(string key, PCGGeometry geometry)
{
    NodeOutputCache[$"__external_output__.{key}"] = geometry;
}
```

**文件 2**: `Assets/PCGToolkit/Editor/Nodes/Utility/SubGraphNode.cs`

第 47 行改为：
```csharp
var subGraphAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<PCGGraphData>(subGraphPath);
if (subGraphAsset == null)
{
    ctx.LogWarning($"SubGraph: Failed to load subgraph at {subGraphPath}");
    return SingleOutput("geometry", new PCGGeometry());
}
```

第 54-55 行改为：
```csharp
// 创建子图执行器
var subExecutor = new PCGGraphExecutor(subGraphAsset);
```

---

# Phase 2: P1 级 Bug 修复

---

## BUG-4: NodeExecutionResult.OutputGeometry 属性不存在

### 问题描述
`Assets/PCGToolkit/Editor/Graph/PCGGraphEditorWindow.cs` 第 186 行引用了 `result.OutputGeometry`，但 `NodeExecutionResult` 只有 `Outputs` (Dictionary)。

### 修改方案

**文件**: `Assets/PCGToolkit/Editor/Graph/PCGAsyncGraphExecutor.cs`

在 `NodeExecutionResult` 类中（第 42 行 `}` 之前）添加便捷属性：

```csharp
/// <summary>
/// 便捷属性：获取第一个 Geometry 输出
/// </summary>
public PCGGeometry OutputGeometry
{
    get
    {
        if (Outputs == null || Outputs.Count == 0) return null;
        foreach (var kvp in Outputs)
        {
            if (kvp.Value != null) return kvp.Value;
        }
        return null;
    }
}
```

---

## BUG-5: BendNode 角度为 0 时除零崩溃

### 问题描述
`Assets/PCGToolkit/Editor/Nodes/Deform/BendNode.cs` 第 58-59 行：当 `angle = 0` 时，`angleRad = 0`，`radius = captureLength / 0` 产生 Infinity。

### 修改方案

在第 57 行之后（`float angleRad = angle * Mathf.Deg2Rad;` 之后）插入：

```csharp
// 角度接近 0 时不进行弯曲变形
if (Mathf.Abs(angleRad) < 0.0001f)
{
    ctx.Log("Bend: angle is near zero, no deformation applied");
    return SingleOutput("geometry", geo);
}
```

---

## BUG-6: CopyToPointsNode orient 属性类型转换异常

### 问题描述
`Assets/PCGToolkit/Editor/Nodes/Distribute/CopyToPointsNode.cs` 第 85 行：`(Vector3)orientAttr.Values[pointIdx]` 硬转换，如果属性值不是 Vector3 会抛出 InvalidCastException。

### 修改方案

将第 83-87 行替换为：

```csharp
if (orientAttr != null && pointIdx < orientAttr.Values.Count)
{
    var orientVal = orientAttr.Values[pointIdx];
    if (orientVal is Vector3 euler)
    {
        rotation = Quaternion.Euler(euler);
    }
    else if (orientVal is Vector4 quat)
    {
        rotation = new Quaternion(quat.x, quat.y, quat.z, quat.w);
    }
    else if (orientVal is Quaternion q)
    {
        rotation = q;
    }
}
```

---

## BUG-7: PCGGraphView 双重注册 graphViewChanged

### 问题描述
`Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs`:
- 第 49 行：构造函数中 `graphViewChanged += OnGraphViewChanged;`
- 第 156 行：`Initialize()` 中又注册了一个匿名 lambda `graphViewChanged += change => { ... }`

两个 handler 都会触发 `OnGraphChanged`，导致脏状态事件重复触发。

### 修改方案

将第 156-179 行的匿名 lambda 中的端口过滤逻辑合并到 `OnGraphViewChanged` 方法中。

**删除** 第 156-179 行的 `graphViewChanged += change => { ... };`

**修改** 第 837 行的 `OnGraphViewChanged` 方法，在方法开头添加端口过滤逻辑：

```csharp
private GraphViewChange OnGraphViewChanged(GraphViewChange change)
{
    // 迭代一：通知脏状态变更
    OnGraphChanged?.Invoke();

    // 迭代四修复：在拖拽连线时检测端口类型（用于搜索窗口过滤）
    if (change.edgesToCreate != null && change.edgesToCreate.Count > 0)
    {
        var edge = change.edgesToCreate[0];
        if (edge.output != null && edge.output.portType != typeof(object))
        {
            var portType = PCGPortType.Any;
            if (edge.output.portType == typeof(PCGToolkit.Core.PCGGeometry)) portType = PCGPortType.Geometry;
            else if (edge.output.portType == typeof(float)) portType = PCGPortType.Float;
            else if (edge.output.portType == typeof(int)) portType = PCGPortType.Int;
            else if (edge.output.portType == typeof(bool)) portType = PCGPortType.Bool;
            else if (edge.output.portType == typeof(string)) portType = PCGPortType.String;
            else if (edge.output.portType == typeof(Vector3)) portType = PCGPortType.Vector3;
            else if (edge.output.portType == typeof(Color)) portType = PCGPortType.Color;

            _filterPortType = portType;
            _filterDirection = Direction.Input;
        }
    }

    // 处理新建连线 → 隐藏内联编辑器
    if (change.edgesToCreate != null)
    {
        foreach (var edge in change.edgesToCreate)
        {
            // ... 保持原有逻辑不变 ...
        }
    }

    // 处理删除元素 → 如果删除的是边，恢复内联编辑器
    if (change.elementsToRemove != null)
    {
        // ... 保持原有逻辑不变 ...
    }

    return change;
}
```

---

# Phase 3: P2 级 Bug 修复

---

## BUG-8: ScatterNode sourcePrim 属性创建后未填充值

### 问题描述
`Assets/PCGToolkit/Editor/Nodes/Distribute/ScatterNode.cs` 第 129 行创建了 `sourcePrim` 属性但从未添加值。

### 修改方案

需要在生成点的循环中记录每个点来自哪个面。将第 94-131 行替换为：

```csharp
// 按面积加权随机选择面并生成点
List<Vector3> points = new List<Vector3>();
List<int> sourcePrimIndices = new List<int>(); // 记录每个点的来源面

for (int i = 0; i < count; i++)
{
    // 按面积随机选择面
    float r = (float)rng.NextDouble() * totalArea;
    float accum = 0f;
    int selectedPrim = -1;

    for (int j = 0; j < primAreas.Count; j++)
    {
        accum += primAreas[j];
        if (accum >= r)
        {
            selectedPrim = primIndices[j];
            break;
        }
    }

    if (selectedPrim >= 0)
    {
        Vector3 point = SamplePointOnPrim(inputGeo, selectedPrim, rng);
        points.Add(point);
        sourcePrimIndices.Add(selectedPrim);
    }
}

// 松弛迭代（可选）
if (relaxIterations > 0 && points.Count > 1)
{
    points = RelaxPoints(points, relaxIterations);
}

// 输出点几何体
geo.Points = points;
// 创建索引属性（原始面索引）并填充值
var indexAttr = geo.PointAttribs.CreateAttribute("sourcePrim", AttribType.Int);
foreach (var idx in sourcePrimIndices)
{
    indexAttr.Values.Add(idx);
}

return SingleOutput("geometry", geo);
```

---

## BUG-9: PCGGraphExecutor 存在重复的拓扑排序实现

### 问题描述
`Assets/PCGToolkit/Editor/Graph/PCGGraphExecutor.cs` 第 139-193 行有一个私有的 `TopologicalSort()` 方法，与 `Assets/PCGToolkit/Editor/Core/PCGGraphHelper.cs` 中的 `PCGGraphHelper.TopologicalSort()` 完全重复。`Execute()` 使用 `PCGGraphHelper`，但 `ExecuteIncremental()` 第 106 行使用私有版本。

### 修改方案

**文件**: `Assets/PCGToolkit/Editor/Graph/PCGGraphExecutor.cs`

1. 删除第 139-193 行的私有 `TopologicalSort()` 方法
2. 将第 106 行 `var sortedNodes = TopologicalSort();` 改为：
```csharp
var sortedNodes = PCGGraphHelper.TopologicalSort(graphData);
```

需要确保文件顶部有 `using PCGToolkit.Core;`（已有）。

---

# Phase 4: 潜在隐患修复

---

## RISK-1: SubdivideNode 不共享边中点导致几何体裂缝

### 问题描述
`Assets/PCGToolkit/Editor/Nodes/Geometry/SubdivideNode.cs` 第 52-126 行：每个面独立创建边中点，相邻面的共享边会产生重复顶点。

### 修改方案

重写 `SubdivideLinear` 方法，使用全局边中点缓存：

```csharp
private PCGGeometry SubdivideLinear(PCGGeometry geo)
{
    var result = new PCGGeometry();

    // 第一步：复制所有原始顶点
    for (int i = 0; i < geo.Points.Count; i++)
    {
        result.Points.Add(geo.Points[i]);
    }

    // 第二步：为每条边创建共享中点（key 为排序后的顶点对）
    var edgeMidpoints = new Dictionary<(int, int), int>();

    int GetOrCreateMidpoint(int a, int b)
    {
        var key = a < b ? (a, b) : (b, a);
        if (edgeMidpoints.TryGetValue(key, out int midIdx))
            return midIdx;
        midIdx = result.Points.Count;
        result.Points.Add((geo.Points[a] + geo.Points[b]) * 0.5f);
        edgeMidpoints[key] = midIdx;
        return midIdx;
    }

    // 第三步：细分每个面
    foreach (var prim in geo.Primitives)
    {
        if (prim.Length == 4)
        {
            int v0 = prim[0], v1 = prim[1], v2 = prim[2], v3 = prim[3];
            int m01 = GetOrCreateMidpoint(v0, v1);
            int m12 = GetOrCreateMidpoint(v1, v2);
            int m23 = GetOrCreateMidpoint(v2, v3);
            int m30 = GetOrCreateMidpoint(v3, v0);

            // 中心点（每个面独有）
            int center = result.Points.Count;
            result.Points.Add((geo.Points[v0] + geo.Points[v1] +
                               geo.Points[v2] + geo.Points[v3]) * 0.25f);

            result.Primitives.Add(new int[] { v0, m01, center, m30 });
            result.Primitives.Add(new int[] { m01, v1, m12, center });
            result.Primitives.Add(new int[] { center, m12, v2, m23 });
            result.Primitives.Add(new int[] { m30, center, m23, v3 });
        }
        else if (prim.Length == 3)
        {
            int v0 = prim[0], v1 = prim[1], v2 = prim[2];
            int m01 = GetOrCreateMidpoint(v0, v1);
            int m12 = GetOrCreateMidpoint(v1, v2);
            int m20 = GetOrCreateMidpoint(v2, v0);

            result.Primitives.Add(new int[] { v0, m01, m20 });
            result.Primitives.Add(new int[] { m01, v1, m12 });
            result.Primitives.Add(new int[] { m20, m12, v2 });
            result.Primitives.Add(new int[] { m01, m12, m20 });
        }
        else
        {
            // 其他多边形：直接复制（索引已经有效）
            result.Primitives.Add((int[])prim.Clone());
        }
    }

    return result;
}
```

---

## RISK-2: DeleteNode 不更新 PointAttribs

### 问题描述
`Assets/PCGToolkit/Editor/Nodes/Create/DeleteNode.cs` 第 91-121 行：删除点后，`PointAttribs` 中的属性值数组没有同步过滤。

### 修改方案

在第 98 行 `geo.Points = newPoints;` 之后，第 100 行之前，插入属性同步代码：

```csharp
// 同步 PointAttribs：过滤被删除点的属性值
foreach (var attr in geo.PointAttribs.GetAllAttributes())
{
    if (attr.Values.Count == 0) continue;
    var newValues = new List<object>();
    for (int i = 0; i < attr.Values.Count && i < geo.Points.Count + toDelete.Count; i++)
    {
        if (!toDelete.Contains(i))
            newValues.Add(attr.Values[i]);
    }
    attr.Values = newValues;
}
```

注意：这段代码需要在 `geo.Points = newPoints;` 之前执行（因为需要用原始的 Points.Count 来判断范围），或者使用一个变量记录原始点数。更安全的做法是在 `geo.Points = newPoints;` 之前插入：

```csharp
// 同步 PointAttribs
int originalPointCount = geo.Points.Count;
foreach (var attr in geo.PointAttribs.GetAllAttributes())
{
    if (attr.Values.Count == 0) continue;
    var newValues = new List<object>();
    for (int i = 0; i < Mathf.Min(attr.Values.Count, originalPointCount); i++)
    {
        if (!toDelete.Contains(i))
            newValues.Add(attr.Values[i]);
    }
    attr.Values = newValues;
}
```

将这段代码插入到第 91 行（`var newPoints = new List<Vector3>();` 之前）。

同样，在第 121 行 `geo.Primitives = newPrims;` 之后，添加 PrimAttribs 同步：

```csharp
// 同步 PrimAttribs：过滤被删除面的属性值
var deletedPrimIndices = new HashSet<int>();
for (int i = 0; i < geo.Primitives.Count; i++)
{
    bool hasDeletedPoint = false;
    foreach (int idx in geo.Primitives[i])
    {
        if (toDelete.Contains(idx)) { hasDeletedPoint = true; break; }
    }
    if (hasDeletedPoint) deletedPrimIndices.Add(i);
}
```

注意：这段逻辑需要在 `geo.Primitives = newPrims;` 之前执行。建议重构为：在过滤面的循环中同时记录被删除的面索引，然后用这些索引过滤 PrimAttribs。

实际上更简洁的做法是：在第 100-121 行的面过滤循环中，同时记录保留的面索引：


### 文件: `Assets/PCGToolkit/Editor/Nodes/Create/DeleteNode.cs`

将整个 Execute 方法（第 36-139 行）替换为以下完整实现：

```csharp
public override Dictionary<string, PCGGeometry> Execute(
    PCGContext ctx,
    Dictionary<string, PCGGeometry> inputGeometries,
    Dictionary<string, object> parameters)
{
    var geo = GetInputGeometry(inputGeometries, "input").Clone();
    string group = GetParamString(parameters, "group", "");
    string filter = GetParamString(parameters, "filter", "");
    bool deleteNonSelected = GetParamBool(parameters, "deleteNonSelected", false);

    if (string.IsNullOrEmpty(group) && string.IsNullOrEmpty(filter))
    {
        return SingleOutput("geometry", geo);
    }

    // 确定要删除的点索引集合
    HashSet<int> toDelete = new HashSet<int>();

    if (!string.IsNullOrEmpty(group))
    {
        if (geo.PointGroups.TryGetValue(group, out var groupPoints))
        {
            toDelete = new HashSet<int>(groupPoints);
        }
    }
    else if (!string.IsNullOrEmpty(filter))
    {
        toDelete = EvaluateFilter(geo, filter);
    }

    if (deleteNonSelected)
    {
        var newToDelete = new HashSet<int>();
        for (int i = 0; i < geo.Points.Count; i++)
        {
            if (!toDelete.Contains(i))
                newToDelete.Add(i);
        }
        toDelete = newToDelete;
    }

    // 构建索引映射：旧索引 -> 新索引
    var indexMap = new Dictionary<int, int>();
    int newIndex = 0;
    for (int i = 0; i < geo.Points.Count; i++)
    {
        if (!toDelete.Contains(i))
        {
            indexMap[i] = newIndex++;
        }
    }

    // ===== 同步 PointAttribs =====
    int originalPointCount = geo.Points.Count;
    foreach (var attr in geo.PointAttribs.GetAllAttributes())
    {
        if (attr.Values.Count == 0) continue;
        var newValues = new List<object>();
        for (int i = 0; i < Mathf.Min(attr.Values.Count, originalPointCount); i++)
        {
            if (!toDelete.Contains(i))
                newValues.Add(attr.Values[i]);
        }
        attr.Values = newValues;
    }

    // 创建新的顶点列表
    var newPoints = new List<Vector3>();
    for (int i = 0; i < geo.Points.Count; i++)
    {
        if (!toDelete.Contains(i))
            newPoints.Add(geo.Points[i]);
    }
    geo.Points = newPoints;

    // 过滤面：删除包含被删除点的面，同时记录保留的面索引
    var newPrims = new List<int[]>();
    var keptPrimIndices = new List<int>();
    for (int primIdx = 0; primIdx < geo.Primitives.Count; primIdx++)
    {
        var prim = geo.Primitives[primIdx];
        bool keep = true;
        foreach (int idx in prim)
        {
            if (toDelete.Contains(idx))
            {
                keep = false;
                break;
            }
        }
        if (keep)
        {
            var newPrim = new int[prim.Length];
            for (int i = 0; i < prim.Length; i++)
                newPrim[i] = indexMap[prim[i]];
            newPrims.Add(newPrim);
            keptPrimIndices.Add(primIdx);
        }
    }
    geo.Primitives = newPrims;

    // ===== 同步 PrimAttribs =====
    foreach (var attr in geo.PrimAttribs.GetAllAttributes())
    {
        if (attr.Values.Count == 0) continue;
        var newValues = new List<object>();
        foreach (int ki in keptPrimIndices)
        {
            if (ki < attr.Values.Count)
                newValues.Add(attr.Values[ki]);
        }
        attr.Values = newValues;
    }

    // 更新分组
    var newPointGroups = new Dictionary<string, HashSet<int>>();
    foreach (var kvp in geo.PointGroups)
    {
        var newGroup = new HashSet<int>();
        foreach (int idx in kvp.Value)
        {
            if (indexMap.TryGetValue(idx, out int mapped))
                newGroup.Add(mapped);
        }
        if (newGroup.Count > 0)
            newPointGroups[kvp.Key] = newGroup;
    }
    geo.PointGroups = newPointGroups;

    // ===== 同步 PrimGroups =====
    var newPrimGroups = new Dictionary<string, HashSet<int>>();
    // 构建旧面索引 -> 新面索引的映射
    var primIndexMap = new Dictionary<int, int>();
    for (int i = 0; i < keptPrimIndices.Count; i++)
    {
        primIndexMap[keptPrimIndices[i]] = i;
    }
    foreach (var kvp in geo.PrimGroups)
    {
        var newGroup = new HashSet<int>();
        foreach (int idx in kvp.Value)
        {
            if (primIndexMap.TryGetValue(idx, out int mapped))
                newGroup.Add(mapped);
        }
        if (newGroup.Count > 0)
            newPrimGroups[kvp.Key] = newGroup;
    }
    geo.PrimGroups = newPrimGroups;

    return SingleOutput("geometry", geo);
}
```

---

## RISK-3: MountainNode 使用 UnityEngine.Random 污染全局随机状态

### 文件: `Assets/PCGToolkit/Editor/Nodes/Deform/MountainNode.cs`

将第 64-70 行替换为：

```csharp
// 使用独立的 System.Random 实例，避免污染全局随机状态
var rng = new System.Random(seed);
Vector3 offset = new Vector3(
    (float)rng.NextDouble() * 1000f,
    (float)rng.NextDouble() * 1000f,
    (float)rng.NextDouble() * 1000f
);
```

需要确保文件顶部没有 `using UnityEngine.Random;` 的冲突。`System.Random` 已在 `System` 命名空间中。

---

## RISK-4: PCGGeometryToMesh.PointInTriangle 退化三角形除零

### 文件: `Assets/PCGToolkit/Editor/Core/PCGGeometryToMesh.cs`

将第 256-268 行的 `PointInTriangle` 方法替换为：

```csharp
private static bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
{
    Vector3 v0 = c - a, v1 = b - a, v2 = p - a;
    float dot00 = Vector3.Dot(v0, v0);
    float dot01 = Vector3.Dot(v0, v1);
    float dot02 = Vector3.Dot(v0, v2);
    float dot11 = Vector3.Dot(v1, v1);
    float dot12 = Vector3.Dot(v1, v2);
    float denom = dot00 * dot11 - dot01 * dot01;
    // 退化三角形保护：面积为零时直接返回 false
    if (Mathf.Abs(denom) < 1e-8f) return false;
    float inv = 1f / denom;
    float u = (dot11 * dot02 - dot01 * dot12) * inv;
    float v = (dot00 * dot12 - dot01 * dot02) * inv;
    return u >= 0 && v >= 0 && u + v <= 1;
}
```

---

## RISK-5: NormalNode 不处理 "vertex" 类型

### 文件: `Assets/PCGToolkit/Editor/Nodes/Geometry/NormalNode.cs`

当前 switch 语句（第 51-92 行）中 `"vertex"` 会 fallthrough 到 `default:` 即 `"point"` 分支。虽然不会崩溃，但语义不正确。Vertex 法线应该是每个面的每个顶点独立计算（硬边效果），而不是共享点法线。

将第 51-92 行的 switch 替换为：

```csharp
switch (type.ToLower())
{
    case "primitive":
        // 面法线
        var primNormals = geo.PrimAttribs.CreateAttribute("N", AttribType.Vector3);
        for (int p = 0; p < geo.Primitives.Count; p++)
        {
            Vector3 normal = CalculateFaceNormal(geo, p);
            primNormals.Values.Add(normal);
        }
        break;

    case "vertex":
        // 顶点法线（每个面的每个顶点独立，实现硬边效果）
        // 使用 cusp angle 判断是否共享法线
        float cuspRad = cuspAngle * Mathf.Deg2Rad;
        float cuspCos = Mathf.Cos(cuspRad);

        // 先计算所有面法线
        Vector3[] faceNormals = new Vector3[geo.Primitives.Count];
        for (int p = 0; p < geo.Primitives.Count; p++)
        {
            faceNormals[p] = CalculateFaceNormal(geo, p);
        }

        // 为每个点收集相邻面
        var pointToFaces = new Dictionary<int, List<int>>();
        for (int p = 0; p < geo.Primitives.Count; p++)
        {
            foreach (int idx in geo.Primitives[p])
            {
                if (!pointToFaces.ContainsKey(idx))
                    pointToFaces[idx] = new List<int>();
                pointToFaces[idx].Add(p);
            }
        }

        // 为每个面的每个顶点计算法线（考虑 cusp angle）
        // 存储到 VertexAttribs（如果有的话）或 PointAttribs
        // 这里简化为：对每个点，只平均 cusp angle 内的相邻面法线
        Vector3[] vertexNormals = new Vector3[geo.Points.Count];
        for (int i = 0; i < geo.Points.Count; i++)
        {
            if (!pointToFaces.TryGetValue(i, out var adjacentFaces))
            {
                vertexNormals[i] = Vector3.up;
                continue;
            }

            Vector3 avgNormal = Vector3.zero;
            foreach (int faceIdx in adjacentFaces)
            {
                // 检查与其他相邻面的角度
                bool withinCusp = true;
                foreach (int otherFaceIdx in adjacentFaces)
                {
                    if (faceIdx == otherFaceIdx) continue;
                    float dot = Vector3.Dot(faceNormals[faceIdx], faceNormals[otherFaceIdx]);
                    if (dot < cuspCos)
                    {
                        withinCusp = false;
                        break;
                    }
                }
                float area = weightByArea ? CalculateFaceArea(geo, faceIdx) : 1f;
                avgNormal += faceNormals[faceIdx] * area;
            }

            vertexNormals[i] = avgNormal.sqrMagnitude > 0.0001f
                ? avgNormal.normalized
                : Vector3.up;
        }

        for (int i = 0; i < geo.Points.Count; i++)
        {
            normalAttr.Values.Add(vertexNormals[i]);
        }
        break;

    case "point":
    default:
        // 点法线：平均相邻面的法线
        Vector3[] pointNormals = new Vector3[geo.Points.Count];
        float[] weights = new float[geo.Points.Count];

        for (int p = 0; p < geo.Primitives.Count; p++)
        {
            var prim = geo.Primitives[p];
            Vector3 faceNormal = CalculateFaceNormal(geo, p);
            float area = CalculateFaceArea(geo, p);
            float weight = weightByArea ? area : 1f;

            foreach (int idx in prim)
            {
                pointNormals[idx] += faceNormal * weight;
                weights[idx] += weight;
            }
        }

        // 归一化
        for (int i = 0; i < geo.Points.Count; i++)
        {
            if (weights[i] > 0)
                pointNormals[i] /= weights[i];
            pointNormals[i] = pointNormals[i].normalized;
            normalAttr.Values.Add(pointNormals[i]);
        }
        break;
}
```

---

## RISK-6: SavePrefabNode prefabPath 输出类型不匹配

### 问题描述
`Assets/PCGToolkit/Editor/Nodes/Output/SavePrefabNode.cs` 第 37 行声明 `prefabPath` 输出为 `PCGPortType.String`，但第 116 行返回的是一个 `PCGGeometry` 对象（用 DetailAttribs 包装字符串值）。下游节点如果期望 String 类型会无法正确读取。

### 文件: `Assets/PCGToolkit/Editor/Nodes/Output/SavePrefabNode.cs`

将第 113-117 行替换为：

```csharp
// 将 prefabPath 通过 GlobalVariables 传递（与 ConstNode 模式一致）
ctx.GlobalVariables[$"{ctx.CurrentNodeId}.prefabPath"] = savePath;

return new Dictionary<string, PCGGeometry>
{
    { "geometry", geo }
};
```

同时修改 Outputs schema（第 33-39 行），移除 prefabPath 输出端口（因为字符串值通过 GlobalVariables 传递，不需要 Geometry 端口）：

```csharp
public override PCGParamSchema[] Outputs => new[]
{
    new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
        "Geometry", "透传输入几何体"),
    new PCGParamSchema("prefabPath", PCGPortDirection.Output, PCGPortType.String,
        "Prefab Path", "保存的 Prefab 路径"),
};
```

注意：Outputs schema 保持不变（仍然声明 prefabPath 端口），但值通过 `ctx.GlobalVariables` 传递而非 PCGGeometry。这与 ConstNode 的模式一致——执行器在 `ExecuteNode` 方法中会从 `ctx.GlobalVariables` 读取上游的字符串值。

---

## RISK-7: SubdivideNode 不共享边中点导致几何体裂缝

### 文件: `Assets/PCGToolkit/Editor/Nodes/Geometry/SubdivideNode.cs`

将第 52-126 行的 `SubdivideLinear` 方法替换为：

```csharp
private PCGGeometry SubdivideLinear(PCGGeometry geo)
{
    var result = new PCGGeometry();

    // 第一步：复制所有原始顶点
    for (int i = 0; i < geo.Points.Count; i++)
    {
        result.Points.Add(geo.Points[i]);
    }

    // 第二步：为每条边创建共享中点（key 为排序后的顶点对）
    var edgeMidpoints = new Dictionary<(int, int), int>();

    int GetOrCreateMidpoint(int a, int b)
    {
        var key = a < b ? (a, b) : (b, a);
        if (edgeMidpoints.TryGetValue(key, out int midIdx))
            return midIdx;
        midIdx = result.Points.Count;
        result.Points.Add((geo.Points[a] + geo.Points[b]) * 0.5f);
        edgeMidpoints[key] = midIdx;
        return midIdx;
    }

    // 第三步：细分每个面
    foreach (var prim in geo.Primitives)
    {
        if (prim.Length == 4)
        {
            int v0 = prim[0], v1 = prim[1], v2 = prim[2], v3 = prim[3];
            int m01 = GetOrCreateMidpoint(v0, v1);
            int m12 = GetOrCreateMidpoint(v1, v2);
            int m23 = GetOrCreateMidpoint(v2, v3);
            int m30 = GetOrCreateMidpoint(v3, v0);

            // 中心点（每个面独有）
            int center = result.Points.Count;
            result.Points.Add((geo.Points[v0] + geo.Points[v1] +
                               geo.Points[v2] + geo.Points[v3]) * 0.25f);

            result.Primitives.Add(new int[] { v0, m01, center, m30 });
            result.Primitives.Add(new int[] { m01, v1, m12, center });
            result.Primitives.Add(new int[] { center, m12, v2, m23 });
            result.Primitives.Add(new int[] { m30, center, m23, v3 });
        }
        else if (prim.Length == 3)
        {
            int v0 = prim[0], v1 = prim[1], v2 = prim[2];
            int m01 = GetOrCreateMidpoint(v0, v1);
            int m12 = GetOrCreateMidpoint(v1, v2);
            int m20 = GetOrCreateMidpoint(v2, v0);

            result.Primitives.Add(new int[] { v0, m01, m20 });
            result.Primitives.Add(new int[] { m01, v1, m12 });
            result.Primitives.Add(new int[] { m20, m12, v2 });
            result.Primitives.Add(new int[] { m01, m12, m20 });
        }
        else
        {
            // 其他多边形：直接复制（索引已经有效，因为原始顶点已在 result 中）
            result.Primitives.Add((int[])prim.Clone());
        }
    }

    return result;
}
```

---

## RISK-8: PCGAttribute.Clone() 浅拷贝说明

### 文件: `Assets/PCGToolkit/Editor/Core/AttributeStore.cs`

当前第 49-54 行的 `PCGAttribute.Clone()` 使用 `new List<object>(Values)` 做浅拷贝。对于 PCG Toolkit 中实际使用的类型（`float`, `int`, `Vector2`, `Vector3`, `Vector4`, `Color` 都是值类型，`string` 是不可变引用类型），浅拷贝在实际使用中不会出问题。

但为了安全和代码清晰，添加注释说明：

```csharp
public PCGAttribute Clone()
{
    var clone = new PCGAttribute(Name, Type, DefaultValue);
    // 浅拷贝 Values 列表。对于 PCG 中使用的类型（float, int, Vector3, Color, string 等）
    // 这是安全的，因为它们要么是值类型，要么是不可变引用类型。
    // 如果未来添加可变引用类型的属性值，需要改为深拷贝。
    clone.Values = new List<object>(Values);
    return clone;
}
```

---

## BUG-1 详细修改清单（PCGEdgeData 字段名统一）

这是上一轮方案中最关键的修改，这里给出完整的全局替换清单。

### 需要修改的文件和精确行号：

**文件 1: `Assets/PCGToolkit/Editor/Graph/PCGGraphData.cs`**
- 第 75-77 行：删除以下两行
  ```csharp
  [NonSerialized] public string OutputPortName;  // 运行时兼容
  [NonSerialized] public string InputPortName;   // 运行时兼容
  ```
- 第 157 行：`OutputPortName = outputPortName,` → `OutputPort = outputPortName,`
- 第 159 行：`InputPortName = inputPortName,` → `InputPort = inputPortName,`

**文件 2: `Assets/PCGToolkit/Editor/Graph/PCGGraphExecutor.cs`**
- 第 217 行：`outputs.TryGetValue(edge.OutputPortName, out var geo)` → `outputs.TryGetValue(edge.OutputPort, out var geo)`
- 第 219 行：`inputGeometries[edge.InputPortName] = geo;` → `inputGeometries[edge.InputPort] = geo;`
- 第 236 行：`$"{edge.OutputNodeId}.{edge.OutputPortName}"` → `$"{edge.OutputNodeId}.{edge.OutputPort}"`
- 第 239 行：`parameters[edge.InputPortName] = val;` → `parameters[edge.InputPort] = val;`

**文件 3: `Assets/PCGToolkit/Editor/Graph/PCGAsyncGraphExecutor.cs`**
- 第 302 行：`outputs.TryGetValue(edge.OutputPortName, out var geo)` → `outputs.TryGetValue(edge.OutputPort, out var geo)`
- 第 304 行：`inputGeometries[edge.InputPortName] = geo;` → `inputGeometries[edge.InputPort] = geo;`
- 第 322 行：`$"{edge.OutputNodeId}.{edge.OutputPortName}"` → `$"{edge.OutputNodeId}.{edge.OutputPort}"`
- 第 326 行：`parameters[edge.InputPortName] = val;` → `parameters[edge.InputPort] = val;`

**文件 4: `Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs`**
- 第 398 行：`OutputPortName = outputVisual.FindPortSchemaName(edge.output),` → `OutputPort = outputVisual.FindPortSchemaName(edge.output),`
- 第 400 行：`InputPortName = inputVisual.FindPortSchemaName(edge.input)` → `InputPort = inputVisual.FindPortSchemaName(edge.input)`
- 第 460 行：`edgeData.OutputPortName` → `edgeData.OutputPort`
- 第 461 行：`edgeData.InputPortName` → `edgeData.InputPort`
- 第 658 行：`edgeData.OutputPortName` → `edgeData.OutputPort`
- 第 659 行：`edgeData.InputPortName` → `edgeData.InputPort`
- 第 666 行：`edgeData.InputPortName` → `edgeData.InputPort`
- 第 736 行：`OutputPortName = outputVisual.FindPortSchemaName(edge.output),` → `OutputPort = outputVisual.FindPortSchemaName(edge.output),`
- 第 738 行：`InputPortName = inputVisual.FindPortSchemaName(edge.input)` → `InputPort = inputVisual.FindPortSchemaName(edge.input)`

**验证方法**: 修改完成后，全局搜索 `OutputPortName` 和 `InputPortName`，确保 .cs 文件中不再有任何引用。

---

## BUG-2 完整代码（ExtrudeNode 重写）

### 文件: `Assets/PCGToolkit/Editor/Nodes/Geometry/ExtrudeNode.cs`

将第 41-160 行的 `Execute` 方法**完整替换**为以下代码：

```csharp
public override Dictionary<string, PCGGeometry> Execute(
    PCGContext ctx,
    Dictionary<string, PCGGeometry> inputGeometries,
    Dictionary<string, object> parameters)
{
    var geo = GetInputGeometry(inputGeometries, "input").Clone();
    string group = GetParamString(parameters, "group", "");
    float distance = GetParamFloat(parameters, "distance", 0.5f);
    float inset = GetParamFloat(parameters, "inset", 0f);
    int divisions = Mathf.Max(1, GetParamInt(parameters, "divisions", 1));
    bool outputFront = GetParamBool(parameters, "outputFront", true);
    bool outputSide = GetParamBool(parameters, "outputSide", true);

    if (geo.Primitives.Count == 0)
    {
        ctx.LogWarning("Extrude: 输入几何体没有面");
        return SingleOutput("geometry", geo);
    }

    var result = new PCGGeometry();

    // 第一步：复制所有原始顶点到 result（保持 1:1 索引映射）
    // 这样非挤出面的原始索引在 result 中仍然有效
    for (int i = 0; i < geo.Points.Count; i++)
    {
        result.Points.Add(geo.Points[i]);
    }

    // 确定要挤出的面
    HashSet<int> primsToExtrude = new HashSet<int>();
    if (!string.IsNullOrEmpty(group) && geo.PrimGroups.TryGetValue(group, out var groupPrims))
    {
        primsToExtrude = groupPrims;
    }
    else
    {
        for (int i = 0; i < geo.Primitives.Count; i++)
            primsToExtrude.Add(i);
    }

    // 第二步：添加未挤出的原始面（索引仍然有效，因为原始顶点已 1:1 复制到 result）
    for (int i = 0; i < geo.Primitives.Count; i++)
    {
        if (!primsToExtrude.Contains(i))
        {
            result.Primitives.Add((int[])geo.Primitives[i].Clone());
        }
    }

    // 第三步：处理挤出面
    foreach (int primIdx in primsToExtrude)
    {
        var prim = geo.Primitives[primIdx];
        if (prim.Length < 3) continue;

        Vector3 normal = CalculateFaceNormal(geo.Points, prim);

        Vector3 center = Vector3.zero;
        foreach (int idx in prim) center += geo.Points[idx];
        center /= prim.Length;

        // 第一层使用原始顶点索引（已在 result.Points 中，索引 0 ~ geo.Points.Count-1）
        int[] prevLayerVertices = (int[])prim.Clone();

        for (int d = 1; d <= divisions; d++)
        {
            float t = (float)d / divisions;
            float offset = distance * t;
            float insetAmount = inset * t;

            int[] layerVertices = new int[prim.Length];
            for (int i = 0; i < prim.Length; i++)
            {
                Vector3 origPos = geo.Points[prim[i]];
                Vector3 toCenter = (center - origPos);
                float toCenterMag = toCenter.magnitude;
                Vector3 toCenterDir = toCenterMag > 0.0001f ? toCenter / toCenterMag : Vector3.zero;
                Vector3 newPos = origPos + normal * offset + toCenterDir * insetAmount;

                int newIdx = result.Points.Count;
                result.Points.Add(newPos);
                layerVertices[i] = newIdx;
            }

            // 创建侧面
            if (outputSide)
            {
                for (int i = 0; i < prim.Length; i++)
                {
                    int next = (i + 1) % prim.Length;
                    result.Primitives.Add(new int[]
                    {
                        prevLayerVertices[i], prevLayerVertices[next],
                        layerVertices[next], layerVertices[i]
                    });
                }
            }

            prevLayerVertices = layerVertices;
        }

        // 输出顶面
        if (outputFront)
        {
            // 使用最后一层的顶点，反转绕序使法线朝外
            int[] frontPrim = new int[prim.Length];
            for (int i = 0; i < prim.Length; i++)
            {
                frontPrim[i] = prevLayerVertices[i];
            }
            result.Primitives.Add(frontPrim);
        }
    }

    return SingleOutput("geometry", result);
}
```

**关键修复点说明**：
- 原始代码的问题：非挤出面使用原始索引 `geo.Primitives[i].Clone()` 添加到 `result.Primitives`，但 `result.Points` 中只有挤出面创建的新顶点，原始索引无效
- 修复方案：先将所有原始顶点 1:1 复制到 `result.Points`（第一步），这样非挤出面的原始索引在 result 中仍然有效。挤出面的新顶点追加到末尾
- 额外修复：`toCenter.normalized` 在零向量时会返回 `Vector3.zero`，但 Unity 的 `normalized` 对零向量返回零向量所以实际不会崩溃。为了代码清晰，显式处理了这个情况

---

## BUG-8: ScatterNode sourcePrim 属性未填充（完整代码）

### 文件: `Assets/PCGToolkit/Editor/Nodes/Distribute/ScatterNode.cs`

将第 94-131 行替换为：

```csharp
// 按面积加权随机选择面并生成点
List<Vector3> points = new List<Vector3>();
List<int> sourcePrimIndices = new List<int>(); // 记录每个点的来源面索引

for (int i = 0; i < count; i++)
{
    // 按面积随机选择面
    float r = (float)rng.NextDouble() * totalArea;
    float accum = 0f;
    int selectedPrim = -1;

    for (int j = 0; j < primAreas.Count; j++)
    {
        accum += primAreas[j];
        if (accum >= r)
        {
            selectedPrim = primIndices[j];
            break;
        }
    }

    if (selectedPrim >= 0)
    {
        Vector3 point = SamplePointOnPrim(inputGeo, selectedPrim, rng);
        points.Add(point);
        sourcePrimIndices.Add(selectedPrim);
    }
}

// 松弛迭代（可选）
if (relaxIterations > 0 && points.Count > 1)
{
    points = RelaxPoints(points, relaxIterations);
}

// 输出点几何体
geo.Points = points;

// 创建索引属性（原始面索引）并填充值
var indexAttr = geo.PointAttribs.CreateAttribute("sourcePrim", AttribType.Int);
foreach (var idx in sourcePrimIndices)
{
    indexAttr.Values.Add(idx);
}

return SingleOutput("geometry", geo);
```

---

## BUG-9: PCGGraphExecutor 删除重复拓扑排序（完整修改）

### 文件: `Assets/PCGToolkit/Editor/Graph/PCGGraphExecutor.cs`

1. **删除**第 136-193 行的整个私有 `TopologicalSort()` 方法（包括注释）

2. **修改**第 106 行，将：
```csharp
var sortedNodes = TopologicalSort();
```
改为：
```csharp
var sortedNodes = PCGGraphHelper.TopologicalSort(graphData);
```

3. 确保文件顶部已有 `using PCGToolkit.Core;`（如果 `PCGGraphHelper` 在该命名空间下）。如果 `PCGGraphHelper` 在其他命名空间，请添加对应的 using。

---

## RISK-3: MountainNode 改用 System.Random（完整修改）

### 文件: `Assets/PCGToolkit/Editor/Nodes/Deform/MountainNode.cs`

将第 64-70 行：
```csharp
// 设置随机种子
UnityEngine.Random.InitState(seed);
Vector3 offset = new Vector3(
    UnityEngine.Random.value * 1000f,
    UnityEngine.Random.value * 1000f,
    UnityEngine.Random.value * 1000f
);
```

替换为：
```csharp
// 使用独立的 System.Random 实例，避免污染全局随机状态
var rng = new System.Random(seed);
Vector3 offset = new Vector3(
    (float)rng.NextDouble() * 1000f,
    (float)rng.NextDouble() * 1000f,
    (float)rng.NextDouble() * 1000f
);
```

---

## RISK-4: PointInTriangle 退化三角形保护（完整修改）

### 文件: `Assets/PCGToolkit/Editor/Core/PCGGeometryToMesh.cs`

将第 256-268 行的 `PointInTriangle` 方法替换为：

```csharp
private static bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
{
    Vector3 v0 = c - a, v1 = b - a, v2 = p - a;
    float dot00 = Vector3.Dot(v0, v0);
    float dot01 = Vector3.Dot(v0, v1);
    float dot02 = Vector3.Dot(v0, v2);
    float dot11 = Vector3.Dot(v1, v1);
    float dot12 = Vector3.Dot(v1, v2);
    float denom = dot00 * dot11 - dot01 * dot01;
    // 退化三角形保护：面积为零时直接返回 false
    if (Mathf.Abs(denom) < 1e-8f) return false;
    float inv = 1f / denom;
    float u = (dot11 * dot02 - dot01 * dot12) * inv;
    float v = (dot00 * dot12 - dot01 * dot02) * inv;
    return u >= 0 && v >= 0 && u + v <= 1;
}
```

---

## RISK-6: SavePrefabNode prefabPath 输出类型修正（完整修改）

### 文件: `Assets/PCGToolkit/Editor/Nodes/Output/SavePrefabNode.cs`

将第 113-117 行：
```csharp
return new Dictionary<string, PCGGeometry>
{
    { "geometry", geo },
    { "prefabPath", new PCGGeometry { DetailAttribs = new AttributeStore().SetAttribute("value", savePath) } }
};
```

替换为：
```csharp
// 将 prefabPath 通过 context 的 GlobalVariables 传递
// 这样下游节点可以通过 ctx.GlobalVariables 读取字符串值
ctx.GlobalVariables[$"{ctx.CurrentNodeId}.prefabPath"] = savePath;

return new Dictionary<string, PCGGeometry>
{
    { "geometry", geo }
};
```

**注意**: `Outputs` schema（第 33-39 行）保持不变，仍然声明 `prefabPath` 端口。但由于 PCG 的执行器对非 Geometry 类型的输出通过 `GlobalVariables` 传递，这与现有的 ConstNode 模式一致。

---

## RISK-8: PCGAttribute.Clone() 添加安全注释

### 文件: `Assets/PCGToolkit/Editor/Core/AttributeStore.cs`

将第 49-54 行替换为：

```csharp
public PCGAttribute Clone()
{
    var clone = new PCGAttribute(Name, Type, DefaultValue);
    // 浅拷贝 Values 列表。对于 PCG 中使用的类型（float, int, Vector3, Color 等值类型
    // 以及 string 不可变引用类型），浅拷贝是安全的。
    // 如果未来添加可变引用类型的属性值，需要改为深拷贝。
    clone.Values = new List<object>(Values);
    return clone;
}
```

---

# 验证清单

修复完成后，请逐一验证以下场景：

### 基础功能验证
1. **图保存/加载**: 创建 Box → Normal → ExportMesh 图，保存、关闭编辑器窗口、重新打开，验证所有连线完整（端口名不为 null）
2. **ExtrudeNode**: Box → Extrude(group="top", distance=1) → 验证输出几何体中非挤出面的顶点位置正确，不出现错位
3. **ExtrudeNode 全面挤出**: Box → Extrude(distance=0.5, divisions=3) → 验证侧面和顶面正确生成
4. **BendNode angle=0**: Grid → Bend(angle=0) → 不崩溃，输出与输入相同
5. **ScatterNode**: Grid → Scatter(count=50) → 验证输出点的 `sourcePrim` 属性有 50 个值
6. **DeleteNode**: Box → Delete(group="top") → 验证删除后 PointAttribs 和 PrimAttribs 数量与剩余点/面一致
7. **SubdivideNode**: Box → Subdivide → 验证细分后相邻面共享边中点，无裂缝
8. **MountainNode**: Grid → Mountain(seed=42) → 验证执行前后 `UnityEngine.Random.state` 未被修改
9. **NormalNode vertex 类型**: Box → Normal(type="vertex") → 不崩溃，输出法线属性

### 编译验证
10. 全局搜索 `OutputPortName` 和 `InputPortName`，确保 .cs 文件中无残留引用
11. 全局搜索 `PCGGraphAsset`，确保 SubGraphNode 中已改为正确类型
12. Unity 编辑器中无编译错误

### 边界情况验证
13. 空几何体输入到各节点 → 不崩溃
14. 退化三角形（三点共线）→ PointInTriangle 返回 false，不产生 NaN
15. PCGGeometryToMesh 转换包含 N-gon 的几何体 → 正确三角化