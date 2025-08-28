# Task Management: US001 Sprint 燃盡圖視覺化與進度警示

> **檔案編號**: TASK-US001-sprint-burndown-visualization  
> **建立日期**: 2025-08-28  
> **執行方式**: Test-Driven Development (TDD)  
> **對應技術設計**: [tech-design-us001-sprint-burndown.md](./tech-design-us001-sprint-burndown.md)

## 📋 任務執行原則

### TDD 執行流程
1. **Red**: 先寫測試，確保測試失敗（因為功能尚未實現）
2. **Green**: 實現最小可行代碼讓測試通過
3. **Refactor**: 重構代碼，保持測試通過
4. **驗證**: 確保所有相關測試都通過

### 任務優先級
- **P0**: 必須完成的核心功能
- **P1**: 重要的用戶體驗功能
- **P2**: 錦上添花的增強功能

---

## 🎯 Phase 1: 核心功能驗證與修正 (P0)

### Task 1.1: 健康狀態計算邏輯驗證與修正

**目標**: 確保後端健康狀態計算完全符合 AC02, AC03 要求

#### 子任務 1.1.1: 撰寫健康狀態計算單元測試
- **檔案**: `backend-dotnet/Tests/SprintHealthStatusTests.cs` (新建)
- **測試案例對應**: TC-002, TC-003
- **預期結果**: 測試失敗（因為可能邏輯不正確）

**測試場景**:
```csharp
[Test] 正常進度_返回Normal() // 第5天/10天，50%完成，50%理想
[Test] 稍微落後10Percent_返回Normal() // 落後10%，仍正常
[Test] 稍微落後15Percent_返回Warning() // 落後15%，黃色警示
[Test] 稍微落後25Percent_返回Warning() // 落後25%，黃色警示邊界
[Test] 嚴重落後30Percent_返回Danger() // 落後30%，紅色危險
[Test] 邊界值測試() // 精確的10%和25%邊界測試
```

#### 子任務 1.1.2: 檢查並修正健康狀態計算邏輯
- **檔案**: `backend-dotnet/GoogleSheetsService.cs`
- **目標**: 讓 1.1.1 的測試全部通過
- **驗證**: `dotnet test` 全部 green

**預期修正內容**:
```csharp
private string CalculateSprintHealthStatus(double completionRate, int daysElapsed, int totalWorkingDays)
{
    double timeElapsedPercentage = (double)daysElapsed / totalWorkingDays;
    double progressGap = timeElapsedPercentage - completionRate;
    
    // AC 要求: 10-25% 範圍為 warning, >25% 為 danger
    if (progressGap <= 0.10) return "normal";
    if (progressGap <= 0.25) return "warning";
    return "danger";
}
```

#### 子任務 1.1.3: 整合測試驗證
- **檔案**: `backend-dotnet/Tests/SprintBurndownIntegrationTests.cs` (可能已存在)
- **目標**: 驗證端到端的 API 回應正確性
- **測試案例對應**: TC-001, TC-002, TC-003

---

### Task 1.2: 前端健康狀態顏色同步驗證

**目標**: 確保進度條與燃盡圖實際線顏色完全一致

#### 子任務 1.2.1: 撰寫前端顏色同步測試
- **檔案**: `frontend/__tests__/burndown-chart.test.tsx` (新建或擴展)
- **測試案例對應**: TC-001, TC-002, TC-003
- **工具**: React Testing Library + Jest

**測試場景**:
```typescript
test('正常狀態_顯示綠色', () => {})
test('警示狀態_顯示黃色', () => {})  
test('危險狀態_顯示紅色', () => {})
test('進度條與燃盡圖顏色一致性', () => {})
```

#### 子任務 1.2.2: 檢查並修正顏色映射邏輯
- **檔案**: `frontend/components/burndown-chart.tsx`
- **目標**: 確保顏色映射邏輯正確，測試通過
- **檢查點**: 
  - `getStatusColor` 函數
  - 進度條顏色邏輯
  - Recharts 線條顏色設定

#### 子任務 1.2.3: 視覺化回歸測試
- **工具**: Playwright + 視覺比對
- **檔案**: `tests/visual/sprint-burndown-colors.spec.ts` (新建)
- **目標**: 截圖比對確保顏色正確顯示

---

### Task 1.3: 時間邊界處理驗證與修正

**目標**: 確保實際線只顯示到當前日期，未來日期處理正確

#### 子任務 1.3.1: 撰寫時間邊界單元測試
- **檔案**: `backend-dotnet/Tests/SprintTimeCalculationTests.cs` (新建)
- **測試案例對應**: TC-004, TC-005, TC-006

**測試場景**:
```csharp
[Test] 當前第8天_實際數據只到第8天()
[Test] Sprint第1天_顯示起始狀態()
[Test] Sprint最後1天_顯示完整數據()
[Test] 未來日期_ActualRemaining為Null()
```

#### 子任務 1.3.2: 前端時間邊界顯示測試
- **檔案**: `frontend/__tests__/burndown-chart-boundaries.test.tsx` (新建)
- **測試案例對應**: TC-004

**測試場景**:
```typescript
test('未來日期Tooltip顯示正確文字', async () => {
  // 模擬第8天/10天數據
  // 驗證Day9, Day10的tooltip顯示"實際剩餘: 未來日期"
})
```

#### 子任務 1.3.3: E2E 時間邊界測試
- **檔案**: `tests/e2e/sprint-burndown-boundaries.spec.ts` (新建)
- **工具**: Playwright
- **測試案例對應**: TC-004, TC-005, TC-006

---

## 🛠️ Phase 2: 錯誤處理增強 (P1)

### Task 2.1: 空資料錯誤處理實現

**目標**: 實現友善的空資料錯誤處理 UI

#### 子任務 2.1.1: 撰寫錯誤處理組件測試
- **檔案**: `frontend/__tests__/sprint-burndown-error.test.tsx` (新建)
- **測試案例對應**: TC-007

**測試場景**:
```typescript
test('空資料時顯示錯誤訊息', () => {})
test('顯示重新載入按鈕', () => {})
test('點擊重新載入觸發重試', () => {})
test('載入狀態正確顯示', () => {})
```

#### 子任務 2.1.2: 實現錯誤處理 UI 組件
- **檔案**: `frontend/components/sprint-burndown-error.tsx` (新建)
- **目標**: 實現符合 AC05 要求的錯誤處理 UI

**組件設計**:
```typescript
interface SprintBurndownErrorProps {
  error: string;
  onRetry: () => void;
  isLoading?: boolean;
}
```

#### 子任務 2.1.3: 整合錯誤處理到容器組件
- **檔案**: `frontend/components/sprint-burndown-container.tsx`
- **目標**: 修改容器組件整合錯誤處理邏輯
- **檢查點**: 
  - 錯誤狀態管理
  - 重試機制實現
  - 載入狀態處理

#### 子任務 2.1.4: E2E 錯誤處理測試
- **檔案**: `tests/e2e/sprint-burndown-error-handling.spec.ts` (新建)
- **測試案例對應**: TC-007
- **場景**: 模擬 API 錯誤、網路錯誤、空資料回應

---

### Task 2.2: 載入狀態優化

**目標**: 改善載入狀態顯示，避免閃爍和混亂

#### 子任務 2.2.1: 撰寫載入狀態測試
- **檔案**: `frontend/__tests__/sprint-burndown-loading.test.tsx` (新建)
- **測試場景**: 各種載入狀態的 UI 正確性

#### 子任務 2.2.2: 實現優化的載入 UI
- **檔案**: `frontend/components/sprint-burndown-skeleton.tsx` (新建)
- **目標**: 實現 Skeleton Loading 效果

---

## 🎨 Phase 3: 健康狀態 Badge 增強 (P2)

### Task 3.1: 健康狀態 Badge 組件實現

**目標**: 在右上角顯示具體的健康狀態文字

#### 子任務 3.1.1: 撰寫 Badge 組件測試
- **檔案**: `frontend/__tests__/health-status-badge.test.tsx` (新建)

**測試場景**:
```typescript
test('正常狀態顯示綠色Badge', () => {})
test('警示狀態顯示黃色Badge', () => {})
test('危險狀態顯示紅色Badge', () => {})
test('文字內容正確顯示', () => {})
```

#### 子任務 3.1.2: 實現 Badge 組件
- **檔案**: `frontend/components/health-status-badge.tsx` (新建)
- **設計**: 基於 shadcn/ui Badge 組件擴展

#### 子任務 3.1.3: 整合 Badge 到燃盡圖容器
- **檔案**: `frontend/components/sprint-burndown-container.tsx`
- **位置**: 右上角位置，與完成率卡片協調佈局

---

## 🧪 Phase 4: 完整測試驗證 (P0)

### Task 4.1: E2E 測試完整實現

**目標**: 實現所有測試案例的 E2E 自動化驗證

#### 子任務 4.1.1: TC-001~TC-003 功能測試腳本
- **檔案**: `tests/e2e/sprint-burndown-functionality.spec.ts` (新建)
- **測試案例**: TC-001, TC-002, TC-003
- **工具**: Playwright

#### 子任務 4.1.2: TC-004~TC-006 邊界測試腳本
- **檔案**: `tests/e2e/sprint-burndown-boundaries.spec.ts` (已在 Task 1.3.3)
- **測試案例**: TC-004, TC-005, TC-006

#### 子任務 4.1.3: TC-007 異常處理測試腳本
- **檔案**: `tests/e2e/sprint-burndown-error-handling.spec.ts` (已在 Task 2.1.4)
- **測試案例**: TC-007

### Task 4.2: 效能測試實現

**目標**: 確保燃盡圖載入和渲染效能符合要求

#### 子任務 4.2.1: 載入效能測試
- **檔案**: `tests/performance/sprint-burndown-performance.spec.ts` (新建)
- **目標**: 頁面載入時間 < 3秒

#### 子任務 4.2.2: 渲染效能測試
- **工具**: Lighthouse CI
- **目標**: 圖表渲染時間 < 1秒

---

## 📋 任務執行順序與檢查點

### Week 1: Phase 1 - 核心功能驗證
| 日期 | 任務 | 檢查點 | 負責人 |
|------|------|--------|--------|
| Day 1 | Task 1.1.1 | 單元測試撰寫完成 | Dev |
| Day 1 | Task 1.1.2 | 後端邏輯修正，測試通過 | Dev |
| Day 2 | Task 1.2.1 | 前端顏色測試撰寫完成 | Dev |
| Day 2 | Task 1.2.2 | 顏色同步邏輯修正 | Dev |
| Day 3 | Task 1.3.1 | 時間邊界測試撰寫完成 | Dev |
| Day 3 | Task 1.3.2 | 前端時間邊界修正 | Dev |
| Day 4 | Task 1.1.3 | 整合測試驗證 | Dev |
| Day 4 | Task 1.3.3 | E2E 時間邊界測試 | Dev |
| Day 5 | Review | Phase 1 完整驗證 | PM + Dev |

### Week 2: Phase 2 - 錯誤處理增強
| 日期 | 任務 | 檢查點 | 負責人 |
|------|------|--------|--------|
| Day 1 | Task 2.1.1 | 錯誤處理測試撰寫完成 | Dev |
| Day 1 | Task 2.1.2 | 錯誤處理 UI 實現 | Dev |
| Day 2 | Task 2.1.3 | 錯誤處理整合完成 | Dev |
| Day 2 | Task 2.1.4 | E2E 錯誤處理測試 | Dev |
| Day 3 | Task 2.2.1~2.2.2 | 載入狀態優化 | Dev |
| Day 4 | Task 3.1.1~3.1.2 | Badge 組件實現 | Dev |
| Day 5 | Task 3.1.3 | Badge 整合完成 | Dev |

### Week 3: Phase 3&4 - 完善與驗證
| 日期 | 任務 | 檢查點 | 負責人 |
|------|------|--------|--------|
| Day 1 | Task 4.1.1 | E2E 功能測試完成 | Dev |
| Day 2 | Task 4.1.2~4.1.3 | E2E 邊界和異常測試 | Dev |
| Day 3 | Task 4.2.1~4.2.2 | 效能測試實現 | Dev |
| Day 4 | 整體測試 | 所有測試案例通過 | Dev |
| Day 5 | 文檔和 Review | 完整功能交付 | PM + Dev |

---

## ✅ 任務完成標準

### 每個任務的 Definition of Done (DoD)

1. **測試優先**: 
   - [ ] 測試代碼撰寫完成
   - [ ] 測試執行並確認初始失敗（Red）

2. **實現階段**:
   - [ ] 功能代碼實現完成
   - [ ] 所有相關測試通過（Green）
   - [ ] 代碼 review 完成

3. **品質確保**:
   - [ ] TypeScript 型別檢查無錯誤
   - [ ] ESLint 無警告
   - [ ] 相關文檔更新

4. **驗收確認**:
   - [ ] 對應 AC 驗證通過
   - [ ] 對應 TC 測試案例通過
   - [ ] Manual testing 確認

---

## 🔧 開發環境準備

### 測試工具設置檢查清單
- [ ] Jest 配置確認 (`frontend/jest.config.js`)
- [ ] React Testing Library 配置
- [ ] Playwright 配置 (`tests/playwright.config.ts`)
- [ ] .NET 測試專案配置 (`backend-dotnet/Tests/`)

### Mock 資料準備
- [ ] 測試用 Sprint 資料集 (正常/警示/危險狀態)
- [ ] 空資料和錯誤狀態 Mock
- [ ] 時間邊界測試資料 (第1天/中間/最後1天)

---

## 📝 任務執行記錄

| 任務 ID | 開始日期 | 完成日期 | 狀態 | 備註 |
|---------|----------|----------|------|------|
| Task 1.1.1 | - | - | ⏳ 待開始 | |
| Task 1.1.2 | - | - | ⏳ 待開始 | |
| ... | - | - | ⏳ 待開始 | |

### 狀態說明
- ⏳ 待開始
- 🔄 進行中  
- ✅ 已完成
- ❌ 失敗/阻塞
- ⚠️ 需要協助

---

**下一步**: 開始執行 Task 1.1.1 - 撰寫健康狀態計算單元測試

> **重要提醒**: 每個任務都要先寫測試，確保測試失敗，再實現功能讓測試通過。這是 TDD 的核心原則！
