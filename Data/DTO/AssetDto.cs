// Data/DTO/AssetDto.cs
using System;

namespace AutoInvest.Data.DTO
{
    public class AssetDto
    {
        public string Ticker { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
