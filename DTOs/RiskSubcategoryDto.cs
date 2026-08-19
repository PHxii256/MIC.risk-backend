namespace MIC.risk.DTOs
{
    public record RiskSubcategoryResponseDto(
        long Id,
        string NameEn,
        string NameAr,
        string Category
    );

    public record RiskSubcategoryDto(
        long Id,
        string NameEn,
        string NameAr
    );

    public record CreateRiskSubcategoryRequestDto(
        string NameEn,
        string NameAr,
        string Category
    );

    public record UpdateRiskSubcategoryRequestDto(
        string NameEn,
        string NameAr,
        string Category
    );

    public record RiskCategoryResponseDto(
        string NameEn,
        string NameAr,
        IEnumerable<RiskSubcategoryDto> RiskSubcategories
    );
}