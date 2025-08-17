using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using JiraDashboard.Models;

namespace JiraDashboard.Services;

public class GoogleSheetsService
{
    private readonly HttpClient _httpClient;
    private readonly string _sheetId;
    private readonly string _sheetName;
    private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(5);
    private List<Dictionary<string, object?>>? _cache;
    private DateTime _cacheTimestamp;
    private List<string>? _sprintCache;
    private DateTime _sprintCacheTimestamp;

    public GoogleSheetsService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _sheetId = configuration["GoogleSheets:SheetId"] ?? throw new InvalidOperationException("SheetId not configured");
        _sheetName = configuration["GoogleSheets:SheetName"] ?? throw new InvalidOperationException("SheetName not configured");
    }

    private string GetCsvUrl() => $"https://docs.google.com/spreadsheets/d/{_sheetId}/gviz/tq?tqx=out:csv&sheet={_sheetName}&range=A:W";
    
    private string GetSprintCsvUrl() => $"https://docs.google.com/spreadsheets/d/{_sheetId}/gviz/tq?tqx=out:csv&sheet=GetJiraSprintValues&range=C:C";

    private async Task<List<Dictionary<string, object?>>> FetchAndCacheDataAsync()
    {
        if (_cache != null && DateTime.UtcNow - _cacheTimestamp < _cacheDuration)
        {
            return _cache;
        }

        var url = GetCsvUrl();
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.ToLower().Replace(" ", "_").Replace(".", "_")
        });

        var records = new List<Dictionary<string, object?>>();
        await foreach (var record in csv.GetRecordsAsync<dynamic>())
        {
            records.Add(new Dictionary<string, object?>(record));
        }

        _cache = records;
        _cacheTimestamp = DateTime.UtcNow;
        return _cache;
    }

    private async Task<List<string>> FetchAndCacheSprintDataAsync()
    {
        if (_sprintCache != null && DateTime.UtcNow - _sprintCacheTimestamp < _cacheDuration)
        {
            return _sprintCache;
        }

        var url = GetSprintCsvUrl();
        using var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

        var sprints = new List<string> { "All" }; // 預設第一個選項
        
        await foreach (var record in csv.GetRecordsAsync<dynamic>())
        {
            var sprintDict = new Dictionary<string, object?>(record);
            var sprintValue = sprintDict.Values.FirstOrDefault()?.ToString()?.Trim();
            
            if (!string.IsNullOrEmpty(sprintValue) && 
                sprintValue != "N/A" && 
                sprintValue != "Sprint Name" && // 跳過表頭
                !sprints.Contains(sprintValue))
            {
                sprints.Add(sprintValue);
            }
        }
        
        // 排序（除了 "All"）
        var sortedSprints = sprints.Skip(1).OrderBy(s => s).ToList();
        sprints = new List<string> { "All" };
        sprints.AddRange(sortedSprints);
        sprints.Add("No Sprints");

        _sprintCache = sprints;
        _sprintCacheTimestamp = DateTime.UtcNow;
        return _sprintCache;
    }

    public async Task<TableSummary> GetSummaryAsync()
    {
        var data = await FetchAndCacheDataAsync();
        var firstRecord = data.FirstOrDefault();
        var columns = firstRecord?.Keys.Select(key => new ColumnInfo(key, "string")).ToList() ?? new List<ColumnInfo>();

        return new TableSummary(
            SheetId: _sheetId,
            SheetName: _sheetName,
            TotalRows: data.Count,
            TotalColumns: columns.Count,
            Columns: columns,
            LastUpdated: _cacheTimestamp
        );
    }

    public async Task<TableDataResponse> GetPaginatedDataAsync(
        int page, int pageSize, string sortBy, string sortOrder, string? sprintFilter = null)
    {
        var allData = await FetchAndCacheDataAsync();

        // Apply sprint filter first
        var filteredData = ApplySprintFilter(allData, sprintFilter);

        // Sorting
        if (!string.IsNullOrEmpty(sortBy) && filteredData.Any() && filteredData.First().ContainsKey(sortBy))
        {
            var orderedData = sortOrder.Equals("desc", StringComparison.OrdinalIgnoreCase) 
                ? filteredData.OrderByDescending(r => r[sortBy]) 
                : filteredData.OrderBy(r => r[sortBy]);
            filteredData = orderedData.ToList();
        }

        // Pagination
        var totalRecords = filteredData.Count;
        var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);
        var paginatedData = filteredData.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var paginationInfo = new PaginationInfo(
            CurrentPage: page,
            PageSize: pageSize,
            TotalPages: totalPages,
            TotalRecords: totalRecords,
            HasNext: page < totalPages,
            HasPrev: page > 1
        );


        return new TableDataResponse(
            Data: paginatedData,
            Pagination: paginationInfo
        );
    }

    public async Task<List<string>> GetSprintOptionsAsync()
    {
        return await FetchAndCacheSprintDataAsync();
    }

    public async Task<DashboardStats> GetDashboardStatsAsync(string? sprintFilter = null)
    {
        var allData = await FetchAndCacheDataAsync();
        var filteredData = ApplySprintFilter(allData, sprintFilter);

        var totalIssues = filteredData.Count;
        
        // 尋找 Story Points 欄位
        var storyPointsColumn = FindStoryPointsColumn(filteredData);
        var totalStoryPoints = CalculateTotalStoryPoints(filteredData, storyPointsColumn);

        // 計算已完成的 Issues
        var doneData = filteredData.Where(row => IsDoneStatus(row)).ToList();
        var doneIssues = doneData.Count;
        var doneStoryPoints = CalculateTotalStoryPoints(doneData, storyPointsColumn);

        return new DashboardStats(
            TotalIssues: totalIssues,
            TotalStoryPoints: totalStoryPoints,
            DoneIssues: doneIssues,
            DoneStoryPoints: doneStoryPoints,
            LastUpdated: _cacheTimestamp
        );
    }

    public async Task<StatusDistribution> GetStatusDistributionAsync(string? sprintFilter = null)
    {
        var allData = await FetchAndCacheDataAsync();
        var filteredData = ApplySprintFilter(allData, sprintFilter);

        var totalCount = filteredData.Count;
        var statusCounts = new Dictionary<string, int>();

        // 計算各狀態的數量
        foreach (var row in filteredData)
        {
            var status = GetStatusValue(row);
            if (!string.IsNullOrEmpty(status))
            {
                statusCounts[status] = statusCounts.GetValueOrDefault(status, 0) + 1;
            }
        }

        // 轉換為分布項目
        var distribution = statusCounts.Select(kvp => new StatusDistributionItem(
            Status: kvp.Key,
            Count: kvp.Value,
            Percentage: totalCount > 0 ? Math.Round((double)kvp.Value / totalCount * 100, 1) : 0
        )).OrderByDescending(item => item.Count).ToList();

        return new StatusDistribution(
            Distribution: distribution,
            TotalCount: totalCount,
            LastUpdated: _cacheTimestamp
        );
    }

    private static string? FindStoryPointsColumn(List<Dictionary<string, object?>> data)
    {
        if (!data.Any()) return null;
        
        var firstRow = data.First();
        foreach (var key in firstRow.Keys)
        {
            var keyLower = key.ToLower();
            if (keyLower.Contains("story") && keyLower.Contains("point"))
            {
                return key;
            }
        }
        return null;
    }

    private static double CalculateTotalStoryPoints(List<Dictionary<string, object?>> data, string? storyPointsColumn)
    {
        if (string.IsNullOrEmpty(storyPointsColumn)) return 0;

        double total = 0;
        foreach (var row in data)
        {
            if (row.TryGetValue(storyPointsColumn, out var value) && value != null)
            {
                if (double.TryParse(value.ToString(), out var points))
                {
                    total += points;
                }
            }
        }
        return total;
    }

    private static bool IsDoneStatus(Dictionary<string, object?> row)
    {
        var status = GetStatusValue(row);
        if (string.IsNullOrEmpty(status)) return false;
        
        var statusLower = status.ToLower();
        return statusLower.Contains("done") || statusLower.Contains("resolved") || statusLower.Contains("closed");
    }

    private static string GetStatusValue(Dictionary<string, object?> row)
    {
        // 嘗試不同的狀態欄位名稱
        var statusKeys = new[] { "status", "Status", "issue_status", "Issue_Status" };
        
        foreach (var key in statusKeys)
        {
            if (row.TryGetValue(key, out var value) && value != null)
            {
                return value.ToString()?.Trim() ?? "";
            }
        }
        return "";
    }

    private static List<Dictionary<string, object?>> ApplySprintFilter(List<Dictionary<string, object?>> data, string? sprintFilter)
    {
        if (string.IsNullOrEmpty(sprintFilter) || sprintFilter == "All")
        {
            return data;
        }

        if (sprintFilter == "No Sprints")
        {
            return data.Where(row => 
            {
                if (!row.ContainsKey("sprint")) return true;
                var sprintValue = row["sprint"]?.ToString()?.Trim();
                return string.IsNullOrEmpty(sprintValue);
            }).ToList();
        }

        return data.Where(row => 
        {
            if (!row.ContainsKey("sprint")) return false;
            var sprintValue = row["sprint"]?.ToString()?.Trim();
            return sprintValue == sprintFilter;
        }).ToList();
    }
}
