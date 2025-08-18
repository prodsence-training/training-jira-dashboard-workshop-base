# US-001 Sprint 燃盡圖視覺化 - 實作任務清單

> **檔案編號**: TASK-001-sprint-burndown-implementation  
> **建立日期**: 2025-08-18  
> **最後更新**: 2025-08-18  
> **狀態**: 進行中  
> **對應 User Story**: [US-001](./spec01-us01-sprintprogress.md#us-001-sprint-燃盡圖視覺化)

## 📋 任務總覽

基於確認的技術方案，以下是完整的實作任務清單：

### ✅ **已確認的技術需求**
- **資料來源**: 從現有 `rawData` 計算 Sprint 燃盡數據
- **頁面布局**: 放在 Issue Status Distribution 正下方
- **Sprint 選擇**: 複用現有的 Sprint 篩選器
- **時間計算**: 排除週末，讀取 `GetJiraSprintValues` 的 startDate/endDate
- **開發優先級**: 完成率 → 燃盡圖 → 色彩警示

---

## 🏗️ Phase 1: 後端資料層實作

### Task 1.1: 建立資料模型 🔄
**狀態**: 準備開始  
**估時**: 0.5 天  
**描述**: 建立 Sprint 燃盡圖所需的資料模型

**技術細節**:
```csharp
// 需要建立的資料模型
public class SprintBurndownData
{
    public string SprintName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalStoryPoints { get; set; }
    public int CompletedStoryPoints { get; set; }
    public int CurrentWorkingDay { get; set; }
    public int TotalWorkingDays { get; set; }
    public double CompletionRate { get; set; }
    public ProgressStatus HealthStatus { get; set; }
    public List<DailyProgress> DailyProgress { get; set; }
}

public class DailyProgress
{
    public DateTime Date { get; set; }
    public int WorkingDay { get; set; }
    public int CompletedStoryPoints { get; set; }
    public int RemainingStoryPoints { get; set; }
    public int IdealRemaining { get; set; }
}

public enum ProgressStatus
{
    Normal,   // 綠色
    Warning,  // 黃色  
    Danger    // 紅色
}
```

**驗收標準**:
- [x] 建立完整的資料模型類別
- [x] 定義 ProgressStatus 枚舉
- [x] 確保所有屬性符合 AC 需求

---

### Task 1.2: 實作工作日計算邏輯 🔄
**狀態**: 準備開始  
**估時**: 0.5 天  
**描述**: 實作排除週末的工作日計算功能

**技術細節**:
```csharp
public static class WorkingDayCalculator
{
    public static int CalculateWorkingDays(DateTime startDate, DateTime endDate)
    {
        // 排除週六、週日的工作日計算
    }
    
    public static int GetCurrentWorkingDay(DateTime sprintStart, DateTime currentDate)
    {
        // 計算當前是 Sprint 的第幾個工作日
    }
}
```

**資料來源**:
- `GetJiraSprintValues` 表的 startDate (Column F) 和 endDate (Column G)
- 按照 table-schema.md 的欄位定義：F 是 startDate，G 是 endDate

**驗收標準**:
- [x] 正確排除週末（週六、日）
- [x] 處理跨月份的日期計算
- [x] 返回準確的工作日數量

---

### Task 1.3: 擴展 GoogleSheetsService 🔄
**狀態**: 準備開始  
**估時**: 1 天  
**描述**: 新增 Sprint 燃盡數據的讀取和計算功能

**資料欄位對應**:
- `rawData` 表：
  - Sprint 欄位：索引 6 (Column G)
  - Story Points 欄位：索引 15 (Column P)
  - Status 欄位：索引 5 (Column F)
  - Resolved 欄位：索引 21 (Column V)
- `GetJiraSprintValues` 表：
  - Sprint Name 欄位：索引 2 (Column C)
  - startDate 欄位：索引 5 (Column F)
  - endDate 欄位：索引 6 (Column G)

**功能需求**:
```csharp
public async Task<SprintBurndownData> GetSprintBurndownDataAsync(string sprintName)
{
    // 1. 從 GetJiraSprintValues 取得 Sprint 時間資訊
    // 2. 從 rawData 篩選該 Sprint 的 Issues
    // 3. 計算完成狀態（Done 算已完成）
    // 4. 計算每日燃盡數據
    // 5. 計算進度健康度
}
```

**驗收標準**:
- [x] 正確讀取 Sprint 時間資訊
- [x] 準確計算故事點數統計
- [x] 生成每日燃盡數據
- [x] 計算進度健康度（綠/黃/紅）

---

### Task 1.4: 新增 API 端點 🔄
**狀態**: 準備開始  
**估時**: 0.5 天  
**描述**: 建立 Sprint 燃盡圖的 API 端點

**API 設計**:
```csharp
[HttpGet("sprint/{sprintName}/burndown")]
public async Task<ActionResult<SprintBurndownData>> GetSprintBurndown(string sprintName)
```

**驗收標準**:
- [x] API 端點正常運作
- [x] 返回正確的 JSON 格式
- [x] 處理錯誤情況（Sprint 不存在等）
- [x] 遵循現有的 API 模式

---

## 🎨 Phase 2: 前端組件實作

### Task 2.1: 建立完成率卡片組件 (Priority 1) 🔄
**狀態**: 準備開始  
**估時**: 1 天  
**描述**: 實作 Sprint 完成率顯示卡片

**組件設計**:
```typescript
interface CompletionRateCardProps {
  sprintName: string;
  completedSP: number;
  totalSP: number;
  completionRate: number;
  healthStatus: 'normal' | 'warning' | 'danger';
}
```

**視覺要求**:
- 顯示 Sprint 名稱
- 進度條視覺化（65% 完成）
- 故事點數分解（已完成/剩餘/總計）
- 色彩狀態指示（綠/黃/紅）
- 警告圖示（⚠️/🚨）

**驗收標準**:
- [x] 符合 AC-001-01 的視覺效果
- [x] 正確的色彩狀態顯示
- [x] 響應式設計
- [x] 與現有 UI 風格一致

---

### Task 2.2: 實作資料 Hook 🔄
**狀態**: 準備開始  
**估時**: 0.5 天  
**描述**: 建立資料獲取和狀態管理的 Hook

**Hook 設計**:
```typescript
export const useSprintBurndown = (selectedSprint: string) => {
  // 資料獲取、載入狀態、錯誤處理
  return { data, loading, error, refetch };
}
```

**驗收標準**:
- [x] 正確的 API 呼叫
- [x] 載入狀態處理
- [x] 錯誤狀態處理
- [x] 資料快取機制

---

### Task 2.3: 建立燃盡圖組件 (Priority 2) 🔄
**狀態**: 準備開始  
**估時**: 1.5 天  
**描述**: 使用 Recharts 實作 Sprint 燃盡圖

**圖表需求**:
- 理想燃盡線（虛線，灰色）
- 實際燃盡線（實線，依健康度變色）
- X 軸：工作日（Day 1, Day 2...）
- Y 軸：剩餘故事點數
- Tooltip 顯示詳細資訊

**色彩系統**:
- 正常：綠色 (#10b981)
- 警示：黃色 (#f59e0b)
- 危險：紅色 (#ef4444)

**驗收標準**:
- [x] 正確的雙線顯示
- [x] 依健康度變色
- [x] 互動功能正常
- [x] 響應式設計

---

### Task 2.4: 建立主容器組件 🔄
**狀態**: 準備開始  
**估時**: 0.5 天  
**描述**: 整合所有子組件的主容器

**組件架構**:
```
SprintBurndownContainer
├── CompletionRateCard
└── BurndownChart
```

**驗收標準**:
- [x] 正確的資料傳遞
- [x] 統一的載入和錯誤狀態
- [x] 良好的布局間距

---

## 🔗 Phase 3: 整合與測試

### Task 3.1: 整合到主儀表板 🔄
**狀態**: 準備開始  
**估時**: 0.5 天  
**描述**: 將燃盡圖組件加入現有儀表板頁面

**整合位置**:
- 放在 Issue Status Distribution 組件正下方
- 複用現有的 Sprint 篩選器

**驗收標準**:
- [x] 正確的頁面布局
- [x] 與現有篩選器同步
- [x] 不影響現有功能

---

### Task 3.2: 進度健康度測試 🔄
**狀態**: 準備開始  
**估時**: 0.5 天  
**描述**: 測試各種進度狀況的色彩警示

**測試場景**:
- 正常進度（綠色）
- 警示狀態（黃色，落後 10-20%）
- 危險狀態（紅色，落後 >20%）
- 邊界值測試（9%, 10%, 19%, 20%, 21%）

**驗收標準**:
- [x] 所有 AC 場景通過
- [x] 邊界值正確處理
- [x] 視覺效果符合設計

---

### Task 3.3: 響應式設計測試 🔄
**狀態**: 準備開始  
**估時**: 0.5 天  
**描述**: 確保在不同裝置上的顯示效果

**測試範圍**:
- 桌面瀏覽器 (>1024px)
- 平板裝置 (768-1024px)  
- 手機裝置 (<768px)

**驗收標準**:
- [x] 符合 AC-001-07 的響應式需求
- [x] 所有元素可正常操作
- [x] 文字清晰可讀

---

## 📊 進度健康度計算邏輯

### 計算公式
```typescript
function calculateProgressHealth(completionRate: number, timeProgressRate: number): ProgressStatus {
  const progressDiff = timeProgressRate - completionRate;
  
  if (progressDiff < 10) return 'normal';    // 綠色：正常或超前
  if (progressDiff < 20) return 'warning';   // 黃色：稍微落後
  return 'danger';                           // 紅色：嚴重落後
}
```

### 完成狀態定義
基於 `rawData` 表的 Status 欄位（索引 5），以下狀態視為「已完成」：
- `Done`

其他狀態視為「進行中」或「未開始」。

---

## 📝 實作檢查清單

### Backend 檢查項目
- [ ] Task 1.1: 資料模型建立完成
- [ ] Task 1.2: 工作日計算邏輯實作
- [ ] Task 1.3: GoogleSheetsService 擴展
- [ ] Task 1.4: API 端點建立
- [ ] API 測試通過

### Frontend 檢查項目
- [ ] Task 2.1: 完成率卡片組件
- [ ] Task 2.2: 資料 Hook 實作
- [ ] Task 2.3: 燃盡圖組件
- [ ] Task 2.4: 主容器組件
- [ ] 組件單元測試通過

### Integration 檢查項目
- [ ] Task 3.1: 主儀表板整合
- [ ] Task 3.2: 進度健康度測試
- [ ] Task 3.3: 響應式設計測試
- [ ] 所有 AC 場景驗證通過

---

## 🚀 實作排程

### Week 1 (目標：完成率功能上線)
- **Day 1**: Task 1.1 + 1.2 (後端基礎)
- **Day 2**: Task 1.3 + 1.4 (API 完成)
- **Day 3**: Task 2.1 + 2.2 (完成率卡片)
- **Day 4**: Task 3.1 (整合測試)
- **Day 5**: 測試與優化

### Week 2 (目標：燃盡圖功能上線)
- **Day 1-2**: Task 2.3 (燃盡圖組件)
- **Day 3**: Task 2.4 (主容器整合)
- **Day 4**: Task 3.2 + 3.3 (全面測試)
- **Day 5**: 文件更新與部署

---

## 📞 技術支援聯絡

遇到問題時的處理流程：
1. 檢查 table-schema.md 確認資料欄位
2. 參考現有的 GoogleSheetsService 實作模式
3. 確保符合所有 AC 和測試案例要求

## 📝 變更記錄

| 日期       | 版本 | 變更內容 | 狀態 |
| ---------- | ---- | -------- | ---- |
| 2025-08-18 | 1.0  | 初版任務清單建立，準備開始實作 | 🔄 進行中 |
