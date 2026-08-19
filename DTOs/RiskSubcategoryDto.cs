namespace MIC.risk.DTOs
{
    public record RiskSubcategoryResponseDto(
        long Id,
        string Name,
        string Category
    );

    public record RiskSubcategoryDto(
        long Id,
        string Name
    );

    public record CreateRiskSubcategoryRequestDto(
        string Name,
        string Category
    );

    public record UpdateRiskSubcategoryRequestDto(
        string Name,
        string Category
    );

    public record RiskCategoryResponseDto(
        string Name,
        IEnumerable<RiskSubcategoryDto> RiskSubcategories
    );
}