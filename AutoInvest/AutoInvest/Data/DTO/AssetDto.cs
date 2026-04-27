// Data/DTO/AssetDto.cs
using System;

namespace AutoInvest.Data.DTO
{
    public class AssetDto
    {
        public string Ticker { get; set; }
        public string Name { get; set; }
        public string Currency { get; set; }
        public bool IsActive { get; set; }
    }
}
