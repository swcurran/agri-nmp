﻿using System.ComponentModel.DataAnnotations;

namespace Agri.Models.Configuration
{
    public class CropYield : Versionable
    {
        [Key]
        public int CropId { get; set; }
        [Key]
        public int LocationId { get; set; }
        public decimal? Amount { get; set; }
        public Crop Crop { get; set; }
        public Location Location { get; set; }
    }
}