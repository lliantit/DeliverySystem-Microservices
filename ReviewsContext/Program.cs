using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ReviewsContext
{
    public class Review
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        
        public long RestaurantId { get; set; }
        public long MenuItemId { get; set; }
        public string? CustomerName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new MongoClient("mongodb://localhost:27019");
            var database = client.GetDatabase("ReviewsDb");
            var reviewsCollection = database.GetCollection<Review>("Reviews");

            var newReview = new Review
            {
                RestaurantId = 1,
                MenuItemId = 101,
                CustomerName = "Олександр",
                Rating = 5,
                Comment = "Піца супер! Дуже гостра, як і обіцяли. Рекомендую.",
                CreatedAt = DateTime.UtcNow
            };

            await reviewsCollection.InsertOneAsync(newReview);
            Console.WriteLine($"Відгук успішно додано! Згенерований ID документа: {newReview.Id}");

            var savedReview = await reviewsCollection
                .Find(r => r.MenuItemId == 101)
                .FirstOrDefaultAsync();

            Console.WriteLine($"\nЗчитано з бази MongoDB: {savedReview.CustomerName} поставив {savedReview.Rating} зірок.");
            Console.WriteLine($"Текст: {savedReview.Comment}");
        }
    }
}