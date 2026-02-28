using SqlSugar;

namespace PmsAI.Api.Db.Entities;

[SugarTable("Documents")]
public class DocumentEntity
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid DocId { get; set; }
    public Guid TenantId { get; set; }
    [SugarColumn(IsNullable = true)]
    public Guid? HotelId { get; set; } // null=集团通用
    public string Title { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty; // md/pdf/docx
    public string SourcePath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty; // SHA256
    public string Status { get; set; } = string.Empty; // Pending/Processing/Done/Failed
    [SugarColumn(IsNullable = true)]
    public string? RolesCsv { get; set; } // null=所有角色可见
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
