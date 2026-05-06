using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ECommerce.Domain.Entities
{
    public class Category : Common.BaseEntity
    {
        /// <summary>Maksimum kategori derinligi: Elektronik(1) > Telefonlar(2) > Akilli Telefonlar(3)</summary>
        public const int MaxDepth = 3;

        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        /// <summary>Bu kategorinin agactaki derinligi (1=root, 2=alt kategori, 3=alt-alt)</summary>
        public int Depth { get; set; } = 1;

        /// <summary>Siralama onceligi (ayni seviyedeki kategoriler arasinda)</summary>
        public int SortOrder { get; set; } = 0;

        // Hiyerarsik yapi: Ust Kategori
        public Guid? ParentCategoryId { get; set; }
        public Category? ParentCategory { get; set; }

        public ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public ICollection<Product> Products { get; set; } = new List<Product>();

        // ── BUSINESS LOGIC ───────────────────────────────────────────────

        /// <summary>
        /// Verilen isimden URL-uyumlu slug olusturur.
        /// "Akıllı Telefonlar" → "akilli-telefonlar"
        /// </summary>
        public static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Kategori ismi bos olamaz.");

            var slug = name.ToLowerInvariant().Trim();

            // Turkce karakter donusumu
            slug = slug
                .Replace("ı", "i").Replace("ğ", "g").Replace("ü", "u")
                .Replace("ş", "s").Replace("ö", "o").Replace("ç", "c")
                .Replace("İ", "i").Replace("Ğ", "g").Replace("Ü", "u")
                .Replace("Ş", "s").Replace("Ö", "o").Replace("Ç", "c");

            // Alfanumerik olmayan karakterleri tire ile degistir
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-");
            slug = Regex.Replace(slug, @"-+", "-");
            slug = slug.Trim('-');

            return slug;
        }

        /// <summary>
        /// Bu kategoriye alt kategori eklenebilir mi kontrol eder.
        /// Maksimum derinlik asilmamali.
        /// </summary>
        public bool CanAddSubCategory()
        {
            return Depth < MaxDepth;
        }

        /// <summary>
        /// Ust kategori atandiginda derinligi hesaplar.
        /// Root kategoriler icin depth=1, her alt seviye +1.
        /// </summary>
        public void SetParent(Category? parent)
        {
            if (parent == null)
            {
                ParentCategoryId = null;
                ParentCategory = null;
                Depth = 1;
                return;
            }

            if (!parent.CanAddSubCategory())
                throw new InvalidOperationException(
                    $"Maksimum kategori derinligi ({MaxDepth}) asildi. " +
                    $"'{parent.Name}' kategorisi altina yeni alt kategori eklenemez.");

            // Dongusel referans kontrolu — bir kategori kendi atasi olamaz
            if (parent.Id == Id)
                throw new InvalidOperationException("Bir kategori kendisinin alt kategorisi olamaz.");

            ParentCategoryId = parent.Id;
            ParentCategory = parent;
            Depth = parent.Depth + 1;
        }

        /// <summary>
        /// Kategoriyi deaktive eder. Alt kategorileri de deaktive edilmeli (service katmaninda).
        /// </summary>
        public void Deactivate()
        {
            IsActive = false;
            UpdateAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Kategoriyi aktive eder. Ust kategorisi aktif degilse aktive edilemez.
        /// </summary>
        public void Activate()
        {
            IsActive = true;
            UpdateAt = DateTime.UtcNow;
        }
    }
}
