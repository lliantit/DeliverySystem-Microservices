using System;
using System.Collections.Generic;

namespace CatalogContext.Models
{
    // Головна сутність закладу
    public class Restaurant
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public bool IsDeleted { get; set; }

        // Навігаційні властивості
        public RestaurantDetail Detail { get; set; } // 1:1
        public ICollection<MenuItem> MenuItems { get; set; } // 1:N
    }

    // Зв'язок 1:1 (Деталі закладу)
    public class RestaurantDetail
    {
        public long RestaurantId { get; set; } // Одночасно PK та FK
        public string TaxId { get; set; }
        public string ContactEmail { get; set; }
        
        public Restaurant Restaurant { get; set; }
    }

    // Зв'язок 1:N (Страви меню)
    public class MenuItem
    {
        public long Id { get; set; }
        public long RestaurantId { get; set; } // FK
        public string Name { get; set; }
        public decimal Price { get; set; }

        public Restaurant Restaurant { get; set; }
        public ICollection<MenuItemCategory> MenuItemCategories { get; set; }
    }

    // Категорії страв
    public class Category
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public ICollection<MenuItemCategory> MenuItemCategories { get; set; }
    }

    // Проміжна сутність для M:N (Страви <-> Категорії) із власним полем
    public class MenuItemCategory
    {
        public long MenuItemId { get; set; }
        public MenuItem MenuItem { get; set; }

        public long CategoryId { get; set; }
        public Category Category { get; set; }

        public DateTime AssignedAt { get; set; } // Власне поле сутності
    }
}